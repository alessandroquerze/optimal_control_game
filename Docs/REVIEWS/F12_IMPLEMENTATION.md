# F12 — Chapter 1: traiettorie, equilibri e linearizzazione

Data: 11 ottobre 2026. **F12 completata e verificata. F13 non iniziata.**

## Perimetro e prerequisiti

Specifica principale: F12 di `OPTCON_IMPLEMENTATION_PLAN.md`. Consegnata **C1.L2**, «Traiettorie, equilibri e linearizzazione», in dodici step di produzione. Non duplica l'introduzione x/u/y/t di C1.L1; la usa come prerequisito per trattare coppie stato-input, equilibri, nominali valide, perturbazioni, Jacobiani e confronto locale vero/lineare.

Prima delle modifiche risultavano soddisfatti i prerequisiti: F11 completata, C1.L1 nel Bootstrap salvato, confronto A/B immutabile, renderer/proiezioni/semantic registry, Formula HUD tipizzato, authoring strutturato, Lesson Logic e progresso versionato. Baseline separata di **581 file** in Assets/Docs/Packages/ProjectSettings; confronto con questa baseline, indipendente dalla disponibilità di Git. Letti i quattro documenti principali, audit e remediation AF08, report F09–F11 e report precedenti pertinenti; consultate le pagine effettive dei PDF.

Il piano rimanda a **F13** C1.L3–L4, costo/vincoli, riferimento desiderato e grafici relativi. Nessun nuovo costo, vincolo, solver, gradiente di ottimizzazione, Hessiana, IterationHistory, Chapter 2/3, Player build o polish generale.

**Policy dei prerequisiti:** il prompt proponeva un lock in assenza di C1.L1; F09 e l'architettura prescrivono invece prerequisiti **consultivi con esplorazione consentita**. Si applica la precedenza esplicita del piano: `MissingPrerequisiteIds=[C1.L1]`, avviso nel menu, accesso consentito. Dopo completion C1.L1 l'avviso scompare. Prototipo e fixture F10 non soddisfano quel prerequisito e non producono progressi C1.L2. Nessun hard lock introdotto o aggirato.

## Accesso, identità e asset di produzione

Percorso normale, senza cambiare Inspector o catalogo:

```text
Assets/OptimalControl/Scenes/Bootstrap.unity → Play
  OptimalControlCourse.asset   optimal-control, v1/schema1
    Chapter01.asset           C1, v1/schema1
      C1_L1.asset             Stato, input, output e tempo
      C1_L2.asset             Traiettorie, equilibri e linearizzazione, v1/schema1
        PendulumLinearization.asset  c1.linearization.pendulum, v1/schema1
          PendulumLinearizationScenario.prefab
```

Bootstrap e CourseDefinition non richiedono modifiche: il corso contiene già Chapter01; il capitolo aggiunge C1.L2 mantenendo C1.L1, GUID e fonti precedenti. Il nuovo esperimento riusa il provider pendolo, T=100, δ=.02 s, m=l=1, g=9.81, x0=[.35,0], input nulli; **nessun CostDefinition o target**. Nuovo prefab composto dalle primitive mature, distinto dal prefab del prototipo. Nessun asset sotto Tests/Fixtures espone la produzione.

Completion persistente: soltanto `courseId=optimal-control`, `lessonId=C1.L2`, `lessonVersion=1`, `lessonSchemaVersion=1`. Formato file e schema di persistenza F09 invariati; nessuna migrazione. C1.L1 già salvata resta valida; aggiungere C1.L2 non la ricompleta. Il runtime riparte sempre dal primo step, anche con progresso persistente completato.

## Fonti, notazione ed errata

Documento I: `OPTCON-RL_introduction_optimal_control_2026_09_14.pdf`, SHA-256 `ae169f3492dc0915d0777d62e90184107d7017aee03b8620bbfe0aee9bd3a469`, 36 pagine fisiche/35 slide. Numeri di pagina fisica e slide distinti nei SourceReference.

| Concetto | Fonte: pagina fisica / slide | Uso in F12 |
| --- | --- | --- |
| Orizzonte, successioni x/u, x0 separato | I 10 / 9 | T input, T+1 stati compreso x0 |
| Coppia stato-input dinamicamente valida | I 11 / 10 | Ogni transizione soddisfatta; una curva di fase da sola non basta |
| Equilibrio x_e=f(x_e,u_e) | I 11 / 10 | Traiettoria costante; nessuna affermazione di stabilità |
| Nominale e linearizzazione lungo traiettoria | I 12 / 11; richiamo I 13 / 12 | A/B sulla nominale, ricorrenza delle perturbazioni |
| Forward Euler | I 27 / 26 | Adattamento didattico al pendolo, già dichiarato in F11 |
| Pendolo e unità fisiche | I 33 / 32 | x=[θ,ω], input coppia, y=θ preservato |
| Equilibri del pendolo | I 34 / 33 | **Errata documentata:** prima condizione ω_e=x₂,e=0, non θ_e=x₁,e=0; u_e=mgl sin θ_e |
| Convenzione delle derivate | Architettura e convenzione del corso già documentata | Core: righe uscita/colonne ingresso; A=∇₁fᵀ, B=∇₂fᵀ |

