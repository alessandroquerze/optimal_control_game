# F14 — C1.L5: drone planare

Data: 13 ottobre 2026. Specifica operativa: `OPTCON_IMPLEMENTATION_PLAN.md`, F14. **F14 completata e verificata. F15 non iniziata.**

## Scope e prerequisiti estratti dal piano

F14 produce **C1.L5**, nuovo modello concreto planare con sei stati e due input, usando l'infrastruttura F00–F13. Comprende posa, spinte, traiettorie/proiezioni multidimensionali, Formula HUD, riferimento campionato, costo diagonale discreto, diagnostica dei limiti di quota/input, attività didattiche e progresso persistente. Il piano non prescrive una versione iniziale: la nuova lesson e l'esperimento hanno **contentVersion 1 / schemaVersion 1**.

Prerequisiti verificati prima dell'implementazione: F13 completata, contratti immutabili di reference/cost/constraints disponibili; resolver tipizzato, selezione esplicita delle componenti e proiezioni configurabili presenti; remediation MR-01/02/04/05 verificata con baseline **425 EditMode + 101 PlayMode**. MR-03 rimane documentato: questa lesson usa intervalli per componente, senza assumere geometrie di ellissi o superfici non ancora erogate.

Capability richieste: `Parameters | Trajectory`, attraverso il provider dinamico esistente. Output, reference, costi e vincoli utilizzano i rispettivi contratti del risultato. Non viene dichiarata una linearizzazione del drone.

Criteri del piano: ordine `[p_x,v_x,p_y,v_y,θ,ω]`, J esplicitamente momento d'inerzia, modello dichiarato planare, Euler sullo stato vecchio, infrastruttura riutilizzata; hover, segni, dimensioni, costi/limiti e stati diversi alla stessa posizione verificabili singolarmente. Le attività corrispondenti sono elencate sotto.

**F15 resta fuori dallo scope:** nessuna quadratura del costo continuo, confronto di discretizzazioni a durata comune, trasferimento del pendolo, integratore generale, solver, Newton, gradient descent, KKT, costate, IterationHistory o Matrix runtime generale.

## Fonti ricontrollate

Letti integralmente vision, curriculum, architettura, piano, audit F08, mini-review F11–F13, remediation MR e report pertinenti. Letto integralmente il PDF `Docs/OPTCON-RL_introduction_optimal_control_2026_09_14.pdf`; controllate direttamente anche le immagini delle pagine del drone.

SHA-256 PDF: `ae169f3492dc0915d0777d62e90184107d7017aee03b8620bbfe0aee9bd3a469`.

| Pagine fisiche / slide | Uso in F14 |
|---|---|
| 26–27 / 25–26 | Modello planare, ordine degli stati, due spinte, segni, fattore L/(2J), Forward Euler |
| 28 / 27 | Stato/output e obiettivo sul centro di massa |
| 29 / 28 | Riferimento di posizione/velocità, costo stage e terminale, fattori ½ |
| 30 / 29 | Esempi di limiti sulle singole spinte e sulla quota |

Precisazione del dominio: la slide finale usa anche una scrittura dei vincoli indicizzata 1…T; il contratto generale della fonte (p.21) e quello F13 richiedono **T input, t=0…T−1**. F14 dichiara esplicitamente questo dominio per tutti i propri vincoli di percorso, compresa la quota. Non si inventa u_T; non si dichiara verificato un vincolo terminale sulla quota. I limiti numerici del preset sono scelte editoriali esemplificative, non parametri obbligatori del PDF.

## Modello, configurazione e discretizzazione

`PlanarDroneConfiguration` è matematica pura e immutabile. `PlanarDroneDynamics.Evaluate` contiene l'unica implementazione della transizione. Dato lo stato **vecchio**, applica simultaneamente:

