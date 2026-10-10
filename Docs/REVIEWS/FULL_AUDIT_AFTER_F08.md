# Audit completo dopo F08

Data: 10 ottobre 2026. Stato esaminato: F00–F08, incluso il primo vertical slice. Specifica operativa: OPTCON_IMPLEMENTATION_PLAN.md.

## 1. Executive summary

**Decisione: FIX BEFORE F09.**

Il vertical slice realizza la pipeline prevista: modello matematico → simulazione → risultato immutabile → metriche → formule, HUD, visualizzazione e percorso didattico. Nei casi ordinari esaminati, equazioni, indici, tempo fisico opzionale, costi e confronto A/B sono corretti. Non è emersa la necessità di ricostruire l'architettura.

Sono però stati riprodotti **tre problemi MAJOR** che conviene correggere prima di introdurre la progressione persistente di F09:

1. Un risultato matematico valido ma non rappresentabile in float può interrompere il dispatch: la sessione avanza di revisione mentre HUD e visualizzazione conservano quella precedente.
2. Una navigazione richiesta da un observer della sessione può scollegare l'HUD durante lo stesso dispatch; un callback già acquisito dalla multicast delegate viene poi eseguito sul presenter smontato.
3. Un'azione d'ingresso fallita può lasciare indice interno e stato didattico pubblicato disallineati, consentendo di completare un passo la cui condizione non è soddisfatta.

I **256 test esistenti passano**, ma non coprono questi tre percorsi di errore. Le riproduzioni aggiuntive sono state eseguite in una copia isolata del progetto, senza aggiungere test o correggere codice nel progetto originale.

Il percorso guidato è utilizzabile e distingue correttamente stato, input, transizione, traiettoria, costo, confronto e terminale. Prima della produzione didattica vanno migliorati l'accesso alle equazioni concrete del pendolo e del costo e la leggibilità del pannello. Sono problemi distinti dai tre blocchi strutturali.

Chapter 2 e Chapter 3 non sono ancora inseribili semplicemente tramite asset: F10 prevede già il contratto comune per esperimenti statici e dinamici; iterazioni, matrici, derivate e KKT appartengono alle fasi successive. È un limite dichiarato della roadmap, da verificare concretamente con il secondo esperimento, non una prova che l'attuale infrastruttura supporti già ogni capitolo.

### Conteggio dei rilievi

| Gravità | Numero | ID |
| --- | ---: | --- |
| CRITICAL | 0 | — |
| MAJOR | 3 | AF08-01–AF08-03 |
| MINOR | 5 | AF08-04–AF08-08 |
| POLISH | 2 | AF08-09–AF08-10 |
| LIMITATION | 4 | AF08-11–AF08-14 |
| **Totale** | **14** | |

LIMITATION identifica un confine di scope accettabile alle condizioni indicate, non un errore applicativo. Le lacune di test collegate a un problema sono descritte insieme al problema e non conteggiate una seconda volta.

## 2. Perimetro, metodo ed evidenze

### Materiale letto

Letti integralmente:

- OPTCON_PROJECT_VISION.md, OPTCON_CURRICULUM.md, OPTCON_ARCHITECTURE.md e OPTCON_IMPLEMENTATION_PLAN.md.
- Tutti i sette report Markdown preesistenti in Docs/REVIEWS/: MINI_AUDIT_F00_F03.md, A01_REMEDIATION.md, F04_IMPLEMENTATION.md, F05_IMPLEMENTATION.md, F06_IMPLEMENTATION.md, F07_IMPLEMENTATION.md e F08_IMPLEMENTATION.md.
- I tre PDF sorgente, per un totale di **137 pagine**.

La directory Docs/CourseSources/ indicata nella richiesta non esiste nello stato esaminato. I PDF effettivi sono direttamente in Docs/, come registrato dagli asset SourceDocumentDefinition e dal curriculum. Sono stati letti da quella posizione e confrontati con gli hash registrati, senza spostarli.

| Sigla usata nel report | PDF | Pagine fisiche |
| --- | --- | ---: |
| I | OPTCON-RL_introduction_optimal_control_2026_09_14.pdf | 36 |
| O | OPTCON-RL_optimization_2026_09_14.pdf | 81 |
| K | OPTCON-RL_optimality_conditions_KKT_2026_10_08.pdf | 20 |

I numeri di pagina qui indicati contano anche la copertina. Sono state inoltre ispezionate le pagine renderizzate I 22, 27, 33 e 34 per verificare notazione, Euler, pendolo e l'errata sull'equilibrio.

Esaminati i sorgenti Runtime, Editor, test e fixture, i riferimenti tra assembly, gli asset editoriali, scene, prefab, configurazioni di proiezione, UXML/USS, input e impostazioni del progetto coinvolte nel slice.

### Esecuzione indipendente

Verifica effettuata con Unity **6000.3.8f1**, su Windows, in una copia isolata. Prima delle esecuzioni, Assets, Packages e ProjectSettings della copia coincidevano byte per byte con il progetto originale. I controlli di hash su **422 file originali** non hanno rilevato modifiche durante l'audit. Le modifiche F07/F08 già presenti nel working tree sono state preservate; non viene dichiarato un working tree Git pulito.

| Verifica | Esito |
| --- | --- |
| Compilazione pulita finale, senza helper di audit | Exit code 0; nessun errore o warning C# |
| Tutti gli EditMode test | **206 passati, 0 falliti, 0 skipped** |
| Tutti i PlayMode test | **50 passati, 0 falliti, 0 skipped** |
| Percorso nativo completo dei sette step | PASS |
| Reset, ritorno al menu e riapertura | PASS nel percorso ordinario |
| Fixture index-only nativa, terminale e costo | PASS |
| Prove aggiuntive sui percorsi di errore | Confermano AF08-01–AF08-06, nei termini descritti sotto |
| Catture della Game View | 10 immagini, 1440 × 979 |

Le suite sono state rieseguite il 10 ottobre: EditMode 01:26:06–01:26:10 UTC, PlayMode 01:27:33–01:27:56 UTC. I log delle esecuzioni ordinarie e della compilazione finale non riportano eccezioni applicative. Le eccezioni delle riproduzioni avversariali sono state intercettate e registrate separatamente: il loro riscontro non equivale a un fallimento delle suite esistenti.

La prova nativa usa l'Editor in Play Mode con rendering, UI Toolkit e asset reali. Il percorso è stato pilotato con eventi UI e comandi; le Game View sono state ispezionate visivamente. Non sostituisce una sessione di usabilità con uno studente, né un test di una build Player.

### Conservazione delle prove

Per rispettare il vincolo di creare soltanto questo documento, log, XML, helper e nuove immagini rimangono fuori dal repository, nella directory temporanea:

C:/Users/querz/AppData/Local/Temp/OptCon-Audit-F08-cdf3860b6faf4ca8bce9ca4b1ce3370e/

File principali:

