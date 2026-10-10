# F10 — Authoring formule e prova di estensibilità

**Stato: COMPLETATA — 10 ottobre 2026.**

**READY FOR CHAPTER 1 PRODUCTION / F11. F11 non iniziata.**

## Ambito e prerequisiti

Implementata esclusivamente F10 del piano operativo. Letti integralmente visione, curriculum, architettura, piano e tutti i report precedenti in `Docs/REVIEWS/`. Il progetto iniziale coincideva con il manifest verificato di F09: nessuna modifica applicativa successiva non esaminata. F09 risultava completata con 268 EditMode e 64 PlayMode; AF08-01…07 erano chiusi. Le suite finali di questa consegna conservano tutti quei test.

Il problema affrontato era il vincolo implicito dei consumer comuni a `SimulationSession`: anche un valore statico avrebbe richiesto storia, cursor e transizioni. Il nuovo confine comune pubblica un risultato immutabile, lasciando questi concetti alla capability dinamica. La prova di riuso avviene attraverso le scene e il composition root esistenti.

Fonti matematiche: PDF O, pagina fisica 23/slide 22, programma quadratico; I, pagina fisica 31, costo continuo; K, pagina fisica 10, notazione compatta. Verificate le estrazioni dei PDF contro le impronte delle fonti già revisionate. Gli asset conservano riferimenti alle fonti; la specializzazione diagonale con termine lineare nullo è dichiarata come adattamento. Il feedback delle precedenti Game View e la nuova ispezione nativa guidano il layout minimo; nessuno studio di usabilità è dichiarato.

## Contratto comune, snapshot e capability

`IExperimentConfiguration` espone identità e parametri puri congelati. `IExperimentResult` associa revisione e configurazione. `IExperimentSnapshot` associa quel risultato a capability, revisione del tentativo, errore e chiusura. Il contratto non contiene cursor, T, history, input, δ, τ, stage cost o iterazioni.

`IExperimentSession` espone pubblicazione corrente, subscription con lease idempotente, supporto dei comandi, `SessionNotificationScope`, diagnostica del dispatch e lifetime. Mantiene il DTO `SimulationCommand` per compatibilità: lo statico supporta esclusivamente Reset e SetParameter e rifiuta gli altri comandi. Il nome storico del DTO non introduce transizioni nello statico.

| Percorso | Capability | Pubblicazione e risultato | Specificità conservate |
| --- | --- | --- | --- |
| Dinamico | Parameters, Trajectory | `SessionState`, `SimulationResult` | `SimulationSnapshot` per campione, rollout, cursor, playback, input, tempo fisico opzionale, costo e A/B |
| Statico | Parameters, Objective | `ObjectiveSnapshot`, `ObjectiveResult` | decisione, pesi e valore dell'obiettivo; nessun campione temporale |

`SimulationSession` implementa il contratto adattando il dispatcher esistente, senza riscrivere rollout o playback. `SimulationConfiguration` e `SimulationResult` implementano i contratti puri. `SimulationResult.Select(cursor)` adatta la vecchia API di consultazione di risultati conservati: seleziona esclusivamente la storia di quel risultato e non esegue un rollout.

L'adattamento comune è posto su **SessionState**, che già contiene risultato/configurazione e stato della pubblicazione; `SimulationSnapshot` resta il campione matematico della history. Questo è il dettaglio applicativo adottato rispetto alla formulazione sintetica «adattare SimulationSnapshot» del piano: evita di attribuire al singolo campione identità/configurazioni estranee e preserva A04.

## Obiettivo statico e oracolo

`IObjectiveFunction` dichiara dimensione e `Value(z)`. `DiagonalQuadraticObjective` calcola soltanto:

`ℓ(z) = 1/2 Σ_i w_i z_i²`, con pesi finiti strettamente positivi.

È una specializzazione di `1/2 zᵀQz + qᵀz`, Q diagonale e q=0. Non riutilizza `ICostModel` o la dinamica e non calcola gradienti, minimi, Hessiane o sequenze iterative.

| Decisione | Pesi | Valore atteso indipendente |
| --- | --- | --- |
| [1, 2] | [2, 4] | 9 |
| [2, 2] | [2, 4] | 12 |
| [2, 2] | [6, 4] | 20 |
| [0, 0] | [2, 4] | 0 |

`ObjectiveConfiguration` congela decisione, funzione/pesi e metadati dei parametri. `ObjectiveResult` valuta la funzione una volta e conserva valore e configurazione insieme. `ObjectiveSession` costruisce e pubblica una nuova revisione soltanto dopo una valutazione riuscita. A rimane interpretabile dopo B; un tentativo fallito conserva l'ultimo risultato valido completo, con errore e revisione del tentativo separati. HUD e visualizzazione leggono il risultato valido; Reset ripristina il preset. Una request fallita non viene usata per reinterpretarlo.

