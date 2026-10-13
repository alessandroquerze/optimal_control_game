# F13 — costo, vincoli e curva desiderata

Verifica conclusa l'11 ottobre 2026, Unity **6000.3.8f1**. **F13 completata: C1.L3 e C1.L4 di produzione**, accessibili dal Bootstrap normale. Compilazione senza errori o warning C#; **388/388 EditMode, 97/97 PlayMode**, zero falliti o saltati. Conservati e passanti tutti i 356+89 casi di F12. Nessuna implementazione F14/F15.

**AF08-11: RESOLVED FOR REQUIRED F13 SCOPE.** Il riferimento campionato e i contributi nominati sono contratti comuni, puri e immutabili; matrici piene, costi arbitrari, solver e quadratura continua restano futuri.

## Scope e prerequisiti verificati prima dell'implementazione

La specifica operativa è la sezione F13 di `OPTCON_IMPLEMENTATION_PLAN.md`: produrre **C1.L3–L4**, costo/vincoli e riferimento/traiettoria, con diagnostiche h/g, regione ammissibile, pesi/limiti modificabili, riferimento a gradino e confronto desired/actual. Il raggruppamento comprende due lesson, anche se il prompt usa talvolta il singolare. Dipendenze F07 e F12: costo scomposto, confronto A/B e nominali/traiettorie disponibili; F12 completata con 356+89 test e manifest applicativo verificato. I 485 file applicativi di partenza coincidevano con il manifest F12.

Letti vision, curriculum, architettura, piano, audit F08, remediation AF08, report F09–F12 e report precedenti pertinenti a sessione, resolver, visualizzazione e costi. Confermati i confini Core → Simulation → sessione → resolver → Formula/HUD e visualizzazione. L'esercizio ottimizzato di C1.L4 è assegnato a **F28**, il drone a F14 e continuo/quadratura a F15. F13 configura e valuta un problema, senza cercarne automaticamente l'ottimo.

La policy F09 rimane **consultiva**: C1.L3 dichiara C1.L2; C1.L4 dichiara C1.L3. Il menu segnala i prerequisiti mancanti e consente l'esplorazione. Non è stato aggiunto un blocco né cambiata la policy dei capitoli.

## Fonti matematiche e adattamento

Fonte locale: `OPTCON-RL_introduction_optimal_control_2026_09_14.pdf`, 36 pagine fisiche; SHA256 `ae169f3492dc0915d0777d62e90184107d7017aee03b8620bbfe0aee9bd3a469`. Ricontrollati integralmente gli intervalli pertinenti, testo ed equazioni; ispezionate anche le pagine renderizzate. Gli asset conservano riferimenti al documento e alle pagine.

| Pagina PDF / slide | Contratto usato |
| --- | --- |
| 14 / 13 | decisione z, costo, h=0 e g≤0 |
| 16 / 15 | norma quadratica pesata; erratum: il secondo peso dell'espansione è w₂, non il secondo w₁ stampato |
| 19–21 / 18–20 | h_t=f_t−x_(t+1); J come somma degli stage più terminale; x₀ fissato; r(x₀,x_T)=0; g_t per t<T |
| 22–23 / 21–22 | tracking con fattore ½, Q/Q_T semidefinite positive, R definita positiva; terminale distinto senza u_T |
| 24–25 / 23–24 | output y=h_t(x,u); curva desiderata stato/input e output desiderato ottenuto dalla mappa di output; desiderato non necessariamente compatibile con la dinamica |
| 35 / 34 | gradino fra equilibri del pendolo, velocità desiderata nulla e coppia d'equilibrio sui due lati |

Il pendolo conserva Euler esplicito F11/F12, **δ=.02 s**. F13 usa una **somma discreta senza fattore δ**, non un integrale. Il preset del gradino ha T=20, salto al campione 10 da θ_des=0 a .5 rad, ω_des=0 e u_des=9.81 sin(θ_des) per m=l=1, g=9.81. È un adattamento discreto dichiarato della pagina 35. I lati sono equilibri del preset, il salto no. Modificare un campione desiderato o il modello non rigenera implicitamente gli altri campioni congelati: l'authoring e le ipotesi lo dichiarano.

La lettera h è usata dal PDF sia per l'output sia per l'uguaglianza impilata. Le formule le contestualizzano; i binding restano separati: `reference.output` e `constraint.h`/`constraint.desired-h`.