- [EditMode-results.xml](C:/Users/querz/AppData/Local/Temp/OptCon-Audit-F08-cdf3860b6faf4ca8bce9ca4b1ce3370e/EditMode-results.xml), [PlayMode-results.xml](C:/Users/querz/AppData/Local/Temp/OptCon-Audit-F08-cdf3860b6faf4ca8bce9ca4b1ce3370e/PlayMode-results.xml) e Compile-clean.log.
- [lesson-runtime-results.txt](C:/Users/querz/AppData/Local/Temp/OptCon-Audit-F08-cdf3860b6faf4ca8bce9ca4b1ce3370e/lesson-runtime-results.txt): verifiche del percorso ordinario.
- [adversarial-probes.txt](C:/Users/querz/AppData/Local/Temp/OptCon-Audit-F08-cdf3860b6faf4ca8bce9ca4b1ce3370e/adversarial-probes.txt) e [AuditProbes.cs](C:/Users/querz/AppData/Local/Temp/OptCon-Audit-F08-cdf3860b6faf4ca8bce9ca4b1ce3370e/AuditProbes.cs): esiti e riproduzioni dei casi aggiuntivi.
- Game View: [stato](C:/Users/querz/AppData/Local/Temp/OptCon-Audit-F08-cdf3860b6faf4ca8bce9ca4b1ce3370e/lesson-state.png), [previsione](C:/Users/querz/AppData/Local/Temp/OptCon-Audit-F08-cdf3860b6faf4ca8bce9ca4b1ce3370e/lesson-predict.png), [transizione](C:/Users/querz/AppData/Local/Temp/OptCon-Audit-F08-cdf3860b6faf4ca8bce9ca4b1ce3370e/lesson-transition.png), [traiettoria](C:/Users/querz/AppData/Local/Temp/OptCon-Audit-F08-cdf3860b6faf4ca8bce9ca4b1ce3370e/lesson-trajectory.png), [costo](C:/Users/querz/AppData/Local/Temp/OptCon-Audit-F08-cdf3860b6faf4ca8bce9ca4b1ce3370e/lesson-cost.png), [A/B](C:/Users/querz/AppData/Local/Temp/OptCon-Audit-F08-cdf3860b6faf4ca8bce9ca4b1ce3370e/lesson-comparison.png), [terminale](C:/Users/querz/AppData/Local/Temp/OptCon-Audit-F08-cdf3860b6faf4ca8bce9ca4b1ce3370e/lesson-terminal.png), [completamento](C:/Users/querz/AppData/Local/Temp/OptCon-Audit-F08-cdf3860b6faf4ca8bce9ca4b1ce3370e/lesson-completed.png), [reset](C:/Users/querz/AppData/Local/Temp/OptCon-Audit-F08-cdf3860b6faf4ca8bce9ca4b1ce3370e/lesson-reset.png), [index-only](C:/Users/querz/AppData/Local/Temp/OptCon-Audit-F08-cdf3860b6faf4ca8bce9ca4b1ce3370e/lesson-index-terminal.png).

Questi percorsi temporanei non sono artefatti versionati e potranno essere eliminati dal sistema. Le riproduzioni essenziali e i risultati sono quindi riportati anche nel documento. Le precedenti catture F08 in Docs/REVIEWS/ restano disponibili e non sono state modificate.

## 3. Correttezza matematica

### Dinamica, indici e tempo

| Aspetto | Riscontro |
| --- | --- |
| x_t | Vettore di stato salvato al campione corrente; non la sola posizione del corpo |
| u_t | Input della transizione t → t+1; non uno stato né un input interpolato |
| x_(t+1) | Campione successivo dello stesso rollout e della stessa revisione |
| Orizzonte | T transizioni, T input, T+1 stati; x0 incluso |
| t | Indice discreto, disponibile anche senza tempo fisico |
| δ | Passo fisico opzionale del modello, indipendente dal playback |
| τ | Tempo fisico della discretizzazione quando presente; τ_t = t δ nel pendolo |
| Playback | Campioni al secondo di presentazione; seek e play non rivalutano la dinamica |
| t=T | x_T disponibile; nessun u_T, x_(T+1) o stage inesistente |

Il pendolo implementa Euler esplicito:

θ_(t+1) = θ_t + δ ω_t

ω_(t+1) = ω_t + δ [−(g/l) sin(θ_t) + u_t/(m l²)].

Entrambe le componenti usano lo stato vecchio. Il renderer non introduce una seconda integrazione e non viene utilizzata la fisica Unity. Angolo in rad, velocità in rad/s e coppia in N·m sono coerenti con I pp.27 e 33. Massa e lunghezza sono positive, g può essere zero; non vengono introdotti automaticamente damping, saturazione o wrapping.

Il preset conserva δ=0.02 s, T=100 e τ_T=2 s. La fixture index-only x0=2, u0=3, T=1 produce x1=5 e non inventa δ o τ. Non è stata osservata una regressione di A01.

La rappresentazione della traiettoria è una proiezione degli stati: la storia conserva anche gli input. Le linee tra campioni sono guide visive dichiarate, non una soluzione continua o un'integrazione supplementare. Questa distinzione è coerente con I pp.9–11 e K pp.2–3.

### Costi, riferimento e confronto

Il criterio discreto implementato è:

J = Σ_(t=0)^(T−1) ℓ_t(x_t,u_t) + ℓ_T(x_T).

Nel preview quadratico diagonale:

ℓ_t = ½ (x_t−x^des)^T Q (x_t−x^des) + ½ (u_t−u^des)^T R (u_t−u^des)

ℓ_T = ½ (x_T−x^des)^T Q_T (x_T−x^des).

Sono corretti i seguenti punti:

- Tracking ed effort compongono lo stage; il terminale viene aggiunto una sola volta.
- C_j somma gli stage da 0 a j−1: C0=0 e J=C_T+ℓ_T. Il costo corrente ℓ_j non è già incluso in C_j.
- Il terminale dipende da x_T, non dallo stato del cursore; il resolver ne conserva il SampleIndex T.
- I pesi non modificano la dinamica. La rivalutazione del solo costo conserva gli oggetti dei campioni di stato.
- Il costo è dichiarato adimensionale per campione, con pesi che compensano le unità delle componenti. Non applicare δ è corretto per questo criterio discreto. La quadratura del costo continuo è un'altra capacità, prevista dopo.
- Il riferimento costante è esplicitamente dichiarato non necessariamente ammissibile; il target non viene presentato come traiettoria ottima.
- A conserva configurazione, storia, riferimento, pesi e costo propri dopo la creazione di B. La UI espone le configurazioni e non dichiara che il valore minore tra obiettivi diversi identifichi un miglior controllo.

Caso indipendente verificato: x0=2, u0=3, x1=5, Q=R=Q_T=2, riferimenti uguali a zero. Tracking=4, effort=9, stage=13, terminale=25, **J=38**. Nessun secondo fisico è necessario.

Nel preset del pendolo senza input, lo stage iniziale è 0.6125. Nel percorso nativo con le modifiche descritte dal helper, J passa a circa 108.577 nello step costo; al terminale della configurazione B è circa 191.807, composto da C_T≈190.53 e ℓ_T≈1.27733. Questi valori dipendono dalle modifiche del percorso, non sono nuovi default.

Conclusione matematica: **nessuna formula errata o violazione sistematica degli indici è stata trovata nel slice corrente**. Rimangono il rischio di unità editoriali incoerenti AF08-05 e la necessità didattica AF08-08. Le estensioni del costo sono trattate in AF08-11.

### Rapporto con i PDF

I pp.20–22 supporta la separazione stage/terminale e il preview di tracking quadratico; I p.27 l'Euler esplicito e I p.33 il pendolo. Non sono state applicate alla lettera le errata già registrate nel curriculum: in particolare l'equilibrio del pendolo di I p.34 deve imporre velocità nulla, non angolo necessariamente nullo.

Le errata su Newton, dimensioni della dualità, merit/barrier in O e su terminale/indici/condizioni in K sono correttamente elencate nei documenti. Le relative funzionalità non sono ancora implementate: la loro lettura verifica la direzione del progetto, non costituisce una verifica di un solver inesistente.

## 4. Architettura e source of truth

### Confini effettivi

| Layer | Responsabilità osservata | Dipendenze |
| --- | --- | --- |
| Core | Vettori immutabili, descrittori, configurazioni pure, dinamica, costo | Nessun engine reference |
| Simulation | Rollout, storia, risultato/configurazione, costi valutati, revisione, cursor/playback | Core; nessun engine reference |
| Content | Asset editoriali, conversione in configurazione, contratti metrici/formula/lesson/navigation | Core e Simulation |
| Application | Composizione, navigazione, host, input, orchestrazione educativa | Content, Simulation, Core, Presentation, Input System |
| Presentation/UI | Proiezioni, renderer, metric binding, HUD e policy visuale | Content, Core, Simulation |
| Editor/Test | Authoring, validazione e verifiche | Separati dal runtime distribuito |

Non sono emersi cicli tra assembly o riferimenti dal math/simulation layer a GameObject, LineRenderer o HUD. Runtime/Lessons usa l'assembly Application tramite asmref; Presentation conosce ILessonRuntime di Content, senza dipendere da Application.

La presenza del playback in SimulationSession è una scelta già esplicita dell'architettura: il clock è puro e separato da δ. Non è una seconda simulazione fisica.

### Stato e immutabilità

SimulationSession è l'unico proprietario delle revisioni. SimulationResult conserva SimulationConfiguration, modello/costo puri, storia e valutazione del costo. I vettori e le collezioni matematiche pubblicate sono immutabili.

