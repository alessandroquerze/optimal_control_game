# F04 — Sessione runtime, lifecycle e risultati immutabili

Data: 9 ottobre 2026. **F04 completata e verificata. A03 e A04 risolti. F05 non avviata.**

Letti integralmente visione, curriculum, architettura, piano operativo, mini-audit F00–F03 e remediation A01. Specifica operativa applicata: F04 del piano. A01 è preservato e rieseguito nei test; A02 rimane aperto per la fase prevista prima di Formula/HUD.

## Architettura introdotta

```text
ExperimentDefinition + ModelDefinition
    → SimulationConfiguration immutabile, senza asset Unity
    → SimulationSession / ICommandSink
    → SimulationResult { Configuration, History }
    → SessionState { Result, Sample, tentativo, errore, playback }
    → consumer diagnostico

AppBootstrap → CourseManager / LessonLoader
             → ExperimentHost → PlaybackController
```

- `ModelConfiguration` definisce il contratto puro di configurazione matematica immutabile. `PendulumConfiguration` conserva parametri SI immutabili e passo Euler, espone valori/unità tramite descrittori puri e crea il modello esistente. Nessuna seconda implementazione delle equazioni.
- `ModelDefinition` converte l'authoring in configurazione/sessione. `PendulumModelDefinition` richiede il passo fisico e i tre parametri del proprio modello, senza conoscere chapter o lesson ID. Dimensioni, unità, limiti editoriali e invarianti matematiche vengono verificati.
- `SimulationConfiguration` congela modello, x0, input, riferimento opzionale e identità dell'esperimento. Vettori e collezioni pubbliche sono immutabili; gli asset non sono conservati nel risultato.
- `SimulationSession` è l'unico proprietario di configurazione richiesta, ultimo risultato valido, revisione/tentativo, cursore e playback. `ICommandSink` e `SimulationCommand` sono puri. Sono disponibili play/pause, step avanti/indietro, seek, reset, modifica input singolo/coda, stato iniziale, parametro, passo fisico e rate di playback.
- `ExperimentHost` possiede il lifetime della sessione. `PlaybackController.Update` passa `Time.unscaledDeltaTime` al clock di presentazione, senza usare FixedUpdate, fisica Unity o δ come cadenza di riproduzione.
- `SessionDiagnosticsPresenter` mostra soltanto revisione, t/T, δ/τ, play/pause ed eventuale tentativo fallito. Legge una pubblicazione coerente, senza calcolare la dinamica. `ExperimentHostEditor` permette di inviare gli stessi comandi dall'Inspector durante Play; non è il pannello dello studente.

Le dipendenze restano acicliche: Core senza Unity; Simulation → Core, senza Unity; Content → Core/Simulation; Presentation → Content/Simulation; Application → Content/Simulation/Presentation/Core. Editor e test rimangono separati. Nuovi provider estendono il contratto tramite classi/configurazioni proprie; navigazione, sessione e diagnostica non contengono switch sui capitoli.

### Ricalcolo, reset e tempo

`RolloutEngine.RecalculateFrom` valida il confine e gli input del prefisso. Per una modifica di input a modello/x0 invariati, la sessione riusa il prefisso verificato e ricalcola dal primo input cambiato. Un cambio di configurazione matematica ricalcola l'intero rollout. Il modello produce un risultato completo prima della sostituzione atomica e della singola notifica della nuova revisione.

Seek e rewind leggono campioni già salvati. Il clock accumula secondi di presentazione e avanza a un rate dichiarato in campioni/secondo; cambia il cursore, non i risultati. La tolleranza `1e-9` riguarda soltanto l'arrotondamento del clock di presentazione. Al terminale si ferma in pausa, senza inventare input o stato successivo.

Reset ripristina la configurazione immutabile catturata all'apertura, riporta t a zero e mette in pausa. Non rilegge asset eventualmente modificati nel frattempo. Il rate di presentazione resta distinto dal preset matematico.

## A03 — Risoluzione del lifecycle/readiness

La navigazione risolve la destinazione e crea un `NavigationTransition` con ID progressivo, lesson ID catturato, riferimento alla destinazione e percorso scena. L'identità accompagna ogni `NavigationUpdate`, anche quando due lesson condividono lo stesso workspace.

| Fase pubblicata | Garanzia |
| --- | --- |
| `Requested` | Richiesta accettata dopo preflight; transizione protetta da nuove richieste. Il vecchio runtime viene rilasciato. |
| `SceneLoaded` | Scena disponibile e attiva; non significa sessione pronta. Il payload non espone ancora una sessione. |
| `Composing` | Il bootstrap crea configurazione, sessione e istanza dello scenario della destinazione. |
| `Binding` | Identità attiva e sessione sono assegnate; il bootstrap collega esplicitamente i consumer. `IsReady` è ancora false. |
| `Ready` | Composizione e binding terminati. Una lesson ha sessione e consumer disponibili; il menu è pronto con sessione assente. |
| `Failed` | Caricamento/composizione non riusciti; nessuna sessione dichiarata pronta, errore esplicito e navigazione di recupero disponibile nella scena caricata. |

