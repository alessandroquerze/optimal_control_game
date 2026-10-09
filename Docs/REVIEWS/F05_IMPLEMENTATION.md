# F05 — Primitive riutilizzabili e dinamica manipolabile

Data: 9 ottobre 2026. **F05 completata e verificata. F06 non iniziata.**

Letti integralmente visione, curriculum, architettura, piano operativo, mini-audit F00–F03 e remediation A01; esaminati codice/asset delle fasi completate e rapporto F04. Il piano è la specifica operativa principale.

## Prerequisiti verificati prima dell'integrazione

F04 risulta completata nel piano. Le evidenze native precedenti confermano 115 EditMode e 15 PlayMode passati. **I 179 file applicativi del manifest F04 coincidono con la baseline precedente a F05**, senza differenze SHA-256. Sono disponibili provider concreto del pendolo, sessione pura, command sink, storia immutabile, configurazione associata al risultato e lifecycle con binding prima di Ready. Le suite finali rieseguono anche questi contratti.

A01 non è stato ridisegnato. A03/A04 non sono riaperti: il nuovo host collega i consumer visuali e il router prima di restituire la sessione alla barriera Ready esistente; legge la configurazione del risultato, conservando le garanzie di F04. **A02 resta aperto per l'inizio di F06**, prima del resolver Formula/HUD.

## Sistemi e provenienza dei dati

```text
Core / modello matematico invariato
    → SimulationSession
    → SessionState { Result.Configuration, Result.History, Sample }
    → ExperimentVisualization
         → StateTrajectoryView / StateProjection / TrajectoryRenderer
         → StateMarker e HighlightTarget
         → AngularStateView / AngularBodyView
         → CurvedTorqueRenderer

InputActionAsset → LessonInputRouter → comandi puri della sessione
                                  → LayerController, per i soli toggle visivi
```

| Componente | Dati ricevuti | Responsabilità |
| --- | --- | --- |
| `StateProjection` | Storia salvata, indice, assi configurati | Proiezione esplicita di componenti dello stato, tempo fisico, indice o piano costante; unità/scale e componenti omesse dichiarate. Conversione double→float soltanto al confine visivo. |
| `TrajectoryRenderer` | Storia e proiezione | Una posizione per ciascuno dei T+1 campioni, nell'ordine degli indici. Conserva il riferimento alla storia; il buffer float del LineRenderer è solo geometria di rendering. |
| `StateTrajectoryView` | Un'unica pubblicazione di sessione | Aggiorna curva quando cambia il risultato e marker corrente/successivo agli indici t/t+1. Nessun secondo offset da binding di formula. |
| `StateMarker` | Posizione proiettata, revisione, indice assoluto opzionale | Marker generico; sfera corrente/cubo successivo configurati nei prefab. Il next non esiste al terminale. |
| `AngularStateView` | Stato/next e parametri di `Result.Configuration.Model` | Associa una componente angolare in rad e un parametro di lunghezza in m alle pose. Non legge asset o parametri richiesti di un tentativo fallito. |
| `AngularBodyView` | Angolo, lunghezza, revisione/indice | Conversione geometrica della posa: zero verso il basso, positivo antiorario in XY. Non integra né ricalcola la dinamica. |
| `CurvedTorqueRenderer` | Input applicato del medesimo campione, unità | Arco con punta e segno, valore numerico e unità. Raggio/ampiezza del simbolo sono fissi e dichiarati; non codificano una forza o una magnitudine non calibrata. Zero e terminale non generano frecce. |
| `VectorRenderer` | Origine, vettore grafico, unità e scala esplicite | Freccia riutilizzabile; impiegata per gli assi. Non interpreta un input del pendolo come forza lineare. |
| `PlotAxes` | Proiezione e descrittore del modello | Titoli, unità per componente, scale e dimensioni omesse. I segmenti sono dichiarati guide tra campioni discreti. |
| `LayerController` | Elenco configurato di root visive | Mostra/nasconde corpo, fase e stato-tempo senza riferimenti alla sessione o cambi del modello. |
| `HighlightRegistry` / `HighlightTarget` | Ruolo semantico e indice già risolto | Evidenzia target corrente/next/input; conserva identità di revisione e campione. Non contiene resolver di formule o Lesson Logic. |
| `ExperimentVisualization` | Riferimento alla sessione e pubblicazioni coerenti | Un'unica subscription distribuisce il medesimo stato ai consumer configurati; bind/disable/enable/close rimuovono e ripristinano esplicitamente il collegamento. |
| `LessonInputRouter` | Action asset, sessione, incremento/componente/unità configurati | Converte tastiera in comandi; legge l'input della sequenza richiesta per editarlo, senza possederne una copia persistente. Ogni lesson usa un clone delle azioni, disabilitato/rilasciato alla chiusura. |