Le matrici esplicite del pendolo, i valori numerici e l'errore sono **derivazioni/adattamenti** del modello e della linearizzazione, non una trascrizione letterale di una nuova slide. I SourceReference lo dichiarano. Nessuna parte dei richiami a costi/vincoli in I 13 è implementata.

La dinamica Euler resta quella di F02/F11:

```text
θ_next = θ + δ ω
ω_next = ω + δ[−(g/l)sin θ + u/(m l²)]
A_t = [[1, δ], [−δ(g/l)cos θ̄_t, 1]]      n×n
B_t = [[0], [δ/(m l²)]]                  n×m
```

Due rollout validi condividono modello e orizzonte:

```text
Δu_t = u_t − ū_t;  Δx_t = x_t − x̄_t
Δxᴸ_0 = x0 − x̄0
Δxᴸ_(t+1) = A_t Δxᴸ_t + B_t Δu_t
x̂_t = x̄_t + Δxᴸ_t
e_t = x_t − x̂_t = Δx_t − Δxᴸ_t
```

La ricorrenza lineare usa **uguaglianza**, il confronto con il modello nonlineare usa **approssimazione**. Con iniziale fissato Δx0=0; perturbare esplicitamente x0 è un secondo esperimento. A/B indicano Jacobiani, distinti dalle etichette A/B del confronto di revisioni F07. `e` è errore di previsione, **non** residuo f(x_t,u_t)−x_(t+1), costo o certificato di ottimalità. Non si promette monotonia dell'errore per ogni modello, ampiezza o orizzonte.

## Architettura e nuova capability

```text
Core: IDifferentiableDynamics + NumericMatrix + PendulumDynamics
  ↓
SimulationResult immutabile nominale / rollout corrente
  ↓
LinearizationComparison immutabile, posseduta dalla pubblicazione SessionState
  ↓                         ↓
LinearizationMetricSource   LinearizationComparisonView
  ↓                         proiezioni/glyph, senza ricalcolo
resolver centrale F06
  ↓
FormulaPresenter / HUD / semantic registry

LessonSession: orchestrazione, policy, fatti e condizioni; nessuna dinamica/derivata
```

`IDifferentiableDynamics` è la capability **opzionale prevista da F12**, estende IDiscreteDynamics con StateJacobian/InputJacobian. Non obbliga ogni modello a fornire derivate o secondi. PendulumDynamics implementa i Jacobiani analitici e mantiene Evaluate invariato. NumericMatrix è un contenitore puro immutabile, finito, con shape e sola moltiplicazione per vettore: supporto minimo della ricorrenza, nessun MatrixMetric generale, inversa o solver.

`SimulationResult` conserva il provider differenziabile della propria configurazione e la costanza della propria storia già valida. `IsConstantStateInput` descrive la costanza dell'intero rollout finito; non è un solver di equilibrio o certificato di stabilità. `ExperimentCapabilities.Linearization` è aggiunta senza cambiare i valori esistenti; provider editoriale pendolo e sessione concordano.

`LinearizationComparison` possiede **entrambi i SimulationResult originali**, quindi configurazioni, campioni, output e costi eventuali restano attribuiti alle revisioni corrette. Congela T matrici A/B e Δu, T+1 differenze reali/lineari, previsioni ed errori. Tutti i valori sono calcolati nel layer puro una sola volta alla nuova coppia; seek/playback riusano l'oggetto. Nessun consumer conserva una propria copia della matematica.

La nominale si cattura tramite CaptureComparison F07, senza un secondo archivio o comando concorrente. Modello diverso per identità immutabile o orizzonte diverso → confronto lineare N/A con diagnostico e richiesta di nuova cattura, mai reinterpretazione della nominale. Reset della Lesson conserva la policy esistente che rimuove il confronto; reset diretto di Simulation continua a seguire il contratto F07.

**Ingressi autonomi e Previous:** il contratto F08 ripristina preset e rimuove il confronto su Indietro. Ogni attività F12 che necessita della linearizzazione ora ricostruisce nei propri EntryActions la nominale di equilibrio e i dati necessari: Δu0=1 per Jacobiani/ricorrenza/confronto input; Δθ0=.1 e t=1 per l'attività sull'ampiezza; .8 e t=1 per errore/terminale. Si usano soltanto Reset, SetInitialState, CaptureComparison, SetInput e Seek esistenti. Tra attività possono esserci nuove revisioni di una nominale matematicamente identica; **durante gli edit della stessa attività il medesimo oggetto nominale resta congelato**. Le vecchie coppie trattenute non cambiano. Gli ingressi editoriali non accreditano azioni dell'utente; condizioni `linearization.available` impediscono di proseguire con un confronto assente. Nessuna modifica a Enter/Previous o al lifecycle comune.