`SceneReady` del vecchio loader è stato rimosso: il loader gestisce soltanto le scene e restituisce la scena nel callback. Il binding è una chiamata diretta della composizione, non una subscription a un evento di navigazione. `ICourseNavigation.IsReady` distingue esplicitamente disponibilità e caricamento; `ActiveLesson` da sola non è una garanzia di readiness.

Il coordinatore protegge anche il dispatch finale con un guard separato dal flag di caricamento: durante `Ready`, `IsTransitioning` è false ma un observer non può avviare una transizione annidata. Il loader protegge a sua volta il callback finale. Dopo il ritorno dal dispatch la navigazione è nuovamente accettata, senza attendere frame.

Il cambio tra due lesson nello stesso workspace ricompone immediatamente host/sessione e aggiorna i consumer con la nuova destinazione. Non attende un nuovo evento nativo `sceneLoaded`. Alla chiusura vengono staccate le subscription diagnostiche, fermato il clock, chiusa la sessione e distrutto lo scenario; dispose elimina i listener della sessione. Il bootstrap persistente resta unico.

**A03 risolto**, verificato con eventi e binding reali in PlayMode, compresi cambio di identità nello stesso workspace, richieste rientranti e chiusura/riapertura.

## A04 — Configurazione appartenente al risultato

`SimulationResult` conserva insieme la configurazione pura e immutabile e la storia che essa ha prodotto. `SessionState.Sample` viene costruito dalla storia di quel risultato. Un consumer deve interpretare il campione tramite **`state.Result.Configuration`**, non tramite i default editoriali o `RequestedConfiguration`.

La configurazione del pendolo conserva massa, lunghezza, gravità, passo, dimensioni/unità; quella dell'esperimento conserva x0, sequenza input, orizzonte e riferimento. Conservare un risultato A mantiene questi oggetti immutabili e i campioni di A. Modificare un parametro crea una nuova configurazione e un risultato B; nessun consumer deve mantenere una propria copia dei parametri.

Un errore di validazione o rollout non sostituisce il risultato valido. La pubblicazione mantiene `Result` con la sua configurazione e storia, e identifica separatamente tentativo, errore e configurazione richiesta. I tentativi falliti non riutilizzano l'identità della revisione valida. Il contratto resta puro: nessun GameObject, asset, presenter o parametro letto tardivamente.

**A04 risolto**, verificato conservando A, generando B con un parametro diverso e provocando poi un overflow: configurazione e campioni del risultato valido rimangono associati e immutati.

## Verifiche e risultati

Unity **6000.3.8f1**, esecuzioni native su copia isolata con Packages/ProjectSettings iniziali del workspace. L'Editor dell'utente non è stato chiuso o usato per le prove. I verificatori temporanei sono stati rimossi prima della compilazione finale.

| Verifica | Risultato |
| --- | --- |
| Compilazione finale, senza verificatori temporanei | **PASS**, exit code 0; nessun errore o warning C# nel log finale. |
| Tutti gli EditMode | **115/115 PASS**, zero saltati/falliti: 60 CatalogValidatorTests, 26 PendulumDynamicsTests, 13 RolloutEngineTests, 16 SimulationSessionTests. |
| Tutti i PlayMode | **15/15 PASS**, zero saltati/falliti: nove regressioni F02 e sei nuovi test lifecycle/sessione. |
| Prova nativa F04 | Bootstrap → menu → workspace → input/parametro/step/play/pause/seek → menu → riapertura → reset: **PASS**, zero errori runtime. |
| Regressione nativa F00–F02 | **3/3 cicli PASS**, due con domain reload e uno senza. Una root persistente, scena contenuto, camera e presenter; pavimento, luce, Volume e URP presenti; nessuno script mancante. |
| Verifica visiva | Game View ispezionata: diagnostica `r2`, `t=2/100`, `δ=0.02 s`, `τ=0.04 s`, pausa; testi della lesson dagli asset e workspace conservato. |
| A01 / pendolo | Tutti i casi precedenti passano; il provider aggiunge sei casi di rifiuto di passo assente, zero, negativo, NaN e ±∞. Preset e 101 campioni del pendolo confrontati con il modello precedente. |
| Index-only | La nuova sessione scalare produce `2+3=5`, T=1, δ/τ null a t=0 e t=1, terminale senza input/next. Catalogo e serializzazione dei casi A01 restano validi. |
| Identità del codice/asset verificati | **179 file di Assets/OptimalControl, zero differenze SHA-256** tra copia verificata e workspace. |
| Perimetro | Nessuna modifica a A01/mini-audit, altre specifiche, PDF, pacchetti, ProjectSettings, URP, SampleScene, formule/binding e metriche A02. Nessun file preesistente eliminato. |