```text
p_x,next = p_x + δ v_x
v_x,next = v_x − δ (u₁+u₂) sinθ / M
p_y,next = p_y + δ v_y
v_y,next = v_y + δ ((u₁+u₂) cosθ / M − g)
θ_next   = θ + δ ω
ω_next   = ω + δ L (u₁−u₂) / (2 MomentOfInertia)
```

L è la distanza completa tra i rotori; u₁ è la spinta a destra, u₂ quella a sinistra; θ positivo è antiorario. Entrambe le forze hanno direzione `[-sinθ,cosθ]` nel piano XY. Ogni input è una **forza in N**, non una coppia. La coppia emerge dal braccio e dalla differenza di spinta. Il viewport Unity è tridimensionale; il modello rimane planare.

| Dato | Shape / unità |
|---|---|
| x | Vector(6): `[m, m/s, m, m/s, rad, rad/s]` |
| u | Vector(2): `[N,N]` |
| y=h(x) | Vector(2): `[p_x,p_y]`, `[m,m]` |
| M | kg; positivo finito |
| L | m; positivo finito |
| g | m/s²; non negativo finito |
| MomentOfInertia, simbolo PDF J | kg·m²; positivo finito |
| δ | s; positivo finito obbligatorio per questo modello Euler |

Il nuovo `StateComponentsOutput` seleziona e congela una lista ordinata di componenti. Il drone usa indici `[0,2]`, conservando un output distinto dallo stato. I campioni output sono prodotti dalla simulazione e salvati nel risultato, inclusi y_T e il reference output a T.

Preset: **M=1, L=1, g=9.81, MomentOfInertia=.25, δ=.02 s, T=50**, x₀=`[0,0,1,0,0,0]`, spinte hover `[4.905,4.905]`. La durata del preset è un secondo. Il playback modifica soltanto la presentazione. Pendolo e modelli index-only mantengono i contratti A01: nessun tempo fisico viene aggiunto ai secondi.

Il provider verifica dimensioni e i quattro target/metadata contro i parametri del modello. Modificare massa, geometria, gravità o inerzia produce una nuova configurazione immutabile. Nessun parametro viene riletto da un asset mutabile per interpretare un risultato precedente.

## Architettura e modifiche minime ai contratti condivisi

```text
PlanarDroneModelDefinition → PlanarDroneConfiguration / PlanarDroneDynamics
    → SimulationSession → immutable SimulationResult
    → output / reference / cost / constraint evaluations
    → RuntimeMetricResolver
    → Formula HUD, visualizzazione, LessonMetricEvidenceEvaluator
```

Stessi `ExperimentHost`, sessione, CourseManager, composition root, FormulaPresenter, HUD e progression service delle lesson precedenti. Nessuna dipendenza Unity in Core/Simulation; nessuna equazione dinamica nel renderer/HUD/condition. La specializzazione `PlanarDroneBodyView` calcola solo la geometria della posa e dei glyph dalle quantità pubblicate.

Estensioni necessarie e retrocompatibili:

- Output: selezione ordinata di più componenti, senza sostituire i precedenti output.
- `ComponentBound`: intervallo opzionale asimmetrico, con `g=max(min−v,v−max)`. Il vecchio costruttore simmetrico e gli asset con `isInterval=false` conservano `|v|−limit`. `.limit` continua a identificare il massimo; `.minimum` esiste solo per gli intervalli.
- Evidenza MR-01: scope `NextTimestep` e confronto firmato `GreaterThanBaseline`; il binding next al terminale viene rifiutato. Nessuna equazione duplicata nelle condizioni.
- Disclosure editoriale degli indici di stato/input modificabili: liste vuote mantengono il comportamento precedente, liste esplicite sono validate e congelate nel piano della lesson.
- Simbolo editoriale del costo totale, default `J` per gli asset esistenti, **ℓ_tot** per il drone. J resta il momento d'inerzia nella dinamica e nei live term.
- Tutti gli `HighlightTarget` di un marker ricevono revisione/indice, permettendo ruoli generici e ruoli di componente sullo stesso oggetto.
- Validazione delle proiezioni del prefab contro il modello realmente composto; label compatte configurabili soltanto sul nuovo prefab. Legenda HUD comune resa indipendente dai nomi dei layer del pendolo.