Una nuova simulazione fallita conserva risultato valido **e la sua coppia lineare**. Un confronto non calcolabile, dopo un rollout valido, diventa N/A diagnosticato senza falsificare la validità di quel rollout. Vecchie pubblicazioni trattenute mantengono A e B propri dopo modifiche successive. Output F11 rimane immutabile e autorevole, non ricalcolato da Formula o visualizzazione.

Nuovi comandi SetInitialPerturbation/SetInputPerturbation: ampiezza **assoluta rispetto alla nominale**. Δu modifica soltanto la transizione selezionata, conserva le altre componenti e non accumula ogni evento slider. NaN/∞, componente fuori range, input terminale, confronto assente o editing dopo request fallita sono rifiutati prima di creare una revisione. Ripetere lo stesso valore è un no-op, senza pubblicazione/revisione né credito educativo.

Core e Simulation conservano `noEngineReferences=true`; **13 assembly, grafo senza cicli**. Nessuna dipendenza esterna. I contratti di composition root, IExperimentSession, dispatch, scope di notifica, Ready, generation/lifetime e persistence non sono ridisegnati.

## Formula, resolver, unità e SampleIndex

Sei nuovi asset di formule: `c1.trajectory`, `c1.pendulum-equilibrium`, `c1.trajectory-perturbation`, `c1.pendulum-jacobians`, `c1.linearized-dynamics`, `c1.linearization-error`, tutti v1/schema1. Riutilizzata anche la formula Euler esistente. Formule contestuali precedono ogni domanda; lista live e dettaglio selezionabile restano nel Formula HUD condiviso.

La matrice 2×2 A e la 2×1 B usano **authoring strutturato F10 con celle Scalar**, non un vettore artificiale o Matrix runtime. Simboli compatti A₀₀…B₁₀, righe/colonne spiegate, valori/unità e campione nei dettagli. Il resolver centrale conserva l'unica regola di tipizzazione A02.

| Metrica `linearization.*` | Kind / shape | Campione / unità |
| --- | --- | --- |
| nominal-state, delta-true, delta-linear, prediction, error | Vector n | @t, unità dello stato; pendolo [rad, rad/s] |
| nominal-input, delta-input | Vector m, anche m=1 | @t, unità input; N·m nel pendolo; N/A a T |
| delta-initial | Vector n, Snapshot | Nessun SampleIndex artificiale |
| delta-next | Vector n, NextTimestep | @t+1, offset applicato una sola volta; N/A a T |
| fixed-initial | Scalar 1/0, Snapshot | [1], nessun SampleIndex artificiale |
| a.r.c / b.r.c | Scalar | @t, unità stato-riga / stato-o-input-colonna; N/A a T |

Unità esatte delle celle: A00/A11=`1`, A01=`(rad)/(rad/s)`, A10=`(rad/s)/(rad)`, B00=`(rad)/(N·m)`, B10=`(rad/s)/(N·m)`. Nessuna conversione automatica. Scalar dell'intero Vector n=1 resta Invalid; componente esplicita valida, componente fuori range e unità/tipo errati diagnosticati.

Senza nominale/capability compatibile le metriche sono NotApplicable con shape/unità note e diagnostico. Al terminale predizione/errore/nominale @T rimangono disponibili; Δu, next e A_T/B_T **non esistono**, quindi nessun valore o SampleIndex inventato. `SessionState.Select(T)` conserva la stessa coppia per lo scope Terminal. t resta indice, δ=.02 s e τ=tδ nel pendolo; la dinamica differenziabile index-only di test conserva δ/τ assenti.

Semantic roles generici: `linearization.nominal`, `.prediction`, `.delta`, `.error`, `.input`. Formula e registry risolvono revisione/campione della stessa pubblicazione; un termine di una revisione trattenuta non evidenzia una rappresentazione di una revisione diversa. Nessun FindObjectOfType di renderer concreti nel Formula system.

## Interazione e visualizzazione

`PerturbationPanelPresenter` compone cattura nominale, status, campi numerici e slider per le dimensioni/unità del modello. Gli slider hanno range **di presentazione −1…1**; i campi DoubleField consentono altri valori finiti. Il thumb viene limitato graficamente, il dato matematico non viene saturato. I valori mostrati provengono dal resolver. Dopo editing rifiutato si ripristinano i valori pubblicati con feedback; dopo request fallita si disabilita l'editing, indicando Reset/recupero. Al terminale il gruppo Δu è nascosto e lo status dichiara N/A.

Il pannello è vicino a formula/istruzione/domanda, con disclosure dei soli controlli pertinenti. È un **opt-in editoriale tramite i nuovi control ID**, non l'attivazione automatica per ogni pendolo: prototipo/C1.L1 mantengono la precedente superficie di focus anche in esplorazione libera. I nuovi slider possiedono le frecce quando focalizzati; non generano contemporaneamente uno step del router.