## Provider, host e composizione

`ExperimentProviderDefinition` è il factory per asset, con capability e creazione della sessione. `ModelDefinition` diventa il provider dinamico conservando le proprie API. `QuadraticObjectiveDefinition` compone la sessione statica e verifica dimensione, parametri, target, range, tipo e unità; rifiuta dichiarazioni dinamiche o un costo di traiettoria.

`ExperimentDefinition.Provider` accetta il nuovo campo provider e mantiene il fallback al campo model degli asset preesistenti. Due provider differenti assegnati contemporaneamente sono rifiutati. Nessun asset precedente è stato riserializzato o migrato.

La pipeline verificata è:

`FixtureCourse → QuadraticLesson → QuadraticExperiment → QuadraticObjectiveDefinition → AppBootstrap → ExperimentHost → ObjectiveSession/ObjectiveSnapshot → CourseRuntimeServices.Metrics → FormulaPresenter/MetricPointVisualization`.

`AppBootstrap`, `CourseManager` e `ExperimentHost` compongono entrambi i percorsi. Il prefab offre un solo `IExperimentPresentation` compatibile. Playback e input dinamico vengono collegati soltanto all'adattatore dinamico. Le vecchie proprietà tipizzate `Session`/`ActiveSession` sono mantenute come accessi di compatibilità; per uno statico restituiscono null. Le nuove proprietà `Experiment`/`ActiveExperiment` sono autorevoli.

`CourseRuntimeServices` resta il punto di composizione di resolver e condition registry introdotto in F09. Il medesimo resolver viene passato a host, HUD e presentazione e può essere passato alla validazione. I presenter foglia non ricostruiscono provider. Il test custom passa una metrica Snapshot attraverso composizione, resolver, HUD reale e condizione runtime; un test EditMode verifica anche il catalogo con quella stessa configurazione.

## Metriche, scope e tipizzazione

`RuntimeMetricResolver` conserva l'estensione dinamica F09 `IRuntimeMetricSource` e accetta `IExperimentMetricSource` per pubblicazioni comuni. Il provider `ObjectiveMetricSource` legge esclusivamente valori salvati:

| Metrica | Tipo/shape | Unità | Scope | SampleIndex |
| --- | --- | --- | --- | --- |
| decision.current | Vector n×1 | 1 per componente | Snapshot | null |
| objective.weights | Vector n×1 | 1 per componente | Snapshot | null |
| objective.value | Scalar 1×1 | 1 | Snapshot | null |
| parameter.* | Scalar 1×1 | metadato congelato del risultato | Snapshot | null |
| time.index, time.step, time.physical nello statico | Scalar senza valore, NotApplicable | 1 / s / s | Snapshot | null |

La regola centrale A02 resta unica: Vector di dimensione 1 rimane Vector; una componente deve essere selezionata esplicitamente; componente fuori range, expected kind, shape, unità e scope incompatibili producono diagnostica. Nessun consumer valuta nuovamente l'obiettivo o la dinamica. Gli scope temporali nello statico sono invalidi; `CurrentIteration` continua a essere non supportato.

Per il pendolo t resta discreto, δ=.02 s e τ=tδ. Per index-only t esiste, δ/τ sono N/A. Al terminale non vengono creati input, next state o stage cost. La consultazione di A usa configurazione e history di A anche dopo B. `FormulaPresenter.Cursor` è nullable e nello statico è null; i binding si basano sul resolver, non su questa proprietà di compatibilità.

## Formula system e authoring

`FormulaDefinition` aggiunge un layout opzionale a ID di nodi stabili. L'albero di display è separato da expression/source transcription, termini, binding, unità e significato. Le formule esistenti continuano a usare la loro espressione Unicode e gli stessi termini live.

`IFormulaView` e `StructuredFormulaView` supportano Text, Term, Row, Fraction, Script, Sum, Integral e Matrix. I termini inline chiamano la selezione comune; selezione, disclosure, pannello del significato, valori live e highlight mantengono il contratto F06. Il renderer non è un parser o un valutatore matematico. Le espressioni larghe sono consultabili con scorrimento orizzontale.

`FormulaLayout.Validate` verifica ID, riferimenti, termini, arità, cicli, profondità, nodi irraggiungibili e numero di celle rows×columns. Il layout usa nodi piatti serializzati; riutilizzare un nodo non introduce cicli né copie di stato matematico.