Lifecycle, dispatch, `SessionNotificationScope`, publication isolation, generation/lifetime guards, stato `NonRepresentable`, Enter atomico, receipt e percorso di persistenza non sono stati semplificati né sostituiti.

## Formula bindings e visualizzazioni

Sette `FormulaDefinition`: stato, dinamica, hover, reference, costo, bounds e terminale. La formula dello stato riutilizza il layout strutturato F10 con vettore colonna a sei righe; le equazioni e i metadata degli altri casi rimangono editoriali. Ogni termine live passa dal resolver centrale.

`state.current` è Vector(6), `input.applied` è Vector(2), `output.current` è Vector(2). u₁/u₂ sono selezioni **esplicite** 0/1 del vettore originale. Non esistono conversioni implicite vector→scalar. Sono presenti binding per componenti dello stato e next, parametri M/L/g/J, t/τ/δ, reference, contributi di costo, costo totale/terminale e residui. `SampleIndex` distingue t dal next t+1; stessa revisione in HUD, body, grafici, reference e diagnostica.

Ruoli semantici: `state.current`, `state.component.i`, `state.next.component.i`, `input.applied`, `input.thrust1`, `input.thrust2`, `output.position`, reference e cost/constraint. Selezionare l'output evidenzia la posizione/body e il marker XY, senza attribuire all'output le quattro componenti omesse. Selezionare u₁/u₂ indirizza le due frecce separate. Nessuna ricerca di renderer concreti nel Formula system.

Il prefab riusa sette `StateTrajectoryView` e i renderer di traiettoria esistenti:

- Vista fisica XY: posizione del centro di massa e body orientato secondo θ; braccio L, due spinte, corridoio diagnostico di quota. Le velocità omesse sono dichiarate.
- Traiettoria di posizione XY: assi p_x/p_y in m, componenti omesse esplicite.
- Sei grafici separati: ciascuna componente rispetto a **τ [s]**, con propria unità e scala dichiarata. Nessun piano θ/ω usato come sostituto dello stato completo.
- Riferimento: tratteggio e diamante distinti dalla linea/marker reali, attraverso `ReferenceTrajectoryView` F13.
- Costo: stage, cumulativo degli stage precedenti al campione e terminale separato, mediante il visualizzatore esistente.
- Vincoli: tre intervalli con identità, unità, residuo, revisione e indice; niente saturazione.

Campi di editing pertinenti: solo v_y nel confronto a pari posizione, entrambi gli input negli esperimenti collettivo/differenziale, solo θ nel tilt, solo u₁ nel limite input, solo p_y nel limite quota. I foldout e lo scroll esistenti conservano formula e controlli raggiungibili; non vengono mostrati otto editor contemporaneamente nel percorso guidato. Le catture reali sono state ispezionate; il polish globale MR-06 resta F29.

## Reference, costo e limiti

Reference campionato immutabile F13: T+1 stati e T input. Preset `p_x^des=.5τ`, `v_x^des=.5`, `p_y^des=1`, altre componenti zero, **u^des=[0,0]**. L'output desiderato è h applicata agli stati desiderati. Con gravità, questo riferimento non è un rollout fattibile: h_des diagnostica il difetto; l'utente modifica un campione senza ricampionamento implicito.

Costo diagonale riutilizzato:

```text
e_t = x_t − x_t^des
ℓ_t = ½ Σ_i q_i e_t[i]² + ½ r₁ u₁² + ½ r₂ u₂²
ℓ_T = ½ Σ_i q_T,i e_T[i]²
ℓ_tot = Σ_(t=0)^(T−1) ℓ_t + ℓ_T
```