RequestedConfiguration e Result.Configuration hanno ruoli diversi: la prima può descrivere un tentativo fallito; la seconda interpreta sempre i campioni dell'ultimo risultato valido. Questa separazione preserva A04, ma richiede ai consumer di non mischiarne gli indici: AF08-04 dimostra un caso in cui accade.

LessonSession possiede fatti educativi, risposte, modalità e indice del passo. Non duplica stati o input e non rivaluta equazioni. I presenter formattano valori e inviano comandi; le formule matematiche del modello e del costo non sono ricopiate in MonoBehaviour. Le descrizioni testuali negli asset sono authoring, non una seconda implementazione eseguibile.

Le copie di riferimento a un risultato nei consumer e il confronto A/B sono intenzionali. Non sono copie indipendenti dello stato matematico. Il problema di AF08-01 è la consegna incompleta di una nuova revisione, non la mutabilità della vecchia.

### A01–A04

| Rilievo storico | Esito dell'audit |
| --- | --- |
| A01, tempo fisico obbligatorio | Risolto e preservato: index-only valido, δ/τ assenti; pendolo con 0.02 s |
| A02, metriche e tipi ambigui | Risolto per il contratto F06 corrente; nessuna conversione implicita vector[1] → scalar |
| A03, readiness/lifecycle | Ordine di composizione e Ready corretti nei casi ordinari e nei test di navigazione; nuovo caso di teardown durante dispatch della sessione in AF08-02 |
| A04, configurazione del risultato | Risolto e preservato; A e ultimo valido mantengono la propria configurazione anche dopo B/fallimento |

Il flusso normale distingue richiesta, scene loaded, destinazione, composizione, binding e Ready con identità della destinazione. I test coprono anche due lesson sullo stesso workspace e richieste rientranti durante le notifiche di navigazione. Tale guardia non copre ogni evento di altri sistemi: AF08-02 non va confuso con il caso Ready già verificato.

Le responsabilità delle classi sono nel complesso ragionevoli. La dimensione crescente di CatalogValidator, il wiring dei provider e le azioni editoriali meritano consolidamento in F09/F10; non è giustificato dividere classi solo per ridurre il numero di righe. I problemi concreti sono descritti nei rilievi.

## 5. Rilievi dettagliati

I percorsi dei sorgenti seguenti sono relativi alla root del progetto. I numeri di riga si riferiscono allo stato auditato.

### AF08-01 — Un errore visuale interrompe la pubblicazione della revisione

- **Gravità:** MAJOR — ARCHITECTURAL PROBLEM.
- **File/classi:** Runtime/Simulation/SimulationSession.cs:86–97,140–144; Runtime/Visualization/ExperimentVisualization.cs:41–51; Runtime/Visualization/StateProjection.cs:93–100; Runtime/Visualization/TrajectoryRenderer.cs:15–17; Runtime/UI/ParameterPanelPresenter.cs:45–50. Tutti sotto Assets/OptimalControl/.
- **Evidenza:** riproduzione nativa sul pendolo. Aprire l'esplorazione libera e inviare SetInput(0, [1e40], Tail). Il valore è finito in double e produce un rollout e un costo finiti. La conversione delle coordinate in float lancia ArgumentException. Il log registra sessionRevision=1, hudRevision=0, visualPublicationRevision=0, mathematicalError=null e totalCost≈4.94205336×10^82. Reset riallinea tutti alla revisione 2.
- **Causa:** il risultato viene committato prima di Publish; la multicast Changed non isola gli errori dei consumer. Un renderer che fallisce impedisce l'esecuzione dei subscriber successivi. Una parte delle modifiche visuali può essere già applicata prima dell'eccezione.
- **Perché è un problema:** dopo un comando accettabile matematicamente, numeri e scena possono rappresentare risultati diversi. Il controllo di finitezza in Core non garantisce la rappresentabilità del display.
- **Conseguenza futura:** nuovi modelli, scale o input rendono più frequente il disallineamento; lo studente può confrontare una formula vecchia con una sessione nuova senza un diagnostico.
- **Soluzione consigliata:** gestire esplicitamente rappresentabilità e fallimento nel layer Presentation; preparare/validare la proiezione prima di aggiornare il display, oppure pubblicare uno stato visuale unavailable con diagnostico. Garantire che il fallimento di un consumer non impedisca agli altri di ricevere la revisione, con una policy esplicita di error reporting. Non restringere o clampare silenziosamente la matematica per adattarla ai float.
- **Quando:** prima di F09.
- **Verifica da aggiungere:** risultato valido in double ma fuori range float; un consumer che fallisce; gli altri ricevono la revisione corretta e il display fallito è esplicitamente diagnosticato.
- **Riferimenti:** OPTCON_ARCHITECTURE.md, sezioni sessione/pubblicazione e conversione double → float, in particolare righe 103,130,200. Nessuna formula del PDF autorizza un cambio implicito di stato per limiti grafici.

### AF08-02 — Navigazione durante un evento di sessione esegue un callback dell'HUD scollegato

- **Gravità:** MAJOR — ARCHITECTURAL PROBLEM.
- **File/classi:** Runtime/Course/CourseManager.cs:56–79,112–121; Runtime/Application/AppBootstrap.cs:79–91; Runtime/UI/LessonHudPresenter.cs:66–74,89–95; Runtime/Simulation/SimulationSession.cs:140–150.
- **Evidenza:** prova nativa: registrare un observer di SimulationSession.Changed che chiama ReturnToMenu prima della subscription dell'HUD, riproducendo un HUD ricollegato dopo l'osservatore. Eseguire Seek(1). La navigazione è accettata, la vecchia sessione chiusa e l'HUD scollegato; il callback Render già presente nella lista di invocazione viene comunque eseguito e lancia NullReferenceException su timeline.Render, riga 70. Il menu arriva poi a Ready.
- **Causa:** CourseManager protegge il proprio dispatch di lifecycle, non quello della sessione. Unsubscribe e Changed=null non eliminano callback già acquisiti dalla multicast delegate. Render non verifica lifetime/generazione del binding prima di usare i presenter disposti.
- **Perché è un problema:** il teardown non è sicuro rispetto alle notifiche già in corso e dipende dall'ordine delle subscription.
- **Conseguenza futura:** observer di progressione, diagnostica o automazioni introdotti in F09 possono navigare mentre un evento è in corso, causando eccezioni o aggiornamenti della destinazione precedente.
- **Soluzione consigliata:** definire un confine di dispatch sicuro anche tra sessione, lesson e navigazione, differendo o rifiutando esplicitamente la richiesta rientrante fino al completamento della notifica. Aggiungere guardie di lifetime/generazione nei callback dei consumer, conservando l'identità della destinazione. Evitare attese arbitrarie di un frame.
- **Quando:** prima di F09.
- **Verifica da aggiungere:** observer prima e dopo l'HUD, rebind, ritorno al menu e apertura di un'altra lesson durante Changed; nessun callback obsoleto deve operare su presenter scollegati.
- **Riferimenti:** requisito A03, OPTCON_ARCHITECTURE.md lifecycle e F04_IMPLEMENTATION.md. È un percorso ulteriore rispetto alla navigazione rientrante durante Ready già coperta.

### AF08-03 — Il fallimento di un'azione d'ingresso può saltare una condizione didattica

