# F11 — C1.L1 di produzione

Data: 11 ottobre 2026. Perimetro: esclusivamente F11. F12 non implementata.

## Prerequisiti e risultato

Riletti integralmente visione, curriculum, architettura, piano e tutti i report in `Docs/REVIEWS/`. Verificati F08 e F10: slice didattico completo, provider comune dinamico/statico, authoring strutturato, progressione versionata e remediation precedenti. Prima dell'intervento tutti i 416 file applicativi del manifest F10 corrispondevano al workspace. La consegna preesistente conteneva modifiche non committate: l'inventario F11 è calcolato rispetto a una fotografia SHA-256 iniziale, senza attribuire a F11 il lavoro F10.

Il Bootstrap **salvato** espone ora `OptimalControlCourse → Chapter01 → C1_L1`. La voce normale del menu è **Stato, input, output e tempo**, sotto **Chapter 1 — Introduction to Optimal Control**. Non servono cambio di corso nell'Inspector, fixture, helper o apertura manuale del workspace.

| Oggetto | Stable ID | Versione / schema |
| --- | --- | --- |
| CourseDefinition `OptimalControlCourse` | `optimal-control` | `1` / `1` |
| ChapterDefinition `Chapter01` | `C1` | `1` / `1` |
| LessonDefinition `C1_L1` | `C1.L1` | `1` / `1` |
| ExperimentDefinition `PendulumIntroduction` | `c1.introduction.pendulum` | `1` / `1` |

Il nuovo esperimento riusa **PendulumScenario.prefab** e il provider del pendolo, con T=100, δ=0.02 s, m=1 kg, l=1 m, g=9.81 m/s² e preset x₀=[0.35,0], u=0. Non ha costo o riferimento. Il prototipo conserva asset, identità, formule e percorso a sette passaggi; F10 conserva il proprio catalogo fixture. Nessuno dei due appartiene al corso di produzione e nessuno è prerequisito di `C1.L1`. Il progresso già salvato per questi ID non viene promosso.

## Fedeltà alle fonti e disclosure

Fonte matematica: **I**, `Docs/OPTCON-RL_introduction_optimal_control_2026_09_14.pdf`, 36 pagine fisiche. SHA-256 verificato e invariato: `ae169f3492dc0915d0777d62e90184107d7017aee03b8620bbfe0aee9bd3a469`.

| Contenuto | Pagine fisiche / slide | Uso |
| --- | --- | --- |
| Motivazione, tracking, indice di prestazione, limiti; aircraft, robotics, autonomous driving, drones, spacecraft | 2–7 / 1–6 | Contesto e feedback editoriali; nessuna scena decorativa o implementazione di costi/vincoli |
| Stato, input, output e sistema continuo | 8 / 7 | Distinzione f/h e derivata/transizione; τ sostituisce editorialmente il t continuo della slide |
| Dinamica discreta, condizione iniziale | 9 / 8 | x_(t+1)=f_t(x_t,u_t), x₀=x⁰ |
| Orizzonte e vettori impilati | 10 / 9 | T transizioni/input; T+1 stati includendo x₀; stack x della fonte esclude x₀ |
| Forward Euler | 27 / 26 | Adattamento dichiarato dal drone al pendolo |
| Pendolo: x=[θ,ω], coppia e y=θ | 33 / 32 | Modello fisico e funzione di output |

Nel laboratorio **u** indica la coppia; **τ** indica il tempo fisico. La slide del pendolo usa τ per la coppia: questa differenza di notazione è esplicitata nel materiale Euler riusato. Non si identifica t con τ. Le viste dei campioni Euler non sono presentate come soluzione continua esatta. Non si insegna l'errata ωₑ della slide successiva; equilibri e linearizzazione restano F12.

Le formule/termini contengono simboli, significato, forma, unità, binding e fonti; costituiscono anche il glossario necessario a questa lezione, senza un secondo archivio di definizioni. Le equazioni concrete Euler, i parametri e sin(0)=0 sono disponibili **prima** della previsione. Un'etichetta contestuale nel percorso mostra la stessa `FormulaDefinition.Expression` del Formula HUD. Non contiene una seconda implementazione o un mapping matematico nel MonoBehaviour.