Q=`[2,1,2,1,.5,.2]`, R=`[.02,.02]`, QT=`[5,2,5,2,1,.4]`, con unità inverse quadrate delle componenti: contributi e totale adimensionali. Spinte e θ/ω sono penalizzati verso zero. Non si sottrae implicitamente il thrust hover dal costo input. Nessun fattore δ: costo discreto per campione, senza quadratura F15.

Oracolo del preset hover: contributo stato stage₀ **.125**, contributo input **.4811805**, terminale **.875**, totale **35.226525**. Il cumulativo all'arrivo a t esclude lo stage t e il terminale.

Limiti diagnostici: u₁∈[0,8] N, u₂∈[0,8] N, p_y∈[.5,2] m, dominio t<T. Tre componenti/ID distinti; input 9 N resta 9 N e g₁=1 N; p_y=2.5 resta 2.5 e g_quota=.5 m. Nessun enforcement, penalità implicita o certificazione continua tra campioni. Non vengono aggiunti vincoli su angolo/velocità fuori dai limiti richiesti dal piano.

## Lesson Logic, review didattica e MR-01

Ogni ingresso ripristina il proprio preset; il baseline viene catturato dopo l'ingresso atomico. Le condizioni usano resolver/result autorevoli e identità di attività/generation/lifetime MR-01. Le risposte corrette alternano la posizione nell'elenco e sono associate a ID stabili, non all'ordine.

| Attività | Informazioni/formula disponibili | Evidenza richiesta oltre alla risposta |
|---|---|---|
| 1 Stato/input/output | Ordine delle sei componenti, unità, shape, h(x) | Selezione di x, u e y |
| 2 Pari posizione | Glossario M/L/g/J/δ, Euler, v_y e p_y,next, costo totale | p_x/p_y invariati, v_y=.5, p_y,next maggiore del baseline, costo cambiato |
| 3 Hover | Somma Mg, differenza zero, M/g live, J disambiguato | Entrambi gli input Mg/2, tutte le sei componenti next uguali all'equilibrio e rollout costante |
| 4 Spinta collettiva | Equazioni della somma/differenza ed Euler | Entrambe le spinte aumentate, v_y,next maggiore, ω_next e p_y,next invariati |
| 5 Spinta differenziale | Equazioni e J momento d'inerzia | u₁ aumenta/u₂ diminuisce, ω_next maggiore, v_y,next e θ_next invariati |
| 6 Tilt | Segno −sinθ e unità rad | θ=.2, v_x,next diminuisce, posizione iniziale invariata |
| 7 Traiettoria | Proiezione XY e sei grafici con omissioni dichiarate | t=10 e selezione output; baseline allo stesso campione |
| 8 Reference | Preset desiderato, u_des=0, h_des | y_des[0]=.4 a t10, y reale invariato, selezione h_des |
| 9 Costo | Formula diagonale, Q₃₃, J distinto da ℓ_tot | Peso 1→2, contributo stato aumenta, v_y invariato |
| 10 Limite input | Bounds separati, g e assenza di saturazione | u₁=9, u₂ invariato, g₁=1, g₂ invariato |
| 11 Limite quota | Intervallo in m, nessuna forza/clamp | p_y=2.5, g_quota=.5, residui input invariati |
| 12 Terminale | T/τ, costo finale, domini dichiarati | t=T e selezione input/next/stage indisponibili |

Review come studente: i simboli nuovi sono spiegati nel contesto; le componenti e unità sono identificabili; le equazioni necessarie sono mostrate nello step, senza cambiare selector; è possibile prevedere somma/differenza e segni prima della domanda. La posa mostra posizione/assetto, le frecce mostrano forze, i grafici le quantità nominate. Gli step numerici non passano con una modifica qualsiasi o con la sola risposta statica.

## IDs, catalogo e persistenza

Percorso **salvato**, senza sostituzione del Course: `Bootstrap.unity → OptimalControlCourse (optimal-control) → Chapter01 (C1) → C1.L5 «Drone planare: sei stati e due spinte»`.