## Architettura del reference e del risultato

`SampledReference` è un oggetto Core puro e immutabile: **T+1 stati desiderati e T input desiderati**, unità per componente, dimensioni e dominio verificati. Include esplicitamente il riferimento terminale; nessun u_T. Copia le collezioni ricevute; i `NumericVector` sono già immutabili. Shape, unità, orizzonte o terminale incompatibili sono rifiutati, senza conversione o ricampionamento.

`SampledReferenceDefinition` congela i dati editoriali una volta durante la composizione. Per lo scope F13 il riferimento è composto nel `QuadraticTrackingCost`, esposto attraverso `SimulationConfiguration.Reference` e posseduto dalla medesima configurazione del risultato. Non esistono tabelle di riferimento indipendenti in HUD o renderer.

`SimulationResult` possiede:

- `CostEvaluation`: contributi stage, prefissi, terminale e J;
- `ReferenceEvaluation`: riferimento, errori di stato x−x_des e output desiderati calcolati dalla mappa di output del modello congelato;
- `ConstraintEvaluation`: diagnostiche h, h_des, g e r, con la configurazione che le ha prodotte.

Tutti si riferiscono alla stessa history/revisione. Il seek seleziona campioni salvati; i consumer non eseguono dinamica o costo. Se l'output richiede un controllo, y_des(T) è N/A; per il pendolo l'output è θ, dimensione 1, mentre lo stato desiderato è [θ,ω], dimensione 2. Non è stata introdotta l'identità implicita output=stato.

Il vecchio `ReferenceState` resta una vista di compatibilità del campione zero. Il contratto runtime indicizzato è **`StateReferenceAt(sample)`**: resolver e target visuale lo usano. Il costruttore legacy a riferimento costante conserva parametri e comportamento F07/F08.

## Cost contract e contributi

Un'unica implementazione produttiva della somma quadratica valuta:

```text
e_t = x_t − x_t^des
ℓ_t = ½ Σ_i Q_ii e_t[i]² + ½ Σ_j R_jj (u_t[j] − u_t^des[j])², t<T
ℓ_T = ½ Σ_i Q_T,ii (x_T[i] − x_T^des[i])²
C_t = Σ_(k=0)^(t−1) ℓ_k
J = C_T + ℓ_T
```

Pesi diagonali, Q/Q_T≥0 e R>0; costo adimensionale `[1]`, pesi con le unità inverse al quadrato pertinenti. Il provider conserva l'ID legacy `cost.quadratic-tracking.diagonal.v1`. Il nuovo overload **`EvaluateTerminal(T, state)`** permette al contratto di ricevere l'indice terminale; il default delega alla vecchia API per le implementazioni compatibili. Il provider campionato verifica che l'indice sia il suo T. L'interfaccia `ICostModel` espone anche questo overload: eventuali implementazioni esterne dirette dell'interfaccia devono implementarlo; nel progetto non ve ne sono oltre a `CostConfiguration`.

`CostTerm` porta ID stabile, nome, simbolo, descrizione, unità e valore finito. Il contenitore immutabile `CostContribution` porta **ruolo Stage/Terminal, SampleIndex e RevisionId** comuni ai termini che contiene; non vengono replicate copie di questo contesto nei consumer. Valida termini non nulli, ID unici e somma finita. Gli asset F13 dichiarano `state-deviation` e `control-deviation`, senza assumere nomi Tracking/Effort nel presenter.

I vecchi constructor/accessor `Tracking`/`Effort` e metriche legacy restano per il prototipo. Le nuove metriche `cost.contribution.<id>` e `cost.terminal-contribution.<id>` risolvono i contributi per identità; un ID inesistente è **Invalid**, un contributo noto ma assente nel ruolo/campione è **NotApplicable**. Il terminale campionato contiene soltanto il contributo di stato: non viene fabbricato un costo su u_T. Per la compatibilità il terminale legacy mantiene l'addendo effort nullo già esistente.

Una variazione di Q/R/Q_T o reference produce un nuovo costo sotto un nuovo criterio, senza cambiare gli stati a input/modello invariati. Non si assegna un'etichetta di ottimalità al confronto di J sotto criteri diversi.

## Vincoli e dominio di ammissibilità

`ConstraintConfiguration` è puro e immutabile, con ID unici e componenti/unità validate sul modello. Offre il minimo comune necessario:

- limite simmetrico di una componente di stato/input: **g=|componente|−limite≤0**, limite finito strettamente positivo;
- uguaglianza fra estremi: **r=x_T[i]−x₀[i]−spostamento=0**;
- residui dinamici **h_t=f_t(x_t,u_t)−x_(t+1)**, calcolati tramite il provider reale;
- residui dinamici della curva desiderata, valutati separatamente dagli errori di tracking.

L'asset di produzione limita la coppia e richiede r=θ_T−θ₀=0. La tolleranza diagnostica è 1e−9 per componente nelle rispettive unità; non viene calcolata una norma che mescola unità diverse. `IsFeasible` include h/g/r del rollout. `DesiredSatisfiesDynamics` riguarda **soltanto h_des**, senza affermare che il desiderato soddisfi ogni vincolo.

Il limite è duro nel problema matematico, ma questa fase **diagnostica la violazione senza enforcement**: non satura input, non aggiunge penalità a J, non modifica il rollout e non esegue un solver. Nessun attuatore abilitato: input richiesto/applicato coincidono, come dichiarato nel termine HUD. L'utente può completare l'attività didattica riconoscendo correttamente una curva inammissibile; completion didattica non significa fattibilità della curva.

## Formula, metriche e visualizzazione

Cinque nuove definizioni editoriali: `PerformanceIndex`, `SampledTracking`, `TerminalTracking`, `ConstraintResiduals`, `DesiredCurve`. Il performance index riusa il layout strutturato della sommatoria; le formule contestuali e la spiegazione precedono la previsione/domanda e l'editing pertinente.

| Binding | Tipo e indice |
| --- | --- |
| `reference.state`, alias `state.reference` | Vector n, unità stato, campione corrente incluso T |
| `reference.input` | Vector m, unità input, t<T; N/A a T |
| `reference.output` | Vector p della mappa di output; disponibilità esplicita |
| `reference.state-error` | Vector n, unità stato, campione corrente |
| `constraint.h`, `constraint.desired-h` | Vector n, unità stato, t<T; N/A a T |
| `constraint.g.<id>` | Scalar nell'unità della componente, t<T |
| `constraint.r.<id>` | Scalar con SampleIndex=T, scope Terminal |
| `constraint.feasible`, `constraint.desired-dynamics-valid` | Scalar `[1]`, Snapshot; il secondo verifica solo la dinamica |
| `cost.contribution.<id>` | Scalar nell'unità costo, stage corrente |
| `cost.terminal-contribution.<id>` | Scalar nell'unità costo, Terminal/T |
| `parameter.<target>` | Scalar/unità dai parametri della stessa configurazione congelata |

La regola A02 rimane unica nel resolver: Vector dimensione 1 resta Vector; solo una selezione esplicita valida produce Scalar. Shape, componenti, unità, scope, disponibilità e revisione sono verificati. Gli scope legacy del costo totale rimangono compatibili, con SampleIndex nullo per J.

`ReferenceTrajectoryView` riceve il `SessionState` da `ExperimentVisualization`, proietta i campioni desiderati salvati e usa **tratteggio + diamante + legenda**, rispetto a linea continua e sfera del rollout. Le frazioni usate per disegnare i trattini sono uno stile grafico, non nuovi campioni o integrazione. `TargetVisualizer` legge il reference dello stesso campione. La curva desired è esplicitamente etichettata come potenzialmente incompatibile con f.

`ConstraintRegionVisualizer` riceve la stessa pubblicazione, trova un bound per ID, disegna intervallo/bordi e marker del valore realmente applicato, mostra il g già salvato e una scritta di violazione. Il rettangolo rappresenta un **intervallo unidimensionale di coppia**, non una regione di stati o una traiettoria ammissibile completa. A T il marker input e g sono N/A. I riferimenti visuali riusano il semantic registry: `reference.state`, `constraint.path`, stage/terminale e state/input; nessun lookup di renderer concreto dal Formula system.

Il nuovo prefab contiene solo la composizione richiesta da F13. Le nuove legende sono state posizionate dopo ispezione delle catture; il marker desiderato usa una forma a diamante anche quando proviene dal target preesistente. Restano le limitazioni generali di densità/layout assegnate al polish, senza rifacimento del viewport.

## Lesson di produzione e progressione

