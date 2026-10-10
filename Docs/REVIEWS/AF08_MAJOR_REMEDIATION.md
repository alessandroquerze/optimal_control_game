# Remediation dei MAJOR dopo F08

Data: 2026-10-10. Unity: **6000.3.8f1**, Windows, URP.

Ambito: esclusivamente **AF08-01, AF08-02, AF08-03**. Riferimenti letti integralmente: vision, architettura, piano operativo, audit completo e precedenti report in `Docs/REVIEWS/`. Il piano, l'audit originale e gli altri report sono invariati. **F09 non è stata iniziata.**

## Metodo ed evidenze

Prima delle correzioni sono stati aggiunti quattro test di regressione: due EditMode e due PlayMode. Eseguiti contro il codice precedente, **falliscono tutti e quattro**, senza errori di compilazione: subscriber successivo non notificato, ingresso fallito che lancia e lascia avanzare, overflow float che interrompe la pubblicazione, teardown anticipato durante Changed. Sono stati inoltre rieseguiti i probe originali dell'audit, riproducendo nativamente i tre guasti, inclusa la NullReference dell'HUD.

Le verifiche usano un clone isolato con gli stessi sorgenti, asset, package e impostazioni del workspace, per non interferire con l'Editor dell'utente. Gli helper nativi sono stati installati soltanto nel clone e rimossi prima delle suite conclusive. Nessun helper di verifica è stato aggiunto al progetto consegnato. Nessuna dipendenza esterna introdotta.

Directory delle evidenze locali:

`C:\Users\querz\AppData\Local\Temp\OptCon-AF08-Remediation-db7049cf6dbc435d9fb4722169f1685f`

Contiene baseline SHA-256, manifest delle modifiche, XML e log delle suite, sorgenti degli helper, riproduzioni prima/dopo e undici Game View. Sono evidenze temporanee locali: non vengono importate come asset del progetto.

| Evidenza nella directory | Contenuto |
| --- | --- |
| `red-edit.xml`, `red-play.xml` | Due test falliti per suite contro il codice precedente |
| `red-native-probes.txt`, `red-native.log` | Riproduzioni originali, inclusa la NullReference |
| `edit-final-clean.xml`, `edit-final-clean.log` | Tutti i 222 EditMode della versione finale |
| `play-final-clean.xml`, `play-final-clean.log` | Tutti i 56 PlayMode della versione finale |
| `post-native-probes.txt`, `post-native-final.log` | Riproduzioni dopo le correzioni |
| `lesson-runtime-results.txt`, `flow-native.log` | Percorso completo nativo F08 e fixture index-only |
| `presentation-unavailable.png`, `lesson-*.png` | Undici catture Game View |
| `baseline.json`, `delivery-manifest.json` | Controllo di integrità e inventario della consegna |

## AF08-01 — RESOLVED

**Causa e riproduzione originale.** La sessione committa correttamente un risultato finito in double, ma `Changed?.Invoke` interrompe i callback al primo errore del renderer. Nel pendolo, in esplorazione, `SetInput(0, [1e40], Tail)` produce un risultato valido e costo circa `4.94205336000001E82`; la proiezione float lancia. Il probe originale rileva sessione r1, Formula HUD r0, pubblicazione visuale r0, errore matematico assente. Una traiettoria poteva anche essere modificata prima di completare la proiezione.

**Soluzione e policy esplicita.**

- `SimulationSession.Publish` cattura la lista dei subscriber e consegna la stessa istanza di `SessionState` a ciascuno. Raccoglie gli errori, continua gli altri consumer e infine lancia `SessionPublicationException`, con pubblicazione, identità dei consumer e tutte le eccezioni. `LastPublicationFailure` conserva la diagnostica e viene azzerata alla pubblicazione successiva. Un errore applicativo non viene nascosto né trasformato in errore matematico. Se la sessione viene chiusa esplicitamente, i callback residui della sessione chiusa vengono scartati.
- `StateProjection.ToFloat` segnala il caso previsto con `NonRepresentableDisplayException`, sottotipo di `ArgumentException`. Nessun clamp e nessuna modifica ai double.
- `TrajectoryRenderer` prepara tutti i punti prima di modificare `LineRenderer` o `Source`: una proiezione fallita conserva integralmente la geometria precedente.
- `ExperimentVisualization` espone `VisualizationStatus`, riferito all'esatta pubblicazione ricevuta: `Available`, `NonRepresentable` oppure `Failed`, con diagnostica. `DisplayedState` identifica la pubblicazione elaborata; lo stato di disponibilità distingue esplicitamente l'avvenuto rendering.
- Se un elemento del gruppo fallisce, tutti i Renderer del gruppo vengono nascosti e i target semantici diventano non disponibili. Geometria vecchia o parziale non rimane presentata come valida. Le precedenti abilitazioni dei Renderer vengono ripristinate prima del tentativo successivo; la visibilità dei layer continua a essere governata dal sistema esistente.
- L'errore previsto di rappresentazione viene gestito. Un errore inatteso espone `Failed`, nasconde il gruppo e viene rilanciato al dispatch controllato della sessione. L'HUD legge la stessa revisione matematica e mostra la diagnostica 3D attraverso `IVisualizationStatus`, senza conoscere i renderer concreti.