## Percorso didattico

Undici passaggi originali; nessuna copia della sequenza F08:

| Passaggio | Attività e verifica | Disclosure |
| --- | --- | --- |
| 1 `context` | Scegliere un dominio e leggere il suo esempio; la scelta è engagement, non risposta giusta/sbagliata | x/u/y; solo corpo |
| 2 `purpose` | Distinguere inseguimento dell'output da indice di prestazione e limiti | Concetti introdotti prima della domanda; nessun sistema di costo |
| 3 `state` | Selezionare x e modificare x₀[1] da 0 a 0.5, mantenendo θ=0.35; riconoscere velocità e posa distinte | Editing x₀, corpo e piano di fase |
| 4 `output` | Selezionare y e riconoscere y=θ, non stato completo o coppia | h esplicita, valore y live e confronto con x |
| 5 `continuous` | Selezionare ẋ e distinguere derivata da prossimo campione | Formula continua; derivata descrittiva N/A, nessun valore numerico inventato |
| 6 `predict` | Ripristinare x₀=[0,0] e prevedere il primo passo con u₀=1 | Equazioni Euler concrete prima della risposta; input non ancora modificabile |
| 7 `intervene` | Selezionare u, modificarlo e verificare che Euler usi la velocità precedente | Input, coppia, ghost; θ₁=0, ω₁=0.02 |
| 8 `transition` | Selezionare next e avanzare; riconoscere il campione già calcolato come nuovo current | Step/seek e piano di fase; nessuna dinamica ricalcolata nel renderer |
| 9 `time` | Avanzare da t=0; selezionare τ; distinguere indice, δ, τ e playback | t=1, δ=0.02 s, τ=0.02 s; index-only spiegato come privo di δ/τ |
| 10 `spaces` | Selezionare x e avanzare; interpretare la terza coordinata come tempo | Corpo, fase (θ,ω), stato-tempo (θ,ω,τ); segmenti come guide fra campioni |
| 11 `horizon` | Seek a T, selezionare u indisponibile e rispondere sul dominio terminale | T/T+1, x_T e y_T presenti; u_T e x_(T+1) assenti |

Risposte errate, selezioni automatiche, editing senza cambiamento e azioni non consentite non danno credito sufficiente. Le condizioni verificano azioni e comprensione attraverso le risposte; non costituiscono un interprete generale delle istruzioni. Ad esempio `initial.changed` rileva una modifica effettiva di x₀, mentre la domanda verifica il ruolo della velocità; non impone numericamente il valore 0.5. La procedura verificata usa il valore richiesto.

La completion richiede tutti i passaggi. Esplorazione libera, sessione incompleta, errore matematico, lesson disposta o guardia di navigazione non producono progresso persistente. Restart ritorna al preset e al primo passaggio; Previous rinnova le condizioni della destinazione. La completion persistente resta distinta dallo stato runtime appena riaperto.

## Contratto matematico minimo aggiunto

Era dichiarata la dimensione dell'output, ma non esisteva una funzione runtime h né il relativo valore nella revisione. Per rendere y verificabile senza UI matematica è stata aggiunta una capability **opzionale**:

`ModelConfiguration.CreateOutputFunction() → IOutputFunction → OutputEvaluation → SimulationResult.Outputs`.

`IOutputFunction` è puro e richiede implementazioni immutabili; dichiara dimensione e necessità del controllo. `StateComponentOutput` è una scelta esplicita del modello, con validazione di stato/input/componente. Il pendolo restituisce la componente θ dello stato; il framework non presume y=x per gli altri provider. Gli asset legacy restano validi e i modelli privi di h pubblicano output indisponibile.

`OutputEvaluation` valuta h una volta per campione durante la costruzione atomica del risultato, conserva la funzione e la stessa history, ed espone una collezione read-only di `NumericVector` immutabili. Non rivaluta la dinamica. Se h richiede input, al terminale non la valuta e non inventa u_T. Se non lo richiede, h_T(x_T,null) è valutabile: il pendolo conserva y_T=θ_T. Un errore di h impedisce la pubblicazione dell'intera nuova revisione; l'ultimo risultato valido conserva configurazione, stati e output.

