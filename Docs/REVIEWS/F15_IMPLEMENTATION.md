# F15 — Continuo e trasferimento del pendolo

Data: 13 ottobre 2026. **F15 completata e verificata. F16 non iniziata.** Unity 6000.3.8f1. Corso di produzione `optimal-control` v1, Chapter `C1`, lesson **C1.L6 v1/schema1**, esperimento `c1.transfer.pendulum` v1/schema1. Dodici attività raggiungibili dal Bootstrap salvato, senza sostituzione del corso. Prerequisito `C1.L5` consultivo secondo F09.

## Scope estratto dal piano prima delle modifiche

Autorità: piano → curriculum → PDF → report dei contratti già verificati. Letti integralmente i quattro documenti principali, FULL_AUDIT_AFTER_F08, MINI_REVIEW_F11_F13, MR01_REMEDIATION_BEFORE_F14, F13_IMPLEMENTATION, F14_IMPLEMENTATION e il PDF Introduction; riesaminati i contratti precedenti necessari. Baseline F14: **484 EditMode + 108 PlayMode**.

| Voce F15 | Specifica e decisione operativa |
|---|---|
| Lesson/capitolo | C1.L6, chiusura del blocco Chapter 1 |
| ID/versione | Il piano prescrive C1.L6 ma non una versione iniziale: nuovo contenuto v1/schema1, senza alterare le versioni precedenti |
| Obiettivi | Distinguere problema continuo, campionamento Euler e approssimazione del funzionale; equilibri, riferimento e rollout manuale |
| Modello | Pendolo esistente, due equilibri e reference a gradino; riuso di corpo, fase, comandi e sessione |
| Capability nuove | Semantica esplicita del costo continuo; griglia fisica e leggi campionabili; quadratura; confronto a H comune |
| Formule | Dinamica continua/Euler, equilibri, desired curve, integrale, densità, somma rettangolare, cumulativo e terminale |
| Quadratura | Il piano non nomina una regola specifica. Scelta minima dichiarata: rettangolo sinistro coerente con Forward Euler, nessun framework di integrazione |
| Confronto | H fisso, δ variabile, N variabile; stesso problema fisico e campionamenti indipendenti |
| Completion/verifica | Esiti numerici e risposte, integrale noto, raffinamento, equilibri, validazione e controllo delle sei lesson; rinvii F28 visibili |
| Fuori scope | F16 superfici/derivate di obiettivi/QP; trasferimento ottimizzato F28; nessun solver, ottimizzatore o integratore generale |

Prerequisiti F12/F14 verificati: produzione precedente e report completi, test conservati, pendolo/equilibri/reference/confronto disponibili. La formula integrale usa il rendering strutturato già introdotto; non è stata creata una seconda UI.

## Fonte matematica e notazione

Fonte: [Introduction PDF](../OPTCON-RL_introduction_optimal_control_2026_09_14.pdf), pagine fisiche **27 e 31–36** (slide 26 e 30–35). Controllati direttamente anche i rendering delle pagine pertinenti. SHA256: `ae169f3492dc0915d0777d62e90184107d7017aee03b8620bbfe0aee9bd3a469`.

- p.27: Forward Euler sullo stato vecchio.
- pp.31–32: problema continuo e decisione costituita da curve; il funzionale restituisce uno scalare.
- p.33: θ̇=ω, ω̇=−(g/l)sinθ+u/(ml²), output θ.
- p.34: equilibrio ω_e=0, u_e=mgl sinθ_e. Preservata la rettifica documentata: la prima riga deve indicare velocità nulla, non imporre ogni angolo uguale a zero.
- p.35: reference fra due equilibri, cambio a metà durata.
- p.36: L=q₁(θ−θ_des)²+q₂ω²+r(u−u_des)²; Φ=q_T1(θ−θ_des(H))²+q_T2ω². **Nessun fattore ½** in questa fonte.

Il tempo continuo della fonte viene indicato **τ**, la durata continua **H**, il numero di transizioni **N**; `t` resta indice discreto. Le API usano `DurationSeconds`, `StepSeconds`, `Transitions`. La relazione è **H=Nδ**, con N+1 stati e N input. Il playback continua a usare campioni/secondo di presentazione e non cambia la matematica.

