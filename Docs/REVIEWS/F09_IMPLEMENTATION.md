# F09 — Stabilizzazione, validazione e progressione persistente

Data: 10 ottobre 2026. Unity **6000.3.8f1**, Windows, URP. **F09 completata e verificata. F10 non iniziata.**

## Prerequisiti e perimetro

Letti integralmente visione, curriculum, architettura, piano operativo e tutti i nove report Markdown preesistenti in `Docs/REVIEWS/`, inclusi audit completo e remediation dei MAJOR. Esaminati catalogo/asset, assembly, composizione, sessione, Lesson Logic, presenter, provider, validatori e test. Specifica operativa principale: F09 della roadmap.

Prima delle modifiche, i **390 file condivisi** della baseline (Assets/OptimalControl, Docs, Packages, ProjectSettings) coincidevano per SHA-256 con il manifest finale della remediation. Disponibili F00–F08, vertical slice accettato, osservazioni native di funzionamento/leggibilità, separazione authoring/runtime, risultati immutabili, provider e lifecycle. Evidenze precedenti: **222 EditMode e 56 PlayMode** passati. I prerequisiti F09 erano soddisfatti; i tre MAJOR non sono stati riaperti.

Non sono introdotti contenuti Chapter 1, esperimenti statici, nuovi solver/costi, layout matematici, scorrimento/camera o polish delle fasi successive. Il catalogo di produzione e le sue 18 lesson restano futuri.

## Architettura della progressione

```text
Course / Chapter / LessonDefinition → identità/versioni/prerequisiti congelati
                                          ↓
                                    ProgressionService
                                          ↕
                                     IProgressStore
                                Memory / file JSON v1

LessonSession → completion educativa pubblicata con successo
                      ↓ LessonCompletion
              LessonProgressBinding
                      ↓ fine del SessionNotificationScope
        verifica lifetime + destinazione + risultato + consumer
                      ↓
              commit progressione → store
                      ↓
                ICourseProgress → menu / stato persistente
```

**Course content** descrive i contenuti. **LessonSession** conserva step, condizioni, errori e completion della singola apertura. **ProgressionService** conserva esclusivamente completamenti validi e versionati: non salva cursore, configurazione matematica, risposte, confronto A/B o step corrente. Il riavvio della lesson ricrea il runtime dal preset anche quando il suo completamento è già salvato.

Il servizio congela ID, versioni e grafo all'inizializzazione, senza rileggere asset editoriali per reinterpretare un risultato. I presenter conoscono soltanto il contratto Content `ICourseProgress`. Store e dominio della progressione non dipendono da PlayerPrefs, HUD, renderer o nomi di scena/GameObject. Il backend JSON usa Unity JsonUtility soltanto come adattatore di serializzazione.

Il DAG resta aciclico: Core → nessun engine; Simulation → Core; Content → Core/Simulation; Presentation → Content/Simulation/Core; Application compone; Editor dipende dai livelli sottostanti. L'unico riferimento assembly aggiunto è **Editor → Presentation**, per verificare layer del prefab con il loro contratto reale.

## Modello persistente, identità e versioni

`ProgressDocument` v1 contiene record immutabili con `courseId`, `courseVersion`, `lessonId`, `lessonVersion`, `lessonSchemaVersion`. Sono vietati record null e duplicati per corso/lesson/versione/schema.

Il credito richiede esattamente **courseId + lessonId + lessonVersion + lessonSchemaVersion**. `courseVersion` conserva la provenienza del record; un riordino o un aggiornamento del catalogo non invalida lesson che mantengono la propria identità/versione. Cambiare versione o schema della lesson richiede una nuova completion; il record storico resta conservato.

Nessuna chiave deriva da indice, ordinamento, titolo, scena, GameObject o posizione del capitolo. Nuove lesson iniziano senza completion; record di ID rimossi o di altri corsi restano conservati senza assegnare credito a ID differenti. Non esistono alias o promozioni implicite.

Stati e disponibilità:

- **Non completata:** nessun record valido per la versione corrente. Comprende sia una lesson mai aperta sia una visitata ma non conclusa. F09 non persiste un flag «started»; il menu usa questa dicitura senza affermare che lo studente non l'abbia mai aperta.
- **Available:** la lesson può essere esplorata. Prerequisiti mancanti sono indicati esplicitamente.
- **Completed:** completion valida effettivamente salvata per quella identità/versione.
- **In progress:** appartiene soltanto al runtime educativo; non è uno stato aggiuntivo del salvataggio.
- **Locked:** non introdotto. L'architettura richiede prerequisiti orientativi e consente l'esplorazione.