Lesson `C1.L5`, esperimento `c1.drone.planar`, modello provider `model.drone.planar.explicit-euler`: v1/schema1. Vincoli `c1.constraints.drone.bounds`; formule `c1.drone.state/dynamics/hover/reference/cost/bounds/terminal`. Prerequisito **consultivo C1.L4**, non hard lock.

La completion attraversa il receipt esistente e `LessonProgressBinding`; viene accreditata soltanto C1.L5. Completion precedenti C1.L1 v2, C1.L2 v2, C1.L3 v2, C1.L4 v1 o prototipo non promuovono la nuova lesson. Riavviare la sessione non cancella né duplica il progresso persistente; riaprire crea una nuova sessione e un baseline fresco.

## Test e verifiche

Unity **6000.3.8f1**, versione finale senza helper temporanei:

| Verifica | Risultato |
|---|---|
| Compilazione Unity finale | **PASS**, exit 0, nessun errore/warning C# |
| Tutti gli EditMode | **484/484 PASS**, 0 falliti, 0 saltati |
| Tutti i PlayMode | **108/108 PASS**, 0 falliti, 0 saltati |
| Test F14 aggiunti | **59 EditMode** (29 modello/contratti, 30 contenuto/binding/outcome) + **7 PlayMode** |
| Baseline MR pre-F14 | Tutti i **425 + 101** casi conservati per nome completo e passanti |
| Catalogo di produzione / fonti / binding / proiezioni | PASS attraverso `CatalogValidator` e provider reale |
| Runtime nativo completo | PASS: 12 step, esiti/risposte errati, Previous, reset/restart, failure, NonRepresentable, recupero, completion, menu/riapertura |
| Nuovo processo Unity | PASS: progresso C1.L5 ricaricato, sessione fresca, restart conserva completion |
| Identità della copia verificata | **598/598 file applicativi identici** al workspace, inclusi `.meta` |
| Dipendenze | **13 assembly aciclici**; Core/Simulation senza dipendenze UI/Unity/visualizzazione |

Nessuna regressione rilevata in F00–F13: pendolo, index-only, statico F10, prototipo F08, output, linearizzazione, reference/cost/constraints, A/B, provider custom, progressione e failure/lifecycle rimangono coperti dalle suite complete. I probe avversariali MR-01 continuano a essere respinti; A01–A04, AF08-01–07 e MR-01/02/04/05 sono preservati. Nessuna scena salvata, pacchetto o ProjectSettings del workspace modificato. Nessuna dipendenza aggiunta. La copia Editor genera soltanto un proprio `SceneTemplateSettings.json`, esterno al workspace, non parte della consegna.

Le prime esecuzioni hanno rilevato un ID editoriale duplicato formula/vincoli e due attesi numerici con un fattore errato: corretti prima delle verifiche finali. L'ID dei vincoli è ora distinto; gli oracoli riportati sotto usano i valori fisici effettivi, senza modificare la dinamica per adattarla agli attesi.

Evidenze complete: [EditMode.xml](F14_EVIDENCE/EditMode.xml), [PlayMode.xml](F14_EVIDENCE/PlayMode.xml), [verification.json](F14_EVIDENCE/verification.json), [log nativo](F14_EVIDENCE/native-runtime-results.txt), [reload](F14_EVIDENCE/native-reload-results.txt), log delle suite/compilazione, inventario e manifest SHA-256. **20 catture PNG** conservate; ispezionate le viste rappresentative di stato, stessa posizione, differenziale, reference, costo, limiti, terminale e catalogo.

Catture per la revisione: [catalogo normale](F14_EVIDENCE/production-menu.png), [stato diverso a pari posizione](F14_EVIDENCE/drone-same-position.png), [due spinte e next live](F14_EVIDENCE/drone-differential-live.png), [reference](F14_EVIDENCE/drone-reference-live.png), [costo e J inerzia](F14_EVIDENCE/drone-cost-live.png), [quota fuori limite](F14_EVIDENCE/drone-altitude-limit.png), [terminale N/A](F14_EVIDENCE/drone-terminal-live.png).