**Architettura preservata.** Core e matematica invariati. Simulation contiene soltanto contratti puri di pubblicazione; non conosce float, HUD, GameObject o renderer. La conversione e l'indisponibilità visuale restano in Presentation. Formula e costo leggono direttamente `SimulationResult`, anche quando il 3D non può rappresentarlo.

**File coinvolti.** `SimulationSession.cs`, nuovi `SessionPublicationException.cs` e `VisualizationStatus.cs`, `StateProjection.cs`, `TrajectoryRenderer.cs`, `ExperimentVisualization.cs`, `LessonHudPresenter.cs`, `AppBootstrap.cs`; test in `SimulationSessionTests.cs`, `StateProjectionTests.cs`, `VisualizationTests.cs`.

**Test e risultato dopo la correzione.** Subscriber che lancia, due subscriber che lanciano, diagnostica aggregata, consegna agli altri consumer, recupero successivo, commit atomico della traiettoria e pendolo reale con input `1e40`. Nel probe nativo: nessuna eccezione prevista propagata, errore matematico assente, sessione/HUD/source visuale **r1**, `NonRepresentable`, Renderer nascosti, input originale conservato. Reset: **r2** in tutti i consumer, rendering `Available`. Cattura `presentation-unavailable.png` ispezionata: diagnostica leggibile e gruppo 3D nascosto.

**Effetti collaterali dichiarati.** La policy nasconde l'intero gruppo visuale quando un suo elemento fallisce, anche se alcuni elementi sarebbero rappresentabili. È una scelta conservativa per evitare display incoerenti. Gli errori inattesi dei subscriber ora arrivano come aggregato dopo la consegna agli altri consumer; i catch che richiedono un preciso tipo di eccezione devono considerare questo contratto. Le vecchie geometrie possono restare memorizzate, ma sono nascoste, prive di target selezionabili e accompagnate da stato esplicito.

## AF08-02 — RESOLVED

**Causa e riproduzione originale.** Un observer installato prima del rebind dell'HUD richiede `ReturnToMenu` durante `SimulationSession.Changed`. Il teardown scollega e smonta il presenter; il delegate multicast ha già catturato il vecchio callback. Il probe originale ottiene `NullReferenceException` in `LessonHudPresenter.Render`, con vecchia sessione chiusa e HUD scollegato.

**Soluzione e policy esplicita.**

- Nuovo `SessionNotificationScope`, puro e per sessione: delimita pubblicazioni e operazioni didattiche annidate. Le operazioni differite vengono eseguite **sincronicamente all'uscita dello scope più esterno**, senza frame di attesa, coroutine o timer. Anche gli errori delle operazioni differite restano espliciti attraverso un aggregato.
- `CourseManager` accetta la prima richiesta valida effettuata dentro lo scope, conserva l'oggetto `NavigationTransition` e la sessione sorgente, e differisce l'inizio del lifecycle e il teardown. Richieste ulteriori mentre una destinazione è pendente sono respinte. `IsTransitioning` include la richiesta pendente e `IsReady` non la nasconde.
- L'operazione didattica che ha originato Changed può terminare e pubblicare la propria Presentation prima del teardown. Differire soltanto fino alla fine del delegate della simulazione sarebbe insufficiente.
- All'uscita, il manager verifica ownership della sessione e identità della richiesta; una richiesta invalidata dalla disposal o dalla perdita della sorgente non viene applicata a un'altra destinazione. Il lifecycle esistente `Requested → SceneLoaded → Composing → Binding → Ready` conserva la destinazione originale e le sue barriere.
- HUD, visualizzazione e diagnostica usano callback che catturano sorgente e generazione del binding. Disconnect/Unsubscribe invalida la generazione **prima** di smontare i consumer. Callback già acquisiti diventano innocui anche dopo un rebind diretto. La stessa protezione è applicata al binding didattico e ai callback input del router.

**Architettura preservata.** Il confine sincrono non conosce navigazione o Unity. Application decide cosa differire; Presentation protegge il proprio lifetime. Nessun consumer deve dipendere dall'ordine delle subscription o leggere tardivamente uno stato globale per recuperare la destinazione.