| Contenuto | Identità / authoring |
| --- | --- |
| Corso | `optimal-control`, `OptimalControlCourse.asset`, invariato |
| Chapter | `C1`, `Chapter01.asset`, ora C1.L1/L2/L3/L4 nell'ordine |
| C1.L3 | `C1.L3`, v1/schema1, **Costi, prestazione e vincoli**, esperimento `c1.costs.pendulum` |
| C1.L4 | `C1.L4`, v1/schema1, **Riferimento desiderato e rollout manuale**, esperimento `c1.desired.pendulum` |
| Reference | `c1.reference.origin`, `c1.reference.step` |
| Costi | `c1.cost.productioncost`, `c1.cost.desiredtracking` |
| Vincoli | `c1.constraints.bounds` |

C1.L3 ha **9 attività**: successioni/decisioni e J; scarto e tracking; Q; R; Q_T/terminale; limite duro; endpoint; costo minore distinto da fattibilità; terminale della somma. C1.L4 ha **7 attività**: desiderato vs rollout; salto non valido; input e rollout manuale; confronto allo stesso campione; riferimento differente con A conservato; reference/costo/vincolo distinti; reference terminale.

Ogni ingresso ricrea autonomamente la configurazione necessaria: Reset, seek, input e confronto quando pertinenti. **Previous non dipende dalle modifiche dello step precedente** e non accredita quelle azioni come interazione dello studente. Wrong answer e no-op non abilitano la completion. Restart, reset, uscita incompleta e riapertura sono verificati.

La persistenza F09 rimane separata dalla sessione: chiave corso/lesson/versione/schema, solo completion didattica valida. Prototype e fixture F10 non accreditano C1.L3/L4 o i loro prerequisiti. Nessun nuovo storage o migrazione; percorso normale `Application.persistentDataPath/OptCon/progress-v1.json`. La prova nativa isola soltanto il percorso del `FileProgressStore`, senza sostituire corso/lesson/experiment. Una riapertura ricrea sessione, reference e confronto runtime iniziali, pur conservando lo stato persistente completato.

## Immutabilità, errori e contratti storici

Result A conserva reference, Q/R/Q_T, T, history e contributi A dopo B; `ComparisonSnapshot` continua a possedere il risultato completo. Il FormulaPresenter può renderizzare esplicitamente A senza rileggere B o asset. Modificare soltanto reference/costo non riscrive la history reale. HUD, target, nuova curva desired e regione ammissibile leggono l'esatta pubblicazione condivisa con i renderer esistenti.

Una candidatura fallita conserva **l'intero ultimo risultato valido**, compresi reference e costo; editing input bloccato secondo AF08-04 e recupero via Reset. Una reference finita troppo grande per la proiezione produce `VisualizationStatus.NonRepresentable`: il risultato matematico resta condiviso, i renderer vengono nascosti e Reset recupera.

Non sono stati modificati `SessionNotificationScope`, il dispatch controllato, generation/lifetime binding, navigazione/teardown, readiness, la macchina `LessonSession` o la registrazione della completion. `SimulationSession` aggiunge soltanto il routing del parametro di limite verso la configurazione pura. Le regressioni coprono A01–A04 e AF08-01–07, provider custom, fallimento Enter/Presentation e navigazione rientrante. Il pendolo conserva δ=.02 s; le dinamiche index-only conservano t e nessun δ/τ.

## Test, oracoli ed esiti

| Suite finale | Passati | Nuovi F13 | Precedenti conservati |
| --- | ---: | ---: | ---: |
| EditMode | **388/388** | 32 | 356 |
| PlayMode | **97/97** | 8 | 89 |

Assemblies EditMode: Content 64, Formula 155, Lessons 80, Mathematics 81, Visualization 8. PlayMode Navigation 97. Zero falliti, saltati o inconclusive. Verificato il fullname di ogni caso precedente, senza test rimossi o ignorati. XML completi e hash dei log in [F13_EVIDENCE](F13_EVIDENCE/TEST_SUMMARY.txt).

Test nuovi:

- `ReferenceCostConstraintTests`: **17 casi**, reference/costo/vincoli puri, lookup/seek/tipi/componenti/unità, shape/T/terminale errati, metadati, immutabilità, failure, index-only e output distinto dallo stato;
- `ProductionCostReferenceTests`: **15 casi**, catalogo reale e fonti, sequenza/formule, completion/persistence per entrambe le identità, Previous per ogni step, wrong/no-op, domini di editing, reference A/B e quattro configurazioni editoriali impossibili;
- `ProductionCostReferenceRuntimeTests`: **8 casi** sul Bootstrap salvato, UI effettiva, stesso risultato nei consumer, limite e marker, seek/terminale, failure/reset, navigazione/restart e persistenza.