HUD e visualizzazione consumano la stessa `SessionState/SimulationResult`. Il renderer del corpo continua a rappresentare la posa dallo stato; il Formula HUD legge **output.current** dal risultato. Nessuno legge valori calcolati dal renderer e nessun presenter implementa h o Euler.

### Metriche e binding

| Dato | Tipo runtime / selezione | Unità | Scope / SampleIndex |
| --- | --- | --- | --- |
| x corrente/next | Vector 2×1, componenti esplicite per θ/ω | rad; rad/s | CurrentTimestep / NextTimestep; indice assoluto già risolto |
| u | Vector 1×1; Scalar solo con component=0 esplicita | N·m | CurrentTimestep; N/A a T |
| y | Vector 1×1 nel provider; Scalar component=0 nelle formule | rad | CurrentTimestep; presente anche a T nel pendolo |
| t | Scalar | 1 | CurrentTimestep |
| δ e τ | Scalar, nullable se non esiste tempo fisico | s | CurrentTimestep; N/A index-only |
| T, numero stati, numero input | Scalar da dimensioni effettive della history immutabile | 1 | Snapshot, SampleIndex null |

`RuntimeMetricResolver` rimane l'unica regola di tipizzazione: nessuna conversione implicita Vector1→Scalar. Forma, componente, unità, scope e SampleIndex errati producono diagnostici. I consumer non duplicano selezioni o indici. Gli output trattenuti di A non vengono reinterpretati tramite B o una request fallita.

Le identità semantiche esistenti collegano x/current, input e next alle viste. y resta selezionabile e spiegato nel HUD, con valore verificabile rispetto all'angolo del corpo; non gli viene attribuito un target 3D artificiale o l'intero stato. Il relativo pannello dichiara correttamente che non esiste un highlight dedicato. Nessun renderer specifico è cercato dal FormulaPresenter.

## Integrazione minima e contratti preservati

- `ParameterPanelPresenter` aggiunge campi generici x₀[i], con unità dal modello e comandi `SetInitialState`; nessuna fisica nei callback. `LessonStepPresenter` filtra editing, sezioni e controlli usando i permessi già previsti. I campi leggono la configurazione dell'ultimo risultato valido e sono disabilitati durante errore/chiusura.
- `LessonSession` aggiunge il fatto generico `initial.changed`; la registry aggiunge `answer.selected` per la scelta iniziale di contesto. `LessonPlan` rifiuta tale condizione senza scelte. Nessun ramo per Chapter 1 o ID di lezione nei sistemi runtime.
- Bootstrap cambia soltanto il riferimento serializzato al corso. Menu, CourseManager, composition root, host, session notification, progressione, storage, prefab e registry semantica conservano le responsabilità esistenti. Il testo generico del menu descrive ora la verifica guidata senza chiamare la produzione un'anteprima.
- Le suite storiche selezionano esplicitamente `PrototypeCourse` nel loro isolamento del profilo; le nuove suite F11 disabilitano questa sostituzione e usano il Bootstrap salvato. Nessuna asserzione storica è rimossa o indebolita.
- A01: tempo fisico opzionale, pendolo δ=0.02, index-only privo di δ/τ. A02: resolver centrale tipizzato. A03 e AF08-01/02: Ready dopo binding, dispatch controllato, notification scope, teardown/navigation e generation/lifetime invariati. A04: configurazione e output immutabili per revisione. AF08-03: Enter fallito blocca transizione/completion. AF08-04: dominio input dopo errore invariato. AF08-05/06/07: metadati/provider compositi e presenter generici preservati.
- Core e Simulation continuano a non dipendere da UnityEngine, Content, Application o Presentation; assembly invariati. Nessuna dipendenza esterna.

## Test e verifica nativa

Risultati finali e inventario sono riportati nelle sezioni seguenti e nelle evidenze allegate. La verifica nativa usa una copia isolata del progetto con gli stessi file applicativi, il normale Editor Unity con rendering e un file di progresso isolato. Il corso e il contenuto F11 non vengono sostituiti. Le interazioni UI sono automatizzate tramite gli stessi callback/eventi dei controlli e le catture sono state ispezionate visivamente; non si dichiara uno studio di usabilità con studenti o una verifica in Player build.