`ExperimentHost.Initialize` collega visualizzazioni e input oltre al clock. `Close` li scollega prima di disporre la sessione. Non si aggiungono subscription al lifecycle globale, polling del manager, attese di frame o copie della configurazione nei consumer. I layer inattivi continuano a ricevere i dati dal binding, così riapparire non mostra una vecchia revisione.

### Spazi rappresentati e scale

- Corpo: pendolo planare in XY, lunghezza dalla configurazione della revisione, scala **1.5 unità scena/m**. Ghost arancio del prossimo campione, corpo corrente blu; nessuna interpolazione fisica o Rigidbody/collider.
- Piano di fase: **(θ, ω)**, rispettivamente **4 unità scena/rad** e **1 unità scena/(rad/s)**. Le etichette racchiudono il denominatore fra parentesi anche quando contiene una divisione, per evitare ambiguità dimensionali. Non è una posizione fisica bidimensionale.
- Stato-tempo 3D: **(θ, ω, τ)**, scale **3**, **0.65**, **1.4** nelle rispettive unità. Il terzo asse dichiara **tempo fisico in secondi, NON posizione**. Il piano è orientato nel viewport per rendere visibile la terza coordinata.
- Il tempo viene da `TrajectoryHistory.PhysicalTimeAt`, non dal clock di playback. Una proiezione temporale rifiuta una storia index-only; la proiezione configurata con `DiscreteIndex` può usare t senza inventare δ/τ.
- La storia conserva angoli non avvolti e tutte le componenti matematiche. Le proiezioni possono omettere componenti solo dichiarandole; nel pendolo non se ne omette nessuna.
- Una linea collega campioni discreti; non certifica gli stati intermedi di una dinamica continua. Né la posa del corpo né la freccia aggiungono saturazione, damping, collisioni o equazioni alternative.

### Dipendenze

Core e Simulation sono invariati e puri. Le nuove primitive appartengono a **OptCon.Presentation** tramite asmref nella cartella Visualization; Presentation aggiunge il riferimento diretto a Core per tipi/descrittori numerici, mantenendo Content/Simulation. Non dipende da Application né dall'Input System. Application aggiunge il riferimento al pacchetto Input System già presente per il router e compone i livelli. Nessun nuovo pacchetto, assembly vuoto o dipendenza circolare.

## Controlli consegnati

| Tasto | Effetto |
| --- | --- |
| P | Play/pausa del clock di presentazione |
| ← / → | Step indietro/avanti sui campioni salvati |
| R | Reset del preset catturato dalla sessione |
| A / D | Modifica u[0] di −/+0.5 N·m dalla posizione t a tutta la coda |
| Z | Azzera lo stesso intervallo di input |
| 1 / 2 / 3 | Visibilità corpo / piano di fase / stato-tempo |

Componente, unità, incremento e policy Single/Tail sono configurabili nel router; il preset consegnato usa Tail. La legenda dei controlli deriva dalle azioni effettive e dalla configurazione del router. Al terminale la modifica di input viene ignorata: non viene creato u_T. Il rate di playback resta separato da δ e modificabile dal probe Inspector F04; non è stato introdotto il pannello timeline/parametri di F06.