L'Inspector `FormulaDefinitionEditor` offre i dati serializzati, validazione e anteprima selezionabile dello stesso layout. Non inventa valori runtime. La fixture contiene formula quadratica con somma/frazione/indici, Q diagonale 2×2, integrale continuo di I e stack simbolico di K.

**Matrix support: soltanto layout.** Q è una griglia 2×2 con binding Scalar separati per i due pesi e zeri editoriali. Non viene esposto un Matrix runtime, né convertita una matrice in Vector. Lo stack simbolico con ellissi è un esempio di layout, non una history dello statico. Integrale e stack hanno termini descrittivi non associati a metriche eseguibili e mostrano N/A. L'integrale non diventa il costo dello statico o del pendolo.

## Presentazione e HUD

`IExperimentPresentation` associa binding comune, capability richieste e `VisualizationStatus`. `ExperimentVisualization` adatta il percorso dinamico esistente. `MetricPointVisualization` risolve una metrica Vector e una Scalar dalla stessa pubblicazione, proietta due componenti sul piano e il valore su un'ordinata separata. Esegue soltanto geometria/scaling e controlli di rappresentabilità.

Il prefab statico riutilizza `StateMarker`, `HighlightTarget`, `HighlightRegistry` e `LayerController`. Assi espliciti: z1/z2 adimensionali, una unità scena per unità numerica; ordinata obiettivo adimensionale, .25 unità scena per unità numerica. Il piano non è una posizione fisica e non mostra una superficie o un percorso di ottimizzazione. Nessuna linea viene presentata come traiettoria.

I target Snapshot sono disponibili globalmente con revisione e SampleIndex null. Questo è distinto da un target temporale indisponibile con SampleIndex null. La selezione di `objective.value` evidenzia l'ordinata della stessa revisione; A non evidenzia accidentalmente un target di B.

Il medesimo `LessonHudPresenter` usa FormulaPresenter e shell comuni. Compone timeline/pannelli dinamici per una trajectory e `SnapshotParameterPresenter` per lo statico. Lo statico mostra parametri, Reset, revisione, errore del tentativo, termini e valori; nasconde trasporto, seek, costo di traiettoria e legenda dinamica. HUD e Inspector inviano gli stessi comandi attraverso LessonSession. Il testo comune di esplorazione è neutro e non promette tastiera o confronto A/B per ogni esperimento.

Gli assi della fixture hanno scala fissa: valori molto grandi possono uscire dal viewport pur rimanendo matematicamente validi. Il HUD resta autorevole; non viene dichiarato un sistema generale di autoscaling. La leggibilità minima dei casi nominali è stata verificata nelle catture finali.

## Lifecycle, progress e failure

Richiesta → SceneLoaded → Composing → Binding → Ready conserva identità di destinazione. Ready viene pubblicato dopo creazione di sessione/lesson, presentazione, diagnostica e HUD. Il test statico verifica lo stesso oggetto snapshot in HUD e visualizzazione già dentro l'observer Ready.

Le notifiche della sessione statica hanno dispatch controllato: ogni consumer viene chiamato anche se un precedente fallisce; gli errori vengono aggregati in `SessionPublicationException`; la configurazione matematica pubblicata rimane valida. Comandi rientranti durante dispatch sono rifiutati. Navigazione e teardown usano lo stesso `SessionNotificationScope`, quindi si differiscono al confine esterno. Lease e generation/lifetime guard impediscono callback residue dopo unbind/reopen.

`LessonSession`, receipt e `LessonProgressBinding` consumano il contratto comune, conservando le protezioni esistenti: Enter fallito non commette una nuova identità didattica e non può completare; errore matematico, chiusura, errore Presentation, NonRepresentable, consumer failure e navigazione annullano l'accredito persistente. Reset e riapertura non duplicano completion. Runtime completion e persistent progress restano distinti, compreso il caso NonRepresentable.

Lo storage e lo schema F09 sono invariati. Identità della fixture: corso `fixture.f10.course`, lesson `fixture.f10.static`, esperimento `fixture.f10.objective`. Il corso di fixture è esplicitamente un prototipo e include anche l'asset `prototype.pendulum`; nessuna promozione a Chapter 1. Nessun completamento statico completa il pendolo. L'app predefinita conserva PrototypeCourse; il catalogo F10 si seleziona per la prova descritta sotto.