Oracolo indipendente T=2, x=[1,2,3], u=[1,1], x_des=[0,1,1], u_des=[0,2], Q=R=Q_T=2: stage **2,2**, contributi stato **1,1**, controllo **1,1**, prefissi **0,2,4**, terminale **4**, J=**8**. Con reference terminale 2 e Q=4, B ha J=**7**, A resta 8; candidatura successiva con peso invalido non altera B. Expected calcolati a mano, non dal provider testato.

Altri oracoli: g con limite 1 vale **−1,0,+1** per input 0,1,2; h_des della curva additiva vale [−1,2], mentre h del rollout è zero; r di ritorno vale 2. Nel gradino pendolo h_des(9)=[**−.5,0**], output desiderato dimensione 1 e stato dimensione 2. A reference/input zero reali, reference terminale [.5,0] e Q_T=[5,1] danno terminale **.625**. Q₀₀ 2→4 a θ₀=.35 raddoppia il contributo .1225→**.245**; R .2→.4 con scarto input 1 produce **.2**; limite 2→.5 con u=1 produce **g=.5**, senza clamp e senza cambiare J.

Le due modifiche ai test precedenti aggiornano soltanto le liste esatte di lesson del catalogo/menu da L1–L2 a L1–L4 (`ProductionLinearizationTests`, `ProductionIntroductionRuntimeTests`). Non sono stati cambiati expected matematici, lifecycle o requisiti di progressione per far passare l'implementazione.

Durante lo sviluppo: una validazione inizialmente troppo ampia respingeva un vecchio draft statico, ed è stata limitata al nuovo contratto dei vincoli discreti; la prima suite aveva 370/371. Un nuovo test PlayMode ha rilevato che lo step di editing reference non permetteva il seek richiesto: abilitato nei suoi dati editoriali; gli altri 96 casi erano verdi. Questi difetti sono corretti nelle suite finali complete. Un helper esterno di acquisizione è stato corretto per uscire a fine percorso e attendere il frame della cattura prima dello scroll; non era un difetto applicativo e non viene consegnato nel progetto.

## Smoke test e prova nativa

Eseguiti in Unity nativo con grafica, partendo dalle scene e dal catalogo serializzati:

1. Bootstrap → C1 → C1.L3, uscita incompleta/riapertura, candidatura costo fallita, blocco input, NonRepresentable/recupero, Restart, tutte le nove attività e completion valida.
2. Menu → C1.L4, tutte le sette attività, wrong answers in ogni attività, edit reference .5→.8 al campione 10, seek 9/10, identità di pubblicazione, A conservato e Previous con nuova azione richiesta. Terminale T20 senza input/stage, completion.
3. Menu con solo C1.L3/L4 completate, riapertura fresca, stop/start Play e **nuovo processo Unity**: file store ripristina soltanto quei due record v1/schema1; nessuna promozione di L1/L2 o prototipo.
4. Regressione nativa C1.L1 → tutte le 12 attività C1.L2: equilibri, Jacobiani, perturbazioni, true/linear, errore, Previous, failure/NonRepresentable/reset, progressione e riapertura.
5. Regressione nativa F08/pendolo/costi/A-B/index-only, gating e failed entry; regressione F10 statica con input z e snapshot. Tutte PASS; zero Error/Exception/Assert runtime.

Le interazioni sono **automatizzate attraverso i callback reali della UI nell'Editor nativo**, con asserzioni sul risultato; le catture Game View sono state ispezionate visivamente. Non si dichiara una sessione di click di una persona, uno studio di usabilità o un player build. Suite finali e compilazione sono eseguite senza helper temporanei. I 540 file applicativi consegnati coincidono byte per byte tramite SHA256 con la copia verificata; 13 assembly aciclici, Core/Simulation `noEngineReferences=true`. Packages, ProjectSettings, fonti PDF, vecchi report/evidenze e vecchi asset di produzione/prototipo sono invariati.

Archiviate **23 catture originali PNG**, XML, risultati nativi, manifest e inventario. Esempi:

- [Catalogo Bootstrap](F13_EVIDENCE/production-menu-new.png), [peso e scomposizione live](F13_EVIDENCE/L3-weight-cost-live.png), [vincolo e g=.5](F13_EVIDENCE/L3-path-limit-term-live.png);
- [salto desiderato e residuo](F13_EVIDENCE/L4-jump-term-live.png), [reference .8 e costo della stessa revisione](F13_EVIDENCE/L4-change-reference-term-live.png), [terminale e N/A](F13_EVIDENCE/L4-terminal-term-live.png);
- [completion persistente](F13_EVIDENCE/production-progress.png), [riapertura nuovo processo](F13_EVIDENCE/production-process-reloaded.png), [NonRepresentable](F13_EVIDENCE/production-nonrepresentable.png).

## Procedura manuale in Unity

1. Aprire `Assets/OptimalControl/Scenes/Bootstrap.unity` e premere Play. Nel corso **Optimal Control**, Chapter 1, compaiono quattro lesson. Se il profilo è nuovo, gli avvisi dei prerequisiti sono consultivi. Non cambiare il corso assegnato a Bootstrap.
2. Aprire **Costi, prestazione e vincoli**. Leggere istruzione e formula prima della domanda. Scorrere il pannello sinistro fino a **MATEMATICA LIVE** per selezionare i termini richiesti; il significato e il valore compaiono sotto. Una risposta errata o una selezione incompleta lascia Avanti disabilitato.
3. Nel foldout **Costo discreto, limiti e confronto A/B → Pesi, riferimento, limiti e orizzonte**, modificare i campi abilitati dallo step e confermare con Invio/perdita focus: Q[0] 2→4, R[0] .2→.4, Q_T[0] 5→10 nelle rispettive attività. Confrontare contributi nominati, J e marker stage/terminale. Gli stati non cambiano per questi edit.
4. Nello step del limite, portare **constraint.torque.limit** da 2 a .5. Il marker resta sull'input 1 N·m e fuori dall'intervallo; HUD/regione mostrano g=.5 e violazione. Lo step endpoint mostra r e h; quello successivo distingue ridurre J da soddisfare g. Portare il seek a **T=20** nell'ultimo step: x_T/reference/costo terminale presenti, u_t e stage N/A. Selezionare i termini richiesti e concludere.
5. Tornare al corso e aprire **Riferimento desiderato e rollout manuale**. Desiderato tratteggiato/diamante distinto da rollout continuo/sfera. Selezionare stato/output desiderati e h_des a t=9: [−.5,0], mentre h reale è zero.
6. Nello step manuale aprire **Input e parametri**, scegliere l'ambito **Coda**, impostare input 1 e usare Step. Il core genera il rollout; il gradino non è un rollout realizzabile. Gli ingressi degli step successivi ricostruiscono da soli i preset necessari.
7. Nello step **Stesso controllo, desiderio diverso**, A è già catturata editorialmente. Nel pannello costo modificare **cost.reference.state.10.0** da .5 a .8. Selezionare x_t^des: valore [.8,0], marker desiderato e costo B cambiano insieme; A e la traiettoria reale conservano i propri dati. Seek 9 mostra reference zero, seek 10 quello modificato. Non cambia l'intera curva né si calcola un controllo ottimo.
8. Usare **Indietro** e tornare all'editing: reference .5, confronto e t10 vengono ripristinati, la modifica va eseguita nuovamente. **Ricomincia** riparte dal primo step; **Reset** ripristina la configurazione dell'esperimento secondo la policy esistente. Una semplice risposta corretta senza la nuova azione non avanza.
9. All'ultimo step portare t20: reference [.5,0] a T, costo terminale proprio, input e stage N/A. Concludere, tornare al menu, riaprire e arrestare/riavviare Play: progresso completato persistente, runtime fresco al primo step e nessun confronto residuo.
10. Per verificare il recupero liberamente, usare **Esplorazione libera**, tentare R[0]=0: diagnostica di errore, ultimo risultato valido conservato e input non editabile; **Reset** recupera. T è disabilitato per questi reference campionati: richiede una nuova curva coerente, senza resampling implicito.

## File creati e modificati

Inventario esatto, inclusi tutti i `.meta` e i documenti/evidenze, in [file-inventory.json](F13_EVIDENCE/file-inventory.json). Manifest SHA256 di tutti i 540 file applicativi in [application-manifest.json](F13_EVIDENCE/application-manifest.json). Confronto rispetto allo snapshot iniziale dei 647 file Assets/Docs/Packages/ProjectSettings, senza repository Git disponibile. Nessun file rimosso.