I prerequisiti sono gli ID editoriali già presenti in `LessonDefinition`. Il servizio e il catalogo rifiutano duplicati, riferimenti mancanti e cicli. Il menu mostra gli ID mancanti; il CourseManager resta privo di conoscenza del curriculum. Condividere intenzionalmente lo stesso asset lesson fra capitoli mantiene una sola identità/progressione, come consentiva F02; asset distinti con lo stesso ID sono invalidi.

**Prototipo:** `prototype.pendulum` viene completato soltanto nel suo corso e alla sua versione. Non accredita `C1.L1`, Chapter 1 o un altro corso. Il fixture della seconda lesson usa un prerequisito esplicito al prototipo esclusivamente per verificare il grafo; non è registrato nel catalogo consegnato.

## Storage e compatibilità

Default: **`Application.persistentDataPath/OptCon/progress-v1.json`**. `AppBootstrap.Configure(services, store)` permette di fornire dipendenze prima di Start; dopo l'avvio la riconfigurazione viene rifiutata. I test runtime usano store isolati, senza scrivere il profilo effettivo dell'utente.

| Caso | Comportamento |
| --- | --- |
| File assente | Profilo vuoto, nessun completamento e nessuna scrittura fino alla completion |
| File v1 valido / reload | Ripristino degli stessi record; nuova sessione educativa dal primo step |
| Nuova lesson / riordino | Nessuna promozione; completamenti esistenti conservati per ID/versione |
| ID rimosso / altro corso | Record mantenuto; nessun credito a un'altra identità |
| Schema futuro | Diagnostica e progresso read-only; file originale non riscritto |
| JSON corrotto, schema assente/zero, record invalidi | Diagnostica, nessun credito e nessuna sovrascrittura distruttiva |
| Errore di scrittura | Il servizio conserva il progresso precedente e segnala che la completion non è stata salvata |

La scrittura prepara un file temporaneo adiacente, quindi usa replace/move. Il documento in memoria viene committato soltanto dopo il successo dello store. Restart e completion ripetuta non aggiungono record duplicati e non riscrivono inutilmente il file.

Non esisteva un formato persistente F08 da migrare. Il primo salvataggio crea v1; non si importano retroattivamente completion runtime. Non è introdotto un motore generale di migrazione. Una futura migrazione di schema dovrà essere esplicita; oggi lo schema sconosciuto viene preservato. Gli asset Unity esistenti non richiedono migrazioni e sono byte per byte invariati.

## Lifecycle della completion e preservazione dei MAJOR

`LessonSession.Next` rifiuta anche una sessione chiusa, un errore matematico corrente o una pubblicazione simulativa fallita. Il normale controllo delle condizioni e `LessonPresentationState.Error` resta operativo.

Solo dopo il completamento dell'ultimo step e la **pubblicazione educativa riuscita** viene emessa `CompletionValidated`, con ricevuta `LessonCompletion`: identità/versione congelate, generation, risultato immutabile e sorgente. Un errore di pubblicazione precedente non produce ricevuta. Un observer della ricevuta che lancia la cancella prima del commit differito.

`LessonProgressBinding` attende sincronicamente la fine dello scope più esterno, senza frame/coroutine. Ricontrolla lifetime, generation, completion/condizioni/errori correnti e identità del risultato. Il composition root verifica inoltre:

- navigazione Ready e nessuna destinazione pendente;
- host, lesson e transition ID della destinazione originaria;
- visualizzazione collegata alla stessa sessione, `Available`, con il risultato corrente;
- HUD presenti, collegati alla stessa lesson/sessione e allo stesso risultato.

Una navigazione richiesta durante la pubblicazione finale o da un observer successivo della ricevuta impedisce il salvataggio della destinazione uscente, indipendentemente dall'ordine delle subscription. Dispose, rebind incompatibile, NonRepresentable, errore inatteso di Presentation, errore matematico, Enter fallito o condizione non soddisfatta non concedono progresso. I callback differiti verificano anche la disposal del binding. Release lo scollega prima del teardown dei consumer.