| Contratto precedente | Esito |
| --- | --- |
| A01 — tempo fisico opzionale | Preservato: pendolo .02 s, index-only senza δ/τ, statico senza t/δ/τ |
| A02 — resolver tipizzato | Preservato ed esteso allo scope Snapshot, senza conversioni implicite |
| A03 — readiness | Preservato e verificato sul vero host statico |
| A04 — configurazione immutabile | Preservato per risultato dinamico e statico, anche dopo failure |
| AF08-01/02/03 | Dispatch, scope, NonRepresentable, lifetime, Enter e completion preservati |
| AF08-04 | Policy di editing dinamica invariata; regressione T100/cursor50/request T1 fallita PASS |
| AF08-05 | Validazione metadati provider centralizzata e riusata nello statico; nessuna conversione di unità |
| AF08-06 | Provider autorevole condiviso da composizione/consumer/validator; custom dinamico e statico PASS |
| AF08-07 | Presenter generico; completion neutra e istruzioni specifiche nei dati |

## Catalogo e validazione

`CatalogValidator` verifica provider, dichiarazione di transizioni/capability, compatibilità della presentazione, parametri e riferimenti. I binding si verificano con il resolver sui risultati del provider reale, senza una seconda implementazione della funzione obiettivo. La diagnostica dei binding resta disponibile anche se un piano didattico contiene un riferimento invalido.

`LessonCapabilityRequirements` è il preflight condiviso da validator e runtime per controlli/entry commands/costi. Le condizioni possono dichiarare capability richieste nel registry. Una lesson statica non può richiedere seek, terminale dinamico, input o stage cost; range/target degli entry SetParameter sono controllati staticamente. IDs, ownership, versioni, prerequisiti e metadati completion F09 conservano le validazioni precedenti. Il runtime rimane responsabile dei fallimenti che dipendono dallo stato corrente.

## Test e compilazione

Unity **6000.3.8f1**, esecuzione in copia isolata con Library già disponibile e sorgenti sincronizzati. L'Editor aperto dall'utente non è stato chiuso o riconfigurato. Gli helper temporanei di authoring e prove native sono rimossi prima delle suite finali e non appartengono alla consegna. Nessun pacchetto o dipendenza esterna aggiunto.

Compilazione finale: **exit 0, zero errori e zero warning C#**. Grafo delle 13 assembly aciclico; Core e Simulation conservano `noEngineReferences: true`. Le 416 risorse/file applicativi della consegna coincidono byte per byte con quelli della copia verificata. Asset editoriali preesistenti, scene, prefab precedenti, PDF, specifiche/review precedenti, Packages e ProjectSettings conservano le impronte iniziali.

| EditMode | Passati |
| --- | ---: |
| CatalogValidatorTests | 64 |
| CostSystemTests | 30 |
| ExperimentExtensibilityTests, nuovi | 28 |
| FormulaResolverTests | 28 |
| LessonSessionTests | 34 |
| PendulumDynamicsTests | 26 |
| ProgressionTests | 46 |
| RolloutEngineTests | 13 |
| SimulationSessionTests | 19 |
| StateProjectionTests | 8 |
| **Totale** | **296/296** |

| PlayMode | Passati |
| --- | ---: |
| CostPresentationTests | 8 |
| CourseNavigationTests | 15 |
| ExperimentExtensibilityRuntimeTests, nuovi | 10 |
| FormulaHudTests | 9 |
| LessonOrchestrationTests | 17 |
| ProgressionRuntimeTests | 8 |
| VisualizationTests | 7 |
| **Totale** | **74/74** |

**Zero falliti, saltati o inconcludenti** in entrambe le suite finali. Nuovi: **28 EditMode + 10 PlayMode**. Tutti i 268+64 test precedenti passano senza alterarne le asserzioni.

I nuovi EditMode coprono oracoli 9/12/20/0, dimensioni/pesi, immutabilità A/B e failure, contratto senza timeline, typing n=1/n=2, unità/componenti/scope, dispatch/close/subscription, catalogo completo, rifiuto della dinamica fittizia, metadati, quattro layout, shape/cicli/arità, capability/presentation/entry incompatibili, custom provider nella validazione, pendolo via contratto comune e assenza di rami specifici nei consumer comuni.

I nuovi PlayMode coprono catalogo→Ready reale, snapshot condiviso, editing dal HUD, highlight globale, completion/identità/restart, cleanup/reopen, navigazione differita, quattro formule strutturate, request matematica fallita, NonRepresentable e recupero, Enter fallito, annullamento receipt per navigazione, custom metrica/condizione attraverso HUD e lesson reali.

Risultati completi: [EditMode XML](F10_EVIDENCE/EditMode-final.xml), [PlayMode XML](F10_EVIDENCE/PlayMode-final.xml), [riepilogo](F10_EVIDENCE/test-summary.json), [manifest sorgenti verificati](F10_EVIDENCE/verified-application-manifest.json).

