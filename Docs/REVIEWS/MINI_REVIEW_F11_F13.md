# Mini-review del blocco Chapter 1 — F11–F13

Data: **12 ottobre 2026**. Unity **6000.3.8f1**, Windows, URP. Review del progetto corrente, senza correzioni applicative e senza iniziare F14.

## 1. Executive summary

**La base matematica e i contratti comuni sono utilizzabili; il gate didattico richiede una correzione circoscritta prima di F14.**

C1.L1 distingue correttamente stato, controllo, output e tempo. C1.L2 implementa realmente Jacobiani sulla nominale e confronto fra rollout nonlineare e previsione lineare con perturbazioni coerenti. F13 produce **due lesson**, C1.L3 «Costi, prestazione e vincoli» e C1.L4 «Riferimento desiderato e rollout manuale»: C1.L4 è inclusa qui nella misura necessaria a verificare il contratto reference/cost condiviso. Non viene assimilata a C1.L3.

Rieseguiti indipendentemente **388/388 EditMode e 97/97 PlayMode**, senza fallimenti o test saltati. Compilazione conclusiva senza helper: exit 0, nessun errore/warning C#. Percorsi nativi ordinari da Bootstrap, terminale, completion, menu, riapertura, reload del profilo ed errori di configurazione/presentazione verificati nel clone isolato.

I probe aggiuntivi dimostrano però **MR-01, MAJOR**: le condizioni di alcune attività accreditano una modifica, senza verificare l'osservazione che lo studente dichiara di aver fatto. L2 accetta «peggioramento» dopo una riduzione dell'errore; L3 accetta «costo più basso» dopo un aumento di J. La UI abilita Avanti e il percorso può arrivare alla completion persistente. La stessa riproduzione passa attraverso il Bootstrap salvato e i controlli UI, senza modificare gli asset.

Questo non smentisce la correttezza dei numeri né riapre AF08-03: il runtime soddisfa le condizioni dichiarate, ma quelle condizioni descrivono insufficientemente l'esito educativo. Le suite correnti proteggono bene matematica, lifecycle e percorsi ordinari; non intercettano questo caso.

L'affermazione **«AF08-11 RESOLVED FOR REQUIRED F13 SCOPE» è giustificata tecnicamente**. Il «READY FOR F14» del report F13 va invece subordinato alla remediation didattica MR-01. Non serve un redesign della sessione o del math core.

## 2. Verdict

**FIX BEFORE F14**

Correggere MR-01 e aggiungere verifiche negative sugli esiti richiesti prima di costruire altre attività sullo stesso schema. Conservare integralmente session publication, notification scope, binding per generazione, risultati immutabili e progress receipt. Nessun fix è stato applicato durante questa review.

### Perimetro e metodo

Letti integralmente i quattro documenti di specifica: PROJECT_VISION, CURRICULUM, ARCHITECTURE e IMPLEMENTATION_PLAN. Esaminati FULL_AUDIT_AFTER_F08, AF08_MAJOR_REMEDIATION e report F09–F13; richiamati i report A01 e F04–F08 pertinenti a tempo, readiness, metriche, immutabilità, visualizzazione, costo e completion. Sono state confrontate implementazione e prove, senza assumere che un precedente PASS certifichi automaticamente le nuove attività.

Fonte primaria delle lesson esaminate: `Docs/OPTCON-RL_introduction_optimal_control_2026_09_14.pdf`, **36 pagine fisiche**, SHA256 `ae169f3492dc0915d0777d62e90184107d7017aee03b8620bbfe0aee9bd3a469`. Letti gli intervalli usati dalle lesson: pp. 2–25, 27, 33–36; controllate anche equazioni renderizzate, in particolare pp. 12, 22 e 34. Nel seguito **I p. N** indica pagina fisica, copertina inclusa. Non è necessario riesaminare integralmente O/K per queste lesson; le convenzioni comuni sono controllate contro curriculum e architettura.

Esaminati asset L1–L4 e relative formule/preset/reference/costi/vincoli, Bootstrap, catalogo, prefab, codice Core/Simulation/Content/Application/Presentation, validator e test pertinenti. Le prove Unity sono eseguite in un clone temporaneo: **601 file in Assets/Packages/ProjectSettings coincidono con il workspace**, inclusi template e impostazioni; nessun sorgente aggiuntivo di verifica è presente durante le suite e la compilazione conclusiva. La baseline di review comprende **737 file in Assets/Docs/Packages/ProjectSettings**; la consegna aggiunge soltanto questo report.

Log, XML, helper, profili e nuove catture restano fuori dal progetto:

`C:/Users/querz/AppData/Local/Temp/OptCon-Review-F11-F13-c469f677495e47c8aa87529c2cf14da7/`

File essenziali: `EditMode.xml`, `PlayMode.xml`, `compile-final.log`, `adversarial-probes.txt`, `native-adversarial-results.txt`, `native-adversarial-profile-final.json`; sottocartelle `native-L1-L2/` e `native-L3-L4/`. Le riproduzioni e i valori necessari sono trascritti sotto: le evidenze temporanee possono non sopravvivere alla pulizia del sistema.

## 3. Conteggio e classificazione

| Severity | Numero | ID |
| --- | ---: | --- |
| CRITICAL | 0 | — |
| MAJOR | 1 | MR-01 |
| MINOR | 4 | MR-02, MR-03, MR-04, MR-05 |
| POLISH | 1 | MR-06 |
| LIMITATION | 2 | MR-07, MR-08 |
| **Totale** | **8** | |

MR-01 comprende più manifestazioni della medesima lacuna di verifica educativa; i test mancanti non sono conteggiati come un secondo MAJOR. LIMITATION indica confini intenzionali, non richieste di implementarli in questo gate.

| ID | Rilievo | Trattamento |
| --- | --- | --- |
| MR-01 | Azione accreditata senza verificare l'esito dichiarato | **Prima di F14** |
| MR-02 | Dimostrazione di costo minore ottenuta cambiando il criterio | Migliorare l'attività insieme a MR-01 |
| MR-03 | Copertura visuale C1.L3 più stretta di quella descritta nel curriculum | Esplicitare la copertura e il recupero previsto |
| MR-04 | Risposta corretta sempre nella prima posizione | Prima di replicare lo schema di assessment |
| MR-05 | Chiusura della roadmap rimasta allo stato F12 | Prossimo aggiornamento documentale autorizzato |
| MR-06 | Densità e troncamento di label/controlli | Polish, con controllo dello scenario F14 |
| MR-07 | Cost/reference e vincoli limitati allo scope dichiarato | Estendere soltanto al bisogno pianificato |
| MR-08 | Evidenze Editor, senza Player/usabilità/profiling | Gate F29/F30 |

## 4. Review C1.L1 — Stato, input, output e tempo