**File coinvolti.** Nuovo `SessionNotificationScope.cs`, `SimulationSession.cs`, `CourseManager.cs`, `LessonSession.cs`, `LessonHudPresenter.cs`, `ExperimentVisualization.cs`, `SessionDiagnosticsPresenter.cs`, `LessonInputRouter.cs`; test in `SimulationSessionTests.cs` e `LessonOrchestrationTests.cs`.

**Test e risultato dopo la correzione.** Confine annidato sincrono; observer prima e dopo HUD; ReturnToMenu; apertura di un'altra lesson nello stesso workspace; rifiuto di una seconda destinazione pendente; operazione lesson conclusa prima della chiusura; callback già catturati durante rebind di HUD/visualizzazione/diagnostica; Ready della nuova identità; vecchia sessione chiusa, delegate Changed svuotato, vecchia lesson inutilizzabile. Nessuna NullReference. Il probe nativo originale ora riporta navigazione accettata, nessuna eccezione, vecchia sessione chiusa dopo la pubblicazione e menu Ready con host assente. I test PlayMode verificano anche la seconda lesson con identità distinta.

**Effetti collaterali dichiarati.** `true` dalla richiesta durante dispatch significa accettazione della destinazione pendente, non teardown già eseguito. Il teardown inizia entro l'uscita dell'operazione sincrona corrente; il caricamento della scena mantiene il normale comportamento asincrono. La prima richiesta accettata prevale. Dispose cancella una richiesta pendente.

## AF08-03 — RESOLVED

**Causa e riproduzione originale.** `Next` incrementava l'indice prima delle azioni Enter. Una Seek fuori dominio lanciava, mentre la Presentation conservava il vecchio `CanAdvance`. Nel fixture index-only T=1, Enter Seek(2) seguito da una seconda Next concedeva completion a t=0. La stessa riproduzione con pendolo T=100/Seek(101) risultava anche valida per il precedente execution validator.

**Soluzione.** `Enter(target, restorePreset)` esegue prima Pause, eventuale ripristino e tutte le azioni necessarie. Solo dopo il successo committa indice, generation, facts e identità del passo. Un comando rifiutato diventa un fallimento controllato: la transizione ritorna false, conserva l'ultimo indice committato e pubblica `LessonPresentationState.Error`, con target e comando. `CanAdvance` è false, completion è false, i facts vengono invalidati. Next ripetute, risposte, input didattici ed Explore non possono aggirare l'errore. Pause e Reset restano disponibili.

`Previous` recupera il passo precedente rispetto all'ultimo indice committato e ripristina il preset; a indice zero ritorna false. `Restart` tenta il primo passo dal preset. L'errore viene rimosso soltanto dopo un recupero riuscito. Anche l'esplorazione mantiene un reset verificato. Un ingresso iniziale fallito continua a impedire la composizione Ready. Un errore inatteso produce diagnostica coerente e viene rilanciato, senza concedere completion.

Non viene implementato un rollback della simulazione: se una prima azione matematica riesce e una successiva fallisce, il risultato matematico valido resta disponibile. La transazione riguarda il progresso didattico, che rimane bloccato finché non si recupera. Questa scelta evita di introdurre copie della configurazione o dello stato nel layer lesson.

`CatalogValidator.ValidateExecutionConfiguration` controlla inoltre i domini editoriali evidenti rispetto al preset: Seek, indice/dimensioni input, dimensioni stato iniziale, target/range parametri. Usa i contratti di configurazione esistenti; segnala `LessonEntryDomain` con l'identità del passo. Il controllo non garantisce il successo dopo modifiche runtime dell'orizzonte o dei parametri: la gestione runtime resta obbligatoria e testata.

**Architettura preservata.** Nessuna formula duplicata, nessuna lesson specifica nel controllo, nessun rollback costruito nella UI. Application orchestra; Content espone una Presentation immutabile con errore; HUD mostra il messaggio e limita i controlli. Core invariato.

**File coinvolti.** `LessonSession.cs`, `LessonPresentationState.cs`, `LessonStepPresenter.cs`, `CatalogValidator.cs`; test in `LessonSessionTests.cs` e `CatalogValidatorTests.cs`.

**Test e risultato dopo la correzione.** Ingressi falliti intermedi e finali, pendolo e index-only; Next ripetute; condizione terminale non soddisfatta; indice/Presentation coerenti; Previous; Restart; azioni parzialmente riuscite; comando valido nel preset che fallisce dopo riduzione runtime di T e riesce dopo recupero. I probe originali ora riportano: nessuna eccezione attesa propagata, indice zero conservato, seconda Next false, completion false, terminal false. Il catalogo rifiuta il pendolo con Seek(101). Il percorso normale dei sette step F08 rimane invariato.