`LinearizationComparisonView` legge solo i campioni salvati: nominale viola, rollout vero blu, previsione ambra, Δx ciano e errore rosso. TrajectoryRenderer riceve anche liste derivate nel dominio della storia reale; non si costruisce una finta traiettoria «dinamicamente valida» della previsione. StateProjection proietta gli spostamenti usando le scale degli assi senza offset o tempo artificiale.

Δx/errore sono vettori **nel piano di fase (θ,ω)**, non frecce nello spazio fisico. Il tempo/stato 3D e il pendolo fisico conservano la semantica precedente. Δu riusa un glyph curvo di coppia, nelle unità dichiarate, senza suggerire una forza cartesiana. Errore ingrandito **×20 soltanto nel grafico**, dichiarato nella legenda e nei dati editoriali; il HUD conserva il valore reale. Al terminale la lesson nasconde il layer della freccia ingrandita e mantiene e_T numerico, evitando una freccia fuori viewport per l'errore accumulato.

I nuovi consumer operano dentro la fanout controllata di ExperimentVisualization. Un valore finito double non rappresentabile come float determina NonRepresentable dell'intero gruppo: nessun rendering parziale, stesso risultato matematico nel HUD, completion bloccata e Reset disponibile. Legenda F12 riposizionata dopo ispezione nativa; nessun refactor del viewport o layout generale.

## Sequenza didattica e review

Per ciascuno dei dodici step sono stati riletti vocabolario, formula prima della domanda, osservabile, previsione, interazione e verifica assumendo soltanto C1.L1. Esito positivo per tutte le sette domande della review richiesta. Le risposte e i feedback sono dati editoriali; non stringhe specifiche nel presenter.

| # / ID | Concetto dominante e prerequisito introdotto | Formula / interazione / riscontro | Condizione e ragionamento verificato |
| --- | --- | --- | --- |
| 1 trajectory | Coppia stato-input, distinta dalla curva di fase; richiama solo i campioni F11 | Traiettoria valida; seleziona u, Step | Termine + avanzamento + risposta: stati/input soddisfano tutte le transizioni |
| 2 equilibrium | Traiettoria costante, diversa da pausa | x_e=f(x_e,u_e), nominale zero; seleziona stato e Step | Costanza dell'intero rollout + azione + risposta pertinente |
| 3 balance | Equilibrio a θ=π/2, velocità nulla; errata esplicita | u_e=mgl sin θ_e; input sulla coda a 9.81 | Input realmente cambiato, rollout costante; coppia bilancia gravità, non angolo necessariamente zero |
| 4 nominal | Traiettoria valida come punto di sviluppo; non riferimento desiderato | Scegli eventualmente x0/u, cattura nominale, seleziona x̄ | Cattura nello step + coppia disponibile + termine + risposta sulla validità |
| 5 differences | Δ significa differenza, non derivata; Δx0=0 fissato | Δx/Δu; Δu0=1, seleziona Δu | Input cambiato + iniziale fissato + termine + risposta vettore zero |
| 6 jacobians | Sensibilità parziale; righe/colonne definite prima della domanda | A/B esplicite e matrici selezionabili; A10, B10 | Entrambe le selezioni + risposta: valutazione sulla nominale |
| 7 linear-model | Ricorrenza lineare, = distinto da ≈ | B0·1=[0,.02]; seleziona Δxᴸ_next | Termine + previsione della velocità al primo passo |
| 8 compare-input | Confronto allo stesso campione; primo passo affine nell'input | Step a 1; seleziona e | Avanzamento + termine + risposta: coincidenza locale non esattezza globale |
| 9 initial-perturbed | Secondo caso: iniziale modificato esplicitamente | Δθ0=.1, Δω0=0; Step; leggi errore non nullo | Iniziale cambiato/perturbato + avanzamento + errore + termine + risposta che nega Δx0=0 |
| 10 amplitude | Limite locale sulla stessa nominale | A t=1, .1→.8; confronto numerico e grafico ×20 | Iniziale realmente cambiato + errore non nullo + risposta località |
| 11 error-meaning | Errore di previsione distinto da residuo dinamico | e=x−x̂; selezione/legame semantico | Termine + risposta: entrambi i rollout restano validi |
| 12 terminal | Termine del confronto senza transizione extra | Seek T=100, seleziona Δu N/A | Terminale live + selezione + risposta corretta + Concludi verifica |

Le ampiezze .1/.8 e i campioni consigliati sono esperimenti riproducibili dichiarati, non hardcoding dei manager. La completion valuta cambiamenti reali e comprensione; non richiede l'uguaglianza di un float slider con una costante editoriale. Risposte errate forniscono feedback e non avanzano; focus editoriale non equivale a selezione utente, no-op non accredita l'azione. Previous/Restart/Reset seguono i contratti F08; esplorazione libera non completa la guida.