**Scope effettivo:** undici attività, `C1.L1` v1/schema1, esperimento `c1.introduction.pendulum`. Dal Bootstrap normale: corso `optimal-control`, Chapter `C1`. Nessun costo/solver introdotto in questa lesson.

| Aspetto | Riscontro |
| --- | --- |
| Stato | x=[θ,ω], due componenti, rad e rad/s; la posa fisica legge θ |
| Input | Coppia u, N·m; rappresentazione curva, senza interpretazione come forza lineare |
| Output | Funzione pura del modello, qui y=h(x,u)=θ, p=1 contro n=2 |
| Immutabilità output | `OutputEvaluation` valuta e salva i campioni nella costruzione del risultato; UI e resolver leggono quei campioni |
| Tempo | t indice, δ=.02 s, τ=tδ; playback separato dalla discretizzazione |
| Index-only | Dichiarato nelle spiegazioni; contratto e fixture conservano t e restituiscono N/A per δ/τ |
| Terminale | T=100: 101 stati, 100 input; y_T disponibile perché l'output non richiede u_T; input e next assenti |
| Continuo/discreto | Derivata presentata come tasso, Euler come adattamento a campioni; nessuna derivata runtime fittizia |

Il caso noto θ₀=ω₀=0, u₀=1, m=l=1, δ=.02 produce **[θ₁,ω₁]=[0,.02]**. Al secondo campione θ₂=.0004. Entrambe le righe Euler usano lo stato precedente. La formula concreta è il focus dell'attività `predict` e viene mostrata prima della domanda.

Il test «stessa posa, velocità diversa» protegge correttamente y₀ uguale e futuri differenti. La relativa attività, invece, non impone questa osservazione: modificare soltanto θ e lasciare ω=0 concede ugualmente credito. È una manifestazione di MR-01, non un errore della funzione di output.

La lesson prepara L2: stato completo, transizioni salvate, coppia/input, orizzonte e spazi visuali sono introdotti prima di parlare di perturbazioni. L'uso di anticipazioni contestuali di costo/vincoli nello step motivazionale non introduce un solver o una competenza richiesta prematuramente.

## 5. Review C1.L2 — Traiettorie, equilibri e linearizzazione

**Scope effettivo:** dodici attività, `C1.L2` v1/schema1, esperimento `c1.linearization.pendulum`, prerequisito consultivo C1.L1. Fonte I pp. 10–13, pendolo/Euler ed errata di I p. 34 esplicitati.

La traiettoria è definita come coppia stato-input che soddisfa tutte le transizioni. L'equilibrio viene verificato avanzando i campioni: non è una pausa del playback. Sono corretti [0,0], u=0 e **[π/2,0], u=9.81 N·m** con m=l=1. È dichiarato che equilibrio non implica stabilità. L'errata della fonte è corretta: velocità nulla, non angolo necessariamente nullo.

`LinearizationComparison` conserva nominale e rollout perturbato, T Jacobiani e input perturbati, T+1 stati perturbati/previsioni/errori. Calcola centralmente:

```text
A_t = [[1,δ], [−δ(g/l)cos(θ̄_t),1]]          n×n
B_t = [[0], [δ/(ml²)]]                     n×m
Δxᴸ_0 = x_0 − x̄_0
Δxᴸ_(t+1) = A_t Δxᴸ_t + B_t Δu_t
x̂_t = x̄_t + Δxᴸ_t
e_t = x_t − x̂_t
```

La nominale è un rollout valido salvato, non il desiderato del tracking. A/B sono valutati **sulla nominale**, con righe di uscita e colonne di ingresso, e la convenzione del corso `A=∇₁fᵀ`, `B=∇₂fᵀ` è esplicita. Le celle hanno unità uscita/ingresso; non viene attribuita un'unica unità alla matrice mista.

Il confronto usa la medesima condizione iniziale perturbata e la medesima Δu nel modello vero e in quello lineare. Nel caso iniziale fissato Δx₀=0; l'attività che modifica x₀ è distinta e lo dichiara. Il primo passo con sola perturbazione dell'input coincide perché la dinamica è affine nell'input; la lesson non ne deduce esattezza globale. Per perturbazione angolare .1/.8, errore locale e numeri sono corretti; a nominale zero il primo termine dell'errore è cubico, senza promettere un termine quadratico non nullo.

La vista legge campioni già prodotti da `LinearizationComparison`: non calcola Jacobiani o rollout. Nominale viola, vero blu, previsione ambra; errore visuale ×20 dichiarato, valori HUD non scalati. Al terminale restano x̂_T/e_T, mentre Δu_T, next e Jacobiani di una transizione successiva sono N/A.

**Problema:** lo step `amplitude` richiede un peggioramento, ma la sua condizione verifica soltanto modifica iniziale ed errore non nullo. MR-01 dimostra che un miglioramento dell'errore è accettato. La matematica sottostante rimane corretta.

## 6. Review C1.L3 — Decisioni, costi e vincoli, con verifica condivisa C1.L4

### Scope, contenuto e ruoli

C1.L3 ha nove attività, esperimento `c1.costs.pendulum`, T=20, x₀=[.35,0], u=1, reference costante zero. Introduce z impilato con x₀ fissato, J, stage/terminale/prefissi, pesi Q/R/Q_T, disuguaglianza di coppia e vincolo end-point. C1.L4 ha sette attività, esperimento `c1.desired.pendulum`, reference a gradino al campione 10 e rollout manuale. Entrambe usano il workspace e i sistemi comuni.

Contributi F13: `state-deviation` e `control-deviation`; il presenter usa metadati/ID e non impone i nomi legacy Tracking/Effort. Il terminale campionato ha solo il termine di stato, non un effort o u_T fittizio. C_t somma gli stage prima di t; J=C_T+ℓ_T. Il criterio è discreto, adimensionale per campione, con pesi nelle unità inverse quadrate; non è una quadratura continua.

Le diagnostiche separano h=f−x_next, g=|u|−limite e r=θ_T−θ₀. Con u=1 e limite 2, g=−1; col limite .5, g=.5. Cambiare il limite non cambia controllo, stato o J e non applica saturazione. Un rollout può avere h=0 e r≠0. Reference, peso, vincolo e solver non vengono identificati tra loro.

C1.L4 aggiunge la verifica indispensabile del reference variabile: θ_des=0 prima di 10, .5 dopo, ω_des=0; u_des=0/4.7031645337 nel preset. La transizione desiderata 9→10 ha **h_des=[−.5,0]**, mentre il rollout reale ha h=0. Entrambi i lati sono equilibri, il salto non è una transizione del sistema. y_des è prodotto dalla mappa di output e ha una componente; non è automaticamente x_des.