**Effetti collaterali dichiarati.** Gli asset con azioni editoriali impossibili vengono ora respinti dall'execution validator. Nessuna migrazione degli asset esistenti: quelli consegnati sono invariati. L'HUD mostra l'errore nel feedback esistente; testo di completion e disclosure non sono stati ridisegnati.

## Test completi e regressioni

Suite conclusive, senza helper temporanei nel clone:

| Suite | Totale | Passati | Falliti | Skipped |
| --- | ---: | ---: | ---: | ---: |
| EditMode | 222 | 222 | 0 | 0 |
| PlayMode | 56 | 56 | 0 | 0 |
| Totale | 278 | 278 | 0 | 0 |

Incremento rispetto all'audit: **16 casi EditMode e 6 PlayMode**, tutti di regressione per i tre MAJOR. Nessun test precedente rimosso o indebolito. L'asserzione preesistente del confine float è stata specializzata da `ArgumentException` al sottotipo diagnostico `NonRepresentableDisplayException`, conservando tutti i casi NaN/infinito/fuori range.

| Fixture EditMode | Passati |
| --- | ---: |
| CatalogValidatorTests | 64 |
| CostSystemTests | 30 |
| FormulaResolverTests | 28 |
| LessonSessionTests | 34 |
| PendulumDynamicsTests | 26 |
| RolloutEngineTests | 13 |
| SimulationSessionTests | 19 |
| StateProjectionTests | 8 |

| Fixture PlayMode | Passati |
| --- | ---: |
| CostPresentationTests | 8 |
| CourseNavigationTests | 15 |
| FormulaHudTests | 9 |
| LessonOrchestrationTests | 17 |
| VisualizationTests | 7 |

Compilazione Unity conclusiva: **nessun errore C#, nessun warning C#**. Un'ambiguità di namespace nel nuovo test è stata corretta durante lo sviluppo; non resta nella versione consegnata. Il primo tentativo di cattura nativa richiedeva una Game View aperta: corretto soltanto l'helper temporaneo e ripetuta con successo la prova.

Smoke nativo tramite UI reale: tutti i sette step, selezione termini/risposte, input, next/step, traiettoria, modifica peso, A/B, terminale, completion, Restart, menu e riapertura. Pendolo con **δ=.02 s**, **τ_T=2 s** a T=100. Formula e visualizzazione condividono lo stesso risultato; modifica del peso cambia J conservando i campioni; A conserva configurazione/costi propri dopo B. Fixture index-only **2+3=5**, **J=13+25=38**, senza δ/τ e senza input/stage/next al terminale. Dieci catture del percorso più una di unavailable; ispezionate anche A/B e terminale index-only. Smoke guidato: zero Error/Exception/Assert.

| Contratto | Esito |
| --- | --- |
| F00–F03: foundation/catalogo/math/proiezioni | Suite complete passate; asset e Core invariati |
| F04 / A03 storico: lifecycle Ready | Test di navigazione e binding prima di Ready passati; destinazione conservata |
| F05: visualizzazione da sessione | Test punti, marker, layer, reset e rebind passati; nuova unavailable esplicita |
| F06 / A02: metriche tipizzate, Formula, SampleIndex | 28 EditMode e 9 PlayMode Formula passati; nessuna conversione ad hoc introdotta |
| F07: costi e A/B | 30 EditMode e 8 PlayMode costo passati; smoke nativo confermato |
| F08: sette step e completion | Suite orchestration e percorso nativo completo passati |
| A01: tempo fisico opzionale | Pendolo .02 s preservato; fixture index-only senza secondi |
| A04: configurazione immutabile del risultato | Test A/B e fallimento candidati passati; nessuna configurazione duplicata nei consumer |

## Inventario preciso rispetto alla baseline iniziale

Creati (7 file):

- `Assets/OptimalControl/Runtime/Simulation/SessionNotificationScope.cs`
- `Assets/OptimalControl/Runtime/Simulation/SessionNotificationScope.cs.meta`
- `Assets/OptimalControl/Runtime/Simulation/SessionPublicationException.cs`
- `Assets/OptimalControl/Runtime/Simulation/SessionPublicationException.cs.meta`
- `Assets/OptimalControl/Runtime/Visualization/VisualizationStatus.cs`
- `Assets/OptimalControl/Runtime/Visualization/VisualizationStatus.cs.meta`
- `Docs/REVIEWS/AF08_MAJOR_REMEDIATION.md`