| Verifica finale | Risultato |
| --- | --- |
| Unity 6000.3.8f1, compilazione senza helper nativi | **PASS**, exit 0, zero errori/warning C# |
| Tutti gli EditMode | **316/316**, zero falliti, inconclusive o saltati |
| Tutti i PlayMode | **80/80**, zero falliti, inconclusive o saltati |
| Confronto nomi dei casi F10 | Tutti i **296+74** presenti e passanti, nessun caso rimosso |
| Nuovi casi F11 | **20 EditMode + 6 PlayMode** |
| Catalogo/contratti/fonti reali | **PASS**, inclusi riferimenti, hash e range pagine |
| Bootstrap normale e percorso completo C1.L1 | **PASS**, senza sostituzione del corso |
| Menu/riapertura/restart Play e nuovo processo Unity | **PASS**, ID/versioni persistenti corretti, runtime fresco |
| Prototipo pendolo F08/F10 | **PASS** nativo: sette passaggi, costo, A/B, terminale, progresso |
| Index-only e failure path precedenti | **PASS** nativo: x₀=2,u₀=3→x₁=5; niente δ/τ; request fallita T=1 da cursor=50, input bloccato e Reset; NonRepresentable ed Enter fallito |
| Obiettivo statico F10 | **PASS** nativo: valori 9→12→20, layout, snapshot senza tempo, navigazione e progressi separati |
| Identità applicazione/copia verificata | **446/446 file**, zero mismatch SHA-256 |

I 20 nuovi EditMode coprono grafo di produzione e Bootstrap serializzato, catalogo e binding con verifica PDF, disclosure prima della previsione, Euler deterministico, stessa posa con velocità diversa, output Vector1/componenti/tipo/unità/scope/SampleIndex/terminale, conteggi Snapshot, conservazione A/B/failure, output con/senza controllo su modello index-only, errori di h atomici e recupero, dimensioni/componenti, risposte errate/no-op/permessi, completion e isolamento degli ID, guardia di navigazione, Previous/Restart/Dispose e collezioni read-only senza ricostruzione durante il seek.

I 6 nuovi PlayMode usano il **Bootstrap salvato**, selezionano dal normale menu, verificano identità e stessa revisione HUD/3D, editing x₀, output y, previsione supportata dalle equazioni, risposte errate bloccate, percorso UI intero, terminale, persistenza/riapertura/restart, errore matematico e recupero, sessione incompleta/esplorazione libera senza completion.

Evidenze consegnate in [F11_EVIDENCE](F11_EVIDENCE/TEST_SUMMARY.txt): XML completi [EditMode](F11_EVIDENCE/EditMode-final.xml) e [PlayMode](F11_EVIDENCE/PlayMode-final.xml), quattro resoconti nativi, manifest e inventario. Acquisite 20 Game View del percorso/recupero e una nel processo successivo; 17 catture rappresentative conservate nel progetto. Fra quelle ispezionate: [menu](F11_EVIDENCE/production-menu.png), [editing dello stato](F11_EVIDENCE/production-state-editing.png), [output live](F11_EVIDENCE/production-output-live.png), [formula prima della previsione](F11_EVIDENCE/production-prediction.png), [tempo live](F11_EVIDENCE/production-time-live.png), [spazi](F11_EVIDENCE/production-spaces.png), [terminale](F11_EVIDENCE/production-terminal-detail.png), [progresso nel menu](F11_EVIDENCE/production-progress.png) e [riavvio del processo](F11_EVIDENCE/production-process-reloaded.png).

Gli helper di automazione sono rimasti nella copia temporanea; sono stati rimossi prima di suite e compilazione finali. Non sono funzionalità o strumenti necessari per aprire la lesson. Non risultano regressioni. I PDF, le specifiche visione/curriculum/architettura, i report precedenti, gli asset del prototipo/F10, i prefab, i package e i ProjectSettings sono invariati rispetto all'inizio di F11.