L'edit `cost.reference.state.10.0` modifica un solo campione. La formula del gradino è dichiarata come descrizione del preset: non promette di rigenerare input/equilibri dopo l'edit. Reference tratteggiato/diamante distinto da rollout continuo/sfera; le linee sono guide fra campioni. A mantiene reference e costo propri dopo B. Nessuna ottimizzazione viene dichiarata; quella di C1.L4 è assegnata a F28.

### MR-02 — Il confronto a criterio fisso del curriculum è sostituito da un cambio di criterio

- **Severity:** MINOR. **Lesson/fase:** C1.L3, F13.
- **File/asset:** `Assets/OptimalControl/Data/Lessons/Chapter01/C1_L3.asset:431`, step `cost-not-feasibility`; `Runtime/UI/CostPanelPresenter.cs`, descrizione A/B; `Docs/OPTCON_CURRICULUM.md:115`.
- **Evidenza:** l'attività modifica Q₀₀ da 2 a 0 sullo stesso rollout. Dimostra correttamente che cambiare un criterio non elimina g/r, ma non costruisce una nuova traiettoria con J inferiore sotto lo stesso criterio. L'edit dell'input è disabilitato in questo step. Il pannello A/B chiarisce correttamente che pesi/target differenti significano criteri differenti.
- **Fonte PDF/piano:** I pp. 18, 20–23; verifica curriculare C1.L3 «costruire una traiettoria a costo minore ma non ammissibile». F13 richiede anche separazione limite/penalità, già presente.
- **Perché / conseguenze:** il contenuto attuale è vero, ma lascia incompleta la prova del confronto fra decisioni nel medesimo problema. Nelle fasi di ottimizzazione una diminuzione ottenuta cambiando Q non equivale a migliorare il candidato.
- **Raccomandazione:** conservare l'esercizio sui pesi e aggiungere/adattare una verifica con criterio e vincoli fissati, due rollout distinti, costo e violazione letti dal risultato. Non serve un solver.
- **Quando:** preferibilmente insieme a MR-01; non richiede un refactor né invalida il contratto reference/cost.

### MR-03 — Copertura visuale curriculare C1.L3 parziale e non esplicitamente tracciata

- **Severity:** MINOR. **Lesson/fase:** C1.L3, F13.
- **File/asset:** `Docs/OPTCON_CURRICULUM.md:94`, in particolare riga 113; `Data/Lessons/Chapter01/C1_L3.asset`; `Data/Formulas/Chapter01/PerformanceIndex.asset`, `SampledTracking.asset`, `ConstraintResiduals.asset`; `Prefabs/PendulumReferenceConstraintsScenario.prefab`; `Docs/REVIEWS/F13_IMPLEMENTATION.md:233`.
- **Evidenza:** il curriculum descrive esempi di decisione scalare/vettoriale, parabola/superficie, ellisse ammissibile e uguaglianza come bordo. La lesson di produzione presenta direttamente z impilato, il costo del pendolo e un intervallo monodimensionale di coppia. I cinque asset Formula F13 non espongono un'attività sul problema statico generale `min ℓ(z)` con la geometria dell'ellisse. La fixture statica F10 non è parte del percorso C1.L3. Il report F13 non registra questa riduzione di copertura.
- **Fonte PDF/piano:** I pp. 14–18; curriculum C1.L3. Il piano operativo F13 è più focalizzato su costo/vincoli/reference; la superficie generale è sviluppata in F16.
- **Perché / conseguenze:** è una differenza reale fra curriculum promesso e contenuto erogato, senza prova di matematica sbagliata. Un autore successivo potrebbe assumere che la geometria generale dei vincoli sia già stata insegnata.
- **Raccomandazione:** esplicitare la copertura e dove verranno recuperati gli esempi; eventualmente aggiungere un richiamo minimo al problema generale. Non implementare anticipatamente CostSurface, solver o proiezioni generali soltanto per questa review.
- **Quando:** chiarire prima di usare quei concetti come prerequisito nelle lesson successive; il drone F14 può riusare correttamente i limiti componente già introdotti.

## 7. Cross-lesson consistency

| Simbolo/concetto | Coerenza osservata |
| --- | --- |
| x_t / u_t / y_t | Stato/input/output distinti; m=1 non implica conversioni di tipo automatiche |
| T / t / δ / τ | T numero di transizioni, t indice, δ passo fisico opzionale, τ tempo; nessun uso del playback per interpretare campioni |
| Δx / Δu | Differenze rispetto alla nominale; iniziale fissato distinto da perturbato |
| A / B | Jacobiani nella linearizzazione; il testo di `LinearizedDynamics.asset` disambigua esplicitamente i nomi delle configurazioni A/B |
| Nominale / desiderato | La nominale locale soddisfa f; il desiderato del costo può non soddisfarla |
| e_t | L2: errore vero−lineare. L3: scarto reale−desiderato. `SampledTracking.asset` esplicita che non è l'errore F12 né h; riuso contestualizzato, non un binding ambiguo |
| h | Il PDF usa h sia per output sia per uguaglianza. Contesto/formule e ID runtime separano `output.current`/`reference.output` da `constraint.h` |
| ℓ_t / ℓ_T / J / C_t | Stage, terminale, totale e prefisso con domini coerenti; nessuno stage a T |
| J futuro drone | Qui indice di prestazione; F14 deve dichiarare il momento d'inerzia del drone con nome esteso, come già richiesto dal curriculum |

Prerequisiti C1.L1→L2→L3→L4 dichiarati tramite ID. La policy è **consultiva**: il menu permette di aprire L3 prima di L2, mostrando il prerequisito mancante. Non è una regressione o un bypass del completamento; corrisponde a F09 e all'architettura. Nel percorso consigliato non emerge un prerequisite gap indispensabile per il drone; il richiamo minimo alla sensibilità/Jacobiano è presente prima delle domande L2.

### MR-05 — La sezione conclusiva della roadmap contraddice lo stato F13

- **Severity:** MINOR. **Lesson/fase:** documentazione del gate F13/F14.
- **File:** `Docs/OPTCON_IMPLEMENTATION_PLAN.md:960` e `:962`.
- **Evidenza:** «F00–F12 sono completate; il prossimo intervento pianificato è F13, non avviato» e «La consegna corrente comprende esclusivamente F12», mentre la sezione F13, il catalogo e il report F13 dichiarano F13 completata e L3/L4 sono eseguibili.
- **Fonte/piano:** stato e criteri della stessa sezione F13; non è una questione delle formule PDF.
- **Perché / conseguenze:** due indicazioni operative incompatibili possono far ripartire F13 o far ignorare il contenuto già esistente.
- **Raccomandazione:** aggiornare soltanto il riepilogo finale alla prossima modifica documentale autorizzata, conservando la storia nei report. Questa review non modifica la roadmap.
- **Quando:** prima di affidare una nuova implementazione sulla base del «prossimo passo».