Preset: H=2 s, δ=.1 s, N=20; scelte editoriali δ=.1/.05/.02 s → N=20/40/100. m=l=1, g=9.81; x₀=[0,0], input iniziale zero. Equilibri θ₁=0 e θ₂=.5 rad, ω=0; gli input sono ottenuti da `PendulumParameters.EquilibriumInput`, non inseriti come costanti nella UI. Secondo input ≈4.703164533707231 N·m.

## Architettura e contratti

Pipeline: configurazione fisica immutabile → griglia e campionamento → rollout → valutazione densità/quadratura → risultato immutabile → resolver → Formula HUD, viste e condizioni.

`CostSemantics` distingue `DiscreteAdditive` (default compatibile) e `ContinuousFunctionalLeftRectangle`. `ContinuousTrackingCost` restituisce densità running e Φ; `CostEvaluation` conserva separatamente `RunningDensities`, contributi integrati `Stages`, cumulativo e terminale. **L'unica conversione δ·densità è `LeftRectangleQuadrature.IntegrateCell`**. HUD, grafici e condizioni non effettuano integrazione, somma dei costi o valutazione della dinamica.

`WeightedSquaredError` condivide il calcolo diagonale: fattore .5 nel costo discreto esistente, fattore 1 nel nuovo funzionale. Le formule, gli asset e la semantica dei costi F07/F13/F14 rimangono invariati. Il costo noto del drone resta **35.226525**, senza δ. Il nuovo `RunningUnit` esplicita la differenza fra densità e costo.

`PhysicalTimeGrid` valida H e δ positivi/finiti, N in [1,10000], compatibilità H=Nδ con tolleranza relativa 1e−10. Non tronca una durata incompatibile per adattarla a N. Il terminale della griglia corrisponde a H.

`PhysicalStepProfile` conserva copie immutabili di nodi, valori e unità: legge costante a tratti, **continua da destra sui nodi**. Il reference vale il secondo equilibrio già a τ=H/2. `ContinuousSampling` conserva reference di stato/input e legge di controllo in secondi; ogni cambiamento di δ genera nuovi N+1/N campioni. Valida anche l'appartenenza degli array alle leggi fisiche congelate. Un array alterato non può dichiarare di provenire da una legge diversa.

L'editing di un input Single modifica l'intervallo fisico [τ_t,τ_(t+1)); Tail modifica [τ_t,H). Raffinare ricampiona quella stessa legge, senza copiare input per indice. Per questo esperimento H non è un campo libero: `SetHorizon` viene rifiutato; si modifica δ e si ricalcola N. Il campo δ consente altre griglie compatibili oltre alle tre scelte del cursore.

`PendulumDynamics.EvaluateDerivative` è l'unica implementazione della derivata continua; Euler la usa sullo stato vecchio. `PendulumTransferModelDefinition` riusa la factory e la validazione del pendolo tramite un piccolo hook di composizione in `ModelDefinition`. Non esistono switch per Chapter/lesson nel runtime comune.

`DiscretizationComparison` conserva A/B e verifica H, modello/versione/parametri/unità, x₀, identità delle leggi fisiche, pesi e vincoli. L'identità delle leggi è intenzionalmente conservativa: due oggetti ricostruiti separatamente non vengono certificati equivalenti. Cambiare pesi, modello, input o reference rende il confronto «stesso problema» falso. Le differenze numeriche restano mostrate sotto gli obiettivi propri, senza etichetta di ottimalità. Differenze non finite sono N/A.

Ogni A conserva δ_A, N_A, reference A, densità, contributi, Φ e totale; B conserva i propri. La sessione pubblica un unico `SessionState`; consumer visuali, HUD e Lesson Logic leggono quella pubblicazione. Il confronto nel pannello costi ora usa il resolver sullo snapshot completo, conservando il contesto A/B, invece di perderlo selezionando soltanto il risultato.