## Test matematici indipendenti

23 casi in `LinearizationTests`, 17 in `ProductionLinearizationTests`, nove PlayMode in `ProductionLinearizationRuntimeTests`: **40 nuovi EditMode e nove nuovi PlayMode** rispetto a F11.

| Oracolo | Atteso / verifica |
| --- | --- |
| θ̄=0, m=l=1, g=9.81, δ=.02 | A=[[1,.02],[−.1962,1]], B=[[0],[.02]], shape/orientamento espliciti |
| m=2,l=3,δ=.03, tre angoli | Jacobiani contro differenze finite centrali della dinamica, perturbazioni indipendenti delle componenti |
| θ̄=.6 | A10=−.161930847645279, da nominale anche dopo perturbazione; variazione lungo la nominale |
| θ=0,u=0; θ=π/2,u=9.81 | Equilibrio esatto su tutti i 100 passi, non singolo frame |
| Δx0=0, Δu0=1 Single | Δxᴸ1=[0,.02], Δxᴸ2=[.0004,.02], primo errore zero |
| Δθ0=.1, u=0, t=1 | ω vero=−.019587316346107685; lineare=−.01962; eω=.00003268365389231713 |
| Δθ0=.8, u=0, t=1 | ω vero=−.1407452650344864; lineare=−.15696; eω=.01621473496551364 |
| Nominale θ=π/6, ε=.001/.0005 | eω/ε²→.04905, rapporto→4: errore locale di secondo ordine |
| Nominale θ=0, ε=.01/.005 | Rapporto→8: qui il termine quadratico si annulla, leading error cubico |
| Dinamica index-only x_next=x+u | Nominale 2+3=5; Δu=1 → vero/lineare 6, errore zero, δ/τ assenti |

Altri casi: immutabilità/copia matrice, shape/finite/range invalidi, NaN/±∞ senza revisione, no-op, editing Single e terminale; coppie trattenute A/B dopo errore, seek senza ricalcolo; modello/orizzonte modificati N/A fino a ricattura; Scalar/Vector n=1, unità/celle/indici/terminale, scope Terminal/Next senza doppio offset; produzione, ID/versioni, prerequisiti consultivi/separati, sequenza/disclosure, no costi; completion/reload e mancata promozione di lesson incomplete.

I PlayMode usano il Bootstrap **salvato**, senza sostituire corso/lesson/esperimento; isolano soltanto lo store in memoria. Verificano percorso completo L1→L2, UI e slider, focus, errore rifiutato, same-publication HUD/visualizzazione, campioni proiettati contro oracoli, highlight, Previous/Restart/uscita/riapertura, request T=1 fallita da cursor50, NonRepresentable e recovery, terminale e progresso senza duplicati.

## Risultati finali e regressioni

| Verifica | Risultato |
| --- | --- |
| Unity 6000.3.8f1, compilazione finale senza helper | **PASS, exit 0; zero errori/warning C#** |
| Tutti gli EditMode | **356/356 PASS**, zero falliti/saltati: catalogo 64, Formula 123, Lesson 80, Mathematics 81, Visualization 8 |
| Tutti i PlayMode | **89/89 PASS**, zero falliti/saltati |
| Conservazione suite F00–F11 | Tutti i **316 EditMode + 80 PlayMode** preesistenti presenti per fullname e passanti; nessun test ignorato/rimosso |
| Percorso nativo di produzione | PASS: Bootstrap normale → L1 completa → L2 dodici step → completion → menu → riapertura → restart Play |
| Nuovo processo Unity | PASS: file di progresso ripristina esattamente L1/L2; sessione fresca a trajectory/t0, nominale assente |
| Regressione nativa pendolo/prototipo | PASS: sette step F08, costi, A/B, terminale, reload, index-only 2+3=5/J=38, failure, NonRepresentable e Enter fallito |
| Regressione nativa statica F10 | PASS: 9→12→20, formula strutturata, istantanea A, nessun tempo/storia inventato, completion separata, navigazione durante notification |
| Integrità / assembly | **485 file applicativi** identici per SHA-256 alla copia verificata; zero extra; 13 assembly aciclici, Core/Simulation puri |