- **Gravità:** MAJOR — ARCHITECTURAL PROBLEM.
- **File/classi:** Runtime/Lessons/LessonSession.cs:41–77; Runtime/Content/Lessons/LessonPlan.cs:49–79; Editor/CatalogValidator.cs:91–109,372–391.
- **Evidenza:** su copie in memoria degli asset reali, creare due step: il primo senza condizioni; il secondo con ingresso Seek(101), T=100 e condizione terminal. ValidateExecutionConfiguration considera il catalogo valido. Next incrementa l'indice e poi lancia InvalidOperationException perché il seek fallisce; Presentation rimane sul primo step con CanAdvance=true. Una seconda Next restituisce true e pubblica IsCompleted=true, pur essendo ancora t=0 e IsTerminal=false. Lo stesso difetto è riproducibile con la fixture index-only.
- **Causa:** l'indice viene cambiato prima di Enter. In caso di eccezione Transition ripristina busy ma non pubblica uno stato coerente né annulla l'avanzamento; Next successiva usa il vecchio CanAdvance con il nuovo indice.
- **Perché è un problema:** un errore editoriale o runtime si trasforma in un completamento non meritato. Gli asset dei sette step attuali non contengono quel seek invalido, ma il contratto generico lo accetta.
- **Conseguenza futura:** F09 potrebbe salvare progressi errati; nuove lesson diventano fragili davanti a comandi d'ingresso incompatibili con la sessione corrente.
- **Soluzione consigliata:** rendere esplicito e coerente il fallimento dell'ingresso: committare indice/presentation soltanto dopo il successo, oppure entrare in uno stato d'errore che blocchi Next e completion. Validare per quanto possibile i comandi editoriali rispetto al preset. Gestire anche i fallimenti non rilevabili staticamente, senza concedere credito.
- **Quando:** prima di F09 e soprattutto prima della persistenza.
- **Verifica da aggiungere:** ingresso fallito su passo intermedio/finale, sequenza di Next ripetute, Previous/Restart e impossibilità di completare condizioni non soddisfatte.
- **Riferimenti:** OPTCON_ARCHITECTURE.md, progressione data-driven e validatori; criteri F08 e consolidamento F09. Il completamento deve descrivere un esito educativo verificato.

### AF08-04 — Un consumer usa il cursore valido per indicizzare una richiesta fallita più corta

- **Gravità:** MINOR — problema localizzato.
- **File/classi:** Runtime/UI/ParameterPanelPresenter.cs:45–50; Runtime/Application/LessonInputRouter.cs:80–86; Editor/ExperimentHostEditor.cs:37–46; Runtime/Simulation/SimulationSession.cs:79–97.
- **Evidenza:** seek a 50 sul pendolo T=100; impostare cost.target.0=1e308, causando overflow del costo; richiedere T=1, che fallisce ancora per lo stesso costo. Il risultato valido resta T=100 e il cursore resta 50; RequestedConfiguration ha T=1. Invocando il callback reale ChangeInput si ottiene ArgumentOutOfRangeException su RequestedConfiguration.Controls[50]. Il probe usa reflection soltanto per chiamare il metodo privato del presenter.
- **Causa:** la UI mostra correttamente il risultato valido, ma costruisce l'edit con un indice appartenente a quel risultato dentro la configurazione richiesta fallita.
- **Perché è un problema:** la separazione corretta tra richiesta e risultato di A04 viene usata in modo incoerente da alcuni consumer.
- **Conseguenza futura:** editing e recupero dopo un errore diventano imprevedibili, soprattutto con orizzonte modificabile.
- **Soluzione consigliata:** definire il dominio di editing dopo un tentativo fallito; verificare indice e shape rispetto alla configurazione effettivamente editata e offrire un recupero esplicito. Non inventare input mancanti né reinterpretare il risultato valido.
- **Quando:** durante la stabilizzazione F09.
- **Verifica da aggiungere:** errore del costo → riduzione dell'orizzonte fallita → edit input da HUD/tastiera/Inspector → reset; nessuna eccezione e messaggio coerente.
- **Riferimenti:** contratto RequestedConfiguration/Result.Configuration, A04 e gestione dell'ultimo risultato valido.

### AF08-05 — Le unità dei parametri editoriali non vengono confrontate con quelle del modello

- **Gravità:** MINOR — problema localizzato di validazione.
- **File/classi:** Editor/CatalogValidator.cs:73–86,308–330; Runtime/Content/Definitions/ModelDefinition.cs:19–33; Runtime/UI/ParameterPanelPresenter.cs:25–31,55–58; Runtime/Core/Models/PendulumConfiguration.cs:17–20.
- **Evidenza:** clonare in memoria il catalogo reale e cambiare l'unità editoriale della massa da kg a m. ValidateExecutionConfiguration restituisce IsValid=true e nessuna issue; il modello continua a dichiarare kg. Il pannello usa la label editoriale [m], mentre il parametro e le metriche runtime mantengono kg.
- **Causa:** le unità di stato/input/output sono confrontate con il descrittore del modello; quelle dei parametri sono controllate come metadati presenti, senza confronto equivalente.
- **Perché è un problema:** lo stesso parametro può ricevere due significati fisici diversi nel HUD. Gli asset attuali hanno unità corrette; il difetto riguarda l'accettazione di nuovi contenuti errati.
- **Conseguenza futura:** errori di authoring difficili da riconoscere, con numeri formalmente validi e spiegazioni fisiche sbagliate.
- **Soluzione consigliata:** confrontare target, tipo e unità di ogni parametro con il descrittore del provider; rifiutare divergenze senza conversione esplicita supportata. Non aggiungere un sistema generale di unit conversion per risolvere questo caso.
- **Quando:** F09, prima della produzione del Chapter 1.
- **Verifica da aggiungere:** massa in m respinta, massa in kg accettata, altri parametri e target mancanti/duplicati diagnosticati.
- **Riferimenti:** OPTCON_ARCHITECTURE.md, parametri e validatori; I p.33 per il significato fisico dei parametri del pendolo.

### AF08-06 — I provider estendibili non raggiungono tutti i punti di composizione reali

- **Gravità:** MINOR — problema architetturale localizzato di wiring.
- **File/classi:** Runtime/UI/FormulaBindingResolver.cs:9; Runtime/UI/FormulaPresenter.cs:16,29–34; Runtime/UI/CostPanelPresenter.cs:17; Runtime/Content/Bindings/RuntimeMetricResolver.cs:125–126; Runtime/Application/AppBootstrap.cs:53; Runtime/Lessons/LessonSession.cs:31; Editor/CatalogValidator.cs:376–378.
- **Evidenza:** un IRuntimeMetricSource aggiuntivo risolve fixture.extra come scalar=7, unità 1, Available tramite RuntimeMetricResolver(source). La medesima formula, passata al FormulaPresenter reale, risulta Invalid: “Metrica sconosciuta: fixture.extra”. FormulaBindingResolver e i presenter creano internamente il resolver di default. Per le condizioni didattiche, LessonSession accetta una registry esterna, ma Bootstrap e validatore costruiscono ciascuno quella di default: questo secondo aspetto è verificato staticamente.
- **Causa:** le API di estensione esistono, ma non sono propagate dal punto di composizione ai consumer e alla validazione.
- **Perché è un problema:** un test del resolver/provider da solo può passare mentre lo stesso contenuto non funziona nel HUD o non supera il validatore della pipeline reale.
- **Conseguenza futura:** per aggiungere nuove metriche o condizioni si rischiano modifiche ripetute ai default, registrazioni divergenti e mapping duplicati.
- **Soluzione consigliata:** comporre il resolver/provider e la registry in un punto esplicito e passarli ai consumer/validatori coinvolti, con la stessa configurazione. Non aggiungere switch per capitolo o un service locator globale.
- **Quando:** consolidamento F09; al più tardi nella prova di estensibilità F10.
- **Verifica da aggiungere:** nuova metrica fino al FormulaPresenter/HUD, nuova condizione fino a Bootstrap e validazione, non soltanto test diretti della registry.
- **Riferimenti:** OPTCON_ARCHITECTURE.md, provider di metriche e aggiunta di capitoli; F10. Questo non riapre A02: la regola di tipizzazione corrente è corretta, il problema è l'accesso ai provider.

### AF08-07 — Il presenter generico di completamento contiene testo specifico del prototipo

- **Gravità:** MINOR — hardcoding localizzato.
- **File/classi:** Runtime/UI/LessonStepPresenter.cs:32–35.
- **Evidenza:** ogni lesson completata riceve “Percorso del prototipo completato” e “Hai collegato stato, input, transizione, traiettoria e costo. Questo completa soltanto il prototipo; nessun'altra lezione è completata.” Il testo è nel presenter, non nella LessonDefinition.
- **Causa:** una precauzione corretta per il slice è stata incorporata nella UI comune.
- **Perché è un problema:** una lesson futura su gradienti o KKT comunicherebbe un risultato educativo falso, pur avendo LessonId e condizioni corretti.
- **Conseguenza futura:** aggiungere capitoli richiederà modifiche alla UI o produrrà messaggi incongruenti.
- **Soluzione consigliata:** testo di completamento neutro comune, oppure metadati editoriali di completamento con default generico. Conservare la distinzione del prototipo nei dati e nella policy di progresso.
- **Quando:** F09, prima di usare la UI per lesson di produzione.
- **Verifica da aggiungere:** due lesson con obiettivi diversi; completion con identità e messaggio corretti, senza termini del pendolo imposti dal presenter.
- **Riferimenti:** architettura data-driven, F09 e F11; il prototipo non deve promuovere automaticamente il completamento di C1.L1.