Tre nuovi file di test: `PlanarDroneTests`, `ProductionDroneTests`, `ProductionDroneRuntimeTests`. Le liste catalogo di quattro test precedenti includono ora la quinta lesson; nessuna asserzione legacy rimossa.

Oracoli indipendenti, con M=2, L=4, g=10, J=2, δ=.1:

| Caso | Atteso |
|---|---|
| Hover u=[10,10] | Rollout intero costante |
| u=[12,12] / [8,8] | v_y,next=.2 / −.2, ω_next=0 |
| u=[12,8] / [8,12] | ω_next=.4 / −.4, v_y,next=0 |
| u=[0,0] | v_y,next=−1 |
| θ=±.2, u=[10,10] | v_x,next=∓.19866933079506122; v_y,next=−.01993342215875837 |
| x=[1,2,3,4,π/2,.5], u=[6,2] | next=[1.2,1.6,3.4,3,π/2+.05,.9] |
| δ=.5, u=[12,12], tre passi | quote [1,1,1.5,2.5], v_y [0,1,2,3] |
| h([1,2,3,4,5,6]) | [1,3] |

Expected numerici fissati indipendentemente dalla funzione sotto test. Altri casi: metadata/unità/dimensioni, δ assente/non positivo/NaN/±∞, parametri invalidi, output congelato, interval residual dentro/bordo/fuori, editing immutabile dei bounds, resolver Vector(6/2), component selection, SampleIndex next, terminale, reference/costo conservati in A dopo B/failure, disclosure invalida, Enter fallito e nessun progresso, tutte le attività, outcome sbagliati, Previous/reset/restart, completion/reload, semantic target e proiezioni.

Le prove native sono **automazioni nell'Editor Unity tramite gli eventi dei controlli UI reali**, dal Bootstrap salvato, con Game View e rendering nativi. Non sono un collaudo manuale umano né uno studio di usabilità. Store di test isolato; nessun progresso personale dell'utente modificato. Gli helper di authoring/verifica sono esterni al progetto consegnato e vengono rimossi dalla copia prima delle suite/compilazione finali.

## Procedura manuale Unity

1. Aprire `Assets/OptimalControl/Scenes/Bootstrap.unity`, entrare in Play. Il corso `optimal-control` è già assegnato al Bootstrap salvato: nel menu, sotto Chapter 1, scorrere verso il basso e cliccare la quinta lesson **Drone planare: sei stati e due spinte**. Nessun cambio Course/scene o strumento Editor richiesto. C1.L4 è indicato come suggerimento consultivo anche su profilo nuovo.
2. Nel pannello scorribile selezionare x_t, u_t, y_t e rispondere. In ogni step la formula pertinente è già visibile; aprire **Input e parametri** quando richiesto e usare il campo indicato nelle istruzioni.
3. Pari posizione: x₀[3]=.5. Hover: **Coda da t**, u[0]=u[1]=4.905. Collettivo: entrambi 5.905. Differenziale: 5.905/3.905. Tilt: x₀[4]=.2. Verificare next, frecce e grafici. La risposta corretta da sola non abilita il passaggio negli step numerici.
4. Traiettoria: osservare playback/step/seek, tornare a t10 e selezionare y. Reference: nel pannello costo/configurazione cambiare `cost.reference.state.10.0` a .4, leggere y_des e h_des. Costo: Q[3] a 2; contributo stato cresce di .125 e J resta inerzia.
5. Limite input: selezionare **Solo transizione t**, u[0]=9; u[1] resta 4.905. Leggere g₁=1 N. Quota: x₀[2]=2.5, g_quota=.5 m. Il modello non clampa i valori.
6. Terminale: t50, τ=1 s. Input, next, stage e g di percorso sono N/A; output e costo terminale esistono. Selezionare i termini richiesti, rispondere e concludere. Tornare al corso, riaprire e riavviare: sessione fresca, progresso conservato; verificare anche chiudendo e riavviando Play/app.
7. Provare una risposta errata, entrambe le spinte diminuite nel collettivo e differenziale invertito: non devono consentire Avanti. Indietro ripristina il preset e richiede di rifare l'esito.
8. In esplorazione libera, provare costo R[0]=0: richiesta rifiutata, risultato valido conservato, input editing disabilitato; Reset recupera. Un p_x iniziale 1e40 produce `NonRepresentable`, mantiene valori matematici validi, impedisce completion; Reset/Restart recuperano.