Index-only non acquisisce secondi: un funzionale continuo viene rifiutato senza passo fisico; il resolver lascia disponibili t e i valori discreti, e restituisce N/A per δ/τ/funzionale non disponibile. Non esiste fallback δ=1.

## Funzionale, unità e terminale

```text
J_c = ∫₀ᴴ L(x(τ),u(τ),τ)dτ + Φ(x(H))
J^δ = Σ_(t=0)^(N−1) δL(x_t,u_t,τ_t) + Φ(x_N)
C₀=0; C_k=Σ_(t=0)^(k−1) δL_t; J^δ=C_N+Φ
```

Convenzione: totale adimensionale `[1]`, L `[1/s]`, δ `[s]`, δL `[1]`, Φ `[1]`. Pesi Q/R: unità inverse al quadrato della componente **per secondo**; QT: inverse al quadrato, senza `/s`. Esempi: qθ `1/(rad)²/s`, qω `1/(rad/s)²/s`, r `1/(N·m)²/s`; qTθ `1/(rad)²`.

Preset Q=[1,.2], R=[.05], QT=[4,1]. I metadata editoriali delle unità vengono confrontati con quelli del provider: mismatch rifiutato dalla composizione e diagnosticato dal CatalogValidator esistente. Non è stato aggiunto un sistema di conversione delle unità. Parametri e vettori restano double finiti, Q/QT non negativi, R strettamente positivo.

Al campione N: x_N, cumulativo, Φ e J disponibili; **u_N, x_(N+1), L_N e cella N N/A**. Φ viene aggiunta una volta, fuori dalla quadratura. Le API legacy chiamate `Stages` contengono qui i contributi integrati e vengono etichettate δL; le densità rimangono una collezione separata. Anche nomi, simboli e unità dei termini integrati differiscono da quelli della densità.

I profili di reference sono congelati al modello d'apertura. In esplorazione, modificare m/l/g non riscrive retroattivamente il desiderato: può cessare di essere un equilibrio del nuovo modello. Questo è dichiarato negli asset. L'attività di accuratezza usa esplicitamente g=0 e conserva quel reference; non lo presenta come equilibrio del modello modificato.

## Formula HUD e visualizzazioni

Nove formule v1/schema1, ID `c1.transfer.functional`, `.dynamics`, `.equilibria`, `.desired`, `.density`, `.quadrature`, `.accuracy`, `.definitions`, `.terminal`. Il costo editoriale ha ID distinto `c1.transfer.cost-functional`; modello editoriale `model.pendulum.transfer.continuous`. Nessuna collisione di ID con catalogo/prototipo.

`ContinuousMetricSource` è composto nel resolver comune; non sostituisce registry/provider custom. Scalar rimane scalar, Vector rimane vector e la componente dell'input è esplicita. Binding principali:

| Metrica | Scope/indice | Unità e significato |
|---|---|---|
| time.index / time.physical / time.step | CurrentTimestep / t | indice `[1]`, τ `[s]`, δ `[s]` |
| continuous.duration / transitions | Snapshot, nessun indice | H `[s]`, N `[1]` |
| continuous.density / contribution | CurrentTimestep / t<N | L `[1/s]` e δL `[1]` già salvati |
| continuous.cumulative | CurrentTimestep / t≤N | C_t `[1]` |
| continuous.terminal | Terminal / N | Φ `[1]` |
| continuous.total / terminal-value | Snapshot, nessun indice | totale e terminale congelati; quest'ultimo utile per confronti di evidenza |
| reference.state / input, constraint.desired-h | CurrentTimestep | reference della propria griglia e residuo della transizione desiderata |
| continuous.comparison.* | Snapshot | stesso problema, differenza totale/terminale, H/δ/N di A |
| continuous.accuracy-angle-error | Snapshot | errore terminale θ `[rad]` soltanto nel caso analitico supportato |
| continuous.accuracy-error / exact-terminal | Terminal / N | vettori con unità del modello, altrimenti N/A |

Le formule usano il medesimo renderer strutturato di integrale e somma e la selezione dei termini già esistenti. Binding semantici `cost.density`, `cost.cumulative`, `cost.terminal`, stati, input e reference passano tramite HighlightRegistry con revisione/indice, senza ricerche di renderer dal HUD.