### AF08-08 — Alcune verifiche didattiche precedono l'accesso alla relazione matematica concreta

- **Gravità:** MINOR — problema didattico localizzato.
- **File/asset:** Data/Lessons/Prototype/PendulumSlice.asset:91–134,215–272; Data/Formulas/PendulumEuler.asset:19–26; Data/Formulas/AdditiveCost.asset:19–27; Runtime/UI/FormulaPresenter.cs:59–64.
- **Evidenza:** il secondo step chiede di prevedere quale componente cambi nel primo passo Euler, ma mantiene la formula astratta x_(t+1)=f_t(x_t,u_t). Nessuno dei sette step abilita il controllo di scelta formula; le equazioni concrete PendulumEuler sono accessibili nell'esplorazione libera. Lo step costo introduce tracking, effort, stage, accumulo, terminale e Q[0] insieme. L'espansione quadratica è in sourceTranscription di AdditiveCost, campo non mostrato dal presenter; il display mostra la somma additiva, i valori e le assumptions.
- **Causa:** il materiale matematico esiste in parte negli asset, ma disclosure e focus non lo rendono disponibile quando serve alla previsione o all'interpretazione del peso.
- **Perché è un problema:** lo studente può rispondere per tentativi o osservare che Q cambia J senza capire la relazione tra errore, peso e costo. Le risposte attuali sono matematicamente corrette; non viene contestata la validità numerica.
- **Conseguenza futura:** la produzione può replicare attività interattive che verificano azioni e riconoscimento di risposte più della comprensione.
- **Soluzione consigliata:** introdurre o richiamare le equazioni Euler prima della previsione e la relazione del costo prima dell'edit di Q, tramite formula focus/help/asset appropriati. Separare le spiegazioni senza trasformare il slice nell'intera lezione C1.L3.
- **Quando:** authoring F10 e preparazione F11–F13; da risolvere prima dell'uso didattico di produzione.
- **Verifica consigliata:** prova con un lettore che non conosce Euler o il tracking quadratico: deve poter motivare la previsione e spiegare cosa cambia al variare di Q.
- **Riferimenti:** I pp.20–22,27,33; visione sul collegamento formula ↔ significato ↔ esperimento; curriculum C1.L1/C1.L3.

### AF08-09 — Lo scroll separa istruzioni, controlli e matematica utile

- **Gravità:** POLISH — UX e leggibilità.
- **File/asset:** UI/CourseMenu.uss:16; UI/LessonHud.uxml; UI/LessonHud.uss; Runtime/UI/LessonStepPresenter.cs.
- **Evidenza:** nelle nuove Game View 1440×979, il primo step occupa quasi tutta l'altezza disponibile con obiettivo, istruzioni e risposte; i termini della formula sono più in basso. Nel secondo step l'input richiesto è sotto la piega; nello step costo pesi, formula e dettaglio richiedono spostamenti frequenti. La lesson-panel ha larghezza fissa 350 px di layout e un unico percorso di scroll contiene molto materiale.
- **Perché è un problema:** lo studente deve ricordare una richiesta mentre cerca il relativo controllo o valore. L'interfaccia resta utilizzabile tramite scroll: non è una relazione matematica errata.
- **Conseguenza futura:** formule lunghe, più componenti e riferimenti ai PDF aumenteranno il carico visivo.
- **Soluzione consigliata:** mantenere accessibili obiettivo corrente, formula/valore essenziale e controlli del passo; usare foldout e una gerarchia più selettiva. Verificare anche l'interazione reale con elementi inizialmente fuori viewport.
- **Quando:** F10 per il layout comune, con rifinitura in F29; prima della produzione didattica raccogliere feedback d'uso.
- **Riferimenti:** visione Live Mathematics HUD; F10/F29. I test che inviano NavigationSubmit a un bottone fuori viewport non certificano questa qualità.

### AF08-10 — Viewport fisso e densità dei layer limitano la leggibilità

- **Gravità:** POLISH — scala, camera e codifica visuale.
- **File/asset:** Scenes/LessonWorkspace.unity; Prefabs/PendulumScenario.prefab; Runtime/Visualization/PlotAxes.cs; HighlightTarget.cs:20–23; HighlightRegistry.cs:17–21.
- **Evidenza:** il preset è leggibile, ma curve A/B, marker, ghost, target, grafico del costo e label sono densi negli ultimi step. La camera ha proiezione ortografica fissa; lunghezza consentita fino a 10 m e scala visuale del corpo possono eccedere il viewport. La selezione sovrascrive il colore del ruolo con quello di highlight. Le catture confermano label e regioni ravvicinate.
- **Perché è un problema:** elementi validi possono uscire dall'inquadratura o essere difficili da distinguere. Il colore da solo non dovrebbe identificare stato, confronto e selezione.
- **Conseguenza futura:** scale, stati o modelli diversi richiederanno configurazioni di camera e proiezione più esplicite.
- **Soluzione consigliata:** limiti di viewport dichiarati o framing/pan appropriato, dimensionamento delle label e legenda/forme che mantengano l'identità durante highlight. Chiarire verbalmente il legame tra terminal cost e stato terminale: evidenziare x_T come argomento di ℓ_T non significa che i due oggetti siano uguali.
- **Quando:** F29; verificare prima la leggibilità degli scenari di Chapter 1 che escono dal preset.
- **Riferimenti:** architettura sulle proiezioni dichiarate e sui semantic binding. Non si raccomanda di clampare stati matematici per farli entrare nella camera.

### AF08-11 — Il contratto del costo conserva specializzazioni del preview

- **Gravità:** LIMITATION — LIMITATION ACCEPTABLE nello scope F07/F08, con estensione necessaria prima di F13.
- **File/classi:** Runtime/Core/Costs/CostConfiguration.cs:6–36; QuadraticTrackingCost.cs; Runtime/Content/Definitions/QuadraticTrackingCostDefinition.cs; Runtime/UI/CostPanelPresenter.cs; Runtime/Simulation/CostEvaluation.cs.
- **Evidenza:** CostConfiguration richiede un ReferenceState costante; CostContribution espone soltanto Tracking/Effort; EvaluateTerminal riceve lo stato senza indice/orizzonte. Il provider disponibile è quadratico diagonale e il pannello espone Q/QT/R e riferimenti di tale modello.
- **Perché è un limite:** una nuova funzione compatibile con queste convenzioni è aggiungibile, ma contributi con nomi diversi, riferimenti dipendenti da t/T, pesi pieni e un terminale dipendente dall'orizzonte richiedono un adattamento del contratto e della presentazione. EvaluateStage riceve t, ma questo da solo non rende time-varying l'intero sistema reference/HUD.
- **Conseguenza futura:** generalizzare senza modificare il contratto porterebbe a chiamare “tracking” un contributo che non lo è o a mostrare il target sbagliato. Il rischio diventa architetturale se il preview viene trattato come API generale definitiva.
- **Soluzione consigliata:** quando richiesta, introdurre capacità/metadati per riferimento e contributi nominati, dipendenza terminale dall'orizzonte e pesi appropriati; mantenerli immutabili nel risultato. Evitare una generalizzazione anticipata durante questo audit.
- **Quando:** F13 per riferimento/curva desiderata; prima delle lesson che richiedono pesi non diagonali o costi diversi. F15 deve introdurre separatamente la quadratura continua.
- **Riferimenti:** I pp.20–22; OPTCON_ARCHITECTURE.md sui contributi di ICostModel; limiti dichiarati in F07_IMPLEMENTATION.md e roadmap F13/F15.

### AF08-12 — Esperimenti statici, iterazioni e matrici non hanno ancora la pipeline comune