**Creati nell'applicazione: 55 file**, compresi `.meta`; percorsi sotto `Assets/OptimalControl/`:

- `Runtime/Core/Costs/SampledReference.cs`, `Runtime/Core/Constraints/ConstraintConfiguration.cs`;
- `Runtime/Simulation/ReferenceEvaluation.cs`, `Runtime/Simulation/ConstraintEvaluation.cs`;
- `Runtime/Content/Definitions/SampledReferenceDefinition.cs`, `Runtime/Content/Definitions/ConstraintDefinition.cs`;
- `Runtime/Visualization/ReferenceTrajectoryView.cs`, `Runtime/Visualization/ConstraintRegionVisualizer.cs`;
- `Data/References/ConstantOrigin.asset`, `Data/References/StepEquilibria.asset`;
- `Data/Costs/ProductionCost.asset`, `Data/Costs/DesiredTracking.asset`, `Data/Constraints/PendulumBounds.asset`;
- `Data/Experiments/PendulumCosts.asset`, `Data/Experiments/PendulumDesired.asset`;
- `Data/Lessons/Chapter01/C1_L3.asset`, `Data/Lessons/Chapter01/C1_L4.asset`;
- `Data/Formulas/Chapter01/PerformanceIndex.asset`, `SampledTracking.asset`, `TerminalTracking.asset`, `ConstraintResiduals.asset`, `DesiredCurve.asset`;
- `Prefabs/PendulumReferenceConstraintsScenario.prefab`;
- `Tests/EditMode/Formula/ReferenceCostConstraintTests.cs`, `ProductionCostReferenceTests.cs`, `Tests/PlayMode/ProductionCostReferenceRuntimeTests.cs`;
- relativo `.meta` di ciascuno dei 26 file sopra e tre `.meta` delle nuove cartelle `Data/References`, `Data/Constraints`, `Runtime/Core/Constraints`.

**Modificati nell'applicazione: 20 file**, sotto `Assets/OptimalControl/`:

| File | Motivo |
| --- | --- |
| `Data/Chapters/Chapter01.asset` | esposizione delle due nuove lesson in produzione |
| `Editor/CatalogValidator.cs` | visita/validazione reference, vincoli e relativi parametri d'ingresso |
| `Runtime/Content/Bindings/RuntimeMetricResolver.cs` | lookup dei nuovi dati salvati e contributi per ID |
| `Runtime/Content/Definitions/ExperimentDefinition.cs` | riferimento editoriale ai vincoli |
| `Runtime/Content/Definitions/ModelDefinition.cs` | composizione della configurazione pura dei vincoli |
| `Runtime/Content/Definitions/QuadraticTrackingCostDefinition.cs` | reference campionato e metadati dei contributi, ramo legacy preservato |
| `Runtime/Core/Costs/CostConfiguration.cs` | reference indicizzato, terminale T e contributi generici |
| `Runtime/Core/Costs/QuadraticTrackingCost.cs` | stessa formula su reference corrente/terminale, parametri campionati |
| `Runtime/Simulation/CostEvaluation.cs` | validazione T, terminale indicizzato, contesto revisione/ruolo |
| `Runtime/Simulation/SimulationConfiguration.cs` | proprietà/validazione/copie immutabili di reference e vincoli |
| `Runtime/Simulation/SimulationResult.cs` | valutazioni salvate reference/vincoli nella revisione |
| `Runtime/Simulation/SimulationSession.cs` | routing SetParameter dei limiti |
| `Runtime/Lessons/LessonConditionRegistry.cs` | condizioni generiche su diagnostiche già salvate |
| `Runtime/UI/CostPanelPresenter.cs` | campi reference/limiti, contributi nominati, policy T e legende |
| `Runtime/UI/LessonStepPresenter.cs` | visibilità dei soli parametri editabili nello step |
| `Runtime/Visualization/ExperimentVisualization.cs` | fanout controllato alle due nuove viste |
| `Runtime/Visualization/TargetVisualizer.cs` | reference indicizzato sul campione corrente |
| `UI/LessonHud.uxml` | titoli/label del pannello costo e limiti |
| `Tests/EditMode/Formula/ProductionLinearizationTests.cs` | lista attesa delle quattro lesson di produzione |
| `Tests/PlayMode/ProductionIntroductionRuntimeTests.cs` | lista attesa dei quattro pulsanti del menu |