Una completion runtime può esistere senza completion persistente, per esempio se la rappresentazione è NonRepresentable o lo store fallisce. Il menu espone separatamente lo stato salvato. Dopo recupero e Restart è possibile completare nuovamente; una completion già salvata rimane tale dopo reset.

**AF08-01/02/03 preservati:** dispatch controllato della simulazione e sua diagnostica, VisualizationStatus/NonRepresentable, SessionNotificationScope, navigazione differita, callback con sorgente/generazione e ingresso educativo transazionale non sono semplificati. CourseManager, scope, status e primitive visuali non sono modificati. La nuova persistenza usa questi contratti; non li aggira.

## Remediation richieste

### AF08-04: RESOLVED

La policy centrale è `SessionState.TryGetEditableInput(index, out input)`, con `InputEditingDiagnostic`. Una richiesta fallita **non costituisce un dominio di editing input**: l'editing rimane indisponibile fino alla correzione della richiesta tramite i parametri oppure a Reset. Anche il command boundary `SimulationSession.SetInput` applica questa policy.

HUD, tastiera e Inspector usano lo stesso accesso protetto. Il HUD mostra sempre i valori dell'ultimo risultato valido; non indicizza la richiesta usando il suo cursore. L'Inspector non costruisce campi per input indisponibili. Non vengono inventati input o modificati gli indici del risultato valido.

Riproduzione verificata: risultato T=100, cursore 50 → target=1e308 fallito → richiesta T=1 fallita → input da HUD/tastiera e accesso dell'Inspector respinti senza eccezioni; risultato/configurazione validi conservati, diagnostica presente, Reset riuscito. Verificato anche il recupero correggendo il costo: si pubblica davvero T=1, il cursore diventa terminale 1, l'input editabile esiste soltanto a 0.

### AF08-05: RESOLVED

`ModelParameterValue` aggiunge il metadata puro `ModelParameterKind` (Real/Integer/Boolean), con default Real compatibile con i provider presenti. Il contratto precedente esponeva soltanto double e unità: questo è il minimo ampliamento necessario per confrontare esplicitamente il tipo editoriale col provider.

`ModelDefinition.CreateConfiguration` confronta ogni target editoriale con i parametri realmente dichiarati dal modello e richiede tipo e unità identici. Target duplicati/assenti e metadati incompatibili sono rifiutati; il validatore li riporta con asset/campo e nome del target. Nessuna conversione automatica di unità. Equazioni e numeri del pendolo invariati.

Test: massa kg valida; massa m o unità mancante invalide; target inesistente/duplicato e tipo incompatibile invalidi. La validazione runtime della configurazione impone la stessa regola, anche aggirando il validator editor.

### AF08-06: RESOLVED

`CourseRuntimeServices` compone resolver e condition registry per il composition root. Bootstrap passa gli stessi provider a LessonSession e HUD; il HUD li distribuisce a FormulaPresenter/FormulaBindingResolver e CostPanelPresenter. I presenter foglia richiedono ora il resolver esplicito, senza creare default propri. Il binding standalone del HUD conserva un default locale soltanto quando funge esso stesso da confine di composizione.

CatalogValidator e CourseContentValidator accettano la stessa configurazione esplicita; i vecchi overload del catalogo restano compatibili e compongono i default per il catalogo corrente. Nessun service locator globale o switch per capitolo. Per contenuti con provider custom occorre passare la medesima istanza/configurazione anche alla validazione: il menu editor standard usa deliberatamente i provider standard.

Prova end-to-end: custom metric source `fixture.extra=7 [1]` → services → Bootstrap → Formula HUD reale, valore Available; condition registry custom → validatore → LessonSession reale, condizione falsa a t=0 e vera a t=1. Stesso risultato fra HUD e visualizzazione. Il validator standard rifiuta questi metadati custom anziché considerarli implicitamente validi.

Questo wiring non richiede il contratto generale di esperimento F10, che resta da implementare nella sua fase.

### AF08-07: RESOLVED

Il presenter usa il default neutro **«Lezione completata» / «Hai completato il percorso guidato di questa lezione.»**. Nessun riferimento al prototipo, pendolo, stato/input/traiettoria/costo imposto dal presenter. Testate due lesson con significato diverso usando lo stesso default neutro. La separazione del prototipo è assicurata dagli ID/versioni del progresso, senza inserirla nella UI comune.

## Validazione consolidata