- **Gravità:** LIMITATION — LIMITATION ACCEPTABLE, già pianificata.
- **File/classi:** Runtime/Simulation/SimulationSession.cs, SimulationResult.cs, SimulationSnapshot.cs; Runtime/Content/Bindings/RuntimeMetricResolver.cs:62–65,145–149; Runtime/UI/FormulaPresenter.cs; Runtime/Application/ExperimentHost.cs.
- **Evidenza:** il risultato e il resolver richiedono una storia di dinamica discreta. CurrentIteration è rifiutato esplicitamente. Matrix esiste come kind riservato ma non come valore runtime implementato. La validità catalografica di una lesson statica non dimostra l'eseguibilità tramite l'host attuale.
- **Perché è un limite:** Chapter 2 deve rappresentare z, obiettivo, gradienti e iterazioni senza inventare stati dinamici, t o tempo fisico; Chapter 3 aggiungerà matrici e condizioni di traiettoria.
- **Conseguenza futura:** servirebbe un refactor costoso se tali esperimenti fossero forzati nella timeline del pendolo. La roadmap, invece, colloca esplicitamente il contratto comune in F10.
- **Soluzione consigliata:** attuare IExperimentSnapshot/contratto di sessione e provider previsti da F10, dimostrando il secondo esperimento attraverso composizione, metriche e HUD reali. Aggiungere iterazioni e derivate soltanto nelle fasi previste.
- **Quando:** F10 come gate prima della produzione; IterationHistory in F18 e capacità KKT nelle fasi dedicate.
- **Riferimenti:** F10 righe 706–715; architettura per esperimenti statici/dinamici; O pp.7 e seguenti, K pp.2–3.

### AF08-13 — Il slice non copre ancora il curriculum né la progressione persistente

- **Gravità:** LIMITATION — LIMITATION ACCEPTABLE.
- **File/asset:** Data/Courses/PrototypeCourse.asset; Data/Lessons/Prototype/PendulumSlice.asset; Runtime/Lessons/LessonSession.cs; Runtime/Core e Simulation nelle capacità attuali.
- **Evidenza:** è disponibile un prototipo con sette step; non sono implementati progresso su disco, gating dei prerequisiti, output runtime generale y, vincoli duri, linearizzazione/Jacobiani, metodi di ottimizzazione o KKT. Il descrittore del pendolo dichiara l'output, ma la pipeline Formula/HUD attuale non è una lesson completa su y.
- **Perché è un limite:** sarebbe scorretto presentare il prototipo come C1.L1 completa o come copertura delle condizioni di ottimalità. Attualmente il catalogo e il completamento locale li distinguono.
- **Conseguenza futura:** il principale rischio è promuovere automaticamente il completamento del prototipo quando si introducono course progress e nuove versioni dei contenuti.
- **Soluzione consigliata:** rispettare ID/versioni e separazione del prototipo; implementare output, vincoli, derivate, solver e verifiche nelle rispettive fasi con oracoli matematici indipendenti.
- **Quando:** F09 per il progresso; F11–F28 per la produzione e le capacità del curriculum.
- **Riferimenti:** criteri F09, F11 e curriculum. L'assenza di funzionalità future non è conteggiata come regressione F00–F08.

### AF08-14 — La verifica attuale non certifica build, prestazioni o usabilità con studenti

- **Gravità:** LIMITATION — limite delle evidenze, non difetto applicativo dimostrato.
- **File/sistemi:** Tests/EditMode; Tests/PlayMode; ProjectSettings; build Player, profiling e scenari non ancora presenti.
- **Evidenza:** compilazione, suite e prove grafiche sono state eseguite nell'Editor Windows. Non è stata prodotta una build Player né una sessione di usabilità con utenti. Non è stato acquisito un profilo di prestazioni per T=10000. Le fixture multidimensionali verificano matematica/metriche/proiezione; non provano una nuova lesson completa con nuovo modello tramite Bootstrap.
- **Perché è un limite:** test Editor e pilotaggio degli eventi UI non garantiscono performance, font, layout, input e comprensione sul dispositivo finale.
- **Conseguenza futura:** possibili problemi emergono tardi se la prova del secondo esperimento e i gate di build vengono saltati.
- **Soluzione consigliata:** secondo esperimento attraverso la pipeline reale in F10; sessioni di usabilità e profiling sui target in F29; build e pacchetto riproducibili in F30. Non introdurre asincronia soltanto per poterla testare.
- **Quando:** secondo i gate F10/F29/F30, con feedback didattico prima di produrre molti contenuti.
- **Riferimenti:** roadmap e visione; risultati dei test descritti sotto.

## 6. Formula system e visualizzazione

### Regola centrale di tipizzazione

La regola è effettivamente unica: RuntimeMetricResolver riceve identificatore, expected kind, scope, componente opzionale, shape attesa e unità; restituisce MetricResolution con risultato di origine, valore, SampleIndex, validità e diagnostico.

| Caso | Comportamento verificato |
| --- | --- |
| Scalar | Rimane Scalar, con unità e indice propri |
| Vector di dimensione 1 | Rimane Vector; nessuna conversione implicita |
| Vector multidimensionale | Mantiene dimensione e unità per componente |
| Componente esplicita | Estrae una Scalar della componente richiesta, mantenendo unità e SampleIndex |
| Componente fuori range | Invalid con diagnostico, non valore di fallback |
| Expected kind/shape/unit incompatibile | Invalid esplicito |
| Termine descrittivo senza binding | NotApplicable; non un numero inventato |
| Dato temporalmente non disponibile | NotApplicable/N/A con motivo |
| Metrica sconosciuta | Invalid; distinta da dato valido ma assente al terminale |
| state.current | Stato del cursore t |
| input.applied | Input della transizione che parte da t |
| state.next | Stato t+1; nessun incremento applicato una seconda volta |
| cost.terminal | Valore di x_T, indice T anche con cursore diverso |
| cost.total | Valore globale del rollout; SampleIndex non fabbricato |
| t, δ, τ | Indice distinto da quantità fisiche opzionali |

Le FormulaDefinition contengono ID stabile, espressione, descrizione, assumptions, termini ordinati, simboli, significati, unità/shape e binding. Non sono stringhe replicate nei singoli MonoBehaviour. La selezione usa il semantic registry, senza cercare un PendulumRenderer concreto.

Il HUD risolve i valori da Result.Configuration/History/Costs della revisione visualizzata. Non legge la configurazione di B per interpretare A, né ricostruisce la dinamica. Seek aggiorna i termini da quella stessa storia. Al terminale non compare una transizione fittizia.

**A02 è realmente risolto per scalar/vector/componenti e per lo scope corrente.** Il collo di bottiglia dei provider di AF08-06 è separato dalla semantica dei valori. Il supporto a matrici/iterazioni è rinviato esplicitamente, non simulato convertendo tutto in Vector.

### Semantica 3D

| Elemento | Dati autorevoli e interpretazione |
| --- | --- |
| Corpo fisico | Componente angolare dello stato salvato; posa, non integratore |
| Current/next marker e ghost | Campioni t e t+1 dello stesso risultato; next assente a T |
| Traiettoria nel piano di fase | Proiezione dichiarata (θ,ω), non percorso nello spazio fisico |
| Traiettoria 3D state-time | Coordinate di stato selezionate e asse temporale dichiarato; il terzo asse non è quota fisica |
| Controllo | Coppia firmata tramite arco; non freccia di forza lineare |
| Target | Riferimento della configurazione del risultato, distinto dal rollout |
| Confronto A/B | Storie dei due risultati conservati, non una copia dei parametri correnti |
| Grafico costo | Valutazioni già presenti in CostEvaluation, non somma rifatta nel renderer |
| Layer | Visibilità educativa/configurabile; non cancellazione dello stato matematico |
| Highlight | Identità semantica e SampleIndex risolti; non ricerca di classi concrete |

Il ghost del primo passo può sovrapporsi al corpo corrente quando cambia soltanto ω: la posa dipende da θ e lo step lo spiega. Non è evidenza di mancata simulazione. Il controllo è disegnato a dimensione di glyph dichiarata: non va interpretato come vettore di forza con lunghezza in metri.

Assi, unità e componenti omesse sono dichiarati nelle proiezioni. Per stati di dimensione maggiore, la curva visuale non pretende di mostrare tutte le componenti. Il collegamento del terminal cost al marker di x_T è un legame argomento/funzione, ammesso dall'architettura; non è stata rilevata un'equazione falsa tra costo e stato.