## 8. Mathematical correctness e formule

Non è stato trovato un errore matematico del core nel perimetro esaminato. Le divergenze di assessment MR-01 non sono corrette alterando i valori matematici.

| Famiglia di formule | Confronto con fonte e runtime |
| --- | --- |
| Stato/output/continuo | I pp. 8, 33; n=2, m=1, p=1. h del pendolo seleziona θ. ẋ è descrittivo e N/A come metrica live, senza fingere una derivata |
| Discreto/orizzonte | I pp. 9–10; x impilato esclude x₀ fissato, u arriva a T−1. Storia con T+1 stati, next a t+1 della stessa revisione |
| Euler | I p. 27 adattata a p. 33; θ_next=θ+δω, ω_next=ω+δ[−(g/l)sinθ+u/(ml²)]. Stato vecchio in entrambe le righe |
| Traiettoria/equilibrio | I pp. 11–12; equilibri zero e π/2 bilanciati. Errata p. 34 normalizzata senza imporre θ=0 |
| Jacobiani | Analitici standard, shape A 2×2/B 2×1, nominale salvata, trasposto rispetto a ∇ del corso dichiarato |
| Perturbazioni/errore | Ricorrenza lineare con =; confronto nonlineare con ≈. Δx₀ condiviso dai due modelli. Errore x−x̂ distinto da h |
| Tracking | I pp. 16, 22; diagonale, fattore ½, Q/Q_T≥0 e R>0; errata w₂ nella fonte dichiarata |
| Additivo/terminale | I pp. 20–23; T stage più un terminale, C₀=0, C_T esclude ℓ_T. Reference terminale al campione T |
| Vincoli | I pp. 19, 21, 23; segno h=f−x_next preservato; g≤0 e r=0 con unità proprie e tolleranza per componente |
| Desired/output | I pp. 24–25, 35; desired distinto da traiettoria valida; y_des calcolato dalla funzione di output, non copiato da x_des |

### Oracoli indipendenti verificati

- Euler all'origine: x₁=[0,.02], θ₂=.0004.
- Jacobiani all'origine: A=[[1,.02],[−.1962,1]], B=[[0],[.02]], oltre a differenze finite centrali con parametri non unitari e più angoli.
- Δu₀=1, Δx₀=0: Δxᴸ₁=[0,.02], Δxᴸ₂=[.0004,.02]; nessun input successivo inventato.
- Δθ₀=.1: ω₁=−.019587316346107685, ω̂₁=−.01962, e_ω≈.00003268365389231713. Δθ₀=.8: e_ω≈.01621473496551364. Secondo ordine locale verificato a nominale non nulla, rapporto cubico a nominale zero.
- Reference additiva: x=[1,2,3], x_des=[0,1,1], u=[1,1], u_des=[0,2], Q=R=Q_T=2. Stage **[2,2]**, prefissi **[0,2,4]**, terminale **4**, totale **8**.
- Gradino pendolo: h_des(9)=[−.5,0]; limite 1 con u=0/1/2 produce g=−1/0/+1; r additivo di ritorno vale 2.
- Reference terminale [.5,0], rollout zero e Q_T=[5,1]: ℓ_T=.625.

### Disclosure prima delle domande

Controllati tutti gli step L1–L3, e quelli L4 pertinenti al reference. `LessonHudPresenter.ApplyLesson` imposta il focus per generazione; `FormulaPresenter.Build` mostra la medesima espressione editoriale anche in `lesson-formula-expression`, prima della domanda guidata. Non occorre sbloccare il selector globale o uscire in esplorazione.

Euler è disponibile prima di `predict`; Jacobiani e ricorrenza prima delle previsioni lineari; espressione quadratica prima degli edit Q/R; terminale prima di Q_T; h/g/r prima del limite; formula e ipotesi del gradino prima del salto. Lo step «costo minore» richiama il costo già spiegato e mostra i vincoli. **Non è stata riprodotta AF08-08 nel blocco di produzione.** Scroll e affollamento restano MR-06, distinti da una formula non accessibile nel percorso.

## 9. Reference/cost gate — AF08-11

**Esito: RESOLVED FOR REQUIRED F13 SCOPE, con limiti espliciti.**

| Vecchia assunzione | Contratto corrente ed evidenza |
| --- | --- |
| Solo ReferenceState costante | Branch legacy preservato; `SampledReference` aggiunge T+1 stati e T input immutabili. `StateReferenceAt(t)` seleziona il campione, compreso T |
| Terminale senza indice | `EvaluateTerminal(T,x_T)` riceve l'indice e il provider campionato richiede il proprio T; la vecchia API delega per compatibilità |
| Solo Tracking/Effort | `CostContribution.Terms` espone ID/nome/simbolo/descrizione/unità/valore; ruolo, SampleIndex e revisione appartengono al contenitore immutabile |
| Reference non variabile | L4 usa il gradino, seek 9/10 e reference finale .5; nessuna rilettura del valore iniziale al terminale |
| Horizon implicito | Dimensioni, unità e T verificati; riferimento incompleto o T incompatibile rifiutati. UI disabilita l'edit dell'orizzonte campionato invece di ricampionare |
| HUD specializzato | Binding per contribution ID e reference indicizzato; termini assenti N/A, ID ignoto Invalid, componenti esplicite |
| Visualization scollegata | Target/reference/costi/constraint leggono lo stesso risultato della UI, con indice/revisione; niente costo o output calcolato nel renderer |
| A reinterpretato tramite B | Test e runtime conservano A/reference/pesi/campioni/costi dopo B e dopo candidato fallito |

`SimulationConfiguration.Reference` proviene dal costo, senza una copia indipendente nei consumer. `ReferenceEvaluation` e `ConstraintEvaluation` producono i dati derivati nel layer puro. La valutazione di h richiama la stessa dinamica autorevole: non è una seconda implementazione delle equazioni. Non vengono sommati nella UI i contributi per ricostruire J.

Il contratto non sostiene già costi pieni arbitrari, reference continuo o attuatori. Le compatibilità `ReferenceState`, Tracking/Effort e terminale legacy sono visibili ma non vengono usate per forzare i nuovi termini nei vecchi nomi. Non c'è motivo di riaprire AF08-11 oltre lo scope richiesto; vedi MR-07.

## 10. Architecture / source of truth, revisioni e gate precedenti

```text
ModelConfiguration / dinamica / output / costo / reference / vincoli puri
    → SimulationResult + valutazioni salvate
    → SessionState e coppia nominale/perturbata immutabile
    → RuntimeMetricResolver
    → Formula HUD / diagnostiche / condizioni educative

SessionState → proiezioni e viste 3D dello stesso risultato
```