Documentazione: creati questo report e `F13_EVIDENCE/`; aggiornati lo stato riepilogativo e la sezione F13 del piano per registrarne il completamento. Nessuna modifica alle altre fasi o ai report storici. Gli helper di authoring/verifica sono temporanei esterni, assenti dagli asset consegnati.

## Limiti residui e deviazioni

Nessuna deviazione di scope dal piano. Le modifiche minime a Core/Simulation sono necessarie al gate AF08-11: reference campionato, terminale indicizzato, contributi nominati e diagnostiche dei vincoli. Non sono workaround nei presenter.

Restano volutamente futuri: pesi a matrice piena, costi arbitrari, reference analitico/continuo e ricampionamento, reference autonomo in un esperimento senza costo, famiglie generali di vincoli, attuatori/saturazione, enforcement/ottimizzazione e quadratura continua. L'endpoint qui è una differenza fra componenti estreme, il path constraint è un limite simmetrico di componente; non sono un linguaggio universale di vincoli. Il valore minimo del limite è strettamente positivo. Il prefab è una composizione pendolo, mentre i contratti e le nuove viste non dipendono dal capitolo.

Il layout/scroll e la densità del viewport ereditano i limiti storici del polish; alcune etichette lunghe richiedono scroll. Le legende nuove non coprono i valori essenziali nelle catture verificate. Nessun player build/profiling/studio di usabilità anticipato. Nessun problema bloccante rimasto per lo scope F13.

## Review didattica

Verificati tutti gli otto punti richiesti: reference spiegato come desiderio; tratteggio/diamante distinto da rollout; limite distinto da target; formula dello stage prima dell'edit; terminale come valutazione dell'esito; previsione dell'effetto del reference attraverso e_t; previsione del peso attraverso ½Qe²/½R(u−u_des)²; formula contestuale disponibile prima della domanda. Le attività richiedono selezioni, edit effettivi e risposte coerenti, non soltanto avanzamento del testo. Non viene ripetuta integralmente C1.L1/L2.

## F13 REFERENCE/COST GATE

1. **Il reference può essere interpretato senza assumere che sia costante? SÌ.** `SampledReference` ha T+1/T campioni tipizzati e unità; il preset a gradino e gli edit di un solo campione sono verificati. La vista legacy sample-zero è esplicitamente distinta dall'API indicizzata.
2. **Stage e terminal usano il reference del campione corretto? SÌ.** Stage legge t; terminale riceve T e legge reference[T]. Oracolo J=8, seek e terminale indipendenti; nessun u_T o stage T.
3. **Result A mantiene reference/cost A dopo B? SÌ.** Configurazioni e valutazioni immutabili conservate, test e catture A/B; fallimento successivo non perde l'ultimo risultato valido.
4. **HUD e visualization leggono la stessa revisione? SÌ.** Identità esatta del `SessionState`/Result asserita in PlayMode e prova nativa, compresi nuove viste, target, costo e binding reference.
5. **Reference e constraint restano distinti? SÌ.** Reference indica desiderio; costo misura scarto; g/r impongono condizioni del problema e h verifica la dinamica. Stringere il limite non cambia J/input; abbassare J non elimina una violazione.
6. **Il cost contract è ancora hardcoded su Tracking/Effort? NO.** Termini per ID/metadati e contesto stage/terminale/indice/revisione; F13 usa identità diverse. Accessor e alias vecchi sono soltanto compatibilità per il preview; il provider produttivo resta intenzionalmente quadratico diagonale.
7. **Quali generalizzazioni restano volutamente future?** Matrici piene, costi arbitrari, reference continuo/resampling/cost-free, vincoli generali, attuazione/enforcement e solver; quadratura F15 e ottimizzazione C1.L4 F28. Non si dichiara AF08-11 chiuso per queste capacità.
8. **Tutte le regressioni F00–F12 passano? SÌ.** 356+89 casi precedenti conservati e verdi nelle suite 388+97; percorsi nativi C1.L1/L2, F08/pendolo/index-only e F10/statico passanti; contratti di sessione/persistenza/provider e fallimenti preservati.

**READY FOR F14.** F14 non iniziata.