Nei casi ordinari osservati, HUD, costi e 3D condividono lo stesso oggetto SimulationResult. La garanzia va resa robusta anche ai fallimenti di presentazione, come richiesto da AF08-01.

## 7. Valutazione didattica dei sette step

| Step | Concetto, controlli e associazioni | Passaggio e giudizio |
| --- | --- | --- |
| 1. Stato | Introduce x_t con angolo e velocità; chiede selezione esplicita e risposta sullo stato. Futuro/input sono nascosti per non anticipare. La posa non pretende di rappresentare da sola ω. | Condizioni coerenti. Lo studente deve riconoscere entrambe le componenti. Istruzioni e formula richiedono scroll: AF08-09. |
| 2. Previsione/input | Solo edit dell'input, a t=0 con ω0=0. L'arco comunica una coppia. La domanda usa correttamente “può cambiare”, evitando di affermare che qualunque coppia produca accelerazione non nulla. | Input realmente cambiato e risposta corretta sono richiesti. L'equazione Euler concreta dovrebbe essere disponibile prima della previsione: AF08-08. |
| 3. Transizione | Rivela x_(t+1), abilita step/seek, introduce t/τ/δ e spiega la possibile sovrapposizione del ghost. Il prossimo stato salvato diventa lo stato corrente dopo Step. | Selezione di next e avanzamento manuale danno credito. L'associazione tra formula, marker e campione è corretta. La sola selezione automatica dello step non dà credito. |
| 4. Traiettoria | Abilita input, step, seek, play e playback rate. Distingue piano di fase e grafico stato-tempo dallo spazio fisico; il playback non modifica δ. | Richiede cambio input e avanzamento. La condizione di avanzamento è legata allo step manuale; premere soltanto Play non la soddisfa. Non è errore matematico, ma l'istruzione “avanza” potrebbe specificarlo meglio. |
| 5. Costo | Permette solo Q[0], mostrando che cambia il criterio ma non il rollout. Esplicita costo per campione, non integrale. Target, stage, terminale, C e J sono disponibili. | Condizione corretta: parametro effettivamente cambiato più risposta. Introduce molti termini insieme e manca la relazione quadratica visibile: AF08-08. Non va confuso con una lezione completa sul costo. |
| 6. A/B | Salva A, modifica l'input di B, espone le due configurazioni e seleziona J. L'help precisa che costo minore non implica ottimalità. | Sequenza sensata e verificata. A mantiene dati/configurazione/costo propri. Quando gli obiettivi differiscono, la UI lo dichiara invece di ordinare i controlli come “migliore/peggiore”. |
| 7. Terminale | Seek a T e selezione del terminal cost. Rimangono x_T, C_T, ℓ_T e J; input, next e stage sono N/A e i relativi oggetti non simulano campioni futuri. | Terminale corrente, selezione e risposta controllano la conclusione. Il completamento è locale alla lesson prototype.pendulum. Messaggio generico da correggere prima delle altre lesson: AF08-07. |

Il percorso complessivo introduce una sola idea dominante per i primi quattro step. Il salto più evidente è tra “osservo una traiettoria” e “leggo tutti i contributi di un criterio quadratico”: mancano una definizione accessibile dell'errore rispetto al riferimento e un collegamento esplicito Q → contributo.

La progressione richiede azioni riuscite: input/parametro invariato non danno credito e le condizioni live vengono rivalutate. Questo è più solido di una sequenza di pannelli statici. Risposte a scelta e selezione di termini non dimostrano però, da sole, comprensione trasferibile a un altro modello: una prova con studenti resta necessaria.

Reset guidato torna al preset e al primo step; Indietro riparte dal preset del passo; esplorazione libera sblocca gli strumenti senza assegnare completion. Queste politiche sono dichiarate e sono state verificate nel percorso ordinario.

## 8. Estendibilità simulata

Le valutazioni distinguono capacità dimostrata da inferenza statica e da modifica già pianificata.

| Aggiunta futura | Stato attuale | Lavoro necessario / rischio |
| --- | --- | --- |
| Dinamica discreta senza tempo fisico | Core/catalogo/resolver supportati; caso x+u verificato anche nel runtime nativo del HUD/costo | Nuovo provider ModelDefinition e visualizzazione configurata per indice o componenti appropriate; non riutilizzare un asse τ obbligatorio o il corpo del pendolo senza adattamento |
| Stato con dimensione >2 | Numerica, rollout, binding vettoriali/componenti e proiezioni configurabili; fixture multidimensionale presente | Configurare componenti mostrate e unità; spiegare dimensioni omesse. Una nuova lesson completa con tale modello non è stata eseguita |
| Più input | Vettori e campi UI generati dalla dimensione del descrittore | Binding visuali espliciti per ogni controllo; l'arco di coppia del pendolo non è un glyph universale |
| Nuovo costo | Provider separato e valutazione centralizzata; aggiungibile entro il contratto attuale | AF08-11 per contributi diversi da tracking/effort e metadati/controlli specifici |
| Target time-varying | Non supportato end-to-end dal contratto reference corrente | Estensione minima di riferimento e terminale in F13, immutabile e risolta per campione; nessuna tabella di target nella UI |
| Pesi non diagonali | Non supportati dal preview | Configurazione di matrice, validazione simmetria/PSD o PD secondo il ruolo, formula e pannello appropriati; non introdurli occultamente come array diagonale |
| Nuove formule sui dati esistenti | Data-driven e funzionanti | Authoring degli asset; per nuove metriche applicare AF08-06; layout strutturato previsto F10 |
| Nuove visualizzazioni | Primitive, proiezione e semantic registry riutilizzabili | Nuovo consumer/prefab; nessuna matematica del modello nel renderer; gestione della failure di AF08-01 |
| Nuova lesson con capability esistenti | ID, step, domande, controlli, condizioni e layer editoriali | Eliminare testo di completamento specifico e validare ingressi/provider: AF08-03,06,07 |
| Nuovo capitolo/PDF | Catalogo e fonti non limitati a tre capitoli; ID stabili indipendenti dall'ordine | Nuovi SourceDocument/Chapter/Lesson asset e verifica delle pagine/hash. Nessun motivo per cambiare CourseManager se si riusano capability disponibili |
| Esperimento statico di Chapter 2 | Soltanto metadati catalografici, non pipeline eseguibile completa | Contratto snapshot/session provider e IObjectiveFunction di F10 |
| Ottimizzazione iterativa | Non ancora implementata | IterationHistory/metriche/visualizzazione di F18; non usare t come iterazione senza dichiarare un altro scope |
| Matrici/Jacobiani/adjoint di Chapter 3 | Tipi e calcoli futuri | Estendere i valori metrici e provider nelle fasi previste; preservare convenzioni PDF, unità, shape e indici |

Non sono presenti switch che instradano la simulazione per Chapter1/Chapter2/Chapter3. La specializzazione PendulumDynamics/PendulumModelDefinition è esplicita e appropriata. Le primitive angolari sono configurabili ma dichiaratamente angolari: non devono essere vendute come rappresentazione fisica universale.

**Valutazione per Chapter 2/3:** i confini tra layer consentono di conservare gran parte di catalogo, authoring, binding, HUD e semantica. Sono necessari adattamenti dei contratti già previsti dalla roadmap, soprattutto F10 e le capacità del costo. Non è dimostrato “nessun refactor”; è ragionevole aspettarsi un adattamento circoscritto, purché avvenga prima di produrre contenuti e sia provato attraverso l'host reale.

## 9. Qualità dei test

### Risultati completi delle suite esistenti

| EditMode fixture | Passati |
| --- | ---: |
| CatalogValidatorTests | 60 |
| PendulumDynamicsTests | 26 |
| RolloutEngineTests | 13 |
| SimulationSessionTests | 16 |
| StateProjectionTests | 7 |
| FormulaResolverTests | 28 |
| CostSystemTests | 30 |
| LessonSessionTests | 26 |
| **Totale EditMode** | **206** |

| PlayMode fixture | Passati |
| --- | ---: |
| CourseNavigationTests | 15 |
| VisualizationTests | 6 |
| FormulaHudTests | 9 |
| CostPresentationTests | 8 |
| LessonOrchestrationTests | 12 |
| **Totale PlayMode** | **50** |