Core non referenzia Unity; Simulation referenzia soltanto Core ed entrambi mantengono `noEngineReferences`. Gli assembly e i confini rimangono aciclici. Application orchestra comandi e composizione; Presentation non possiede una seconda configurazione matematica. Le nuove viste non chiamano Evaluate/StateJacobian e non ricavano dati numerici che il HUD dovrebbe poi leggere dal renderer.

`CourseRuntimeServices` compone resolver/condition registry; i nuovi source si inseriscono nella pipeline esistente. Non sono stati introdotti switch per C1.L1/L2/L3 nel CourseManager. Script di test/helper con switch sui passi sono automazioni editoriali e non logica runtime consegnata.

| Dato A conservato dopo B | Proprietà controllata |
| --- | --- |
| Output | Campioni di `OutputEvaluation` e modello di A; B non li riscrive |
| Jacobiani / perturbazioni / previsione | Coppia `LinearizationComparison` di A, nominale e perturbed propri; seek riusa la coppia |
| Reference / errori / desired output | Curve copiate in readonly, vettori immutabili e valutazioni di A |
| Stage / terminale / totale | `CostEvaluation` con configurazione e storia di A, metadati per revisione |
| Formula e viste | Conservano il riferimento al risultato/pubblicazione; non reinterpretano A con RequestedConfiguration o asset correnti |
| Failure | Ultimo risultato valido e derivati conservati integralmente; tentativo/diagnostica separati |

### Lifecycle, completion e persistenza

Ready resta successivo a composizione e binding. Le nuove viste partecipano al fanout di `ExperimentVisualization`, compreso il percorso NonRepresentable. Un errore di rendering non impedisce agli altri consumer di ricevere la revisione; l'intero gruppo viene nascosto e i semantic target invalidati. Callback catturati verificano sorgente/generazione; `SessionNotificationScope` conserva il confine di dispatch e differisce teardown/navigazione in modo sincrono.

`LessonSession.Enter` committa identità soltanto dopo azioni riuscite; Error blocca il passaggio. La receipt di completion e `LessonProgressBinding` verificano lifetime, risultato corrente, errori, disponibilità e destinazione al confine di notifica. Le nuove capability non introducono un canale diretto di scrittura del progresso.

Progressi separati da asset e runtime: `optimal-control/C1.L1…L4`, contentVersion=1/schema1; store versionato e identità stabili. `prototype.pendulum` e fixture F10 appartengono a identità/corsi distinti e non accreditano Chapter 1. Restart/reopen ricreano il percorso runtime senza cancellare o duplicare la completion persistente. Le suite conservano anche riordino, nuove lesson, riferimenti/versioni e gating consultivo F09.

| Gate storico | Riscontro corrente |
| --- | --- |
| A01 | Pendolo δ=.02 s; index-only 2+3=5 senza δ/τ, anche coi nuovi reference/costi |
| A02 | Unica regola Scalar/Vector/component/shape/unit/scope; Vector dimensione 1 rimane Vector |
| A03 / AF08-02 | Ready, dispatch, destinazione, teardown, generation e stale callback protetti; regressioni passanti |
| A04 | Output/Jacobiani/reference/costi appartengono ai rispettivi risultati immutabili |
| AF08-01 | NonRepresentable esplicito, HUD sulla stessa revisione, gruppo nascosto, reset recupera |
| AF08-03 | Enter fallito/Preset invalido non concede completion; nessuna incoerenza indice/Presentation riprodotta |
| AF08-04 | Input/perturbazioni dopo request fallita rifiutati; T100/cursor50/request T1 fallita non indicizza input mancanti |
| AF08-05/06/07 | Metadata/provider condivisi e testo completion neutro preservati nelle regressioni |

MR-01 riguarda la qualità delle condizioni dichiarate, **non un aggiramento** di questi controlli in presenza di Error, disposal o navigazione.

## 11. Pedagogical progression e qualità dell'evidenza educativa

Legenda: **A** = azione/interazione verificata; **R** = risposta concettuale; **E** = proprietà matematica del risultato verificata. A e R sono utili, ma da sole non provano che l'osservazione richiesta sia avvenuta. I codici formula sotto richiamano le famiglie della sezione 8.

### C1.L1 — tutti gli step

| Step | Concetto / formula / azione / feedback / condizione |
| --- | --- |
| context | Motivazioni x/u/y; scelta del dominio e feedback contestuale. A: una scelta qualsiasi; è engagement, non assessment |
| purpose | Prestazione vs limite, senza costo live prematuro. R su criterio+vincoli; prepara L3 |
| state | x=[θ,ω], modifica velocità mantenendo posa e seleziona stato. A+R, ma proprietà «stesso θ, ω diverso» non verificata: MR-01 |
| output | y=h(x,u)=θ; selezione y e R su output parziale. Significato concreto distinto dallo stato |
| continuous | Derivata prima della domanda; selezione ẋ descrittiva+R sul tasso, non sul next |
| predict | Euler concreto e parametri prima della previsione; nessun edit abilitato. R distingue stato vecchio da semi-implicit Euler |
| intervene | Edit input e osservazione next; A+R sulla velocità usata. Qualunque input cambiato vale come azione, non prova il valore 1 richiesto |
| transition | Next selezionato diventa current dopo Step; A+R su rewind da storia. Non viene confrontata un'annotazione numerica dello studente |
| time | Formula τ=tδ, step, selezione τ e R su t=20. Nessuna equivalenza t=secondi |
| spaces | Viste fisica/fase/stato-tempo; step+selezione+R sull'asse temporale. Legende coerenti |
| horizon | Seek T, input N/A, R sui domini e output state-only. E terminale+A+R, senza input/next inventati |

### C1.L2 — tutti gli step

| Step | Concetto / formula / azione / feedback / condizione |
| --- | --- |
| trajectory | Coppia valida, non curva arbitraria; step/input selezionato+R. Richiama L1 |
| equilibrium | Formula x_e=f(x_e,u_e); step+R e **E storia costante**; distingue pausa/equilibrio |
| balance | Formula coppia di equilibrio π/2; edit coda+R e **E storia costante**. E valida l'esito, oltre al solo click |
| nominal | Salva rollout valido; confronto disponibile+selezione+R. La barra non è un target desiderato |
| differences | Δx/Δu e Δx₀=0; edit Δu, selezione, R e **E iniziale fissato** |
| jacobians | Introduzione righe/colonne, shape/unità; selezioni A10/B10+R, confronto disponibile |
| linear-model | Ricorrenza e previsione prima dello Step; selezione next+R sui valori [0,.02] |
| compare-input | Step+errore selezionato+R sulla coincidenza locale. Nessuna promessa globale |
| initial-perturbed | Edit iniziale, step, e selezionato, R; **E iniziale diverso ed errore non nullo**, caso distinto dal fissato |
| amplitude | .1→.8 richiesto per osservare errore crescente; A+R+E errore non nullo **non certificano il peggioramento**: MR-01 |
| error-meaning | Formula e=x−x̂; selezione+R. Feedback distingue approssimazione da residuo dinamico |
| terminal | Seek T, Δu N/A, R; E terminale, confronto disponibile. Previsione/errore rimangono leggibili |