## Smoke test e runtime nativo

Prove pilotate nell'Editor Unity nativo con Game View e ispezione visiva delle catture, non un'applicazione di test parallela.

**Pendolo: PASS.** Sette passi F08 eseguiti mediante controlli UI reali: risposte/selezioni, input/predizione, next/step, traiettoria, peso di costo, A/B, terminale e completion. Formula e presentazione condividono il risultato. Modificare Q cambia J senza cambiare gli stati. A conserva configurazione e costo dopo B. Al terminale mancano input/stage/next; δ=.02 s, τ_T=2 s. Menu/reopen e nuovo ingresso in Play ricaricano il progresso. Verificati seconda identità/prerequisito, NonRepresentable, Enter fallito, request T1 fallita da cursor50 e Reset. Index-only nativo: x0=2, u0=3, x1=5, J=13+25=38 e nessun δ/τ. [Esito dettagliato](F10_EVIDENCE/lesson-runtime-results.txt).

**Statico: PASS.** Apertura dal pulsante del normale menu di FixtureCourse, composizione nel workspace, Ready, z1 da 1 a 2 e valore 9→12; peso w1 da 2 a 6 e valore 12→20. Formula, point marker e ordinate condividono la stessa pubblicazione; A resta 9. Selezione inline del valore evidenzia il target globale senza SampleIndex. Ispezionati somma/frazione/indici, matrice, integrale e stack. Completion accredita soltanto la fixture. Menu, riapertura e restart di Play ripristinano sessione nuova e progresso indipendente. Request NaN conserva il risultato e Reset recupera; navigazione da observer completa il dispatch e chiude il vecchio lifetime. [Esito dettagliato](F10_EVIDENCE/static-runtime-results.txt).

Entrambe le prove: **zero Error/Exception/Assert applicativi**. La sola correzione richiesta dall'ispezione visiva è il ridimensionamento delle etichette nel nuovo prefab statico; la prova statica è stata ripetuta sulla versione corretta.

Catture conservate: [statico e formula](F10_EVIDENCE/static-edited.png), [matrice](F10_EVIDENCE/static-matrix.png), [integrale](F10_EVIDENCE/static-integral.png), [stack K](F10_EVIDENCE/static-compact-vector.png), [progresso nel menu](F10_EVIDENCE/static-progress-menu.png), [pendolo/costo](F10_EVIDENCE/lesson-cost.png), [terminale](F10_EVIDENCE/lesson-terminal.png), [index-only terminale](F10_EVIDENCE/lesson-index-terminal.png). Per l'integrale la cattura mostra l'estremo iniziale del layout; l'intera espressione è consultabile tramite lo scorrimento orizzontale.

Log completi e helper riproducibili della verifica restano in `C:\Users\querz\AppData\Local\Temp\OptCon-F10-c7bc7c406a1d46479d780a6578ed8460`, insieme alla compilazione finale. Le evidenze essenziali sono copiate nella cartella del report.

## Procedura manuale in Unity

1. Attendere l'import, aprire **Bootstrap.unity**, Play. Con il corso predefinito eseguire il percorso guidato del pendolo; verificare formula, input/step, costo, A/B, terminale, completion, menu e riapertura.
2. Per lo statico, fuori Play assegnare temporaneamente `Tests/Fixtures/F10/FixtureCourse.asset` al campo Course di AppBootstrap nella scena Bootstrap, entrare in Play e al termine ripristinare PrototypeCourse senza salvare la modifica della scena. I test fanno questa stessa selezione tramite iniezione prima di Start. Nessuna scena di produzione dedicata è necessaria.
3. Nel normale menu aprire **F10 fixture: decision and objective**. Stato iniziale z=[1,2], w=[2,4], ℓ=9. Non devono apparire timeline, Play/Step, input o costo di traiettoria. La diagnostica mostra una revisione Snapshot.
4. Modificare z1 a 2 e confermare il campo: ℓ=12, punto (2,2), ordinata a 12 nella scala dichiarata. Modificare w1 a 6: ℓ=20; il punto decisionale resta (2,2). Selezionare ℓ(z) inline o nell'elenco: evidenziata l'ordinata e mostrati significato, unità, Scalar 1×1, Available e nessun campione associato.
5. Usare la scelta formula per Q 2×2, integrale I e stack K; scorrere verticalmente il side panel e orizzontalmente il layout quando necessario. Q legge i pesi live; integrale/stack sono esempi editoriali N/A. In Inspector selezionare i FormulaDefinition e usare **Verifica layout e aggiorna anteprima**; la preview seleziona i termini senza inventare valori.
6. Selezionare la formula quadratica, cambiare z1 rispetto al preset, selezionare il valore e premere Concludi. Tornare al menu: solo la fixture è completata. Riaprire: preset 9 e percorso non ancora runtime-completato; il progresso salvato rimane. Terminare/riavviare Play per verificare il reload.
7. Per failure e lifecycle ripetibili eseguire `ExperimentExtensibilityRuntimeTests`: request NaN, NonRepresentable su copia con range esteso, Enter invalido e navigazione durante dispatch/completion. Nessuna di queste fixture di errore modifica gli asset originali. Reset deve recuperare; i casi invalidi non salvano progresso.
8. Test Runner: **Run All EditMode** e **Run All PlayMode**, attesi 296 e 74. Selezionare FixtureCourse e usare **Validate Selected Course / Execution Configuration**: valido. Le prove di provider custom passano esplicitamente i medesimi CourseRuntimeServices a validazione e runtime.