## File creati e modificati

Percorsi sotto `Assets/OptimalControl/`; ogni nuovo file applicativo elencato ha il proprio `.meta` incluso. Inventario completo e hash della versione verificata in `F14_EVIDENCE/`.

**Creati (27 file applicativi + 27 .meta):**

- `Runtime/Core/Models/PlanarDroneDynamics.cs`
- `Runtime/Content/Models/PlanarDroneModelDefinition.cs`
- `Runtime/Visualization/PlanarDroneBodyView.cs`
- `Tests/EditMode/Mathematics/PlanarDroneTests.cs`
- `Tests/EditMode/Formula/ProductionDroneTests.cs`
- `Tests/PlayMode/ProductionDroneRuntimeTests.cs`
- `Data/Models/PlanarDrone.asset`
- `Data/Experiments/PlanarDrone.asset`
- `Data/References/DroneDesired.asset`
- `Data/Costs/DroneTracking.asset`
- `Data/Constraints/DroneBounds.asset`
- `Data/Lessons/Chapter01/C1_L5.asset`
- `Data/Formulas/Chapter01/DroneState.asset`
- `Data/Formulas/Chapter01/DroneDynamics.asset`
- `Data/Formulas/Chapter01/DroneHover.asset`
- `Data/Formulas/Chapter01/DroneReference.asset`
- `Data/Formulas/Chapter01/DroneCost.asset`
- `Data/Formulas/Chapter01/DroneBounds.asset`
- `Data/Formulas/Chapter01/DroneTerminal.asset`
- `Data/Visualizations/DronePosition.asset`
- `Data/Visualizations/DroneComponent0.asset` … `DroneComponent5.asset` (sei file)
- `Prefabs/PlanarDroneScenario.prefab`

**Modificati (22 file applicativi esistenti):**

- `Data/Chapters/Chapter01.asset`
- `Editor/CatalogValidator.cs`
- `Runtime/Core/Contracts/IOutputFunction.cs`
- `Runtime/Core/Constraints/ConstraintConfiguration.cs`
- `Runtime/Content/Definitions/ConstraintDefinition.cs`
- `Runtime/Content/Definitions/CostDefinition.cs`
- `Runtime/Content/Definitions/LessonStepDefinition.cs`
- `Runtime/Content/Lessons/LessonPlan.cs`
- `Runtime/Lessons/LessonMetricEvidenceEvaluator.cs`
- `Runtime/UI/CostPanelPresenter.cs`
- `Runtime/UI/LessonHudPresenter.cs`
- `Runtime/UI/LessonStepPresenter.cs`
- `Runtime/Visualization/ConstraintRegionVisualizer.cs`
- `Runtime/Visualization/CostContributionVisualizer.cs`
- `Runtime/Visualization/ExperimentVisualization.cs`
- `Runtime/Visualization/PlotAxes.cs`
- `Runtime/Visualization/StateMarker.cs`
- `UI/LessonHud.uxml`
- `Tests/EditMode/Formula/ProductionCostReferenceTests.cs`
- `Tests/EditMode/Formula/ProductionLinearizationTests.cs`
- `Tests/PlayMode/ProductionCostReferenceRuntimeTests.cs`
- `Tests/PlayMode/ProductionIntroductionRuntimeTests.cs`