### C1.L3 — tutti gli step

| Step | Concetto / formula / azione / feedback / condizione |
| --- | --- |
| decisions | z impilato e J prima della domanda; J selezionato+R sulla somma discreta |
| tracking | Scarto, quadrato e peso; selezioni e/ℓˣ+R. Unità dei pesi esplicite |
| weight | Previsione Q 2→4 e rollout invariato; A parametro modificato+R. L'edit annullato resta accreditato: MR-01 |
| effort | R .2→.4, costo del controllo distinto dal limite; A+R. Non verifica il raddoppio numerico richiesto |
| terminal-weight | Q_T 5→10 e formula finale; A+selezione ℓ_T+R. Reference e indice T corretti |
| path-limit | g negativo→positivo; edit+R e **E una violazione di percorso**. Diagnostica distinta da enforcement |
| endpoint | h=0 non implica r=0; r selezionato+R. End-point e desiderato distinti |
| cost-not-feasibility | Q 2→0 richiesto; A+E violazione+R. Nessuna E costo diminuito: **MR-01**; diverso criterio: MR-02 |
| terminal | Seek T e selezioni input/stage N/A+R; E terminale. Chiusura J=C_T+ℓ_T corretta |

C1.L4, per il gate condiviso: `desired` distingue x_des/y_des; `jump` richiede h_des selezionato e non fattibilità dinamica salvata; `manual` input+step+h selezionato; `tracking` confronta allo stesso t; `change-reference` modifica il campione esplicito con A conservato; `reference-not-constraint` separa ruoli; `terminal` richiede T e reference finale. La domanda del salto è supportata da un residuo salvato non nullo, senza confonderlo con lo scarto di tracking.

### MR-01 — Le condizioni non verificano l'esito richiesto e possono approvare un'osservazione falsa

- **Severity:** **MAJOR**, didattico, circoscritto. **Lesson/fasi:** C1.L1 `state`, C1.L2 `amplitude`, C1.L3 `weight`/`cost-not-feasibility`; F11–F13.
- **File/classi/asset:** `Data/Lessons/Chapter01/C1_L1.asset:131`, `C1_L2.asset:669`, `C1_L3.asset:170` e `:431`; `Runtime/Lessons/LessonSession.cs:170` e `:174`; `LessonConditionRegistry.cs` (`action`, `linearization.error-nonzero`, `constraint.path-violated`); test `ProductionIntroductionTests`, `ProductionLinearizationTests`, `ProductionCostReferenceTests` e corrispondenti PlayMode.
- **Evidenza riprodotta, API reali e poi UI nativa:**

| Attività richiesta | Interazione alternativa accettata | Riscontro |
| --- | --- | --- |
| L1, stessa posa con velocità diversa | θ₀ .35→.7, ω₀ resta 0; risposta «La velocità ω, oltre all'angolo θ» | CanAdvance=true, Next=true; la situazione osservata non è quella richiesta |
| L2, .1→.8 e peggioramento | Δθ₀ .1→.05 a t=1; risposta «La linearizzazione descrive perturbazioni locali» alla domanda sul peggioramento | e_ω **3.2683653892313924e−5→4.086989092912108e−6**, quindi diminuisce; CanAdvance=true, Next=true |
| L3, Q 2→4 | Q 2→4→2; risposta «Cambia il criterio…» | Il valore finale è identico al preset, ma l'azione rimane soddisfatta |
| L3, costo minore Q 2→0 | Q 2→4; risposta «Costo più basso, ma vincolo violato…» | **J 5.294120943466966→7.124381891130955**, g=.5; feedback «Corretto», Avanti abilitato |

- **Completion:** proseguendo con l'ultimo step valido, L3 viene salvata. Il probe UI completa **Bootstrap→L1→menu→L2→menu→L3→menu** con un unico store inizialmente vuoto e registra le tre identità/versioni. `native-adversarial-results.txt` e il profilo JSON confermano il risultato; la cattura `adversarial-native-L3.png` mostra simultaneamente la risposta «Costo più basso», «Corretto», Avanti e J=7.12438.
- **Causa:** `facts` conserva eventi di modifica; le condizioni non verificano componente, differenza finale o confronto rispetto al baseline dell'attività. `error-nonzero` certifica e≠0, non un aumento. `constraint.path-violated` certifica g>tol, non una diminuzione di J. Le risposte sono validate per ID, indipendentemente dall'osservazione live.
- **Fonte PDF/piano:** I pp. 8, 11–12, 18–23; verifiche curriculum C1.L1/L2/L3; criteri F11 sullo stato, F12 sul peggioramento locale, F13 sulla distinzione prestazione/ammissibilità; visione sul legame fra formule, esperimento e comprensione.
- **Perché / conseguenze:** viene persistita una completion dopo aver approvato una descrizione incompatibile con i dati mostrati. Replicare questo schema nel drone o nelle ottimizzazioni consolida una verifica di azioni, anziché dell'esito richiesto. Non è necessario dimostrare comprensione assoluta per risolvere il rilievo: almeno il fenomeno oggetto della domanda deve essersi verificato.
- **Raccomandazione:** aggiungere condizioni editoriali/evaluator minimi sugli esiti autorevoli: componente iniziale pertinente con posa mantenuta, errore maggiore al medesimo campione/nominale, J inferiore con g ancora violato, oppure adattare domanda/feedback all'esito effettivamente osservato. Per F12 rimane necessario mostrare concretamente il peggioramento richiesto dal piano. Usare risultati/baseline immutabili e metric resolver; non copiare equazioni nel lesson layer. Non trattare ogni generico `action` come una verifica numerica obbligatoria: correggere gli step che richiedono un esito specifico.
- **Test raccomandati:** edit della componente sbagliata; ampiezza ridotta; Q aumentato invece che ridotto; edit seguito da annullamento; seek diverso dal campione della domanda; il percorso non deve accreditare l'osservazione non avvenuta né salvarne una completion. Conservare i casi positivi e tutti i gate di failure/lifetime.
- **Quando:** **prima di F14**. Nessun refactor generale della progressione o del math core è giustificato.

### MR-04 — Tutte le risposte valutate corrette sono nella prima posizione