Il prefab di produzione riusa corpo/torque del pendolo, fase, traiettoria, reference tratteggiato, costi e confronto A/B. Aggiunti grafici θ/ω vs τ tramite `StateTrajectoryView` e tre proiezioni dichiarate; il grafico u vs τ è parte di `ContinuousFunctionalView`. Questa nuova vista proietta soltanto densità, controlli, tempi e contributi salvati: N celle, N+1 tacche, altezze normalizzate con massimo dichiarato, area corrente letta dal risultato. Non integra il disegno. Scala dell'input dichiarata, nessun segmento u_N.

Il costo cumulativo esclude Φ, il diamante la distingue. A/B in fase dichiara δ/N/H di entrambi; il pannello conserva configurazioni complete e differenza J_B−J_A. Guide fra campioni e rollout Euler non vengono chiamati soluzione continua esatta. L'authoring locale evita sovrapposizioni delle nuove etichette e mantiene visibili le griglie del confronto; nessun refactor globale di camera/layout.

## Lesson Logic, MR-01 e accuratezza

| Attività | Azione/evidenza richiesta |
|---|---|
| continuous | Selezionare H e Φ, distinguere funzionale e approssimazione |
| dynamics | Selezionare t, τ, δ e leggere Euler |
| equilibria | A τ=1 leggere θ_des=.5 e u_des derivato; distinguere ω=0 da θ=0 |
| desired | A τ=.9 verificare h_des,θ=−.5: il salto desiderato non soddisfa la transizione |
| density | A t=10 selezionare L e δL, confrontarne unità e valori |
| weights | Portare qθ a 2: parametro corretto, L cresce, Φ resta invariata |
| cumulative | Raggiungere N e selezionare C e Φ |
| manual | Input Tail=1: input corretto e ω_(t+1) cresce, selezionare x_(t+1) |
| refinement | Da A δ=.1/N20/H2, impostare δ=.05: H invariata, N cresce, δ diminuisce, totale cambia; stesso problema fisico e selezione differenza |
| accuracy | Caso g=0/u=1: δ=.05, stessa H e stesso problema, errore θ terminale −.05 rad |
| definitions | Confrontare due definizioni legittime: ΣL e ΣδL, esempio costante esplicito |
| terminal | Raggiungere N, selezionare input/densità/cella N/A; Φ una volta e rinvio F28 |

`RequireSamePhysicalProblem` estende soltanto il descriptor di evidenza; il valutatore usa il confronto immutabile e la baseline della stessa attività/lifetime/generation. Restano i controlli di errore, indice e revisione MR-01. Nelle attività di confronto le metriche globali non assumono che il terminal index di A sia uguale a quello di B. Risposte corrette alternate fra prima/seconda posizione, ID stabili; la relazione integrale/quadratura viene mostrata prima della domanda sul fattore δ.

La dimostrazione dell'errore usa `PendulumAccuracyDiagnostic`: **solo g=0 e coppia costante**, θ(H)=θ₀+ω₀H+uH²/(2ml²), ω(H)=ω₀+uH/(ml²). Con H=2, m=l=u=1, θ₀=ω₀=0, terminale esatto [2,2]; Euler ha errore angolare −.1 con δ=.1 e −.05 con δ=.05. L'oracolo è indipendente da Euler; non è un secondo integratore. Nel caso nonlineare generale è N/A. Overflow della diagnostica opzionale non invalida un rollout valido. Nessuna promessa di monotonia generale o di ottimo.

Nel confronto nonlineare manuale, J_A≈2.688869644940409 e J_B≈2.2168392440387814; differenza ≈−.472030400901628. Quella differenza confronta due approssimazioni, **non** misura l'errore rispetto al vero continuo.

## Test e verifiche finali