## F11 PRODUCTION LESSON GATE

| Domanda richiesta | Esito e prova |
| --- | --- |
| 1. Raggiungibile dal Bootstrap normale senza fixture? | **SÌ**. Course serializzato `optimal-control`; voce `C1.L1` nel menu, test e prova nativa senza content injection. |
| 2. Produzione, non rename del prototipo? | **SÌ**. Course/chapter/lesson/experiment indipendenti; undici passaggi originali, senza costo; prototipo a sette passaggi invariato. |
| 3. Ogni domanda ha le informazioni prima? | **SÌ**. Istruzioni, esempio/feedback e formule precedono le domande; risposte errate provate e bloccate. |
| 4. Formule necessarie visibili quando servono? | **SÌ**. Espressione editoriale accanto alla domanda, HUD strutturato e termini selezionabili; Euler concreto visibile prima della previsione. |
| 5. x/u/y distinti correttamente? | **SÌ**. x Vector2 misto rad/rad/s, u coppia, y=h(x,u)=θ Vector1 selezionato esplicitamente; posa insufficiente; output terminale secondo dipendenza dall'input. |
| 6. Completion persistente all'ID corretto? | **SÌ**. File e reload contengono `optimal-control/C1.L1`, versione/schema 1; sessioni riaperte non ereditano lo stato runtime completed. |
| 7. Prototype e Fixture F10 separati? | **SÌ**. Non appartengono al corso di produzione, non sbloccano/completano C1.L1; regressioni native e progressi isolati verificati. |
| 8. Regressioni F00–F10 passanti? | **SÌ**. Tutti i 296+74 casi precedenti conservati e passanti; suite finali 316+80, smoke nativi dinamico/statico/index-only e failure path passanti. |

**F11 completata. READY FOR F12.** Nessuna implementazione F12 eseguita.

## Procedura manuale in Unity

1. Aprire **Assets/OptimalControl/Scenes/Bootstrap.unity** e premere **Play**. Nel menu normale scegliere **Stato, input, output e tempo**, sotto **Chapter 1 — Introduction to Optimal Control**. Nessun cambio nell'Inspector.
2. Scegliere un contesto; leggere il feedback. In `purpose` una risposta sbagliata lascia Avanti disabilitato; scegliere l'opzione che comprende indice di prestazione e limiti.
3. In `state`, scorrere a **MATEMATICA LIVE** e cliccare x_t: la selezione iniziale non vale come azione. Aprire **Input e parametri**, porre **x₀[1]=0.5** lasciando **x₀[0]=0.35**, confermare il campo con Enter/focus perso. Il corpo mantiene l'angolo, il punto di fase cambia. Rispondere che la velocità distingue gli stati.
4. In `output` selezionare y_t nel HUD: **y=0.35 rad**, **x=[0.35 rad;0.5 rad/s]**. Rispondere che y contiene θ, non ω o coppia. Il valore proviene dal risultato, non dalla posa del GameObject.
5. In `continuous` selezionare ẋ e rispondere “variazione istantanea per unità di tempo”. Il termine è descrittivo e non finge una derivata numerica.
6. In `predict` il preset è x₀=[0,0]. Leggere Euler sopra la domanda; scegliere **θ₁=0, ω₁=0.02** per u₀=1. L'input è ancora disabilitato. In `intervene` selezionare u, aprire **Input e parametri**, impostare **u[0]=1 N·m**; il menu **Modifica** distingue singola transizione da coda. Confermare e osservare θ next=0 e ω next=0.02; scegliere “velocità vecchia”.
7. In `transition` selezionare next e premere **Step →**: lo stato previsto diventa current. Rispondere che il campione era già salvato.
8. In `time` premere Step da 0 a 1 e selezionare τ. Il HUD mostra **t=1, δ=0.02 s, τ=0.02 s**. Il playback in campioni/s cambia solo osservazione, non δ o risultato. Per la domanda t=20 scegliere **τ=0.4 s**.
9. In `spaces` selezionare x e premere Step. Le etichette distinguono corpo XY, piano (θ,ω) e curva (θ,ω,τ), terza coordinata temporale. Rispondere “tempo fisico”.
10. In `horizon` portare t a **100** con slider/campo numerico; selezionare u. Mostrare **101 stati, 100 input**, x_T e y_T; u_T e next sono **N/A**. La risposta errata non abilita Concludi. Scegliere l'opzione corretta e **Concludi verifica**.
11. **Torna al corso**: compare **Completamento salvato**. Riaprire: progresso conservato, nuova lesson runtime dal primo passo. Uscire/rientrare in Play mantiene il progresso su file. **Ricomincia** ripristina preset/condizioni e non duplica la completion; **Esplorazione libera** non completa la lezione.