- **Severity:** MINOR. **Lesson/fasi:** C1.L1–L3, confermato anche L4; F11–F13.
- **File:** i quattro `Data/Lessons/Chapter01/C1_L*.asset`; `Runtime/UI/LessonStepPresenter.cs:49` conserva l'ordine editoriale.
- **Evidenza:** **31/31** domande valutate di L1–L3 hanno `expectedAnswerId` uguale al primo elemento `answers`; includendo L4 sono **38/38**. La scelta motivazionale L1/context è esclusa, perché qualunque dominio è valido. Il percorso UI aggiuntivo usa sempre la prima risposta e passa tutte le verifiche concettuali, una volta eseguite le azioni.
- **Fonte/piano:** verifiche di comprensione del curriculum e della visione; non è un errore in una formula del PDF.
- **Perché / conseguenze:** una regolarità riconoscibile riduce il valore delle risposte come evidenza di apprendimento, soprattutto quando i distrattori sono nettamente falsi. Le interazioni/E rimangono necessarie: non equivale a completion senza azioni.
- **Raccomandazione:** variare editorialmente l'ordine o usare un ordinamento controllato/testabile; conservare ID e feedback, senza richiedere un motore di assessment complesso.
- **Quando:** prima di replicare il pattern nei capitoli successivi; non richiede di bloccare l'architettura F14.

## 12. Test quality

Le suite rieseguite durante questa review conservano **tutti i fullname dei test F10, F11 e F12**: nessun caso storico mancante. I nuovi casi non si limitano a smoke di compilazione.

| Gruppo | Casi correnti | Protezione effettiva |
| --- | ---: | --- |
| ProductionIntroductionTests | 20 EditMode | Catalogo, output, tipi/shape/unità/terminale, oracoli Euler, revisioni e progresso |
| ProductionLinearizationTests | 17 EditMode | Authoring, binding, scope/indici, terminale, nominale, Previous/restart, progressi |
| LinearizationTests | 23 EditMode | Jacobiani analitici, differenze finite, equilibri, oracoli numerici, ordine dell'errore, immutabilità/failure |
| ReferenceCostConstraintTests | 17 EditMode | Costo noto, reference T+1/T, indici/unità, contributi/metadati, g/r/h_des, A/B e terminale |
| ProductionCostReferenceTests | 15 EditMode | Asset L3/L4, percorsi, errore/no-op, catalogo invalido, Formula A/B e progressi |
| ProductionIntroductionRuntimeTests | 6 PlayMode | Bootstrap/catalogo salvato, consumer reali, output e lifecycle |
| ProductionLinearizationRuntimeTests | 9 PlayMode | Percorso e UI reali, comparison/view, failure e riapertura |
| ProductionCostReferenceRuntimeTests | 8 PlayMode | Reference/HUD/view stessa pubblicazione, limite/semantica, terminale, restart e persistenza |

Differenze finite usano la dinamica per perturbare ingressi, ma non la funzione Jacobiano per produrre l'atteso; sono affiancate da matrici numeriche note. I costi di test sono calcolati a mano e non richiamando `EvaluateStage` per ricostruire l'atteso. Output parziale, errori campionati, residuo del salto e g dentro/bordo/fuori hanno oracoli indipendenti. Non è stata individuata una nuova matematica importante protetta soltanto da un confronto tautologico.

Le asserzioni di identità `SameAs` non sono oracoli matematici: sono correttamente usate per verificare source of truth/revisione e sono accompagnate da asserzioni di valori. I test che interrogano h=0 sul rollout verificano un'invariante, mentre il caso h_des non nullo controlla anche segno/indice e distinzione semantica.

**Lacuna concreta:** i percorsi di produzione usano esattamente gli edit attesi e controllano risposte errate/no-op, ma non un edit valido nel verso sbagliato o annullato. Pertanto la suite verde non dimostra la qualità delle condizioni educative MR-01. La correzione dovrà aggiungere tali casi, senza indebolire i test matematici o storico-architetturali.

## 13. Runtime observations

### Esecuzioni indipendenti

| Verifica | Esito della review |
| --- | --- |
| EditMode completi | **388 passati, 0 falliti, 0 skipped**, 12 ottobre 00:24:38–00:24:44 UTC |
| PlayMode completi, rendering nativo | **97 passati, 0 falliti, 0 skipped**, 00:25:02–00:25:50 UTC |
| Compilazione finale senza helper | **Exit 0**, nessun errore/warning C# |
| Bootstrap→L1→menu→L2 | Percorsi ordinari completi; equilibri, nominale, Δu/Δx, errori .1/.8, terminale, completion e reopen PASS |
| Bootstrap→L3→menu→L4 | Costo, reference/desired, limite/end-point, salto, A/B, terminale, persistence e reopen PASS |
| Percorso aggiuntivo L1→L2→L3, medesimo profilo | **Riproduce MR-01 tramite controlli UI**; salva le tre completion nonostante gli esiti didattici alternativi |
| Request fallita e recupero | Ultimo valido preservato, input/perturbazioni disabilitati/rifiutati, reset recupera |
| NonRepresentable | Gruppo nascosto e HUD sulla stessa revisione; reset ripristina Available; nessun progresso dai percorsi di errore |
| Restart Play / reload store | Completion persistente e sessione nuova, senza residui del comparison precedente |

Le prove partono dal **Bootstrap salvato** e dal corso di produzione; nessuna sostituzione del catalogo o iniezione di contenuti. È isolato soltanto lo storage del profilo. UI Toolkit è pilotata con eventi/comandi dei controlli reali: non è una dichiarazione di click manuali con mouse o di studio con studenti. I percorsi ordinari registrano zero Error/Exception/Assert applicativi.

Il primo helper aggiuntivo aveva un'attesa errata dopo il ritorno al menu, quando non esiste più una lesson attiva: corretto **soltanto l'helper temporaneo**, ripetuto integralmente con nuovo profilo vuoto ed exit 0. Questo non è un difetto dell'applicazione e non modifica i test consegnati.

Le catture native ispezionate includono Jacobiani prima della domanda, errore live .8, g fuori intervallo, salto desiderato, terminale e il controesempio L3. Le catture storiche F11–F13 sono usate come confronto, non al posto della verifica corrente. I nuovi PNG restano nella directory temporanea, per rispettare l'output limitato al report.

### MR-06 — Scroll, densità e troncamenti riducono la leggibilità