## Verifiche e criteri di completamento

Unity **6000.3.8f1**, esecuzioni native su copia isolata con Packages/ProjectSettings iniziali del workspace. L'Editor dell'utente è stato preservato. Le prove usano il servizio licenze locale; solo la copia disabilita l'indicizzazione Search all'avvio. La migrazione automatica dell'asset URP mobile nella copia non è stata trasferita.

| Verifica / criterio | Risultato finale |
| --- | --- |
| Compilazione senza strumenti temporanei | **PASS**, exit 0, nessun errore o warning C#. |
| Tutti gli EditMode | **122/122 PASS**, zero falliti/saltati: 115 regressioni F00–F04/A01 e sette nuovi casi di proiezione. |
| Tutti i PlayMode | **21/21 PASS**, zero falliti/saltati: 15 regressioni e sei nuovi test F05. |
| Ogni punto deriva dalla storia | Confronto di tutti i 101 punti di entrambe le curve con la proiezione dei campioni della stessa revisione; marker t/t+1 aggiornati dopo modifica input e seek. |
| Proiezioni deterministiche | Pendolo m=2, l=3, g=0, δ=0.5, u=18: stati salvati `[0,0]`, `[0,0.5]`, `[0.25,1]`. Le proiezioni di due campioni hanno coordinate note, senza una seconda implementazione Euler. |
| A01 / index-only | Caso `2+3=5`, T=1: δ/τ null; asse fisico rifiutato, asse indicizzato esplicito valido. Tutti i test A01 precedenti, legacy/round trip e validazione del pendolo rieseguiti. |
| Pendolo / tempo fisico | δ=0.02 s conservato; a t=20 τ=0.4 s. Equazioni e preset matematico invariati. |
| Separazione e configurazione del risultato | Core/Simulation non modificati; il corpo usa la lunghezza del risultato B, conservando A. Un parametro rifiutato mantiene risultato e posa validi. |
| Layer | Nascondere tutti i layer non cambia storia/cursore; seek aggiorna anche i target nascosti, che riappaiono sul campione corretto. |
| Coppia e terminale | Eventi Keyboard elaborati dall'Input System verificano il segno del simbolo curvo; zero senza freccia, terminale senza input/next/ghost. |
| Lifecycle A03/A04 | Visualizzazione e input collegati prima di Ready; riapertura con nuova sessione e azioni; disable/enable senza stato residuo; test precedenti di rientranza, stessa scena e risultato immutabile passati. |
| Prova nativa della scena | Bootstrap → menu → workspace → input/step/seek/play/pausa/layer → terminale → menu → riapertura/reset: **PASS**, zero errori runtime. |
| Verifica manuale visiva | Game View **1440×979** ispezionata: corpo/ghost, freccia curva, piano di fase, traiettoria 3D, marker, assi/unità/scale e legenda leggibili. La cattura finale elimina le sovrapposizioni rilevate nella prima prova. |
| Regressione nativa | **3/3 cicli menu/workspace/menu PASS**, due con domain reload e uno senza; root/camera/presenter unici, URP/pavimento/luce/Volume presenti, nessuno script mancante. |
| Sorgenti/asset verificati | **240 file applicativi, zero differenze SHA-256** fra workspace e copia verificata. |

Nuovi PlayMode: `VisualConsumersAndDedicatedInputAreBoundBeforeReady`, `EveryTrajectoryPointAndTwoMarkersUseTheSameSavedResultAfterCommands`, `HiddenLayersNeverChangeSimulationAndHighlightUsesResolvedIndex`, `PhysicalPoseUsesResultConfigurationAndTerminalHasNoGhostOrTorque`, `RealKeyboardChangesInputPlaybackStepsAndResetWithoutMutatingAssets`, `ClosingAndReopeningReleasesVisualSubscriptionsAndClonedActions`.

Gli EditMode aggiungono casi sulle coordinate note, tempo assente/indice esplicito, unità/componenti incompatibili, coordinate NaN/∞/fuori float e proiezione di uno stato con più dimensioni e angoli non avvolti. Nessun test sulle sole scelte cromatiche.