I controlli inferiori richiedono lo scroll del pannello esistente. Per ripetere un profilo privo di progresso usare un profilo isolato; non occorre cancellare i progressi personali per verificare sessione o domande. Il backend normale resta `Application.persistentDataPath/OptCon/progress-v1.json`.

## Limiti e deviazioni

La capability opzionale di output e l'editor generico x₀ sono estensioni minime ai layer precedenti necessarie a rendere x/u/y verificabili; sono testate e non nascoste nella UI. Non è emersa una lacuna che richiedesse un refactor generale. Nessun nuovo scenario, solver continuo, funzione di costo, constraint runtime, equilibrio, Jacobiano, ottimizzazione o contenuto F12 è stato implementato.

Il corpo mostra la posa; il piano di fase mostra entrambe le componenti. y non ha un renderer/highlight dedicato, non necessario per questa lezione. La continuità è concettuale e dichiarata, mentre il laboratorio resta Euler discreto. I domini motivanti sono esempi testuali collegati alle domande. Il pannello conserva lo scroll e il layout esistente; polish, adattamento a schermi piccoli, Player build, profiling e usability study restano alle fasi previste. Nessun picker runtime per altri corsi è stato aggiunto.

## Inventario completo F11

Rispetto alla fotografia iniziale: **57 file creati, 15 modificati, 0 cancellati**. Inclusi .meta e prove in Docs; nessun file di test helper temporaneo consegnato. Elenco machine-readable: [file-inventory.json](F11_EVIDENCE/file-inventory.json).

### File creati