## Compatibilità, deviazioni e limiti

- Nessuna migrazione di asset o progresso. Nuovi campi opzionali e valori enum aggiunti in coda; ID/GUID e campi precedenti sono conservati. Il catalogo F10 è di test e non è aggiunto al corso predefinito.
- Il dettaglio dell'adattamento su SessionState invece di modificare il campione SimulationSnapshot è spiegato sopra. Nessuna riscrittura di SimulationSession; nessun sistema parallelo di navigation/progress/HUD.
- Restano specifici del dinamico il sample adapter del resolver, la timeline, input, playback, cost panel, comparison e le viste di traiettoria. Il presenter Formula non contiene un ramo per pendolo o obiettivo quadratico; l'accesso nullable al cursor è solo compatibilità con il percorso campionato.
- Layout strutturato minimo, non un motore TeX/CAS. Matrix runtime, blocchi numerici generali, gradienti, Hessiane, Jacobiani, superfici, ottimizzatori, IterationHistory e KKT restano alle fasi previste. Il nuovo obiettivo è limitato alla diagonale positiva e alla valutazione del valore.
- AF08-08 disclosure/equazioni didattiche, AF08-09/10 polish di scroll/camera/densità, AF08-11 generalizzazione completa del costo, AF08-12 iterazioni/Matrix runtime, AF08-13 capacità future e AF08-14 build/profiling/usability rimangono rimandati. I soli aggiustamenti visuali sono il layout richiesto da F10 e la leggibilità della sua fixture.
- Nessun Player build, profiling o contenuto Chapter 1 è stato realizzato. Nessun problema bloccante emerso nel perimetro F10.

## F10 EXTENSIBILITY GATE

| Domanda | Risposta verificata |
| --- | --- |
| 1. Uno statico reale attraversa la pipeline completa? | **SÌ**, dal catalogo UI al provider, host, sessione/snapshot, resolver, Formula HUD, presentazione, lesson e progresso. |
| 2. È stato necessario fingere timestep/trajectory? | **NO**. Nessuna history, input, t, δ, τ o iterazione nel risultato statico; scope Snapshot e SampleIndex null. |
| 3. Il pendolo continua a funzionare? | **SÌ**, suite precedenti complete e percorso nativo F08, costo/A-B/terminale/progresso/failure PASS. |
| 4. Formula/HUD sono condivisi? | **SÌ**, stessi presenter, binding, unità, selezione e shell; controlli specifici composti secondo capability. |
| 5. Provider/composition sono condivisi? | **SÌ**, ExperimentDefinition/ExperimentProviderDefinition, AppBootstrap/ExperimentHost e CourseRuntimeServices; custom provider E2E PASS. |
| 6. Cosa resta specifico del dinamico? | Rollout/history/sample/cursor, playback, input/transizioni, δ/τ opzionali, cost system/A-B e viste di traiettoria. Non sono requisiti dello statico. |
| 7. Cosa è intenzionalmente rimandato? | Solver/derivate/iterazioni/Matrix runtime, generalizzazione completa del costo, authoring del curriculum, polish finale, Player build e profiling. |

**Tutti i criteri di F10 e il gate sono soddisfatti. READY FOR CHAPTER 1 PRODUCTION / F11.**

## Inventario dei file

L'inventario seguente è calcolato contro il manifest iniziale F09. Nessun file cancellato. Include metadati Unity e prove conservate. Gli script nativi temporanei e il clone non sono file del progetto.

**Creati: 72 file; modificati: 35; cancellati: 0.**

### File creati