Modificati (19 file):

- `Assets/OptimalControl/Editor/CatalogValidator.cs`
- `Assets/OptimalControl/Runtime/Application/AppBootstrap.cs`
- `Assets/OptimalControl/Runtime/Application/LessonInputRouter.cs`
- `Assets/OptimalControl/Runtime/Content/Lessons/LessonPresentationState.cs`
- `Assets/OptimalControl/Runtime/Course/CourseManager.cs`
- `Assets/OptimalControl/Runtime/Lessons/LessonSession.cs`
- `Assets/OptimalControl/Runtime/Simulation/SimulationSession.cs`
- `Assets/OptimalControl/Runtime/UI/LessonHudPresenter.cs`
- `Assets/OptimalControl/Runtime/UI/LessonStepPresenter.cs`
- `Assets/OptimalControl/Runtime/UI/SessionDiagnosticsPresenter.cs`
- `Assets/OptimalControl/Runtime/Visualization/ExperimentVisualization.cs`
- `Assets/OptimalControl/Runtime/Visualization/StateProjection.cs`
- `Assets/OptimalControl/Runtime/Visualization/TrajectoryRenderer.cs`
- `Assets/OptimalControl/Tests/EditMode/CatalogValidatorTests.cs`
- `Assets/OptimalControl/Tests/EditMode/Lessons/LessonSessionTests.cs`
- `Assets/OptimalControl/Tests/EditMode/Mathematics/SimulationSessionTests.cs`
- `Assets/OptimalControl/Tests/EditMode/Visualization/StateProjectionTests.cs`
- `Assets/OptimalControl/Tests/PlayMode/LessonOrchestrationTests.cs`
- `Assets/OptimalControl/Tests/PlayMode/VisualizationTests.cs`

Eliminati: **nessuno nel workspace**. Helper temporanei rimossi soltanto dal clone.

Verifiche SHA-256: Core matematico, asset `.asset`, prefab, scene, package, impostazioni, piano, audit originale e report preesistenti invariati. Nessun sistema di F09 aggiunto.

## Verifica ripetibile in Unity

1. Aprire `Assets/OptimalControl/Scenes/Bootstrap.unity`, entrare in Play e aprire il prototipo. Percorrere i sette step; verificare Formula, input, marker, costi, A/B e terminale. Restart, menu e riapertura devono ripartire dal primo passo senza A residuo.
2. In esplorazione, nel pannello input impostare `u[0]=1e40` sulla coda. La revisione deve avanzare senza errore matematico; compare la diagnostica 3D unavailable e il gruppo viene nascosto. I valori HUD restano quelli del risultato valido. Reset ripristina rendering e coerenza delle revisioni.
3. In Test Runner eseguire i casi `AF08_02_*` di `LessonOrchestrationTests`: coprono le subscription nei due ordini, menu, seconda lesson, stesso workspace e rebind già durante il dispatch. Il codice dei test contiene le richieste avversariali; nessun nuovo controllo UI di debug è necessario.
4. Eseguire i casi `AF08_03_*` di `LessonSessionTests` e `CatalogValidatorTests`, anche pendolo e index-only. Per prova editoriale usare copie degli asset: dopo un passo riuscito, configurare una Seek oltre T nell'Enter successivo. Validate Execution Configuration deve segnalarla; se si prova comunque runtime, Next fallisce, l'indice resta quello valido, l'errore è visibile e la completion è bloccata. Previous/Restart recuperano secondo la policy descritta.
5. Eseguire il test PlayMode index-only già esistente e quello guidato: t presente, δ/τ assenti, input e next non disponibili a T. Per il pendolo verificare δ=.02 e τ=t·δ.

## Limiti residui e verdetto

Nessun nuovo MAJOR emerso nelle verifiche. AF08-04…14 restano fuori ambito. I probe conservati confermano ancora AF08-04 (editing dopo requested configuration fallita), AF08-05 (unità parametro/provider) e AF08-06 (provider metriche custom): non sono stati corretti. Nessuna modifica intenzionale a testo completion, disclosure, polish o capacità future. Non risultano correzioni incidentali di questi rilievi.

Le modifiche di contratto necessarie sono limitate a pubblicazione controllata, confine sincrono di lifetime, disponibilità visuale e stato d'errore didattico. Non è stato necessario un ridisegno generale né modificare Core.

**AF08-01: RESOLVED — AF08-02: RESOLVED — AF08-03: RESOLVED.**

**READY FOR F09.** F09 rimane non iniziata; il verdetto riguarda la rimozione dei tre blocchi MAJOR, non la chiusura degli altri rilievi dell'audit.