- **Severity:** POLISH. **Lesson/fasi:** L2–L4, in particolare prefab F12/F13.
- **File/asset:** `UI/LessonHud.uxml`, `LessonHud.uss`, `Prefabs/PendulumLinearizationScenario.prefab`, `PendulumReferenceConstraintsScenario.prefab`, label/legende delle viste.
- **Evidenza:** Game View 1440×979: istruzioni/formula/domanda sono disponibili, ma controlli e valori richiedono scroll; alcune label del phase plane e stato prerequisiti sono troncate, i grafici costo hanno testo piccolo e le curve ampie attraversano la zona delle legende. Catture `production-large-live.png`, `L3-path-limit.png`, `L4-jump.png`.
- **Fonte/piano:** visione Live Mathematics HUD, proiezioni esplicite dell'architettura, AF08-09/10 e polish F29.
- **Perché / conseguenze:** aumenta il carico di memoria e rende più difficile leggere simultaneamente richiesta, campo e valore. Con sei stati/due input del drone può peggiorare.
- **Raccomandazione:** gerarchia/viewport/label più selettivi e controllo su schermo reale; mantenere assi/unità e codifica di ruoli senza clamp matematico. Non rifare ora tutto il layout né confondere la leggibilità con dati falsi.
- **Quando:** F29, anticipando soltanto gli adattamenti necessari a rendere lo scenario F14 comprensibile.

## 14. Acceptable technical debt

### MR-07 — Generalizzazione cost/reference/constraint limitata e dichiarata

- **Severity:** LIMITATION, **LIMITATION ACCEPTABLE**. **Fasi:** F13, evoluzioni F14/F15 e seguenti.
- **File/classi:** `Core/Costs/CostConfiguration.cs`, `QuadraticTrackingCost.cs`, `SampledReference.cs`; `Core/Constraints/ConstraintConfiguration.cs`; `SimulationConfiguration.cs`; provider editoriali e `ConstraintRegionVisualizer`.
- **Evidenza:** costo produttivo diagonale; reference T+1/T con T fisso, collegato al costo; limite simmetrico per componente ed endpoint di spostamento. Nessuna conversione/resampling implicita. Accessor legacy ancora presenti, nuovi consumer su termini generici. Non esistono ancora reference indipendente dal costo, matrici piene o vincoli arbitrari.
- **Fonte/piano:** I pp. 20–25; gate AF08-11 limitato allo scope F13; quadratura F15 e capacità successive. Il report F13 dichiara espressamente questi confini.
- **Perché è accettabile / rischio:** copre le lesson attuali e le primitive di costo/limite del drone; non rende impossibile aggiungere provider e nuovi contratti. Diventerebbe un problema se si fingesse di rappresentare un costo pieno usando etichette diagonali o un vincolo non simmetrico dentro il contratto corrente.
- **Raccomandazione / quando:** estendere con oracoli e capability nel momento richiesto. Per F14 distinguere il dominio dei due input e non riusare la legenda θ/ω o il glyph curvo di `LinearizationComparisonView` per grandezze che non sono angoli/coppie. La specializzazione pendolo di quella vista non è un errore nelle lesson correnti.

### MR-08 — Le prove non certificano ancora Player, prestazioni o comprensione da parte di studenti

- **Severity:** LIMITATION, **LIMITATION ACCEPTABLE**. **Fasi:** evidenze F11–F13, gate F29/F30.
- **File/sistemi:** report/evidenze F11–F13 e di questa review; Tests/EditMode/PlayMode; build Player, profiler e studio di usabilità non eseguiti.
- **Evidenza:** compilazione, test e catture sono dell'Editor Windows, con automazione UI; nessuna misura su target distribuito o valutazione con uno studente.
- **Fonte/piano:** AF08-14; verifiche didattiche della visione e fasi F29/F30.
- **Perché è accettabile / rischio:** non serve una build finale per questo gate, ma suite verdi e immagini non dimostrano comprensione o prestazioni sul dispositivo. Non usare questi risultati per dichiarare conclusi i gate successivi.
- **Raccomandazione / quando:** mantenere prove matematiche e runtime; aggiungere feedback didattico durante la produzione e profiling/usabilità/build nelle fasi previste.

Altri rinvii intenzionali non sono difetti nuovi: Matrix runtime generale, IterationHistory, gradienti/Newton/KKT/costate, solver e quadratura continua. F12 ha già una matrice numerica pura per Jacobiani, senza fingere una capacità Matrix runtime del resolver. F13 non deve anticipare le relative pipeline. Nessun redesign preventivo è richiesto dalla loro assenza.

## 15. Risks before F14

1. **Rischio bloccante: replicare MR-01.** Lo schema «modifica qualsiasi + risposta statica» può accreditare un fenomeno mai osservato. Il drone moltiplica componenti, input e unità: va chiarito prima quali esiti sono realmente verificati.
2. **Rischio di confronto improprio:** costo minore va letto sotto lo stesso criterio quando si confrontano decisioni; cambiare pesi/reference definisce un altro criterio. MR-02 segnala la lacuna dell'esercizio, non una mutazione del risultato A.
3. **Rischio di prerequisiti presunti:** la geometria generale descritta dal curriculum C1.L3 non è tutta erogata. Non dare per acquisiti ellisse/superficie o un solver inesistente; i limiti componente sono invece già introdotti.
4. **Rischio di presentazione multidimensionale:** proiezioni esplicite, unità per componente, input applicati e limiti separati. La label pendolo non va trasferita invariata al drone; controllare anche l'ambiguità J costo/momento d'inerzia prevista dalla specifica F14.
5. **Rischio da refactor non necessario:** correggere le condizioni senza semplificare notification scope, NonRepresentable, generation/lifetime, Enter atomico e receipt di completion. Le regressioni indicano che questi contratti funzionano e vanno preservati.

Non emerge un CRITICAL o la necessità di un nuovo CourseManager, un secondo HUD, una seconda sessione o un redesign del contratto reference/cost prima del drone.

## 16. Recommended actions

| Priorità | Azione raccomandata per una successiva attività autorizzata | Evidenza di chiusura |
| --- | --- | --- |
| **Prima di F14** | Remediation MR-01 sui pochi step interessati e relative condizioni, usando dati autorevoli | Casi positivi conservati; componente errata, errore diminuito, J aumentato ed edit annullato non possono accreditare l'osservazione richiesta; controllo finale persistence |
| Insieme alla remediation didattica | Integrare il confronto di rollout a criterio fisso MR-02 e variare l'ordine delle risposte MR-04 | Due candidati sotto lo stesso criterio, violazione esplicita; nessuna posizione che riveli sistematicamente la risposta |
| Documentazione | Tracciare la copertura curriculare MR-03 e riallineare il riepilogo finale MR-05 | Stato F13/F14 univoco, nessuna capacità non erogata data per acquisita |
| Durante F14 | Riutilizzare sessione/HUD/costi/reference/limiti con shape e unità del drone; adeguare solo le viste pertinenti | Hover e segni con oracoli indipendenti, stessi result/revision per tutti i consumer, label/assi coerenti |
| Fasi dedicate | MR-06/07/08 e capacità future | Nessuna espansione anticipata di questo gate |

La review consegna **esclusivamente questo documento**. Codice, asset, scene, prefab, test, PDF, roadmap e report precedenti non sono stati modificati. **F14 non iniziata.**