`CourseContentValidator` è il punto di ingresso completo e offre **Tools → Optimal Control → Validate Selected Course → Full F09 Summary**. Riepilogo e Console identificano asset, codice e campo. Riusa CatalogValidator per ID/versioni, duplicati, fonti/hash/pagine, scene abilitate, dimensioni/unità, prerequisiti/cicli, metadati/step e ingressi.

`BindingValidator` delega tutte le regole metriche al resolver runtime, senza duplicarne tipizzazione/unità/componenti/scope. L'execution validator verifica inoltre target editabili, layer effettivi del prefab, ExperimentHost alla radice e capability attualmente eseguibile. I contenuti statici possono avere metadati catalografici, ma sono diagnosticati come **UnsupportedRuntimeCapability** nell'execution validator finché F10 non introduce il contratto necessario.

La validazione non interpreta la lesson intera e non può garantire il successo di ingressi dopo modifiche runtime: AF08-03 mantiene la propria gestione dell'errore. Non si introducono lavori asincroni solo per testarli.

## Test aggiunti e risultati completi

**46 nuovi EditMode** in `ProgressionTests`: profilo vuoto, completion/reload, prerequisiti orientativi, identità prototipo/produzione, riordino capitoli/lesson, nuova lesson, versioni/schema, ID rimossi/altri corsi, reset/idempotenza, condizioni false/esplorazione, Enter fallito, disposal prima e durante completion, navigazione/Presentation, observer fallito, errore matematico, errore di consumer, store futuro/corrotto/assente/scrittura fallita, record invalidi, grafo/ID/versione invalidi, AF08-04/05/06 e capability/layer/parametri.

**8 nuovi PlayMode** in `ProgressionRuntimeTests`: menu/completion/riapertura/restart, due lesson e default neutro, navigazione durante pubblicazione e dopo accodamento, NonRepresentable/recupero, Enter fallito/Restart, editing fallito da HUD/tastiera/accesso condiviso Inspector, provider custom nella composizione reale. `ProgressStoreIsolation` impedisce alle suite di scrivere il profilo utente. I test F06 aggiornano soltanto l'iniezione del resolver; il percorso F08 aggiorna l'aspettativa del testo e verifica il progresso salvato.

Suite conclusive senza helper temporanei negli Assets:

| EditMode fixture | Passati |
| --- | ---: |
| CatalogValidatorTests | 64 |
| CostSystemTests | 30 |
| FormulaResolverTests | 28 |
| LessonSessionTests | 34 |
| ProgressionTests | 46 |
| PendulumDynamicsTests | 26 |
| RolloutEngineTests | 13 |
| SimulationSessionTests | 19 |
| StateProjectionTests | 8 |
| **Totale** | **268/268** |

| PlayMode fixture | Passati |
| --- | ---: |
| CostPresentationTests | 8 |
| CourseNavigationTests | 15 |
| FormulaHudTests | 9 |
| LessonOrchestrationTests | 17 |
| ProgressionRuntimeTests | 8 |
| VisualizationTests | 7 |
| **Totale** | **64/64** |

**Zero falliti, inconclusive o skipped.** Compilazione finale separata: exit **0**, zero errori/warning C#. Nessun test precedente disabilitato o rimosso. Le prime iterazioni hanno corretto un accesso a State tramite l'interfaccia di navigazione, la compatibilità degli overload del validatore, il catch esplicito di InvalidDataException e una fixture che creava due asset con lo stesso ID. XML/log iniziali conservati; i risultati qui dichiarati sono quelli conclusivi.

## Runtime, smoke e regressioni