**Compilazione exit 0, zero errori/warning C#. EditMode 543/543; PlayMode 114/114; zero falliti/saltati.** Conservati e passanti tutti i 484+108 casi della baseline F14, confrontando i fullname negli XML. Aggiunti **59 EditMode** (33 matematici, 26 produzione/binding) e **6 PlayMode**. Risultati integrali, log, hash e inventario in [F15_EVIDENCE](F15_EVIDENCE/verification.json), [EditMode.xml](F15_EVIDENCE/EditMode.xml), [PlayMode.xml](F15_EVIDENCE/PlayMode.xml).

Known-answer e casi negativi:

- L=3, H=2: integrale running=6 per δ=.1/.05/.02/.25; Φ=7 una volta, totale=13.
- Running nullo, terminale nullo ed entrambi nulli verificati separatamente.
- ∫₀¹τdτ=.5: rettangoli sinistri δ=.25 → .375; δ=.125 → .4375, valori calcolati a mano.
- H comune con N diversi, mismatch H/Nδ, H/N/δ non validi, NaN/∞ e δ assente.
- Equilibri 0 e ±π/2 con input 0 e ±9.81; derivata nulla ed Euler costante. Preset di produzione/reference verificati indipendentemente anche a θ=.5.
- Reference destra al bordo τ=1 per griglie differenti; editing fisico d'intervallo e dati originali immutabili.
- Reference/griglia incompatibili, array input/reference incompatibili con le leggi congelate, unità editoriali errate e unità della densità errate rifiutati.
- Index-only: 2+3=5 senza secondi; quadratura rifiutata, metriche continue/δ/τ N/A.
- Densità della fonte senza ½, contributo δL, Φ e totale noti; costo discreto legacy noto=38 e drone F14=35.226525 invariati.
- Resolver: scope, unità, SampleIndex, terminale senza input/next/densità/cella; A/B senza reinterpretazione.
- Completion/percorso completo, persistenza solo C1.L6, risposta errata, esito assente, δ nel verso errato, problema modificato, Previous con nuova baseline, restart/reset e nessun duplicato.
- Enter fallito non commette step/completion; recupero con restart.
- AF08-04: risultato valido N100/cursor50, richiesta non calcolabile e successiva richiesta N1 fallita; editing bloccato senza eccezione, ultimo risultato/configurazione intatti e reset funzionante.
- PlayMode: Bootstrap salvato, HUD/resolver e viste sulla stessa pubblicazione, celle/input/grid corretti dopo raffinamento, highlight, persistenza/reload, failure e NonRepresentable con recupero.

Regressioni A01–A04, AF08-01…07, MR-01/02/04/05 e F00–F14 passanti nelle suite complete: pendolo, index-only, static F10, linearizzazione, reference, costi discreti, vincoli, provider custom, session publication, navigazione/teardown, failure di Enter e progresso. I meccanismi `SessionNotificationScope`, dispatch, lifetime/generation e receipt di completion non sono stati modificati.

Verificati 13 assembly aciclici e Core/Simulation con `noEngineReferences`, senza import da UI/Content/Application/Unity. **664 file applicativi** identici byte per byte al clone verificato. Package e ProjectSettings invariati. Helper temporanei rimossi dal clone prima della compilazione e delle suite finali; nessun helper aggiunto al progetto consegnato.

## Prova runtime nativa e review visiva

Due processi Unity nativi, exit **0/0**: [percorso](F15_EVIDENCE/native-runtime-results.txt) e [reload](F15_EVIDENCE/native-reload-results.txt). Aperta la scena **Bootstrap salvata**, selezionata C1.L6 dalla normale UI. L'automazione invia eventi ai veri controlli UI; configura soltanto uno store file isolato per non toccare il progresso personale, non sostituisce Course/lesson/prefab.

Completati tutti i 12 step; risposta errata provata a ogni step, esito errato nei due confronti; Previous con ripristino baseline, reset/restart, failure N100→N1, NonRepresentable e recupero; completion, menu, riapertura. Nuovo processo legge il salvataggio v1/schema1 e apre una sessione fresca; restart conserva il progresso. Nessuna promozione di C1.L5 o del prototipo.