La prova del lifecycle e dell'interazione è automatizzata nell'Editor reale, con ispezione manuale delle catture. I PlayMode creano un dispositivo Keyboard virtuale e inviano eventi di stato che attraversano l'Input System e le azioni runtime. Non si dichiara una sequenza di click manuali nell'Editor dell'utente. Prima delle verifiche finali sono stati corretti un binding dei tasti numerici, l'allineamento dei GUID dei nuovi script fra copia e workspace e la dimensione/posizione delle etichette; è stata inoltre resa esplicita l'unità al denominatore delle scale. I GUID preesistenti non cambiano; nessun errore applicativo è stato filtrato. I log iniziali rimangono nelle evidenze.

Evidenze locali: `C:/Users/querz/AppData/Local/Temp/OptCon-F05-b35a9c53a4fe482396eddd580fb8f583/`. File principali: `EditMode-results-final.xml`, `PlayMode-results-final.xml`, `editmode-final.log`, `playmode-final.log`, `final-compile.log`, `visualization-results.txt`, `visualization-final.log`, `visualization-preview.png`, `smoke-results.txt`, `regression-smoke.log`, `baseline.json`, `verified-source-manifest.json`, `change-inventory.json`. Gli strumenti esterni sono rimossi dalla copia prima della compilazione finale e non sono consegnati negli Assets.

## Inventario completo

**62 file creati:** i seguenti 28 file applicativi, ciascuno con il proprio `.meta`, cinque `.meta` di cartelle e questo rapporto. Percorsi applicativi relativi ad `Assets/OptimalControl/`.

```text
Runtime/Application/LessonInputRouter.cs
Runtime/Visualization/OptCon.Presentation.asmref
Runtime/Visualization/StateProjection.cs
Runtime/Visualization/TrajectoryRenderer.cs
Runtime/Visualization/StateMarker.cs
Runtime/Visualization/VectorRenderer.cs
Runtime/Visualization/PlotAxes.cs
Runtime/Visualization/StateTrajectoryView.cs
Runtime/Visualization/AngularBodyView.cs
Runtime/Visualization/AngularStateView.cs
Runtime/Visualization/CurvedTorqueRenderer.cs
Runtime/Visualization/LayerController.cs
Runtime/Visualization/HighlightTarget.cs
Runtime/Visualization/HighlightRegistry.cs
Runtime/Visualization/ExperimentVisualization.cs
Data/Visualizations/PhasePlane.asset
Data/Visualizations/StateTime.asset
Input/LessonControls.inputactions
Materials/Axes.mat
Materials/StateCurrent.mat
Materials/StateNext.mat
Materials/Trajectory.mat
Prefabs/PendulumScenario.prefab
Prefabs/StateMarker.prefab
Prefabs/PlotAxes.prefab
Tests/EditMode/Visualization/OptCon.Visualization.EditModeTests.asmdef
Tests/EditMode/Visualization/StateProjectionTests.cs
Tests/PlayMode/VisualizationTests.cs
```

Metadati di cartelle: `Runtime/Visualization.meta`, `Data/Visualizations.meta`, `Input.meta`, `Materials.meta`, `Tests/EditMode/Visualization.meta`. Rapporto: `Docs/REVIEWS/F05_IMPLEMENTATION.md`.

**8 file modificati**, relativi alla root del progetto:

```text
Assets/OptimalControl/Runtime/Application/ExperimentHost.cs
Assets/OptimalControl/Runtime/Application/OptCon.Application.asmdef
Assets/OptimalControl/Runtime/UI/OptCon.Presentation.asmdef
Assets/OptimalControl/Data/Experiments/PendulumPrototype.asset
Assets/OptimalControl/Scenes/LessonWorkspace.unity
Assets/OptimalControl/UI/CourseMenu.uxml
Assets/OptimalControl/UI/CourseMenu.uss
Docs/OPTCON_IMPLEMENTATION_PLAN.md
```