La verifica del lifecycle nell'Editor è stata automatizzata, seguita dall'ispezione manuale della cattura; non si dichiara una sessione di click manuali nell'Editor dell'utente. L'Inspector diagnostico consegnato rende il percorso ripetibile manualmente come descritto sotto.

L'importazione iniziale ha evidenziato un'asserzione NUnit con overload errato, corretta prima delle suite riuscite. Un conflitto di nomi nello strumento esterno di cattura è stato corretto solo nello strumento. La copia usa l'accesso al servizio licenze locale e l'indicizzazione Search all'avvio disabilitata nei propri UserSettings, come nella verifica A01; nessuna modifica delle preferenze del workspace. L'aggiornamento automatico Unity dell'asset URP mobile nella copia non è stato trasferito.

Evidenze locali: `C:/Users/querz/AppData/Local/Temp/OptCon-F04-3ef1fc98d7284d97a1a460599d552509/`. File principali: `EditMode-results.xml`, `PlayMode-results.xml`, `editmode.log`, `playmode.log`, `final-compile.log`, `session-results.txt`, `session-verification-2.log`, `session-preview.png`, `smoke-results.txt`, `regression-smoke.log`, `baseline.json`, `verified-source-manifest.json`, `change-inventory.json`. Conservati anche gli strumenti esterni e i log iniziali.

### Nuovi test mirati

`SimulationSessionTests` verifica: caso index-only, ricalcolo del solo suffisso con conteggio di Evaluate, input singolo/coda, singola pubblicazione per revisione, rewind senza integrazione, reset, configurazioni/campioni A/B, fallimento numerico e recupero, clock a 30/60/144 aggiornamenti, rate invalidi, limiti editoriali/invarianti fisiche, dispose e rientranza del command sink.

I sei nuovi PlayMode sono:

- `ReadyFollowsCompositionAndBindingAndRejectsReentrantObservers`;
- `TwoLessonsSharingWorkspaceHaveDistinctDestinationsAndSessions`;
- `CloseReopenDisposesSessionsAndDiagnosticSubscriptions`;
- `PlaybackAdapterUsesPresentationSecondsAtDifferentFrequenciesAndStopsAtTerminal`;
- `RuntimeOverridesAndResetNeverWriteAuthoredAssets`;
- `CompositionFailureNeverPublishesReadyAndLeavesReturnToMenuAvailable`.

La suite catalogo aggiunge `ConcreteProviderRejectsMissingOrInvalidPhysicalStep` con sei varianti; il test del catalogo originale ora verifica anche la configurazione eseguibile valida, dato che provider e scenario sono assegnati. I controlli sugli assembly ammettono la dipendenza Content → Simulation prevista per F04, mantenendo vietati i riferimenti inversi e quelli verso Application/Presentation.

## Inventario completo

**34 file creati:** 13 sorgenti C#, due asset, i loro 15 `.meta`, tre `.meta` di cartelle e questo rapporto. Per i seguenti 15 file applicativi è stato creato anche il corrispondente file con suffisso `.meta`; tutti i percorsi sono relativi ad `Assets/OptimalControl/`.

```text
Runtime/Core/Contracts/ModelConfiguration.cs
Runtime/Core/Models/PendulumConfiguration.cs
Runtime/Simulation/SimulationCommands.cs
Runtime/Simulation/SimulationConfiguration.cs
Runtime/Simulation/SimulationResult.cs
Runtime/Simulation/SimulationSession.cs
Runtime/Content/Models/PendulumModelDefinition.cs
Runtime/Application/ExperimentHost.cs
Runtime/Application/NavigationTransition.cs
Runtime/Application/PlaybackController.cs
Runtime/UI/SessionDiagnosticsPresenter.cs
Editor/ExperimentHostEditor.cs
Tests/EditMode/Mathematics/SimulationSessionTests.cs
Data/Models/Pendulum.asset
Prefabs/SimulationScenario.prefab
```

Altri file creati: `Assets/OptimalControl/Runtime/Content/Models.meta`, `Assets/OptimalControl/Data/Models.meta`, `Assets/OptimalControl/Prefabs.meta`, `Docs/REVIEWS/F04_IMPLEMENTATION.md`.

**18 file modificati**, relativi alla root del progetto:

```text
Assets/OptimalControl/Runtime/Content/Definitions/ModelDefinition.cs
Assets/OptimalControl/Runtime/Content/Navigation/ICourseNavigation.cs
Assets/OptimalControl/Runtime/Content/OptCon.Content.asmdef
Assets/OptimalControl/Runtime/Course/CourseManager.cs
Assets/OptimalControl/Runtime/Application/AppBootstrap.cs
Assets/OptimalControl/Runtime/Application/LessonLoader.cs
Assets/OptimalControl/Runtime/Application/OptCon.Application.asmdef
Assets/OptimalControl/Runtime/Simulation/RolloutEngine.cs
Assets/OptimalControl/Runtime/UI/OptCon.Presentation.asmdef
Assets/OptimalControl/Editor/CatalogValidator.cs
Assets/OptimalControl/Editor/OptCon.Editor.asmdef
Assets/OptimalControl/Tests/EditMode/CatalogValidatorTests.cs
Assets/OptimalControl/Tests/PlayMode/CourseNavigationTests.cs
Assets/OptimalControl/Tests/PlayMode/OptCon.Navigation.PlayModeTests.asmdef
Assets/OptimalControl/Data/Experiments/PendulumPrototype.asset
Assets/OptimalControl/Scenes/LessonWorkspace.unity
Assets/OptimalControl/UI/CourseMenu.uxml
Docs/OPTCON_IMPLEMENTATION_PLAN.md
```

Il preset conserva ID, GUID, fonti, dimensioni/unità, T, x0, input, target e parametri fisici. Sono assegnati provider/scenario e aggiornata la descrizione; il salvataggio nativo rende esplicito `hasPhysicalTimeStep: 1` senza modificarne il significato legacy. Il workspace aggiunge il solo consumer diagnostico. Gli altri asset editoriali e tutti i `.meta` preesistenti restano identici alla baseline.

## Deviazioni, compatibilità e problemi aperti

Nessuna deviazione funzionale da F04. La configurazione pura, il wrapper risultato e le fasi di navigazione sono necessari per A03/A04. Il prefab generico `SimulationScenario` contiene solo host/clock: il pendolo visibile e il suo scenario 3D rimangono F05. L'Inspector editor è lo strumento di verifica dei comandi; non introduce il futuro router di input o Formula/HUD.

Le API di navigazione cambiano intenzionalmente: `ICourseNavigation` aggiunge `IsReady`, il callback del loader include la scena e l'evento ambiguo `SceneReady` è sostituito dal lifecycle del coordinatore. `ModelDefinition` aggiunge il contratto di creazione concreto. Tutti i consumer presenti e le fixture sono aggiornati. Nessuna modifica al contratto opzionale di A01 o alle equazioni del pendolo.

**A02 rimane aperto**, da risolvere prima del Formula/HUD come previsto. Esperimenti statici, costi, metriche aggiuntive, confronto A/B come funzionalità e Lesson Logic restano nelle rispettive fasi; il test che conserva due risultati non introduce un sistema di confronto utente. I provider devono rispettare il contratto di configurazione immutabile e dinamica deterministica per il riuso del prefisso. Nessun problema bloccante emerso per F04.

## Procedura manuale in Unity

1. Aprire `Assets/OptimalControl/Scenes/Bootstrap.unity`, attendere compilazione e avviare Play. Aprire la lesson del prototipo. Deve comparire il workspace con l'indicatore `r0`, `t=0/100`, δ=0.02 e τ=0; nella Hierarchy c'è un solo scenario con `ExperimentHost` e `PlaybackController`.
2. Selezionare `SimulationScenario` nella Hierarchy. Nell'Inspector usare Play/Pause, Step +/−, Seek e Reset. A t=100 input/next sono assenti nel campione; Play si arresta al terminale. Cambiare il rate in campioni/secondo cambia la riproduzione, mantenendo δ=0.02.
3. In pausa, selezionare `Input sample=2`, modificare u[0] e scegliere Single oppure Tail. La revisione aumenta una volta per ogni modifica, preservando il prefisso. La diagnostica runtime segue la stessa revisione. Modificare `model.mass` entro 0.1–10: nuova configurazione/rollout; un valore fuori range viene rifiutato e il risultato valido resta disponibile con errore del tentativo.
4. Usare Reset: t=0, pausa, x0=[0.35,0], input nulli e parametri originali. Verificare che l'asset `PendulumPrototype` non sia stato modificato dagli override.
5. Tornare al corso e riaprire: nuova sessione r0 e preset originale, nessuna duplicazione di scenario o listener. Per due lesson con lo stesso workspace usare un catalogo di test duplicato oppure eseguire il test PlayMode dedicato, che verifica il cambio senza frame di attesa.
6. Selezionare `PrototypeCourse.asset` e validare Catalog ed Execution Configuration: entrambi validi. Eseguire tutte le suite dal Test Runner: 115 EditMode e 15 PlayMode. I casi A03/A04 e index-only sono identificati sopra.

Lo stato F04 nel piano è aggiornato a completata soltanto dopo queste verifiche riuscite. **F05 non iniziata.**