Una prima compilazione dei nuovi test ha rilevato un argomento ModelDimensions mancante; un helper esterno aveva un SessionState ambiguo. Corretti prima dei run riusciti. La prima suite da 87 PlayMode era tutta verde. Nella successiva suite da 88 casi il test tastiera legacy ha perso un evento LeftArrow; nell'isolamento ha perso anche D. La diagnostica ha dimostrato **zero eventi InputAction e keyboard.enabled=false**: il focus OS/Game View dell'Editor batch disabilitava il device sintetico. Processare esplicitamente gli eventi non risolveva la disabilitazione; quel tentativo e la diagnostica sono stati rimossi, ripristinando `VisualizationTests.cs` byte per byte. `ProgressStoreIsolation` ora clona temporaneamente InputSettings, abilita input sintetico senza focus OS e instradamento alla Game View, poi ripristina/distrugge la copia. Nessun asset di settings o guardia del focus del HUD viene alterato. Corretto anche un nome enum del package durante questa verifica. **7/7 Visualization PASS**, poi intera suite rieseguita; nessuna asserzione legacy indebolita. Indipendentemente, l'opt-in editoriale del nuovo pannello evita di aggiungere controlli/focus alle lesson precedenti. XML/log intermedi conservati nella cartella temporanea.

Gli unici adeguamenti ai test F11 sono la ricerca di C1.L1 tramite **ID** anziché Single sull'intero capitolo e l'aspettativa esplicita delle due lesson nel menu. Tutti gli assert matematici e di completamento precedenti sono preservati.

La review finale ha inoltre individuato la dipendenza implicita dalla nominale dello step precedente quando si usava Indietro. Corretta nei soli dati editoriali F12 mediante ingressi autonomi. Aggiunti un EditMode che visita **tutti gli step precedenti** verificando configurazione/nominale/previsione e un PlayMode che ripete l'attività .1→.8 dopo Previous. Il percorso nativo esegue lo stesso ritorno e verifica assenza di credito automatico. Una riscrittura YAML intermedia aveva serializzato i riferimenti Unity come mappe a blocchi, impedendo il caricamento corretto di C1.L2 e facendo fallire 20 test: ripristinati i PPtr inline, senza cambiare GUID o identit?. Tutte le suite e le prove native sono state ripetute dopo queste correzioni.

**A01:** δ pendolo .02, τ coerente; index-only senza secondi. **A02:** resolver unico, Scalar/Vector/componenti/unità/indice, N/A tipizzato. **A03:** binding prima di Ready, identità, notification scope, teardown sicuro e generation guards conservati. **A04:** configurazione/output/campioni e confronto locale appartenenti alle proprie revisioni, anche dopo fallimento. **AF08-01/02/03:** publication isolation, NonRepresentable, dispatch controllato, Enter fallito senza completion; **04:** editing della request fallita bloccato; **05:** metadati provider/unità validati; **06:** resolver/provider composti coerentemente, custom provider regressioni verdi; **07:** testi completion editoriali e presenter generico. Nessuno di questi meccanismi è semplificato.

## Verifica nativa, evidenze e limiti della verifica

Editor reale su copia isolata con gli stessi 485 file applicativi, preservando l'Editor aperto dell'utente. Nel percorso di produzione **nessuna sostituzione di course/lesson/experiment**, soltanto FileProgressStore su un file di profilo temporaneo. I probe di regressione selezionano esplicitamente i cataloghi separati prototipo/fixture come strumenti di verifica, senza cambiare Bootstrap consegnato.

Interazioni native automatizzate tramite i callback/eventi dei controlli UI; catture Game View ispezionate visivamente. Non si dichiara una sessione di click manuali nell'Editor dell'utente, un usability study o un Player build. Il percorso negativo prima della completion verifica wrong answer, uscita incompleta/riapertura, overflow con request corta, input edit rifiutato, NonRepresentable e Reset/Restart; **nessun record C1.L2** prima del percorso valido. Nessun Error/Exception/Assert nei percorsi nativi riusciti.

Acquisite 26 catture del percorso e una del nuovo processo; **17 selezionate** archiviate in [F12_EVIDENCE](F12_EVIDENCE/), insieme a XML completi, riepilogo/log hash, risultati nativi, manifest applicativo e inventario. Alcune catture utili:

- [Menu di produzione dopo C1.L1](F12_EVIDENCE/production-menu-prerequisite.png).
- [Equilibrio ad angolo non nullo](F12_EVIDENCE/production-equilibrium-balance.png).
- [Jacobiani strutturati e unità live](F12_EVIDENCE/production-jacobians-matrix.png).
- [Piccola perturbazione](F12_EVIDENCE/production-small-perturbation.png), [errore numerico](F12_EVIDENCE/production-small-live.png).
- [Ampiezza .8 e confronto visivo](F12_EVIDENCE/production-large-perturbation.png), [errore live](F12_EVIDENCE/production-large-live.png).
- [Terminale N/A con t/δ/τ](F12_EVIDENCE/production-terminal-live.png).
- [Progresso persistente](F12_EVIDENCE/production-progress.png), [nuovo processo](F12_EVIDENCE/production-process-reloaded.png).
- [Request fallita](F12_EVIDENCE/production-failed-request.png), [NonRepresentable](F12_EVIDENCE/production-nonrepresentable.png).

Log completi e helper esterni: `C:/Users/querz/AppData/Local/Temp/OptCon-F12-2625bc843fd347d786033d6bc2ff539b/`. Helper rimossi dagli Assets della copia prima delle suite/compilazione finali; nessun helper consegnato.