Il preset cambia solo riferimento allo scenario e descrizione: ID, GUID, provider e tutti i valori matematici rimangono invariati. Il workspace adatta camera e posizione del pavimento per le tre viste; conserva camera/luce/Volume/materiali di base. UXML/USS aggiornano la nota e riducono il pannello editoriale per liberare il viewport; non aggiungono Formula/HUD. Nessun file preesistente eliminato o `.meta` preesistente modificato; Core, Simulation, Content, catalogo, formule/metriche, altre specifiche/review, PDF, SampleScene, pacchetti e ProjectSettings conservati.

## Procedura manuale in Unity

1. Aprire `Assets/OptimalControl/Scenes/Bootstrap.unity` con Unity 6000.3.8f1; attendere compilazione, entrare in Play e aprire il prototipo. Controllare Console vuota, una sola istanza `PendulumScenario`, indicatore r0/t=0/100, δ=0.02 e τ=0.
2. Individuare corpo fisico, piano `(θ,ω)` e grafico 3D `(θ,ω,τ)`. Leggere assi/scale e la dichiarazione tempo ≠ posizione. Nei grafici il corrente è una sfera blu e il prossimo un cubo arancio; sul corpo il prossimo è un ghost arancio. Segmenti e pose mostrano campioni, senza interpolazione.
3. Dare focus alla Game View. Premere D/A e osservare coppia, nuova revisione e traiettorie; ogni pressione cambia di 0.5 N·m la coda da t. Z azzera la coda. Il simbolo curvo cambia orientamento con il segno ed è assente a zero; il valore/segno sono espliciti.
4. Usare ←/→, P e R. Verificare che step/rewind leggano la medesima storia, play avanzi e reset riporti preset e cursore iniziali. L'Inspector di `ExperimentHost` F04 resta disponibile per seek, policy Single/Tail, parametri e rate di playback.
5. Premere 1/2/3 per i layer. Mentre uno è nascosto avanzare un passo, poi mostrarlo: deve riapparire sul campione corrente senza modificare la revisione matematica per il solo toggle.
6. Dall'Inspector portare Seek a 100: τ=2 s, nessun cubo/ghost successivo, coppia N/A e Play fermo. Tornare al corso e riaprire: r0, t=0, input originali e nessuno scenario duplicato.
7. Per verificare punti esatti, selezionare `SavedTrajectory` sotto PhasePlane/StateTime e confrontare le posizioni del LineRenderer con gli assi e i test. Il caso deterministico con m=2/l=3/g=0/δ=0.5/u=18 è riprodotto automaticamente da `StateProjectionTests`; un test index-only verifica l'assenza di secondi artificiali.
8. Eseguire tutte le suite dal Test Runner: 122 EditMode, 21 PlayMode. Il validatore Catalog/Execution Configuration del corso originale rimane valido.

## Deviazioni e problemi aperti

Nessuna deviazione funzionale da F05. L'asmref riutilizza Presentation per le primitive; i piccoli adapter geometrici, il consumer comune e l'assembly di test sono il supporto necessario ai componenti previsti. Il controllo manuale nell'Editor è reso ripetibile dai passi sopra; la verifica eseguita usa automazione nativa e ispezione visiva delle catture.

Le scale e l'inquadratura sono fisse e dichiarate. Input o lunghezze molto grandi possono portare geometria fuori dal viewport: non vengono tagliati i dati matematici o aggiunte saturazioni per adattarli al display. Auto-fit, camera esplorabile e più risoluzioni appartengono al successivo lavoro di leggibilità; non sono criteri richiesti da F05.

**A02 resta aperto prima di F06.** Formula/HUD, selezione dei termini, costo/target/confronto e Lesson Logic non sono implementati. Il registry rende disponibili i ruoli già visualizzati; il resolver e gli ulteriori target della formula saranno collegati nella fase prevista. Nessun problema architetturale bloccante emerso. **Il lavoro termina con F05.**