Documentazione: questo report, **33 file di evidenza** in `Docs/REVIEWS/F14_EVIDENCE/` e aggiornamento dello stato F14 in `Docs/OPTCON_IMPLEMENTATION_PLAN.md` dopo la chiusura delle verifiche. Totale rispetto all'inizio di F14: **88 file creati, 23 modificati, 0 eliminati**. Inventario confrontato con lo stato iniziale di F14, non con l'intero diff Git preesistente. I precedenti report, le altre specifiche e gli asset C1.L1–C1.L4 sono invariati.

## Limiti residui e deviazioni

- Nessuna estensione a F15. Le piccole estensioni output/intervalli/evidenze/disclosure/simbolo sono necessarie a esprimere il caso F14 attraverso i sistemi comuni, senza secondo stack.
- Limiti per componente e costo diagonale soltanto; niente full matrices, superfici generali, enforcement o ottimizzazione. Modello ideale planare, non quadrotor completo o modello di attuatori.
- Proiezioni a scala fissa dichiarata: valori estremi in esplorazione possono uscire dal viewport. Nessun clamp matematico per accomodare il disegno; auto-fit/camera/layout globale rimangono polish F29.
- HUD conserva lo scroll/foldout esistente. Verificata la leggibilità dei casi guidati nella Game View acquisita; MR-06 non è dichiarato risolto globalmente. Player build, profiling e studio di usabilità restano nei gate successivi.
- Nessuna migrazione del salvataggio: nuova identità C1.L5 v1, vecchie versioni/record conservati. Nessuna nuova dipendenza esterna.
- La diagnostica della quota a t<T è una scelta esplicita coerente con il contratto generale; non è una certificazione del terminale o del moto continuo.

## F14 MULTIDIMENSIONAL GATE

| # | Domanda | Esito ed evidenza |
|---|---|---|
| 1 | Il modello usa realmente n=6, m=2? | **SÌ** — descriptor, Euler a sei righe, test degli input accoppiati |
| 2 | I consumer generici supportano dimensioni diverse da 2/1? | **SÌ** — stessi editor/resolver/HUD/sessione con 6/2, sette viste configurate; nessun ramo per ID drone nei presenter comuni |
| 3 | u₁/u₂ restano componenti di Vector(2)? | **SÌ** — binding originali vettoriali, component selection 0/1, scalar implicito rifiutato |
| 4 | Output distinto dallo stato? | **SÌ** — h seleziona [0,2], p=2, output salvato anche a T |
| 5 | Proiezioni dichiarate? | **SÌ** — assi/unità/scale/omissioni XY e sei grafici; pose planare esplicita |
| 6 | Unità corrette per componente? | **SÌ** — metadata del modello, formule, output/reference, pesi e g validati |
| 7 | Segni protetti da oracoli indipendenti? | **SÌ** — hover, somma, differenza ±, gravità, tilt ±, Euler noto e tre passi |
| 8 | J inerzia distinto dal performance index? | **SÌ** — `MomentOfInertia`, nome/unità live, costo totale ℓ_tot in dati/HUD/grafico |
| 9 | Condizioni su outcome, non solo edit? | **SÌ** — metric.evidence baseline/next, componenti/segni/campioni; probe negativi F14 e MR passanti |
| 10 | HUD e visualizzazione sulla stessa revisione? | **SÌ** — identità della pubblicazione verificata in PlayMode e a ogni step nativo; A conserva configurazione/campioni dopo B/failure |
| 11 | Regressioni F00–F13 passanti? | **SÌ** — tutti i 425 EditMode + 101 PlayMode precedenti conservati/passanti; totale 484+108 |
| 12 | F15 non iniziata? | **SÌ** — nessuna lesson C1.L6 o quadratura/solver/pipeline futura introdotta |

Tutti i criteri F14 del piano e i dodici punti del gate sono soddisfatti. Nessun blocco architetturale residuo per avviare la fase successiva; restano soltanto i limiti dichiarati nel loro scope futuro.

**READY FOR F15**