```text
Assets/OptimalControl/Data/Chapters/Chapter01.asset
Assets/OptimalControl/Data/Chapters/Chapter01.asset.meta
Assets/OptimalControl/Data/Courses/OptimalControlCourse.asset
Assets/OptimalControl/Data/Courses/OptimalControlCourse.asset.meta
Assets/OptimalControl/Data/Experiments/PendulumIntroduction.asset
Assets/OptimalControl/Data/Experiments/PendulumIntroduction.asset.meta
Assets/OptimalControl/Data/Formulas/Chapter01.meta
Assets/OptimalControl/Data/Formulas/Chapter01/ContinuousSystem.asset
Assets/OptimalControl/Data/Formulas/Chapter01/ContinuousSystem.asset.meta
Assets/OptimalControl/Data/Formulas/Chapter01/DiscreteSystem.asset
Assets/OptimalControl/Data/Formulas/Chapter01/DiscreteSystem.asset.meta
Assets/OptimalControl/Data/Formulas/Chapter01/FiniteHorizon.asset
Assets/OptimalControl/Data/Formulas/Chapter01/FiniteHorizon.asset.meta
Assets/OptimalControl/Data/Formulas/Chapter01/PendulumOutput.asset
Assets/OptimalControl/Data/Formulas/Chapter01/PendulumOutput.asset.meta
Assets/OptimalControl/Data/Formulas/Chapter01/SampleTime.asset
Assets/OptimalControl/Data/Formulas/Chapter01/SampleTime.asset.meta
Assets/OptimalControl/Data/Formulas/Chapter01/StateInputOutput.asset
Assets/OptimalControl/Data/Formulas/Chapter01/StateInputOutput.asset.meta
Assets/OptimalControl/Data/Lessons/Chapter01.meta
Assets/OptimalControl/Data/Lessons/Chapter01/C1_L1.asset
Assets/OptimalControl/Data/Lessons/Chapter01/C1_L1.asset.meta
Assets/OptimalControl/Runtime/Core/Contracts/IOutputFunction.cs
Assets/OptimalControl/Runtime/Core/Contracts/IOutputFunction.cs.meta
Assets/OptimalControl/Runtime/Simulation/OutputEvaluation.cs
Assets/OptimalControl/Runtime/Simulation/OutputEvaluation.cs.meta
Assets/OptimalControl/Tests/EditMode/Formula/ProductionIntroductionTests.cs
Assets/OptimalControl/Tests/EditMode/Formula/ProductionIntroductionTests.cs.meta
Assets/OptimalControl/Tests/PlayMode/ProductionIntroductionRuntimeTests.cs
Assets/OptimalControl/Tests/PlayMode/ProductionIntroductionRuntimeTests.cs.meta
Docs/REVIEWS/F11_EVIDENCE/EditMode-final.xml
Docs/REVIEWS/F11_EVIDENCE/PlayMode-final.xml
Docs/REVIEWS/F11_EVIDENCE/TEST_SUMMARY.txt
Docs/REVIEWS/F11_EVIDENCE/application-manifest.json
Docs/REVIEWS/F11_EVIDENCE/file-inventory.json
Docs/REVIEWS/F11_EVIDENCE/production-completed.png
Docs/REVIEWS/F11_EVIDENCE/production-context.png
Docs/REVIEWS/F11_EVIDENCE/production-continuous.png
Docs/REVIEWS/F11_EVIDENCE/production-input.png
Docs/REVIEWS/F11_EVIDENCE/production-menu.png
Docs/REVIEWS/F11_EVIDENCE/production-nonrepresentable.png
Docs/REVIEWS/F11_EVIDENCE/production-output-live.png
Docs/REVIEWS/F11_EVIDENCE/production-prediction.png
Docs/REVIEWS/F11_EVIDENCE/production-process-reload-results.txt
Docs/REVIEWS/F11_EVIDENCE/production-process-reloaded.png
Docs/REVIEWS/F11_EVIDENCE/production-progress.png
Docs/REVIEWS/F11_EVIDENCE/production-recovered.png
Docs/REVIEWS/F11_EVIDENCE/production-runtime-results.txt
Docs/REVIEWS/F11_EVIDENCE/production-spaces.png
Docs/REVIEWS/F11_EVIDENCE/production-state-editing.png
Docs/REVIEWS/F11_EVIDENCE/production-terminal-detail.png
Docs/REVIEWS/F11_EVIDENCE/production-terminal.png
Docs/REVIEWS/F11_EVIDENCE/production-time-live.png
Docs/REVIEWS/F11_EVIDENCE/production-transition.png
Docs/REVIEWS/F11_EVIDENCE/regression-pendulum-results.txt
Docs/REVIEWS/F11_EVIDENCE/regression-static-results.txt
Docs/REVIEWS/F11_IMPLEMENTATION.md
```

### File modificati

```text
Assets/OptimalControl/Runtime/Content/Bindings/RuntimeMetricResolver.cs
Assets/OptimalControl/Runtime/Content/Lessons/LessonPlan.cs
Assets/OptimalControl/Runtime/Core/Contracts/ModelConfiguration.cs
Assets/OptimalControl/Runtime/Core/Models/PendulumConfiguration.cs
Assets/OptimalControl/Runtime/Lessons/LessonConditionRegistry.cs
Assets/OptimalControl/Runtime/Lessons/LessonSession.cs
Assets/OptimalControl/Runtime/Simulation/SimulationResult.cs
Assets/OptimalControl/Runtime/UI/FormulaPresenter.cs
Assets/OptimalControl/Runtime/UI/LessonStepPresenter.cs
Assets/OptimalControl/Runtime/UI/ParameterPanelPresenter.cs
Assets/OptimalControl/Scenes/Bootstrap.unity
Assets/OptimalControl/Tests/PlayMode/ProgressStoreIsolation.cs
Assets/OptimalControl/UI/CourseMenu.uxml
Assets/OptimalControl/UI/LessonHud.uxml
Docs/OPTCON_IMPLEMENTATION_PLAN.md
```