| Verifica | Esito |
| --- | --- |
| Percorso nativo F08, sette step tramite UI reale | PASS: stato, input/previsione, transizione, traiettoria, peso, A/B, terminale, completion |
| File, menu, riapertura e nuovo ingresso in Play | PASS; progresso conservato, runtime nuovo dal primo step |
| **Nuovo processo Unity** | PASS; reload di due completion indipendenti dal file e nuova sessione r0/step 0 |
| Seconda lesson/identità/prerequisito | PASS; incompleta prima della propria verifica, prerequisito soddisfatto dopo la completion sorgente |
| Pendolo | δ=.02 s, τ_T=2 s a T=100; stessa revisione HUD/visualizzazione |
| Index-only nativo | x0=2, u0=3, x1=5; J=13+25=38; δ/τ assenti, stage/input/next assenti al terminale |
| NonRepresentable e recupero | PASS; risultato double condiviso, stato visuale esplicito e Reset riuscito |
| Enter fallito e recupero | PASS; nessuna completion, diagnostica coerente, Restart riuscito |
| Richiesta matematica fallita e recupero | PASS sul caso T100/cursore50/T1; nessun accesso fuori range |
| Smoke F00–F02 | **3/3 PASS**, due con domain reload e uno senza; root/scena/camera unici, URP/pavimento/luci/Volume preservati |
| AF08-01/02/03 | Tutte le regressioni precedenti passate; nuovi test di completion non aggirano i contratti |
| A01/A02/A03/A04 storici | Tutte le suite passate; tempo opzionale, resolver unico, readiness e risultato/configurazione immutabili preservati |
| Identità della copia verificata | **359 file Assets/OptimalControl**, zero differenze SHA-256 rispetto al workspace |

Prove in una copia isolata, preservando l'Editor aperto dell'utente. Runtime pilotato nell'Editor nativo con rendering/UI Toolkit; catture ispezionate visivamente. Non si dichiara una sessione di click manuali nell'Editor dell'utente o uno studio con studenti.

Il caso index-only usa una fixture matematica/educativa collegata a HUD e grafico, senza trasformarla in contenuto di produzione o persisterne il completamento. I casi catalogo/progressione della seconda lesson sono anch'essi fixture temporanee.

Evidenze locali: `C:/Users/querz/AppData/Local/Temp/OptCon-F09-7a992ceb3b80482ca16656db76659a09/`. File: `baseline.json`, manifest di consegna/sorgenti, `EditMode-final.xml`, `PlayMode-final.xml`, relativi log, `compile-final.log`, `native-flow.log`, `lesson-runtime-results.txt`, `process-restart.log`, `process-restart-results.txt`, `native-progress.json`, `regression-smoke.log`, `smoke-results.txt`, helper esterni e catture. Gli helper sono rimossi dal clone prima delle suite e compilazione finali.

Catture conservate nel repository: [completion salvata e runtime concluso](F09_COMPLETED.png), [menu dopo riavvio, con seconda lesson di test](F09_PROGRESS_MENU.png). La seconda voce della cattura è una fixture, assente nel catalogo consegnato.

## Inventario esatto

**27 file creati:** i seguenti **12 sorgenti**, ciascuno con il proprio `.meta` (**24 file applicativi**), questo report e i due PNG indicati sopra. Percorsi relativi ad `Assets/OptimalControl/`:

```text
Editor/BindingValidator.cs
Editor/CourseContentValidator.cs
Runtime/Application/CourseRuntimeServices.cs
Runtime/Content/Lessons/ICourseProgress.cs
Runtime/Course/IProgressStore.cs
Runtime/Course/FileProgressStore.cs
Runtime/Course/ProgressionService.cs
Runtime/Course/LessonProgressBinding.cs
Runtime/Lessons/LessonCompletion.cs
Tests/EditMode/Lessons/ProgressionTests.cs
Tests/PlayMode/ProgressionRuntimeTests.cs
Tests/PlayMode/ProgressStoreIsolation.cs
```

**21 file modificati:** i seguenti **20 applicativi**, più `Docs/OPTCON_IMPLEMENTATION_PLAN.md`:

```text
Editor/CatalogValidator.cs
Editor/ExperimentHostEditor.cs
Editor/OptCon.Editor.asmdef
Runtime/Application/AppBootstrap.cs
Runtime/Application/LessonInputRouter.cs
Runtime/Content/Definitions/ModelDefinition.cs
Runtime/Core/Contracts/ModelConfiguration.cs
Runtime/Lessons/LessonSession.cs
Runtime/Simulation/SimulationResult.cs
Runtime/Simulation/SimulationSession.cs
Runtime/UI/CostPanelPresenter.cs
Runtime/UI/CourseMenuPresenter.cs
Runtime/UI/FormulaBindingResolver.cs
Runtime/UI/FormulaPresenter.cs
Runtime/UI/LessonHudPresenter.cs
Runtime/UI/LessonStepPresenter.cs
Runtime/UI/ParameterPanelPresenter.cs
Tests/EditMode/Formula/CostSystemTests.cs
Tests/PlayMode/FormulaHudTests.cs
Tests/PlayMode/LessonOrchestrationTests.cs
```