Nessun test fallito, inconclusive o skipped. Durate XML: circa 4.42 s EditMode e 23.04 s PlayMode, esclusi avvio/import del progetto.

### Copertura che protegge il comportamento

- **Matematica indipendente:** esempi noti di somma discreta, equilibrio, casi angolari del pendolo ed Euler che distinguono l'uso dello stato vecchio da un aggiornamento semi-implicito. Non si limitano a chiamare due volte lo stesso modello.
- **Indici e boundary:** T input/T+1 stati, x0, step/seek, rollback del cursore, next/input assenti al terminale, terminal cost e cumulative.
- **A01:** presenza opzionale del passo, valori assenti/zero/negativi/NaN/infinito quando il provider lo richiede, migrazione dell'asset legacy, conservazione del pendolo e index-only.
- **A02:** scalar, vector[1], vettori multidimensionali, component selection, errore di componente/tipo/shape/unità, SampleIndex e scope; δ/τ opzionali.
- **A04:** risultati e campioni immutabili, A mantenuto dopo B, fallimento di un tentativo senza reinterpretazione dell'ultimo risultato valido.
- **Costi:** oracoli numerici noti, stage/tracking/effort/terminale, somma cumulativa, J, modifiche dei pesi senza rivalutare dinamica, A/B e terminale.
- **Lifecycle ordinario:** scene loaded distinto da Ready, sessione/consumer presenti a Ready, navigazione rientrante nelle notifiche del navigator, workspace condiviso, riapertura e cleanup.
- **Clock:** riproduzione indipendente dal frame rate, senza confondere samples/sec con δ.
- **Didattica:** comandi disabilitati respinti anche fuori UI, azione realmente cambiata, selezione utente distinta da focus automatico, condizioni terminali live, reset/esplorazione e sette step.

### Copertura che non basta

| Test/evidenza esistente | Limite |
| --- | --- |
| Confronto dei punti LineRenderer con StateProjection | Verifica il wiring rispetto alla stessa implementazione di proiezione. I sette test indipendenti della proiezione sono necessari per evitare un errore condiviso |
| J=C_T+ℓ_T | Da solo può replicare l'implementazione; gli oracoli a risultato noto, come J=38, sono ciò che protegge la formula |
| Scalar provider custom nel resolver | Non prova che FormulaPresenter usi quel provider: AF08-06 |
| Nuovo capitolo/seconda lesson ottenuti clonando il pendolo | Provano identità, ordinamento e navigazione, non nuove capability matematiche |
| Catalogo di esperimento statico valido | Non prova che sia eseguibile tramite ExperimentHost: AF08-12 |
| Proiezione fuori range che deve lanciare | Non prova la coerenza della pubblicazione dopo l'eccezione del renderer: AF08-01 |
| Navigazione rientrante durante Ready | Non copre teardown durante SimulationSession.Changed: AF08-02 |
| Provider/ID/termine/layer invalido prima di Ready | Non copre il fallimento di un comando d'ingresso su un passo successivo: AF08-03 |
| Unità editoriali non vuote | Non prova che siano le unità del provider: AF08-05 |
| NavigationSubmit o field.value su elementi UI | Può attivare controlli fuori viewport, senza verificare quanto siano trovabili o leggibili: AF08-09 |

Le prove aggiuntive dell'audit hanno mirato a queste differenze, senza cambiare le aspettative dei test esistenti. AF08-01/02 sono riprodotti con scena e consumer reali. AF08-03/05 usano copie in memoria degli asset e validatore reale. AF08-04 invoca il vero callback di input. AF08-06 confronta resolver configurato e FormulaPresenter reale.

Non sono emerse regressioni nelle suite F00–F08. **Questa frase non implica assenza di difetti:** le nuove riproduzioni dimostrano lacune di contratto/copertura fuori dalle suite.

## 10. Runtime e UX verificati

Nel percorso nativo sono stati osservati:

1. Navigazione dal Bootstrap, composizione prima di Ready e avvio del primo step.
2. Selezione dello stato, risposta, modifica dell'input e previsione.
3. Ghost/next coerente: dopo Step il nuovo current è lo stesso vettore salvato come next.
4. Seek, play/pause, rewind e playback rate; formula, valori e marker aggiornati sulla storia.
5. Edit Q[0], cambiamento del costo senza cambiamento dei campioni; selezione dei termini e highlight.
6. Salvataggio A e modifica di B, confronto delle configurazioni e delle curve.
7. Arrivo a T, N/A per input/stage/next, x_T/C_T/ℓ_T/J disponibili e τ_T=2 s nel pendolo.
8. Conclusione del percorso, reset, menu e riapertura senza confronto o stato didattico residuo nel flusso ordinario.
9. Fixture index-only: x1=5, J=38 e nessuna quantità δ/τ in HUD/costo.

L'ispezione visiva conferma un'interfaccia con valori live, non una raccolta di schermate statiche. Le etichette dei grafici evitano di far passare lo spazio degli stati per lo spazio fisico. I principali limiti d'uso osservati sono scroll, densità e camera, classificati POLISH. Il disallineamento di revisioni dopo un errore e il completamento spurio sono invece problemi di correttezza del sistema.

## 11. Debito accettabile e rischi prima della produzione

### Debito tecnico accettabile nello stato F08

- Costo diagonale, riferimento costante e nessun solver, purché l'anteprima resti esplicitamente tale e AF08-11 venga affrontato prima delle capacità che lo richiedono.
- Espressioni Unicode e layout iniziale, con IFormulaView/layout strutturato e secondo esperimento previsti in F10.
- Nessun progresso persistente, output generale, vincolo, Jacobiano, algoritmo o KKT prima delle fasi dedicate.
- Calcolo sincrono e viewport configurato per il preset, senza dichiarare performance già verificate su orizzonti massimi o altri dispositivi.
- Fixture di nuovi modelli e fonti, senza sostenere che il loro catalogo valido dimostri da solo l'estendibilità runtime completa.

### Rischi e gate consigliati

| Momento | Gate / rischio da chiudere |
| --- | --- |
| **Prima di F09** | AF08-01/02/03: pubblicazione robusta, teardown sicuro durante dispatch, ingresso didattico fallito che non concede completion |
| **F09, prima della persistenza** | Recupero della richiesta fallita, unità dei provider, validazione degli ingressi; identità/versioni del progresso e nessuna promozione del prototipo |
| **F09/F10** | Provider registrati coerentemente in runtime e validatori; testo di completamento comune senza concetti imposti dal prototipo |
| **F10, prima di Chapter 1 production** | Secondo esperimento statico eseguito tramite host/metriche/HUD reali; layout di formule e matrice; feedback reale sulla leggibilità |
| **F11–F13** | Equazioni accessibili nel momento della verifica, distinzione stato/output, introduzione graduale del costo, riferimenti e vincoli dichiarati correttamente |
| **F13 e seguenti** | Estensione del contratto reference/costo prima di time-varying, matrici piene e contributi diversi |
| **F29/F30** | Prestazioni e usabilità sul target, scale/camera/label, build Player e pacchetto riproducibile |

Non è necessario correggere tutti i POLISH o implementare capacità future prima di F09. I tre MAJOR richiedono una remediation mirata, con test dei casi riprodotti; non un rifacimento generale. Le raccomandazioni in questa tabella non costituiscono implementazione o aggiornamento della roadmap.

## 12. Decisione finale

**FIX BEFORE F09**

La base matematica e la separazione dei layer sono sufficientemente solide da conservare. Il vertical slice ha superato compilazione, 206 EditMode, 50 PlayMode e il percorso nativo completo. Tuttavia pubblicazione delle revisioni, teardown rientrante e transizione didattica fallita presentano errori riproducibili che possono rendere incoerenti display o completamenti, proprio nei contratti sui quali F09 deve costruire validazione e progressione persistente.

Chiudere AF08-01–AF08-03 con regressioni mirate prima di proseguire. Mantenere come gate F10 la dimostrazione del secondo esperimento; non dichiarare già acquisita la compatibilità runtime con Chapter 2/3.

**Modifiche effettuate dall'audit:** creato esclusivamente Docs/REVIEWS/FULL_AUDIT_AFTER_F08.md. Nessun file applicativo, asset, test del progetto originale, precedente report o stato della roadmap modificato. Nessuna correzione automatica e nessuna implementazione di F09.