```text
Assets/OptimalControl/Editor/FormulaAuthoring.meta
Assets/OptimalControl/Editor/FormulaAuthoring/FormulaDefinitionEditor.cs
Assets/OptimalControl/Editor/FormulaAuthoring/FormulaDefinitionEditor.cs.meta
Assets/OptimalControl/Runtime/Content/Bindings/ObjectiveMetricSource.cs
Assets/OptimalControl/Runtime/Content/Bindings/ObjectiveMetricSource.cs.meta
Assets/OptimalControl/Runtime/Content/Definitions/ExperimentProviderDefinition.cs
Assets/OptimalControl/Runtime/Content/Definitions/ExperimentProviderDefinition.cs.meta
Assets/OptimalControl/Runtime/Content/Definitions/FormulaLayout.cs
Assets/OptimalControl/Runtime/Content/Definitions/FormulaLayout.cs.meta
Assets/OptimalControl/Runtime/Content/Definitions/QuadraticObjectiveDefinition.cs
Assets/OptimalControl/Runtime/Content/Definitions/QuadraticObjectiveDefinition.cs.meta
Assets/OptimalControl/Runtime/Content/Lessons/LessonCapabilityRequirements.cs
Assets/OptimalControl/Runtime/Content/Lessons/LessonCapabilityRequirements.cs.meta
Assets/OptimalControl/Runtime/Core/Optimization.meta
Assets/OptimalControl/Runtime/Core/Optimization/DiagonalQuadraticObjective.cs
Assets/OptimalControl/Runtime/Core/Optimization/DiagonalQuadraticObjective.cs.meta
Assets/OptimalControl/Runtime/Core/Optimization/IObjectiveFunction.cs
Assets/OptimalControl/Runtime/Core/Optimization/IObjectiveFunction.cs.meta
Assets/OptimalControl/Runtime/Simulation/IExperimentSnapshot.cs
Assets/OptimalControl/Runtime/Simulation/IExperimentSnapshot.cs.meta
Assets/OptimalControl/Runtime/Simulation/ObjectiveSession.cs
Assets/OptimalControl/Runtime/Simulation/ObjectiveSession.cs.meta
Assets/OptimalControl/Runtime/UI/IFormulaView.cs
Assets/OptimalControl/Runtime/UI/IFormulaView.cs.meta
Assets/OptimalControl/Runtime/UI/SnapshotParameterPresenter.cs
Assets/OptimalControl/Runtime/UI/SnapshotParameterPresenter.cs.meta
Assets/OptimalControl/Runtime/UI/StructuredFormulaView.cs
Assets/OptimalControl/Runtime/UI/StructuredFormulaView.cs.meta
Assets/OptimalControl/Runtime/Visualization/IExperimentPresentation.cs
Assets/OptimalControl/Runtime/Visualization/IExperimentPresentation.cs.meta
Assets/OptimalControl/Runtime/Visualization/MetricPointVisualization.cs
Assets/OptimalControl/Runtime/Visualization/MetricPointVisualization.cs.meta
Assets/OptimalControl/Tests/EditMode/Formula/ExperimentExtensibilityTests.cs
Assets/OptimalControl/Tests/EditMode/Formula/ExperimentExtensibilityTests.cs.meta
Assets/OptimalControl/Tests/Fixtures/F10.meta
Assets/OptimalControl/Tests/Fixtures/F10/CompactVectorLayout.asset
Assets/OptimalControl/Tests/Fixtures/F10/CompactVectorLayout.asset.meta
Assets/OptimalControl/Tests/Fixtures/F10/ContinuousIntegralLayout.asset
Assets/OptimalControl/Tests/Fixtures/F10/ContinuousIntegralLayout.asset.meta
Assets/OptimalControl/Tests/Fixtures/F10/FixtureChapter.asset
Assets/OptimalControl/Tests/Fixtures/F10/FixtureChapter.asset.meta
Assets/OptimalControl/Tests/Fixtures/F10/FixtureCourse.asset
Assets/OptimalControl/Tests/Fixtures/F10/FixtureCourse.asset.meta
Assets/OptimalControl/Tests/Fixtures/F10/MatrixLayout.asset
Assets/OptimalControl/Tests/Fixtures/F10/MatrixLayout.asset.meta
Assets/OptimalControl/Tests/Fixtures/F10/QuadraticExperiment.asset
Assets/OptimalControl/Tests/Fixtures/F10/QuadraticExperiment.asset.meta
Assets/OptimalControl/Tests/Fixtures/F10/QuadraticFormula.asset
Assets/OptimalControl/Tests/Fixtures/F10/QuadraticFormula.asset.meta
Assets/OptimalControl/Tests/Fixtures/F10/QuadraticLesson.asset
Assets/OptimalControl/Tests/Fixtures/F10/QuadraticLesson.asset.meta
Assets/OptimalControl/Tests/Fixtures/F10/QuadraticProvider.asset
Assets/OptimalControl/Tests/Fixtures/F10/QuadraticProvider.asset.meta
Assets/OptimalControl/Tests/Fixtures/F10/QuadraticScenario.prefab
Assets/OptimalControl/Tests/Fixtures/F10/QuadraticScenario.prefab.meta
Assets/OptimalControl/Tests/PlayMode/ExperimentExtensibilityRuntimeTests.cs
Assets/OptimalControl/Tests/PlayMode/ExperimentExtensibilityRuntimeTests.cs.meta
Docs/REVIEWS/F10_EVIDENCE/EditMode-final.xml
Docs/REVIEWS/F10_EVIDENCE/PlayMode-final.xml
Docs/REVIEWS/F10_EVIDENCE/lesson-cost.png
Docs/REVIEWS/F10_EVIDENCE/lesson-index-terminal.png
Docs/REVIEWS/F10_EVIDENCE/lesson-runtime-results.txt
Docs/REVIEWS/F10_EVIDENCE/lesson-terminal.png
Docs/REVIEWS/F10_EVIDENCE/static-compact-vector.png
Docs/REVIEWS/F10_EVIDENCE/static-edited.png
Docs/REVIEWS/F10_EVIDENCE/static-integral.png
Docs/REVIEWS/F10_EVIDENCE/static-matrix.png
Docs/REVIEWS/F10_EVIDENCE/static-progress-menu.png
Docs/REVIEWS/F10_EVIDENCE/static-runtime-results.txt
Docs/REVIEWS/F10_EVIDENCE/test-summary.json
Docs/REVIEWS/F10_EVIDENCE/verified-application-manifest.json
Docs/REVIEWS/F10_IMPLEMENTATION.md
```