**Zero eliminati.** Nessun `.meta` preesistente, asset editoriale, prefab, scena, PDF, package, ProjectSettings o report precedente modificato. Le impostazioni temporanee Search/play mode e l'eventuale aggiornamento URP della copia non vengono trasferiti al workspace.

## Procedura manuale in Unity

1. Aprire Bootstrap, attendere la compilazione e avviare Play. Nel menu la lesson non ancora completata mostra «Non completata»; la sola apertura non concede credito. Se esiste già un salvataggio valido, deve apparire il completamento salvato.
2. Percorrere i sette step F08: selezionare stato/risposta, modificare input/previsione, selezionare next e Step, modificare input/Step, cambiare Q[0]/risposta, salvare A e cambiare B/selezionare J, raggiungere T e selezionare terminale/risposta. Usare **Concludi verifica**.
3. Controllare il messaggio neutro di completion e «Completamento salvato». Tornare al corso, riaprire, poi uscire/rientrare in Play o riavviare l'Editor: il progresso rimane, la lesson riparte dal primo step e preset, senza A o subscription precedenti. Il file è nel percorso applicativo indicato sopra. Reset/Ricomincia non cancella il credito salvato.
4. Per prerequisiti, riordino e seconda identità usare `ProgressionRuntimeTests` o copie degli asset: ID distinto, prerequisito esplicito, stesso workspace. Il menu segnala i prerequisiti mancanti e permette esplorazione; completare una lesson non completa l'altra. Non modificare l'ID del prototipo per convertirlo in C1.L1.
5. In esplorazione portare t a 50; impostare `cost.target.0=1e308`, poi T=1. La revisione valida resta T=100/cursore50; input disabilitati e diagnostica di recupero. Provare D, callback HUD e input sample dell'Inspector: nessuna eccezione. Reset ripristina T100/t0/input originali; correggere il costo può invece pubblicare realmente T1.
6. Provare input finito `1e40` sulla coda: NonRepresentable e numeri della stessa revisione, senza progresso nuovo attribuito da una completion durante l'errore visuale. Reset/Ricomincia recuperano. Eseguire i test dedicati per navigazione durante notification, Enter fallito e disposal.
7. Selezionare PrototypeCourse → **Full F09 Summary**: catalogo valido. Su copie provare massa [m], target sconosciuto, prerequisito mancante/ciclico, parametro/layer inesistente: diagnostica localizzata. I provider custom si validano passando gli stessi `CourseRuntimeServices` usati nella composizione.
8. Test Runner: **268 EditMode + 64 PlayMode**. I test index-only verificano x1=5 e assenza di secondi; le prove native esterne documentano anche grafico/HUD al terminale. Il pendolo mantiene δ=.02 e τ=tδ.

## Deviazioni, incompatibilità e limiti residui

Nessuna deviazione funzionale da F09. Prerequisiti orientativi, default neutro della completion, store JSON semplice e assenza di async/migrazioni generali rispettano il perimetro. Le modifiche minime ai layer precedenti sono documentate: metadata puro del tipo parametro, dominio centrale di editing e ricevuta educativa necessaria alla persistenza.

API FormulaPresenter/FormulaBindingResolver/CostPanelPresenter richiedono ora il resolver esplicito; tutti i consumer presenti sono aggiornati. ModelParameterValue aggiunge un argomento opzionale compatibile. Nuovi metadati errati e capability statiche non ancora supportate vengono intenzionalmente rifiutati nell'execution validator. Dopo un tentativo fallito l'input non è editabile fino al recupero: è una policy esplicita, non un rollback del risultato.

Restano AF08-08…14 nelle fasi previste: disclosure, layout/scroll/camera, generalizzazione del costo, esperimenti statici/iterazioni/matrici, capacità future e build/profiling/usabilità. Il backend non implementa sincronizzazione fra istanze concorrenti, profili multipli o conversioni di unità; nessuno di questi sistemi è richiesto da F09. Nessuna build Player o studio di usabilità viene dichiarato.

**AF08-04: RESOLVED.**

**AF08-05: RESOLVED.**

**AF08-06: RESOLVED.**

**AF08-07: RESOLVED.**

**READY FOR F10.** Tutti i criteri F09 e le verifiche richieste sono soddisfatti; nessun blocco infrastrutturale residuo. Il nuovo contratto generale e la prova del secondo esperimento restano lavoro di F10, non capacità già consegnate.