Riaperti dalla UI anche C1.L1–L5, verificati matematica valida, visualizzazione Available e stessa pubblicazione. Ispezionate le cinque catture, insieme a formule/valori/controlli F15. Le suite PlayMode conservano anche i percorsi completi precedenti. Questo è un controllo runtime automatizzato con ispezione delle catture, non uno studio umano di usabilità.

Conservate **37 catture Game View**, fra cui:

- [Integrale strutturato e termini live](F15_EVIDENCE/transfer-continuous-live.png).
- [Densità e contributo della cella](F15_EVIDENCE/transfer-density-live.png).
- [Controlli δ, H e N](F15_EVIDENCE/transfer-grid-controls.png).
- [Configurazioni A/B e stesso H](F15_EVIDENCE/transfer-ab-configuration.png).
- [Oracolo indipendente e errore −.05](F15_EVIDENCE/transfer-accuracy-live.png).
- [Terminale e N/A](F15_EVIDENCE/transfer-terminal-live.png).
- [Completion](F15_EVIDENCE/transfer-completed.png), [menu dopo nuovo processo](F15_EVIDENCE/process-reload-menu.png).

Review didattica sui dieci quesiti: **SÌ** a distinzione t/τ; δ; H fisico; N che cambia con δ; discreto vs integrale; motivo del fattore δ; Φ una volta; desired non automaticamente trajectory; Euler approssimazione campionata; formulazione distinta dalla soluzione ottima. Ogni punto ha istruzione/formula prima della domanda e valori osservabili; quelli operativi hanno evidenza numerica. L'esempio costante esplicita che ΣL cresce con N mentre ΣδL=cH: non chiama «sbagliato» il costo discreto precedente.

## Procedura manuale in Unity

1. Aprire `Assets/OptimalControl/Scenes/Bootstrap.unity` e premere Play. Nel corso `optimal-control`, Chapter 1, scorrere fino a **Continuo e trasferimento del pendolo (C1.L6)** e aprirla. C1.L5 incompleta non impedisce l'accesso.
2. Leggere ogni attività, scorrere il pannello sinistro fino a Matematica live, selezionare i termini richiesti e rispondere. Avanti resta bloccato finché mancano gli esiti. Il rendering strutturato è sotto la scelta della formula; i dettagli del termine sono sotto l'elenco.
3. Attività 2: selezionare t/τ/δ. Attività 3: usare t=10 se si è cambiato il cursore; reference [.5,0] a τ=1. Attività 4 entra a t9 e mostra h_des,θ=−.5.
4. Attività 5: L≈1.355987831556078 [1/s], δL≈.135598783155608 [1], Φ=1. Attività 6: aprire il pannello dei pesi, portare Q[0] a 2 e confermare con Invio; L cresce e Φ resta 1.
5. Attività 7: t=20, selezionare C e Φ. Attività 8: Input e parametri, scope «da qui alla fine», u[0]=1, Invio; selezionare x_(t+1), [0,.1].
6. Attività 9: aprire il pannello Funzionale continuo. «Griglia δ» ha tre posizioni (.1/.05/.02); scegliere la seconda, oppure scrivere .05 nel campo **δ [s] · H fisso** e premere Invio. A resta N20/δ.1/H2, B diventa N40/δ.05/H2. Selezionare la differenza di costo; aprire «Configurazioni A/B» per leggere entrambi gli obiettivi. Nessun valore va ottenuto cambiando Playback.
7. Attività 10: il setup applica g=0 e coppia costante; impostare δ=.05 e selezionare errore. Deve valere −.05 rad, contro −.1 della baseline δ=.1. L'oracolo non è disponibile nel normale caso g=9.81.
8. Attività 11: leggere il confronto discreto/integrale. Attività 12: t=20; input, next, densità e cella N/A, C+Φ=J. Selezionare i tre termini richiesti e concludere la verifica.
9. Tornare al corso, verificare C1.L6 completata, riaprirla e usare Ricomincia: la sessione torna fresca e il progresso rimane. Chiudere/riavviare Play per provare il backend normale.
10. Per esiti negativi usare risposta errata oppure δ=.2 negli step di raffinamento: non avanzano. Previous ricrea baseline e richiede nuovamente gli esiti. Reset/Ricomincia recuperano il preset. In esplorazione, x₀θ=1e200 provoca errore matematico conservando l'ultimo risultato; δ=2 dopo tale failure non rende quel risultato un rollout da un solo passo. Input bloccato; Reset recupera. x₀θ=1e40 produce invece un risultato matematico finito con visualizzazione NonRepresentable; Reset recupera entrambi i consumer.