## File creati

Percorsi relativi ad `Assets/OptimalControl/`: i seguenti **19 file**, ciascuno con `.meta` (38), più `Runtime/Core/Derivatives.meta`: **39 nuovi file applicativi**.

```text
Runtime/Core/Derivatives/IDifferentiableDynamics.cs
Runtime/Core/Numerics/NumericMatrix.cs
Runtime/Simulation/LinearizationComparison.cs
Runtime/Content/Bindings/LinearizationMetricSource.cs
Runtime/UI/PerturbationPanelPresenter.cs
Runtime/Visualization/LinearizationComparisonView.cs
Tests/EditMode/Mathematics/LinearizationTests.cs
Tests/EditMode/Formula/ProductionLinearizationTests.cs
Tests/PlayMode/ProductionLinearizationRuntimeTests.cs
Data/Experiments/PendulumLinearization.asset
Data/Lessons/Chapter01/C1_L2.asset
Data/Formulas/Chapter01/SystemTrajectory.asset
Data/Formulas/Chapter01/PendulumEquilibrium.asset
Data/Formulas/Chapter01/TrajectoryPerturbation.asset
Data/Formulas/Chapter01/PendulumJacobians.asset
Data/Formulas/Chapter01/LinearizedDynamics.asset
Data/Formulas/Chapter01/LinearizationError.asset
Prefabs/PendulumLinearizationScenario.prefab
Materials/LinearizationError.mat
```

Documentazione: questo report e **26 file** sotto `Docs/REVIEWS/F12_EVIDENCE/` (17 PNG, due XML, due JSON, cinque TXT): **66 nuovi file complessivi**. L'inventario JSON elenca ogni percorso singolarmente.

## File modificati

**22 applicativi**, relativi ad `Assets/OptimalControl/`:

```text
Data/Chapters/Chapter01.asset
Runtime/Core/Models/PendulumDynamics.cs
Runtime/Simulation/IExperimentSnapshot.cs
Runtime/Simulation/SimulationCommands.cs
Runtime/Simulation/SimulationResult.cs
Runtime/Simulation/SimulationSession.cs
Runtime/Content/Bindings/RuntimeMetricResolver.cs
Runtime/Content/Models/PendulumModelDefinition.cs
Runtime/Content/Definitions/LessonStepDefinition.cs
Runtime/Content/Lessons/LessonCapabilityRequirements.cs
Runtime/Content/Lessons/LessonPresentationState.cs
Runtime/Lessons/LessonConditionRegistry.cs
Runtime/Lessons/LessonSession.cs
Runtime/UI/LessonHudPresenter.cs
Runtime/Visualization/CurvedTorqueRenderer.cs
Runtime/Visualization/ExperimentVisualization.cs
Runtime/Visualization/StateProjection.cs
Runtime/Visualization/TrajectoryRenderer.cs
UI/LessonHud.uxml
Tests/EditMode/Formula/ProductionIntroductionTests.cs
Tests/PlayMode/ProductionIntroductionRuntimeTests.cs
Tests/PlayMode/ProgressStoreIsolation.cs
```

Più `Docs/OPTCON_IMPLEMENTATION_PLAN.md`: **23 modificati**, zero eliminati. Nessun `.meta` preesistente modificato; nessuna modifica a scene, Bootstrap, vecchi prefab/materiali/lesson/esperimenti/formule, cost system, storage, CourseManager, PDF, visione/curriculum/architettura, vecchi report, Packages o ProjectSettings. Non sono trasferite eventuali migrazioni automatiche della copia Unity.

## Procedura manuale in Unity