### File modificati

```text
Assets/OptimalControl/Editor/BindingValidator.cs
Assets/OptimalControl/Editor/CatalogValidator.cs
Assets/OptimalControl/Editor/ExperimentHostEditor.cs
Assets/OptimalControl/Runtime/Application/AppBootstrap.cs
Assets/OptimalControl/Runtime/Application/ExperimentHost.cs
Assets/OptimalControl/Runtime/Application/NavigationTransition.cs
Assets/OptimalControl/Runtime/Content/Bindings/FormulaBindings.cs
Assets/OptimalControl/Runtime/Content/Bindings/RuntimeMetricResolver.cs
Assets/OptimalControl/Runtime/Content/Definitions/ExperimentDefinition.cs
Assets/OptimalControl/Runtime/Content/Definitions/FormulaDefinition.cs
Assets/OptimalControl/Runtime/Content/Definitions/LessonParameterDefinition.cs
Assets/OptimalControl/Runtime/Content/Definitions/ModelDefinition.cs
Assets/OptimalControl/Runtime/Course/CourseManager.cs
Assets/OptimalControl/Runtime/Course/LessonProgressBinding.cs
Assets/OptimalControl/Runtime/Lessons/LessonCompletion.cs
Assets/OptimalControl/Runtime/Lessons/LessonConditionRegistry.cs
Assets/OptimalControl/Runtime/Lessons/LessonSession.cs
Assets/OptimalControl/Runtime/Simulation/SessionPublicationException.cs
Assets/OptimalControl/Runtime/Simulation/SimulationConfiguration.cs
Assets/OptimalControl/Runtime/Simulation/SimulationResult.cs
Assets/OptimalControl/Runtime/Simulation/SimulationSession.cs
Assets/OptimalControl/Runtime/UI/FormulaBindingResolver.cs
Assets/OptimalControl/Runtime/UI/FormulaPresenter.cs
Assets/OptimalControl/Runtime/UI/LessonHudPresenter.cs
Assets/OptimalControl/Runtime/UI/LessonStepPresenter.cs
Assets/OptimalControl/Runtime/UI/SessionDiagnosticsPresenter.cs
Assets/OptimalControl/Runtime/Visualization/ExperimentVisualization.cs
Assets/OptimalControl/Runtime/Visualization/HighlightRegistry.cs
Assets/OptimalControl/Runtime/Visualization/HighlightTarget.cs
Assets/OptimalControl/Runtime/Visualization/StateMarker.cs
Assets/OptimalControl/Runtime/Visualization/VisualizationStatus.cs
Assets/OptimalControl/Tests/EditMode/Formula/OptCon.Formula.EditModeTests.asmdef
Assets/OptimalControl/UI/LessonHud.uss
Assets/OptimalControl/UI/LessonHud.uxml
Docs/OPTCON_IMPLEMENTATION_PLAN.md
```