## File creati e modificati

Inventario completo contro l'hash baseline **all'inizio di F15**, non contro il diff Git preesistente: [file-inventory.json](F15_EVIDENCE/file-inventory.json). Percorsi seguenti relativi a `Assets/OptimalControl/`.

Creati **33 file applicativi + i rispettivi 33 .meta**:

- `Runtime/Core/Costs/PhysicalTimeGrid.cs`, `PhysicalStepProfile.cs`, `WeightedSquaredError.cs`, `LeftRectangleQuadrature.cs`, `ContinuousTrackingCost.cs`.
- `Runtime/Simulation/ContinuousSampling.cs`, `DiscretizationComparison.cs`, `PendulumAccuracyDiagnostic.cs`.
- `Runtime/Content/Definitions/ContinuousTrackingCostDefinition.cs`, `Runtime/Content/Models/PendulumTransferModelDefinition.cs`, `Runtime/Content/Bindings/ContinuousMetricSource.cs`.
- `Runtime/Visualization/ContinuousFunctionalView.cs`.
- `Data/Models/PendulumTransfer.asset`, `Data/Costs/PendulumFunctional.asset`, `Data/Constraints/TransferDiagnostics.asset`, `Data/Experiments/PendulumTransfer.asset`, `Data/Lessons/Chapter01/C1_L6.asset`.
- `Data/Formulas/Chapter01/TransferFunctional.asset`, `TransferDynamics.asset`, `TransferEquilibria.asset`, `TransferDesired.asset`, `TransferDensity.asset`, `TransferQuadrature.asset`, `TransferAccuracy.asset`, `TransferDefinitions.asset`, `TransferTerminal.asset`.
- `Data/Visualizations/TransferAngle.asset`, `TransferVelocity.asset`, `TransferPhase.asset`; `Prefabs/PendulumTransferScenario.prefab`.
- `Tests/EditMode/Mathematics/ContinuousFunctionalTests.cs`, `Tests/EditMode/Formula/ProductionContinuousTransferTests.cs`, `Tests/PlayMode/ProductionContinuousTransferRuntimeTests.cs`.

Modificati **25 file applicativi**:

- `Data/Chapters/Chapter01.asset`.
- `Runtime/Core/Costs/CostConfiguration.cs`, `QuadraticTrackingCost.cs`, `SampledReference.cs`; `Runtime/Core/Models/PendulumDynamics.cs`, `PendulumParameters.cs`.
- `Runtime/Simulation/SimulationConfiguration.cs`, `CostEvaluation.cs`, `SimulationResult.cs`.
- `Runtime/Content/Bindings/RuntimeMetricResolver.cs`; `Runtime/Content/Definitions/ModelDefinition.cs`, `LessonStepDefinition.cs`; `Runtime/Content/Models/PendulumModelDefinition.cs`; `Runtime/Content/Lessons/LessonPlan.cs`.
- `Runtime/Lessons/LessonMetricEvidenceEvaluator.cs`.
- `Runtime/UI/CostPanelPresenter.cs`, `LessonStepPresenter.cs`; `UI/LessonHud.uxml`.
- `Runtime/Visualization/ExperimentVisualization.cs`, `CostContributionVisualizer.cs`, `ComparisonTrajectoryRenderer.cs`.
- `Tests/EditMode/Formula/ProductionCostReferenceTests.cs`, `ProductionLinearizationTests.cs`; `Tests/PlayMode/ProductionCostReferenceRuntimeTests.cs`, `ProductionIntroductionRuntimeTests.cs`: soltanto aspettative sull'elenco aggiornato delle lesson di produzione.