1. Aprire `Assets/OptimalControl/Scenes/Bootstrap.unity`, attendere compilazione, Play. Nel corso **Optimal Control / Chapter 1** cliccare **Traiettorie, equilibri e linearizzazione**. Per la sequenza consigliata completare prima «Stato, input, output e tempo». Se manca C1.L1 compare il prerequisito consultivo; non cambiare Course nell'Inspector.
2. Step 1: selezionare u_t nella lista Matematica live, **Step →**, risposta sulla coppia che soddisfa tutte le transizioni, **Avanti**. Formula/istruzione restano sopra la domanda; il pannello laterale scorre per i dettagli.
3. Step 2: selezionare stato, Step, osservare [0,0] invariato mentre t avanza; risposta sulla traiettoria costante. Step 3: aprire **Input e parametri**, scegliere **coda** e impostare u[0]=9.81; la curva resta un punto a θ=π/2. Rispondere su velocità/accelerazione nulle.
4. Step 4: eventualmente scegliere x0/input, premere **Salva traiettoria nominale**, selezionare x̄_t, rispondere che deve soddisfare la dinamica. Step 5 ripristina e cattura l'equilibrio zero: impostare **Δu_t[0]=1** a t=0, selezionare Δu, risposta Δx0=0.
5. Step 6: leggere formula esplicita e, nella Matematica live, le matrici A/B; selezionare **A₁₀** e **B₁₀**. Valori −.1962 e .02, unità e @0. Risposta: nominale. Step 7 selezionare Δxᴸ_next e prevedere [0,.02]. Step 8 fare Step, selezionare e: primo errore zero, senza dedurre esattezza globale.
6. Step 9: campo **Δx₀[0]=.1**, lasciare **Δx₀[1]=0**, Step a t=1, selezionare e. Vero ω≈−.01958731635, lineare −.01962, errore≈.00003268365. Risposta: iniziale perturbato distinto dal caso fissato.
7. Step 10: a t=1 portare il medesimo campo a **.8** (anche con slider); errore≈.01621473497. Nominale/Jacobiani invariati, curve blu/ambra divergono; freccia errore ×20 dichiarata. Risposta: approssimazione locale. Step 11 selezionare e e distinguerlo dal residuo dinamico.
8. Step 12: timeline t=100. Selezionare Δu: **N/A**, come next e A/B; previsione/errore @100 disponibili, δ=.02 s e τ=2 s. Risposta corretta e **Concludi verifica**. Tornare al corso: completion C1.L2 salvata. Riaprire/fermare e riavviare Play: progresso conservato, runtime fresco allo step 1 senza nominale.
9. Percorso negativo: rispondere «qualunque curva» al primo step; Avanti resta bloccato, feedback spiega l'errore. Uscire e riaprire senza completare: nessun nuovo progresso. Previous ripristina il passo precedente; Ricomincia ritorna al preset/step 1. Nell'esplorazione libera nessuna completion guidata.
10. Verifiche d'errore deterministiche sono nei test PlayMode: risultato T100/cursor50 → iniziale overflow → request T1 fallita → editing Δu/Δx disabilitato, risultato/coppia precedenti conservati → Reset. Valore 1e40 finito in esplorazione con nominale produce NonRepresentable visuale, HUD autorevole, completion bloccata → Reset. NaN rifiutato prima di una nuova revisione, campo ripristinato.
11. Window → General → Test Runner: eseguire **tutti** gli EditMode e PlayMode, non soltanto i tre nuovi gruppi.

## Limiti residui e decisioni

Nessun blocco architetturale nuovo. Le modifiche ai contratti precedenti sono additive e richieste dalla capability F12: derivate opzionali, coppia immutabile, nuove metriche/comandi/controlli. Nessuna deviazione funzionale dal piano; differenza dal prompt sul lock spiegata sopra con la precedenza richiesta.

Limiti intenzionali: linearizzazione discreta del solo pendolo produttivo e fixture additiva di test; confronto richiede medesimo modello immutabile/orizzonte, altrimenti ricattura. Nessun parametro generale di tolleranza per «quasi equilibrio»: il caso didattico usa equilibri esatti. UI numerica G6, slider float e campo double; viewport fisso e scroll ereditati. Errori grandi possono uscire dal viewport nell'esplorazione libera; non si introducono clamp matematici o riscalature nascoste. Il confronto consigliato a t=1 è leggibile; la freccia ingrandita è nascosta nel passo terminale. Nessuna generalizzazione anticipata del layout o delle capacità future del curriculum.

## F12 PRODUCTION GATE

1. **Raggiungibile dal normale Bootstrap? SÌ.** Corso optimal-control → C1 → C1.L2, senza Inspector/helper/fixture.
2. **Prerequisito C1.L1 funzionante? SÌ secondo F09.** ID stabile, avviso quando manca, soddisfatto dalla sua completion; policy consultiva, nessun hard lock promesso.
3. **Prototype/fixture separati? SÌ.** Cataloghi/ID/progressi distinti, nessuna promozione o soddisfacimento del prerequisito.
4. **Domande risolvibili con quanto già mostrato? SÌ.** Review di tutti i dodici step e feedback errati; solo prerequisiti insegnati in C1.L1.
5. **Formule necessarie disponibili prima delle domande? SÌ.** Espressione contestuale, matrice strutturata e termini live selezionabili.
6. **Nuova matematica con known-answer indipendente? SÌ.** Jacobiani, ricorrenza, equilibri, errori/ordine, index-only; nessun confronto soltanto con la medesima chiamata implementativa.
7. **Completion/persistence sull'ID corretto? SÌ.** optimal-control/C1.L2 v1/schema1, reload FileProgressStore e nuovo processo, nessun record dopo percorsi negativi.
8. **Regressioni F00–F11 tutte passanti? SÌ.** 356 EditMode + 89 PlayMode, inclusi tutti i precedenti 316+80; prove native pendolo/index-only/statiche e lifecycle.
9. **Evitata l'implementazione anticipata di F13? SÌ.** Nessun costo/vincolo/riferimento o solver nuovo; nessun asset C1.L3/L4.

**READY FOR F13.**