Documentazione: questo report, **50 file di evidenza** e aggiornamento dello stato F15 nel piano dopo il superamento dei criteri. Totale F15: **117 file creati, 26 modificati, zero eliminati**. Visione, curriculum, architettura, PDF, precedenti report e asset C1.L1–L5 invariati rispetto alla baseline F15. Nessuna migrazione dei salvataggi, dipendenza esterna o modifica a Packages/ProjectSettings.

## Limiti, rinvii e deviazioni

- Il piano non fissa regola di quadratura/pesi/durata/versione iniziali: rettangolo sinistro, H2, preset Q/R/QT e v1 sono scelte documentate necessarie a rendere F15 eseguibile. Non modificano la matematica della fonte.
- L'oracolo g=0/coppia costante è un adattamento didattico esplicito per verificare l'errore richiesto; nessuna soluzione analitica o integratore generale per il pendolo nonlineare.
- Solo griglie uniformi compatibili, profili costanti a tratti e costo diagonale richiesti; niente griglie adattive, trapezi/Simpson, Runge–Kutta o motore di funzioni arbitrarie.
- Riferimento ideale distinto dal controllo manuale. Nessuna traiettoria viene dichiarata ottima o automaticamente trasferita; **trasferimento ottimizzato in F28**. Le coperture statiche/geometriche residue C1.L3/C1.L4 e la matrice finale dei rinvii restano nei gate F16/F17/F20/F28 già documentati; non vengono promosse a contenuto acquisito.
- Scale dei grafici fissate/dichiarate; esplorazioni estreme possono uscire dal viewport o produrre NonRepresentable. Scroll, foldout e densità globale del HUD restano limiti di polish F29. Corrette le sole sovrapposizioni introdotte nel prefab F15; nessuna chiusura globale di MR-06 dichiarata.
- Prove native nell'Editor con vera UI e ispezione delle catture; Player build, profiling e usability study restano F29–F30.
- Nessun problema architetturale bloccante rimasto nello scope F15.

## F15 CONTINUOUS/DISCRETE GATE

| # | Verifica | Esito |
|---|---|---|
| 1 | Costo discreto legacy invariato? | **SÌ** — default DiscreteAdditive, fattore ½/no δ; oracoli e regressioni |
| 2 | Semantica distinta per quadratura? | **SÌ** — ContinuousFunctionalLeftRectangle e densità separate |
| 3 | δ applicato in un solo layer? | **SÌ** — LeftRectangleQuadrature; consumer leggono contributi salvati |
| 4 | Index-only non inventa secondi? | **SÌ** — rifiuto del funzionale e metriche N/A; t disponibile |
| 5 | Stessa H e N diversi? | **SÌ** — H2, δ.1/.05/.02 e N20/40/100 |
| 6 | Sampling in secondi, non copia per indice? | **SÌ** — leggi immutabili, bordo a τ1 e intervalli input verificati |
| 7 | Terminale una volta? | **SÌ** — Φ fuori dal loop, oracolo costante totale13 |
| 8 | Nessun u_N? | **SÌ** — N input, terminale N/A nei test/HUD |
| 9 | A/B conservano configurazioni/griglie? | **SÌ** — oggetti/revisioni congelati e confronto own-objective |
| 10 | HUD/visualizzazione stessa revisione? | **SÌ** — identità SessionState in PlayMode e a ogni step nativo |
| 11 | MR-01 protetto? | **SÌ** — lifetime/generation/sample, stesso problema fisico, H/N/δ/esito; negativi e Previous |
| 12 | Drone F14 discreto senza δ? | **SÌ** — totale35.226525, asset F14 invariati |
| 13 | Tutte le regressioni F00–F14? | **SÌ** — tutti i 484+108 fullname baseline conservati/passanti |
| 14 | Nessun solver/integratore generale anticipato? | **SÌ** — solo Euler esistente, quadratura sinistra e oracolo chiuso esplicito |
| 15 | F16 non iniziata? | **SÌ** — nessun asset C2, superfici o contratto futuro implementato |

Tutti i criteri del piano e del gate F15 sono soddisfatti. **READY FOR F16.**
