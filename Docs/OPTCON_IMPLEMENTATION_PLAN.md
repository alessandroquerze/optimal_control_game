# Optimal Control — roadmap operativa

Data della revisione: 11 ottobre 2026. **31 fasi, da F00 a F30.** **F00–F07 completate e verificate il 9 ottobre 2026; F08–F10 il 10 ottobre 2026; F11 l'11 ottobre 2026.** F12–F30 restano pianificate e non avviate. La revisione documentale iniziale è conservata; l'implementazione corrente comprende foundation, catalogo, navigazione, core matematico/simulativo puro, sessione runtime, primitive visuali, controlli della dinamica, Formula/HUD live con binding tipizzati, costi discreti, target, confronto A/B, primo vertical slice didattico con Lesson Logic, validazione consolidata, progressione persistente versionata, authoring strutturato verificato su un esperimento statico e C1.L1 di produzione raggiungibile dal Bootstrap predefinito.

## Specifica, fonti e risultato della revisione

La specifica principale è costituita da [visione](OPTCON_PROJECT_VISION.md), [curriculum](OPTCON_CURRICULUM.md), [architettura](OPTCON_ARCHITECTURE.md) e questa roadmap. La roadmap sostituisce la precedente suddivisione D0 / Foundation F1 / Prototype P1 / Foundation F2 / Content production / Polish, mantenendone obiettivi e perimetro. Le precisazioni operative qui riportate risolvono i punti aperti senza modificare gli altri tre documenti.

Il progetto usa Unity `6000.3.8f1`, URP `17.3.0`, Input System `1.18.0`, UI Toolkit disponibile tramite i moduli Unity e Test Framework `1.6.0`. F00 aggiunge `Bootstrap`, `LessonWorkspace` e `AppBootstrap` al template, conservando `SampleScene` e gli asset URP. F01 aggiunge definizioni e catalogo dati con validazione editor. F02 aggiunge il menu e il caricamento del workspace con titolo/obiettivo dagli asset. F03 aggiunge pendolo Euler, rollout e snapshot puri. F04 collega provider, sessione, comandi e clock al lifecycle runtime e risolve A03/A04; A01 resta verificato. F05 aggiunge corpo/ghost, piano di fase, traiettoria stato-tempo 3D, coppia, marker, assi, layer/highlight e router da tastiera. F06 risolve A02 tramite un resolver puro tipizzato e aggiunge formule/termini live, highlight, timeline e pannello input/parametri dalla stessa revisione immutabile. F07 aggiunge costo quadratico discreto, contributi/cumulativo/terminale/J, target e confronto A/B immutabile usando lo stesso resolver e risultato. F08 aggiunge Lesson Logic data-driven, disclosure, controlli filtrati, condizioni/risposte, reset deterministico e percorso didattico completo del prototipo.

| Fonte | Percorso effettivo | Pagine fisiche | Riferimenti ricontrollati |
| --- | --- | --- | --- |
| I | [Introduction to Optimal Control](OPTCON-RL_introduction_optimal_control_2026_09_14.pdf) | 36 | Stato/dinamica pp. 8–12; costo pp. 20–22; drone/Euler pp. 26–30; continuo/pendolo pp. 31–36 |
| O | [Nonlinear Optimization](OPTCON-RL_optimization_2026_09_14.pdf) | 81 | Convessità pp. 10–22; discesa/Newton pp. 27–46; proiezione pp. 48–52; barriere pp. 54 e 78; KKT/QP pp. 56–68; SQP/merito pp. 70–77 |
| K | [Optimality Conditions for Optimal Control](OPTCON-RL_optimality_conditions_KKT_2026_10_08.pdf) | 20 | Curve/manifold pp. 2–7; Lagrangiana e adjoint pp. 8–17; bordo/percorso pp. 18–20 |

I PDF sono invariati rispetto all'analisi iniziale. La lettura integrale dei quattro documenti e il confronto con le fonti confermano la struttura **6 + 8 + 4 lezioni**, la dinamica del pendolo, il costo additivo e il segno delle ricorrenze KKT. Restano valide le rettifiche motivate nel curriculum: in particolare equilibrio del pendolo, peso `w₂`, segno del passo Newton, spazio tangente `Ay=0`, merito con `h` e ipotesi dei risultati di convessità/ottimalità. Le pagine citate sono fisiche, contando la copertina; le slide progressive non diventano lezioni aggiuntive.

### Problemi individuati e decisioni operative

| Punto | Problema o ambiguità | Decisione / fase di verifica |
| --- | --- | --- |
| Responsabilità delle prime fasi | La vecchia Foundation comprendeva dati, matematica, loader, UI e visualizzazione insieme | Separare F00–F08 in consegne eseguibili e verificabili |
| Dipendenze degli assembly | Se UI e definizioni dei passi referenziassero implementazioni in Application, si creerebbero cicli con Content/Presentation | Contratti dei comandi matematici in Simulation; richieste di navigazione e descrittori delle condizioni in Content; implementazioni/evaluatori in Application. Verifica in F01, F04 e F09 |
| Binding temporale | `state.next` è già `x_(t+1)`; applicare nuovamente uno scope Next potrebbe mostrare `x_(t+2)` | Ogni valore espone l'indice campione risolto. Lo scope individua il target una sola volta; valore e highlight usano lo stesso indice. Verifica in F06 e F08 |
| Rappresentazione 3D | Il moto del pendolo e il piano di fase sono planari; la traiettoria 3D non era definita abbastanza precisamente | Aggiungere il grafico stato-tempo `(θ_t,ω_t,τ_t)` nel viewport 3D, con tre assi/unità espliciti, accanto a corpo e piano di fase. Verifica in F05 |
| Valori di formula | `f_t` è un descrittore del modello, mentre `x_t` e `u_t` sono valori numerici; uno stato può contenere unità diverse | Binding distinti per valore e descrittore; unità per componente; nessun valore numerico fittizio di `f_t`. Verifica in F06 |
| Snapshot di ottimizzazione | Lo snapshot iniziale è centrato su stati/transizioni, ma un obiettivo statico e le iterazioni del Chapter 2 non hanno necessariamente t, δ o input dinamici | Un contratto comune di snapshot espone revisione, metriche e selezione; dati di traiettoria e dati di ottimizzazione sono capability distinte, con tempo opzionale. Verifica in F10/F18, senza timeline fisiche artificiali |
| Ampiezza del slice | Solver e strumenti avanzati rischiano di precedere la validazione della pipeline | Mantenere un solo pendolo, input manuali e due formule principali. Conservare costo/A-B come anteprime limitate richieste dalla visione, in F07 |
| Ordine dei capitoli | La generazione ottima di C1.L4/L6 dipende da metodi che verranno sviluppati nei capitoli successivi | Chapter 1 inizialmente usa input manuali e confronti dichiarati; F28 integra gli esperimenti di ottimizzazione già verificati. Nessun solver duplicato anticipato |
| Fonte nel runtime | I PDF in Docs non sono automaticamente contenuto di una build Unity | SourceReference conserva metadati/pagine nel runtime; percorso/hash del PDF sono validati nell'editor. Un visualizzatore PDF runtime non è prerequisito |
| Significato di «completato» | Un menu funzionante o una demo visiva non validano Lesson Logic | Il slice termina soltanto in F08; i completamenti del prototipo sono separati dalle 18 lezioni di produzione |

Il DAG da rispettare è: `Core → nessuna dipendenza Unity`; `Simulation → Core`; `Content → Core/Simulation quando necessari`; `Presentation → Content/Simulation`; `Application → Content/Simulation/Presentation`. Qui la freccia significa «dipende da». Editor e test hanno assembly dedicati. Core e Simulation non referenziano Content, Presentation o Application. Gli assembly vengono creati quando contengono i primi contratti o componenti effettivamente necessari.

## Come eseguire una fase

Nel seguito **A = `Assets/OptimalControl`**. I percorsi `A/...` sono relativi a questa root. «Creare» e «modificare» descrivono il lavoro della relativa fase; lo stato e i rapporti di verifica di F00–F07 distinguono il lavoro eseguito da quello ancora pianificato.

Una fase è conclusa quando i suoi criteri passano e il risultato è riproducibile partendo da Bootstrap o dalla verifica dichiarata per quella fase (editoriale per F01, numerica per il Core). Le dipendenze elencate sono dirette; i loro prerequisiti si intendono transitivi. L'ordine F00 → F30 è quello raccomandato. I test elencati sono da eseguire durante l'implementazione; per F00–F07 le verifiche Unity eseguite sono registrate sotto la fase.

Per ogni fase di contenuto: mantenere ID stabili, fonte/pagina/slide, obiettivo, formule selezionabili, esperimento, spiegazione delle unità/spazi e verifica concettuale. Un candidato numerico viene chiamato «ottimo» soltanto con una giustificazione adeguata alla classe di problema. I test numerici hanno tolleranze assolute/relative dichiarate; i controlli grafici restano manuali quando non richiedono test automatici significativi.

## Indice delle fasi

| Fase | Gruppo | Consegna principale | Dipendenze dirette |
| --- | --- | --- | --- |
| F00 | Foundation | Bootstrap, workspace e confini del progetto | Specifica revisionata |
| F01 | Dati | Definizioni corso/lezione/esperimento/formula e fonti | F00 |
| F02 | Course/lesson | Menu navigabile e caricamento lezioni | F01 |
| F03 | Math/simulation | Pendolo discreto, rollout e snapshot puri | F01 |
| F04 | Simulation in Unity | Host, comandi, clock e revisioni | F02, F03 |
| F05 | Visualization | Primitive comuni, traiettoria 3D e controlli della dinamica | F04 |
| F06 | Formula/HUD | Formula live, termini, valori e highlight | F01, F05 |
| F07 | Anteprime del slice | Costo, target e confronto A/B | F06 |
| F08 | Vertical slice | Lesson Logic e verifica completa della pipeline | F02, F07 |
| F09 | Infrastruttura | Validazione, lifecycle e progressione persistente | F08 |
| F10 | Infrastruttura | Authoring formule e prova di riuso su altro esperimento | F09 |
| F11 | Chapter 1 | C1.L1 completa: stato/input/output/tempo | F08, F10 |
| F12 | Chapter 1 | C1.L2: equilibri e linearizzazione | F11 |
| F13 | Chapter 1 | C1.L3–L4: costo/vincoli e riferimento/traiettoria | F07, F12 |
| F14 | Chapter 1 | C1.L5: drone planare | F13 |
| F15 | Chapter 1 | C1.L6: continuo e trasferimento del pendolo | F12, F14 |
| F16 | Chapter 2 | C2.L1: superfici, minimi, derivate e QP | F10, F15 |
| F17 | Chapter 2 | C2.L2: convessità, μ e L | F16 |
| F18 | Chapter 2 | C2.L3: discesa, passi e storia delle iterazioni | F17 |
| F19 | Chapter 2 | C2.L4: Newton e modello quadratico | F18 |
| F20 | Chapter 2 | C2.L5: proiezione e direzioni ammissibili | F13, F19 |
| F21 | Chapter 2 | C2.L6: KKT, moltiplicatori e QP vincolato | F20 |
| F22 | Chapter 2 | C2.L7: Newton vincolato, SQP e merito | F19, F21 |
| F23 | Chapter 2 | C2.L8: barriere e chiusura del capitolo | F22 |
| F24 | Chapter 3 | C3.L1: curve, manifold e tangenti | F12, F23 |
| F25 | Chapter 3 | C3.L2: Lagrangiana temporale | F21, F24 |
| F26 | Chapter 3 | C3.L3: adjoint e stationarity | F12, F18, F25 |
| F27 | Chapter 3 | C3.L4: vincoli temporali e di bordo | F21, F22, F23, F26 |
| F28 | Consolidamento | Traiettorie ottimizzate e copertura delle 18 lezioni | F14, F15, F26, F27 |
| F29 | Polish | Leggibilità, accessibilità e performance | F10, F28 |
| F30 | Consegna | Build, verifica completa ed estensione del corso | F09, F29 |

**Prima interazione Unity: F02**, attraverso il menu. **Prima manipolazione della dinamica: F05**. **Primo vertical slice completo: F08**.

## Perimetro e contratto del primo vertical slice

«Una coppia cambia il futuro del pendolo» rimane il laboratorio iniziale previsto per C1.L1, con anticipazioni di C1.L2/L3. Un catalogo di prototipo usa l'ID `prototype.pendulum`; la produzione di C1.L1 riutilizza l'esperimento in F11. Questo evita di registrare l'esecuzione di una demo come completamento automatico delle lezioni del corso.

Il sistema discreto, a due stati e un input, applica Euler esplicito al pendolo di I p. 33 usando il metodo di I p. 27. È un adattamento dichiarato del modello continuo, non una formula discreta attribuita direttamente alla slide del pendolo:

```text
x_t = [θ_t,ω_t]ᵀ,  u_t = coppia,  y_t = θ_t
θ_(t+1) = θ_t + δ ω_t
ω_(t+1) = ω_t + δ[−(g/l)sinθ_t + u_t/(m l²)]
τ_t = tδ
```

Preset mantenuto: `m=1 kg`, `l=1 m`, `g=9.81 m/s²`, `δ=0.02 s`, `T=100` transizioni, `x₀=[0.35 rad,0 rad/s]`, target `[0,0]`, input iniziali nulli. Il corpo usa θ; il piano di fase usa `(θ,ω)`; la traiettoria 3D usa `(θ,ω,τ)` con scale visive dichiarate. La terza coordinata è tempo, non una terza posizione fisica. Lo stato matematico resta bidimensionale e l'angolo non viene avvolto automaticamente.

F07 aggiunge soltanto il costo discreto `ℓ_t=½q_θe_θ²+½q_ωe_ω²+½r(u−u^des)²` e `ℓ_T=½q_Tθe_Tθ²+½q_Tωe_Tω²`, con i pesi già proposti `10,1,0.1,20,2`. Si tratta di costo per campione. Il fattore δ per una quadratura dell'integrale continuo viene introdotto in F15; il terminale viene contato una volta. Il cumulativo all'arrivo al campione j è `Σ_(i=0)^(j−1)ℓ_i`.

| Requisito del slice | Introdotto in | Prova finale in F08 |
| --- | --- | --- |
| Sistema discreto, x_t, u_t, x_(t+1) | F03–F04 | Confronto dei valori con il Core, incluso il terminale |
| Traiettoria 3D significativa | F05 | Tutti i punti derivano da stati/tempi salvati; assi/unità corretti |
| Controllo modificabile | F04–F06 | Una modifica ricalcola il futuro e aggiorna tutti i consumer |
| Avanzamento, play/pause/step/reset | F04–F06 | Stessa sequenza matematica a diverse velocità di riproduzione |
| Formula nel HUD e valori live | F06 | Formula, corpo, marker e input condividono revisione e timestep |
| Selezione/spiegazione dei termini | F06 | Simbolo, descrizione, unità, valore e target corrispondono |
| Rewind, scrub, costo, target e A/B | F04–F07 | Rewind dai dati, A immutato, costo terminale corretto |
| Lesson Logic | F08 | Previsione → azione → osservazione → spiegazione → verifica |
| Pipeline completa | F08 | Mathematical Model → Simulation → Visualization → Formula System → HUD → Lesson Logic |

Il slice usa primitive URP, un solo modello e input manuali. Superfici generali, Newton/SQP, costate, drone e rendering matematico complesso non sono prerequisiti di F08.

## F00 — Foundation del progetto e scene di ingresso

- **Stato:** **completata**, 9 ottobre 2026; tutti i criteri sotto verificati. Nessuna fase successiva avviata nella consegna originale di F00.
- **Obiettivo:** predisporre una root applicativa isolata e un punto di avvio riproducibile, riutilizzando la configurazione URP esistente.
- **Prerequisiti:** specifica letta; Unity/versioni e template verificati; nessun codice educativo da migrare.
- **Dipendenze:** nessuna fase precedente; dipende dalla revisione documentale corrente.
- **File/cartelle:** creare `A/Runtime/Application/AppBootstrap.cs`, relativo asmdef, `A/Scenes/Bootstrap.unity` e `LessonWorkspace.unity`; modificare `ProjectSettings/EditorBuildSettings.asset`. Conservare gli asset URP esistenti; creare solo le cartelle già utilizzate.
- **Sistemi Unity:** scene, MonoBehaviour di bootstrap, camera/luce/Volume URP, Build Settings; nessun nuovo pacchetto.
- **Risultato verificabile:** Play da Bootstrap apre il workspace con camera e spazio neutro; nessuna simulazione o UI di lezione ancora.
- **Criteri di completamento:** Console senza errori; un solo bootstrap persistente; dipendenze future degli assembly documentate; SampleScene ancora disponibile.
- **Test:** controllo manuale di importazione, avvio/arresto Play e doppio avvio; nessun test automatico che replichi la sola creazione delle cartelle.

### Consegna e verifica di F00

`OptCon.Application.asmdef` contiene soltanto il bootstrap e ha `references: []`: non referenzia assembly ancora inesistenti. Le dipendenze future ammesse sono `OptCon.Content`, `OptCon.Simulation` e `OptCon.Presentation`, da aggiungere quando le fasi successive introducono codice che le usa. Resta valido il DAG sopra: Core senza Unity, Simulation dipendente da Core, nessuna dipendenza inversa verso Application. Nessun altro assembly o contratto è stato creato in F00.

`AppBootstrap` conserva una sola root con `DontDestroyOnLoad`, elimina gli ingressi duplicati e ripristina lo stato statico entrando in Play. Carica additivamente il percorso serializzato di `LessonWorkspace`, verifica che sia abilitato nei Build Settings e lo rende attivo. Questo è il comportamento della consegna originale di F00; F02 sostituisce l'ingresso diretto con il menu, conservando il workspace. Nella consegna originale il workspace contiene camera, luce direzionale, Global Volume con il profilo URP esistente e un pavimento statico neutro senza collider; nessuna UI o simulazione educativa. Build Settings originali: Bootstrap primo, LessonWorkspace secondo, SampleScene terzo, tutti abilitati.

| Criterio / verifica | Esito |
| --- | --- |
| Importazione e compilazione | Unity `6000.3.8f1`, compilazione finale dopo rimozione dello strumento di verifica, exit code `0`; nessun errore C# |
| Console senza errori durante il funzionamento | Zero Error/Exception/Assert nei tre cicli Play verificati; nessuno script mancante nelle scene |
| Un solo bootstrap persistente | Verificato a ogni avvio, dopo duplicazione del GameObject e dopo ricaricamento additivo di Bootstrap; nessuna duplicazione di camera/workspace |
| Avvio/arresto e doppio avvio | Tre cicli Play → Stop: due con domain reload e uno senza domain reload, sempre con scene reload |
| Risultato visibile | Workspace attivo, camera/luce/Volume presenti; cattura URP `960×600` controllata visivamente, con pavimento e sfondo neutri |
| Dipendenze future | Documentate sopra; asmdef attuale senza riferimenti ad assembly futuri |
| SampleScene e configurazione esistente | SampleScene ancora disponibile e abilitata; hash invariati di asset del template, pacchetti, fonti e altre specifiche; sole modifiche ai ProjectSettings nei Build Settings |

**Modalità di verifica e deviazione dal test previsto:** i comandi di importazione, Play/Stop e doppio avvio sono stati eseguiti attraverso l'Editor Unity in batch su una copia temporanea dei sorgenti finali, anziché tramite click manuali; il rendering URP è stato controllato manualmente tramite cattura. Lo strumento temporaneo verifica il lifecycle reale, non la sola esistenza delle cartelle, e non è incluso nel progetto consegnato. L'eccezione iniziale dell'indice Search nella copia è stata risolta copiando l'indice locale già esistente prima della ripetizione riuscita; nessuna modifica a pacchetti o preferenze del progetto originale.

Evidenze locali della verifica: `C:/Users/querz/AppData/Local/Temp/OptCon-F00-f3ebf22616d540378849e29bd8f02f8d/`, file `play-and-render.log`, `smoke-results.txt`, `workspace-preview.png`, `final-compile.log` e `final-compile-exitcode.txt`. Non sono asset dell'applicazione.

## F01 — Architettura dati, catalogo e riferimenti alle fonti

- **Stato:** **completata**, 9 ottobre 2026; tutti i criteri sotto verificati. F02 e fasi successive non avviate nella consegna originale di F01.
- **Obiettivo:** definire i dati riutilizzabili Course → Chapter → Lesson → Experiment e il formato delle formule, indipendenti dalla logica del capitolo.
- **Prerequisiti:** root applicativa e ingresso funzionanti; campi/ID/unità e source references definiti dalla specifica.
- **Dipendenze:** F00.
- **File/cartelle:** creare `A/Runtime/Content/Definitions/`, `Bindings/`, `SourceReference.cs` e asmdef Content; creare in `A/Runtime/Core/Contracts/` soltanto i contratti puri necessari alle definizioni/provider e il relativo asmdef; creare `A/Data/Courses/PrototypeCourse.asset`, `A/Data/Chapters/PrototypeChapter.asset`, `A/Data/Lessons/Prototype/PendulumSlice.asset`, `Experiments/` e `Sources/`; aggiungere `A/Editor/CatalogValidator.cs` e test di catalogo.
- **Sistemi Unity:** ScriptableObject, Inspector, serializzazione e strumenti editor minimi; riferimenti scena serializzati senza UnityEditor negli assembly runtime.
- **Risultato verificabile:** il catalogo di prototipo è ispezionabile e validabile; fonte I/K/O, pagine e ID sono leggibili. Il preset non è ancora un esperimento eseguibile.
- **Criteri di completamento:** ID unici e stabili; dati correnti assenti dagli asset; parametri/termini inizialmente incorporati quando non serve riuso; validazione distinta tra catalogo e configurazione eseguibile. Nessuna factory fittizia che restituisce null per simulare un modello pronto.
- **Test:** EditMode su ID duplicati, prerequisito inesistente, fonte/pagina fuori range e serializzazione del catalogo; confronto dei metadati con i tre PDF. Il percorso Docs viene verificato nell'editor.

### Consegna e verifica di F01

Le definizioni ScriptableObject rappresentano corso, capitolo, lezione, esperimento, formula e documento sorgente. Ogni asset ha ID esplicito, versione dello schema e versione del contenuto; l'ID non deriva dall'ordine nel corso o dal nome del file. Parametri, termini, riferimenti alle fonti e descrittori dei passi/condizioni sono incorporati nel proprietario. I descrittori dei passi non eseguono Lesson Logic: il prototipo ha `steps: []` fino a F08.

Gli otto asset costituiscono un catalogo editoriale di prototipo, separato dalle lezioni di produzione. Il preset del pendolo contiene dimensioni `2/1/1`, unità per componente, `T=100`, `δ=0.02 s`, `x₀=[0.35,0]`, input/target a zero e i soli parametri fisici `m`, `l`, `g`. Questi sono default di authoring, senza stato corrente, cursore temporale, rollout, override utente o progressi. Le proprietà espongono viste in sola lettura; le copie dei vettori iniziali possono essere modificate da una futura sessione senza mutare l'asset. I campi del preset non sono duplicati tra i parametri.

La formula `x_(t+1)=f_t(x_t,u_t)` contiene quattro termini e binding dichiarativi: stato corrente, input applicato, descrittore del modello e stato successivo. `f_t` non ha forma/unità numeriche inventate; le unità di θ e ω restano distinte. Scope di valore e highlight coincidono; il binding non risolve né incrementa un timestep. Le fonti distinguono pagine fisiche, slide e adattamenti: Euler sul pendolo è dichiarato come adattamento di I p. 27 al modello di I p. 33.

`CatalogValidator.ValidateCatalog` controlla struttura, ID, prerequisiti (anche cicli), riferimenti, unità, preset, formule e PDF in Docs con percorso e SHA-256. I metadati I/O/K sono stati confrontati con i PDF originali: titolo/autore/data, pagine fisiche `36/81/20`, numerazione e riferimenti del prototipo. Non esiste un elenco I/O/K hardcoded nel validatore; nuovi PDF possono trovarsi anche in sottocartelle di Docs.

`ValidateExecutionConfiguration` aggiunge i prerequisiti strutturali per l'esecuzione. Il prototipo passa il catalogo e segnala esattamente `MissingModelProvider` e `MissingScenarioPrefab` per l'esecuzione: entrambi sono intenzionalmente non assegnati. Nessuna factory restituisce null e nessuna lezione viene presentata come eseguibile.

| Criterio / verifica | Esito |
| --- | --- |
| Compilazione Unity | Unity `6000.3.8f1`, importazione finale in batch, exit code `0`; nessun errore C# o di parsing degli asset |
| Suite EditMode | **27/27 passati**, zero fallimenti e zero test saltati; test runner exit code `0` |
| ID unici e stabili | Catalogo valido; test negativi per duplicati/ID invalidi; riordino del corso e rinomina/salvataggio/rilettura di un asset preservano gli ID |
| Prerequisiti e fonti | Test per prerequisito inesistente, ciclo, fonte sconosciuta, pagine fuori range, PDF assente/hash diverso e percorso fuori Docs |
| Serializzazione | Salvataggio e rilettura nativi Unity dell'intero grafo, con dipendenze persistite prima dei proprietari; riferimenti condivisi, Unicode e default double preservati |
| Dati correnti assenti | Revisione dei campi serializzati; modifica delle copie dei vettori verificata senza mutare il preset |
| Parametri/termini incorporati | Nessun asset autonomo superfluo; parametri con target non duplicati e valori finiti/range coerenti |
| Catalogo / esecuzione distinti | Catalogo valido; configurazione di esecuzione con i due problemi previsti, senza modello fittizio |
| Coerenza architetturale | Core con `noEngineReferences: true` e nessun riferimento Unity; Content dipende soltanto da Core; editor/test separati; test anche sulle dipendenze effettive degli assembly |
| Riuso per capitoli futuri | Test di un capitolo/lezione aggiuntivo e un PDF in nuova sottocartella, mediante soli dati/collegamenti; esperimento condiviso senza duplicazione |
| Tempo opzionale | Test di catalogo con esperimento statico privo di timeline/dimensioni dinamiche artificiali |
| Perimetro | File preesistenti invariati salvo questo piano; nessuna modifica a scene, bootstrap, Build Settings, pacchetti, URP, altre specifiche o PDF |

**Decisione minima rispetto all'architettura:** `ModelDefinition` è soltanto una base astratta tipizzata per il riferimento dell'esperimento, con il contratto puro `ModelDimensions`. L'architettura prevede provider concreti che validano e creano modelli C#; il modello matematico e il contratto di creazione verranno introdotti con F03/F04. Questa base evita stringhe di modello, switch per capitolo e factory fittizie, senza anticipare un provider o un sistema di simulazione. Non sono emerse contraddizioni che richiedano refactor.

**Limiti e lavoro futuro previsto:** il validatore è editoriale/strutturale; non certifica matematica, capacità delle metriche/highlight, evaluator o correttezza dell'esecuzione. Queste verifiche richiedono i sistemi delle fasi successive. Il conteggio delle pagine e la numerazione stampata sono metadati revisionati, non estratti automaticamente da un parser PDF Unity. L'integrità del PDF è verificata dall'hash. Non ci sono dipendenze esterne nuove né debiti bloccanti per F01.

**Modalità di verifica:** importazione, compilazione e test reali dell'Editor Unity su una copia temporanea del progetto, con sorgenti F01 identici per hash al workspace. La copia conserva anche Docs, Packages e ProjectSettings. Unity ha migrato automaticamente il solo `Mobile_RPAsset.asset` da versione interna 12 a 13 nella copia; tale migrazione non è trasferita al progetto originale. Nessuno strumento temporaneo è consegnato come asset dell'applicazione.

Evidenze locali: `C:/Users/querz/AppData/Local/Temp/OptCon-F01-3748683d4db644deb6edb10a0e8ae2bf/`, file `editmode-4.log`, `EditMode-results.xml`, `editmode-exitcode.txt`, `final-compile.log`, `final-compile-exitcode.txt`, `verified-source-manifest.json` e `change-inventory.json`. La copia Unity di verifica è temporanea; le evidenze restano esterne al progetto.

**Verifica manuale in Unity:**

1. Aprire il progetto con Unity `6000.3.8f1`, attendere la compilazione e selezionare `A/Data/Courses/PrototypeCourse.asset` nel Project.
2. Nell'Inspector seguire i riferimenti a capitolo, lezione, esperimento e formula; controllare ID, preset, termini e fonti. Aprire anche i tre asset in `A/Data/Sources/` per percorso, hash, data e pagine. Modificando altri cataloghi, assegnare ID stabili espliciti anziché riusare quelli del prototipo.
3. Con il corso selezionato, eseguire `Tools > Optimal Control > Validate Selected Course > Catalog`: il catalogo deve risultare valido.
4. Se si esegue `Execution Configuration`, attendersi i soli due messaggi `MissingModelProvider` e `MissingScenarioPrefab`: il prototipo editoriale non è ancora eseguibile. Questi sono diagnostici richiesti esplicitamente dal menu, non errori di compilazione. Pulire la Console dopo la verifica.
5. Aprire `Window > General > Test Runner`, scheda EditMode, ed eseguire `OptCon.Tests.CatalogValidatorTests`: **27/27** passati. Il test runner può creare fixture temporanee in Assets/Docs e le rimuove al termine.
6. Play da Bootstrap conserva il workspace neutro di F00: F01 si verifica tramite Inspector, validatore e test, senza navigazione o controlli runtime.

### Inventario completo della consegna F01

**70 file creati**: 27 file di contenuto/codice/assembly, i rispettivi 27 `.meta` e 16 `.meta` di cartelle. Tutti i percorsi seguenti sono relativi ad **A**. **Unico file preesistente modificato: `Docs/OPTCON_IMPLEMENTATION_PLAN.md`**, per stato, rapporto e inventario. Nessun file preesistente eliminato.

```text
Data.meta
Data/Chapters.meta
Data/Chapters/PrototypeChapter.asset
Data/Chapters/PrototypeChapter.asset.meta
Data/Courses.meta
Data/Courses/PrototypeCourse.asset
Data/Courses/PrototypeCourse.asset.meta
Data/Experiments.meta
Data/Experiments/PendulumPrototype.asset
Data/Experiments/PendulumPrototype.asset.meta
Data/Formulas.meta
Data/Formulas/PrototypeDiscreteDynamics.asset
Data/Formulas/PrototypeDiscreteDynamics.asset.meta
Data/Lessons.meta
Data/Lessons/Prototype.meta
Data/Lessons/Prototype/PendulumSlice.asset
Data/Lessons/Prototype/PendulumSlice.asset.meta
Data/Sources.meta
Data/Sources/Introduction.asset
Data/Sources/Introduction.asset.meta
Data/Sources/OptimalityConditions.asset
Data/Sources/OptimalityConditions.asset.meta
Data/Sources/Optimization.asset
Data/Sources/Optimization.asset.meta
Editor.meta
Editor/CatalogValidator.cs
Editor/CatalogValidator.cs.meta
Editor/OptCon.Editor.asmdef
Editor/OptCon.Editor.asmdef.meta
Runtime/Content.meta
Runtime/Content/Bindings.meta
Runtime/Content/Bindings/FormulaBindings.cs
Runtime/Content/Bindings/FormulaBindings.cs.meta
Runtime/Content/Definitions.meta
Runtime/Content/Definitions/ChapterDefinition.cs
Runtime/Content/Definitions/ChapterDefinition.cs.meta
Runtime/Content/Definitions/ContentDefinition.cs
Runtime/Content/Definitions/ContentDefinition.cs.meta
Runtime/Content/Definitions/CourseDefinition.cs
Runtime/Content/Definitions/CourseDefinition.cs.meta
Runtime/Content/Definitions/ExperimentDefinition.cs
Runtime/Content/Definitions/ExperimentDefinition.cs.meta
Runtime/Content/Definitions/FormulaDefinition.cs
Runtime/Content/Definitions/FormulaDefinition.cs.meta
Runtime/Content/Definitions/LessonDefinition.cs
Runtime/Content/Definitions/LessonDefinition.cs.meta
Runtime/Content/Definitions/LessonParameterDefinition.cs
Runtime/Content/Definitions/LessonParameterDefinition.cs.meta
Runtime/Content/Definitions/LessonStepDefinition.cs
Runtime/Content/Definitions/LessonStepDefinition.cs.meta
Runtime/Content/Definitions/ModelDefinition.cs
Runtime/Content/Definitions/ModelDefinition.cs.meta
Runtime/Content/Definitions/SourceDocumentDefinition.cs
Runtime/Content/Definitions/SourceDocumentDefinition.cs.meta
Runtime/Content/OptCon.Content.asmdef
Runtime/Content/OptCon.Content.asmdef.meta
Runtime/Content/SourceReference.cs
Runtime/Content/SourceReference.cs.meta
Runtime/Core.meta
Runtime/Core/Contracts.meta
Runtime/Core/Contracts/ModelDimensions.cs
Runtime/Core/Contracts/ModelDimensions.cs.meta
Runtime/Core/OptCon.Core.asmdef
Runtime/Core/OptCon.Core.asmdef.meta
Tests.meta
Tests/EditMode.meta
Tests/EditMode/CatalogValidatorTests.cs
Tests/EditMode/CatalogValidatorTests.cs.meta
Tests/EditMode/OptCon.Content.EditModeTests.asmdef
Tests/EditMode/OptCon.Content.EditModeTests.asmdef.meta
```

## F02 — CourseManager, menu e caricamento della lezione

- **Stato:** **completata**, 9 ottobre 2026; tutti i criteri sotto verificati. F03 e fasi successive non avviate nella consegna originale di F02.
- **Obiettivo:** consentire la prima interazione Unity: scegliere una lezione dai dati, entrare nel workspace e tornare al menu.
- **Prerequisiti:** catalogo valido e scene di F00; un contratto di navigazione basato su ID disponibile in Content.
- **Dipendenze:** F01.
- **File/cartelle:** creare `A/Runtime/Course/CourseManager.cs`, `A/Runtime/Application/LessonLoader.cs`, `A/Runtime/UI/CourseMenuPresenter.cs`, `A/UI/CourseMenu.uxml/.uss`, `A/Scenes/MainMenu.unity`; aggiornare Bootstrap, riferimenti scena e Build Settings. Creare Presentation asmdef con i primi presenter.
- **Sistemi Unity:** SceneManager additivo, UI Toolkit/UIDocument/PanelSettings, Input System per la UI, lifecycle del workspace.
- **Risultato verificabile:** click sulla lezione mostra titolo/obiettivo e un workspace dichiarato ancora privo di simulazione; il pulsante di ritorno funziona.
- **Criteri di completamento:** titoli, ordine e destinazioni provengono dagli asset; nessuna switch sui capitoli; una sola lezione/scena attiva; assenza di completamenti didattici fittizi.
- **Test:** PlayMode del percorso menu → workspace → menu ripetuto due volte; prova manuale di una seconda voce di catalogo di test senza modificare CourseManager; controllo scena mancante.

### Consegna e verifica di F02

`ICourseNavigation`, in Content, espone `Course`, `ActiveLesson`, `IsTransitioning`, `StatusMessage`, l'evento `Changed` e i comandi `OpenLesson(id)` / `ReturnToMenu()`. Non esegue operazioni sulle scene e non contiene progressi o stato simulativo. Presentation dipende soltanto da questo contratto e dai dati editoriali.

`CourseManager` implementa il contratto, risolve gli ID dagli asset e mantiene l'unica identità di lezione attiva. Aggiorna tale identità solo dopo un caricamento riuscito; rifiuta ID sconosciuti, ID assegnati a lezioni distinte e richieste concorrenti. La stessa istanza di LessonDefinition può comparire in capitoli diversi senza duplicare l'identità, coerentemente con il validatore F01. Non esistono switch sui capitoli, vincoli al loro numero, stato matematico, avanzamento didattico o scritture sugli asset.

`LessonLoader` possiede una sola scena di contenuto additiva. Verifica preventivamente che la destinazione sia disponibile nei Build Settings; scarica il contenuto precedente prima di caricare il successivo, lo rende attivo e notifica `SceneReady`. Una destinazione mancante lascia utilizzabile la scena corrente; un fallimento successivo al preflight tenta il ripristino della scena precedente. Il loader non istanzia esperimenti, provider matematici o scenari.

`AppBootstrap` resta la root persistente di F00: compone manager/loader, apre MainMenu e collega i presenter quando una scena è pronta. `CourseMenuPresenter` genera titoli, ordine dei capitoli/lezioni e richieste tramite ID dagli asset; nello stesso albero UXML mostra l'anteprima editoriale del workspace e il ritorno al menu. Il workspace dichiara esplicitamente che simulazione e controlli dell'esperimento non sono disponibili. L'apertura non attribuisce completamenti. Il menu scorre quando lo spazio disponibile non basta.

`CoursePanelSettings` usa il tema runtime Unity e una risoluzione di riferimento `1280×720`. UI Toolkit usa le azioni UI del project-wide Input System già configurato nel template Unity 6; nessun pacchetto, EventSystem uGUI o InputSystemUIInputModule aggiuntivo. Le scene e i riferimenti sono stati creati/salvati con l'Editor Unity. Build Settings: Bootstrap, MainMenu, LessonWorkspace, SampleScene, tutti abilitati; Bootstrap resta primo.

**Dipendenze effettive:** `Core → nessun assembly Unity`; `Content → Core`; `Presentation → Content`; `Application → Content/Presentation`. Il file `Runtime/Course/OptCon.Application.asmref` include CourseManager nell'assembly Application, pur conservando il percorso richiesto dal piano. Evita un assembly Course superfluo e una dipendenza inversa di Presentation verso Application. Non è stato creato un assembly Simulation vuoto.

| Criterio / verifica | Esito |
| --- | --- |
| Compilazione Unity | Unity `6000.3.8f1`; importazione/compilazione native finali e test runner con exit code `0`; nessun errore C#, di parsing degli asset o eccezione runtime nei controlli finali |
| Suite PlayMode F02 | **9/9 passati**, zero fallimenti e zero test saltati |
| Percorso richiesto | Menu → workspace → menu ripetuto due volte; titolo/obiettivo confrontati con gli asset, identità attiva corretta e una sola scena di contenuto anche durante le transizioni |
| Input reale della UI | Mouse virtuale Input System → azioni UI → pulsante UI Toolkit → apertura del workspace; il test lascia due frame per ciascuno stato del mouse tra i diversi stadi del player loop |
| Riuso / seconda voce | Fixture temporanea con nuova lezione, ID/titolo/obiettivo e ordine diversi: il menu genera entrambe le voci e apre la seconda identità senza cambiare CourseManager. Altro test: stessa lezione condivisa da due capitoli |
| Scena mancante / ID errato | Richieste rifiutate, messaggio visibile e menu ancora utilizzabile; successiva apertura valida riuscita |
| Concorrenza / lifecycle | Richieste durante il caricamento rifiutate; bootstrap duplicato e ricaricamento additivo non creano nuove root; rebinding e disable/enable del presenter non duplicano subscription o comandi |
| Nessun completamento fittizio | Asset della lezione invariato prima/dopo due cicli; nessun sistema di progressione introdotto |
| Confini architetturali | Test sulle dipendenze degli assembly compilati: Core senza Unity; Content e Presentation senza Application; nessun modello, formula dinamica o stato simulativo nella UI |
| Regressione F01 | **27/27 test EditMode passati**, zero fallimenti/saltati; catalogo, fonti PDF, ID, serializzazione e separazione catalogo/esecuzione ancora validi |
| Regressione F00 | Tre cicli Play/Stop nativi, due con domain reload e uno senza: menu/workspace/menu riuscito, una root persistente, una camera/presenter/scena di contenuto; pavimento, luce e Volume URP conservati; zero missing scripts/errori runtime |
| Verifica visiva | Acquisite e ispezionate Game View di menu e workspace a `1280×779`: testi dagli asset, pulsanti leggibili, dichiarazione di anteprima e spazio URP visibili |
| Casi matematici deterministici | Non applicabile: F02 non introduce comportamenti matematici o simulativi. Non è stato implementato un modello dimostrativo per anticipare F03; i casi deterministici qui verificano gli esiti della navigazione |
| Perimetro | Nessuna modifica a Core, definizioni/catalogo F01, PDF, altre specifiche, URP, SampleScene o pacchetti; unico ProjectSettings modificato: EditorBuildSettings |

**Modalità di verifica:** Editor Unity reale su copia temporanea isolata, con tutti i file `Assets/OptimalControl` identici per hash al workspace e stessi Packages/ProjectSettings iniziali. L'Editor dell'utente non è stato chiuso né usato per i test. I verificatori temporanei non sono consegnati. La migrazione automatica Unity del solo Mobile_RPAsset nella copia non è stata trasferita all'originale.

Evidenze locali esterne al progetto: `C:/Users/querz/AppData/Local/Temp/OptCon-F02-d71195564371471e883a62c32f3fedc6/`, file `PlayMode-results-final.xml`, `playmode-final.log`, `playmode-final-exitcode.txt`, `EditMode-results-final.xml`, `editmode-final.log`, `editmode-final-exitcode.txt`, `smoke-results.txt`, `smoke.log`, `smoke-exitcode.txt`, `menu-preview.png`, `lesson-preview.png`, `verified-source-manifest.json` e `change-inventory.json`.

**Deviazioni e limiti:** nessuna deviazione funzionale da F02 e nessun problema architetturale grave nelle consegne F00/F01. Contratto Content, asmref, tema/PanelSettings e test sono i file di supporto necessari ai componenti pianificati. Un unico presenter/albero UXML serve menu e anteprima senza introdurre prematuramente LessonHudPresenter. Configurazione di esecuzione ancora intenzionalmente incompleta (`MissingModelProvider`, `MissingScenarioPrefab`); simulazione, formule live, Lesson Logic e progressi restano nelle rispettive fasi. Il ripristino dopo un raro fallimento di caricamento successivo al preflight è gestito ma non è stato riprodotto tramite un guasto nativo indotto; la scena mancante è verificata. Nessun debito bloccante per F02.

**Verifica manuale in Unity:**

1. Aprire `A/Scenes/Bootstrap.unity` con Unity `6000.3.8f1`, attendere la compilazione ed entrare in Play. Deve apparire il catalogo di prototipo; la Hierarchy contiene la root persistente e una sola scena MainMenu di contenuto.
2. Cliccare «Una coppia cambia il futuro del pendolo»: MainMenu viene scaricata e compare LessonWorkspace con titolo, obiettivo e descrizione identici a `A/Data/Lessons/Prototype/PendulumSlice.asset`. Camera, luce, pavimento e Volume restano presenti; non compare una simulazione.
3. Cliccare «Torna al corso» e ripetere il percorso due volte. Controllare Console, conteggio delle scene/root e assenza di completamenti attribuiti. Provare anche Tab/Invio sul pulsante della lezione.
4. Per la prova di estensione, fuori Play duplicare corso/capitolo/lezione in una cartella di test, assegnare nuovi ID espliciti e un titolo/obiettivo riconoscibili, collegare due lezioni nel capitolo duplicato e assegnare il corso duplicato al bootstrap. Validare il catalogo, entrare in Play e aprire la nuova voce: ordine/testi/destinazione devono seguire i dati senza modifiche C#. Ripristinare il riferimento originale al corso al termine. I test automatici fanno la stessa prova con cloni non persistenti, lasciando intatto il catalogo consegnato.
5. Per il caso negativo, nella copia di test della lezione impostare un workspace assente dai Build Settings: il click deve mostrare un messaggio e lasciare il menu utilizzabile. Ripristinare poi il percorso valido.
6. Da `Window > General > Test Runner`, eseguire i 9 test PlayMode di `OptCon.Tests.CourseNavigationTests` e i 27 EditMode di `OptCon.Tests.CatalogValidatorTests`.

**Inventario completo F02:** 31 file creati (13 sorgenti/configurazioni/asset, i 13 relativi `.meta` e 5 `.meta` di cartelle), 6 file preesistenti modificati, nessun file preesistente eliminato. Tutti i percorsi del primo elenco sono relativi ad `A/`.

```text
Runtime/Application/LessonLoader.cs
Runtime/Application/LessonLoader.cs.meta
Runtime/Content/Navigation.meta
Runtime/Content/Navigation/ICourseNavigation.cs
Runtime/Content/Navigation/ICourseNavigation.cs.meta
Runtime/Course.meta
Runtime/Course/CourseManager.cs
Runtime/Course/CourseManager.cs.meta
Runtime/Course/OptCon.Application.asmref
Runtime/Course/OptCon.Application.asmref.meta
Runtime/UI.meta
Runtime/UI/CourseMenuPresenter.cs
Runtime/UI/CourseMenuPresenter.cs.meta
Runtime/UI/OptCon.Presentation.asmdef
Runtime/UI/OptCon.Presentation.asmdef.meta
Scenes/MainMenu.unity
Scenes/MainMenu.unity.meta
Tests/PlayMode.meta
Tests/PlayMode/CourseNavigationTests.cs
Tests/PlayMode/CourseNavigationTests.cs.meta
Tests/PlayMode/OptCon.Navigation.PlayModeTests.asmdef
Tests/PlayMode/OptCon.Navigation.PlayModeTests.asmdef.meta
UI.meta
UI/CourseMenu.uss
UI/CourseMenu.uss.meta
UI/CourseMenu.uxml
UI/CourseMenu.uxml.meta
UI/CoursePanelSettings.asset
UI/CoursePanelSettings.asset.meta
UI/CourseTheme.tss
UI/CourseTheme.tss.meta
```

File preesistenti modificati, con percorso relativo alla root del progetto:

```text
Assets/OptimalControl/Runtime/Application/AppBootstrap.cs
Assets/OptimalControl/Runtime/Application/OptCon.Application.asmdef
Assets/OptimalControl/Scenes/Bootstrap.unity
Assets/OptimalControl/Scenes/LessonWorkspace.unity
ProjectSettings/EditorBuildSettings.asset
Docs/OPTCON_IMPLEMENTATION_PLAN.md
```

## F03 — Mathematical Model e Simulation puri

- **Stato:** **completata**, 9 ottobre 2026; tutti i criteri sotto verificati. F04 e fasi successive non avviate nella consegna originale di F03.
- **Obiettivo:** calcolare il pendolo discreto, il rollout e gli snapshot senza dipendere dalla presentazione Unity.
- **Prerequisiti:** contratti/dimensioni e preset di F01; convenzioni t/T/δ e unità del slice fissate.
- **Dipendenze:** F01.
- **File/cartelle:** creare/estendere `A/Runtime/Core/Models/PendulumDynamics.cs`, tipi vettoriali e validazione dei parametri; creare `A/Runtime/Simulation/RolloutEngine.cs`, `SimulationSnapshot.cs`, `TrajectoryHistory.cs`, descrittori delle metriche e asmdef Simulation; creare test EditMode del Core/rollout con assembly dedicato.
- **Sistemi Unity:** compilazione tramite asmdef `noEngineReferences`, Test Framework EditMode; nessun Transform/Rigidbody/UI nel Core o nella Simulation.
- **Risultato verificabile:** rollout consultabile nei risultati dei test/diagnostiche con T+1 stati e T input; x_t, u_t e x_(t+1) esatti rispetto al modello scelto.
- **Criteri di completamento:** double nel calcolo; entrambe le equazioni Euler usano lo stato vecchio; dati pubblici immutabili; unità per componente; terminale senza u_T/next state; parametri non validi e NaN espliciti.
- **Test:** equilibrio con `ω_e=0, u_e=mgl sinθ_e`; passo con input noto e zero; dimensioni/T=1; ripetibilità del rollout; mutazione di un array esterno incapace di cambiare uno snapshot già prodotto.

### Consegna e verifica di F03

**Perimetro applicato:** la richiesta di questa consegna pone particolare attenzione ai consumer visuali. La F03 operativa definita sopra riguarda però Mathematical Model/Simulation puri; host, playback e primitive 3D appartengono a F04/F05. È stato rispettato questo ordine. La rappresentazione dei dati è verificata con una tabella diagnostica **editor only**, senza creare renderer runtime, scene di lezione, host, sessioni, command sink o controlli di playback.

`NumericVector` è un vettore immutabile di `double` finiti: copia gli array in ingresso e restituisce copie in uscita. `NumericValidation` controlla finitezza, positività e dimensioni. `IDiscreteDynamics.Evaluate(t, state, control)` è il contratto della transizione; parametri immutabili appartengono al modello. `ModelDescriptor` conserva ID/versione, dimensioni, unità per componente e passo fisico opzionale. Per un sistema discreto senza una discretizzazione fisica non vengono inventati δ o τ.

`PendulumParameters` valida massa/lunghezza positive, gravità non negativa e coefficienti rappresentabili in `double`. `PendulumDynamics` applica una sola implementazione delle due equazioni Euler della specifica, usando **entrambe le componenti dello stato precedente**. Mantiene gli angoli non avvolti e non introduce saturazione, damping, collider o fisica Unity. La provenienza è l'adattamento dichiarato di I p. 27 al pendolo di I p. 33; la condizione di equilibrio segue la rettifica di I p. 34 nel curriculum.

`RolloutEngine.Rollout(model, initialState, controls, experimentId, revisionId)` precalcola una traiettoria completa attraverso il contratto, senza conoscere il pendolo o la lezione. `T` deriva dai controlli; sono prodotti `T+1` stati e `T` input. Le richieste sono validate prima di iniziare; errori del modello producono `RolloutException` con timestep e causa. Nessuna storia parziale viene restituita. Una storia valida già ottenuta resta immutata se un calcolo successivo fallisce.

`TrajectoryHistory` è l'unica sorgente dei campioni della revisione; `GetSnapshot(t)` costruisce una vista immutabile dei dati salvati, senza ricalcolare la dinamica. `SimulationSnapshot` espone identità, revisione, modello, t/T, δ/τ opzionali, stato corrente, input applicato e stato successivo. Al terminale input/next sono assenti. Le metriche vettoriali `state.current`, `input.applied`, `state.next` dichiarano forma, unità, validità e **indice già risolto**: `state.next` in t corrisponde al campione t+1, senza un ulteriore offset nei consumer. Il descrittore del modello non viene spacciato per un valore numerico di f_t. La revisione è metadata assegnato dal chiamante; gestione delle revisioni/cursori e ultimo risultato valido della sessione restano a F04.

`PendulumRolloutDiagnostic` è un ingresso editor esplicito per verificare il preset pendolo selezionato: legge x₀, input, T, δ e parametri dagli asset F01, valida dimensioni/unità SI e invoca il core. Non assegna un provider fittizio all'esperimento e non modifica gli asset. `RolloutDiagnosticsWindow` / `RolloutDiagnosticsView` ricevono soltanto una `TrajectoryHistory` e mostrano t, τ, x_t, u_t, x_(t+1), unità, indici e N/A terminali. La tabella è indipendente dal numero di componenti e dal modello; formatta in `G10` con cultura invariante senza alterare i `double` originali. Non valuta equazioni, non interpola e non conserva una seconda simulazione. Dopo un domain reload il contenuto diagnostico si rigenera dal menu.

**Dipendenze effettive:** Core resta con `references: []` e `noEngineReferences: true`; il nuovo Simulation usa soltanto Core, anch'esso con `noEngineReferences: true`. Content, Presentation e Application rimangono invariati. Solo l'assembly editor aggiunge Simulation per la diagnostica. I test matematici hanno un assembly EditMode dedicato con riferimenti Core/Simulation; nessuna dipendenza circolare o da math/simulation verso Unity, UI, Content, scene o Lesson Logic.

| Criterio / verifica | Esito |
| --- | --- |
| Compilazione Unity | Unity `6000.3.8f1`; importazione/compilazione reali e runner EditMode/PlayMode con exit code `0`; nessun errore C# o eccezione runtime nelle verifiche finali |
| Nuovi test F03 | **39/39 passati**: 26 casi PendulumDynamicsTests e 13 RolloutEngineTests; zero saltati/falliti |
| Double e Euler | Passo noto `[0,2]`, coppia `3`, δ=`0.1`, m=l=`1` → `[0.2,2.3]`; il test distingue Euler esplicito dall'uso dell'angolo appena aggiornato |
| Input zero e gravità | `[π/2,0]`, u=`0`, δ=`0.1`, g=`9.81`, l=`1` → `[π/2,-0.981]` |
| Equilibrio | Tre angoli, inclusi non nulli, con ω_e=`0` e u_e=`mgl sinθ_e` restano invariati entro tolleranza |
| Scala fisica | m=`2`, l=`3`, g=`0`, δ=`0.5`, u=`18`: `[0,0] → [0,0.5] → [0.25,1]`; inerzia `18 kg·m²` |
| Riuso / caso scalare | Modello presente solo nei test: x₀=`2`, u₀=`3` → x₁=`5`; anche modello tempo-variante e modello privo di input verificati |
| Storia / ripetibilità | T=`1` e T=`100`; conteggi corretti; due rollout identici producono gli stessi campioni; selezionare campioni precedenti non chiama Evaluate |
| Immutabilità | Mutare array/lista iniziali o copie restituite non modifica storia/snapshot; collezioni e dizionario metriche rifiutano scritture; campioni condivisi per riferimento tra storia, snapshot e metriche |
| Unità / terminale | Unità per componente preservate; next ha indice t+1; in t=T input/next hanno `NotApplicable`, nessun valore e nessun indice; nessun u_T o x_(T+1) fittizio |
| Errori espliciti | NaN/∞, dimensioni, null, T=0, indice/step/revisione invalidi, coefficienti/durata non rappresentabili, overflow e risultato errato del modello verificati |
| Indipendenza | Test sui riferimenti degli assembly compilati e revisione degli import: Core/Simulation senza Unity/Content/Presentation/Application; unica implementazione produttiva della dinamica in PendulumDynamics |
| Diagnostica nativa | Menu/finestra Unity aperti sul preset originale: `101` stati e `100` input, terminale N/A, asset invariato. Stessa vista applicata ai casi pendolo e scalare: testi/unità/indici confrontati con gli snapshot, cattura controllata visivamente |
| Regressione F01 | **27/27 test EditMode passati**; catalogo, PDF, ID, serializzazione e configurazione di esecuzione ancora coerenti |
| Regressione F02 | **9/9 test PlayMode passati**, incluso click Input System, due cicli di navigazione, seconda voce, lezione condivisa, scena mancante e lifecycle |
| Regressione F00 | Tre cicli nativi menu/workspace/menu, due con domain reload e uno senza; una root persistente, una camera/presenter/scena di contenuto, URP/pavimento/luce/Volume intatti; nessun missing script o errore runtime |
| Perimetro | Nessuna modifica a scene, bootstrap, navigazione, UI runtime, dati F01, definizioni Content, pacchetti, ProjectSettings, URP, altre specifiche o PDF |

Le tolleranze dei casi analitici piccoli sono `1e-12 + 1e-12·|expected|` (assoluta più relativa); conteggi, indici, immutabilità e identità dei campioni sono verificati esattamente. La ripetibilità confronta esattamente i risultati nello stesso runtime, senza promettere identità bit a bit tra piattaforme diverse.

**Modalità di verifica:** Editor Unity reale su copia temporanea isolata, con tutti i file `Assets/OptimalControl` identici per SHA-256 al workspace. L'Editor aperto dell'utente è stato preservato. I verificatori temporanei e la loro rappresentazione nella Game View non sono scene/componenti runtime consegnati. La prima cattura del verificatore non aveva aggiornato correttamente camera/pannello; l'acquisizione finale riuscita riusa quelli nativi di F02, senza modificare gli asset. La migrazione automatica del solo Mobile_RPAsset nella copia non è trasferita al progetto originale.

Evidenze locali esterne al progetto: `C:/Users/querz/AppData/Local/Temp/OptCon-F03-e4b0e4bd9b7346a9843c686979b627fc/`, file `EditMode-results.xml`, `editmode.log`, `editmode-exitcode.txt`, `PlayMode-results.xml`, `playmode.log`, `playmode-exitcode.txt`, `diagnostics-results.txt`, `diagnostics-2.log`, `diagnostics-exitcode.txt`, `diagnostics-preview.png`, `smoke-results.txt`, `regression-smoke.log`, `regression-smoke-exitcode.txt`, `verified-source-manifest.json` e `change-inventory.json`.

**Deviazioni / problemi:** nessun refactor delle fasi precedenti e nessun problema architetturale grave individuato. La diagnostica editor è il supporto minimo al risultato «test/diagnostiche» previsto da F03 e alla verifica della rappresentazione richiesta; non anticipa primitive 3D o integrazione runtime. L'ingresso editor specifico del pendolo va distinto dalla vista generica. Il provider ScriptableObject e l'host sono esplicitamente rimandati a F04: il suo prerequisito «provider concreto disponibile» va letto insieme alla creazione del provider prevista nella stessa F04, senza considerarlo già consegnato qui. Il catalogo continua quindi a segnalare i due prerequisiti di esecuzione mancanti previsti. Output, costi, vincoli, derivate e nuove forme di metriche verranno aggiunti con le fasi che li richiedono; F03 pubblica solo i dati effettivamente calcolati. Nessun debito bloccante per questa fase.

**Verifica manuale in Unity:**

1. Attendere la compilazione, selezionare `A/Data/Experiments/PendulumPrototype.asset` e scegliere `Tools > Optimal Control > F03 > Inspect Selected Pendulum Preset`. Non serve entrare in Play. La finestra mostra `T=100`, `states=101`, `inputs=100`, δ=`0.02 s`; la prima riga contiene x₀=`[0.35,0]`, u₀=`[0]`, unità `[rad,rad/s]` / `[N·m]` e next con indice `@1`.
2. Scorrere alla riga t=`100`: τ=`2 s`, input e next `N/A (NotApplicable)`. Controllare che l'asset non sia modificato e che il menu runtime continui ad aprire soltanto l'anteprima F02.
3. Per un caso verificabile a mano, duplicare temporaneamente l'asset esperimento fuori Play: T=`2`, δ=`0.5`, x₀=`[0,0]`, input costante `[18]`, massa=`2`, lunghezza=`3`, gravità=`0`. Eseguire la diagnostica sulla copia: stati `[0,0]`, `[0,0.5]`, `[0.25,1]`; τ=`0,0.5,1 s`. Il next di una riga coincide con il current della successiva. Usare una copia per preservare il preset originale, poi rimuoverla.
4. Da `Window > General > Test Runner`, scheda EditMode, eseguire PendulumDynamicsTests e RolloutEngineTests: **39 casi**. Il caso `KnownScalarAdditionProducesFiveAndTerminalHasNoTransition` riporta anche nei risultati `2+3=5` e il terminale N/A. Eseguire inoltre i 27 CatalogValidatorTests e i 9 CourseNavigationTests in PlayMode.
5. La cattura `diagnostics-preview.png` nelle evidenze mostra la stessa vista con il caso pendolo noto e con il modello scalare di test, verificando il riuso della rappresentazione senza una seconda implementazione delle equazioni.

**Inventario completo F03:** 34 file creati (15 sorgenti/assembly, i 15 relativi `.meta` e 4 `.meta` di cartelle), 2 file preesistenti modificati, nessun file preesistente eliminato. Percorsi dei nuovi file relativi ad `A/`:

```text
Editor/PendulumRolloutDiagnostic.cs
Editor/PendulumRolloutDiagnostic.cs.meta
Editor/RolloutDiagnosticsWindow.cs
Editor/RolloutDiagnosticsWindow.cs.meta
Runtime/Core/Contracts/IDiscreteDynamics.cs
Runtime/Core/Contracts/IDiscreteDynamics.cs.meta
Runtime/Core/Contracts/ModelDescriptor.cs
Runtime/Core/Contracts/ModelDescriptor.cs.meta
Runtime/Core/Models.meta
Runtime/Core/Models/PendulumDynamics.cs
Runtime/Core/Models/PendulumDynamics.cs.meta
Runtime/Core/Models/PendulumParameters.cs
Runtime/Core/Models/PendulumParameters.cs.meta
Runtime/Core/Numerics.meta
Runtime/Core/Numerics/NumericVector.cs
Runtime/Core/Numerics/NumericVector.cs.meta
Runtime/Simulation.meta
Runtime/Simulation/OptCon.Simulation.asmdef
Runtime/Simulation/OptCon.Simulation.asmdef.meta
Runtime/Simulation/RolloutEngine.cs
Runtime/Simulation/RolloutEngine.cs.meta
Runtime/Simulation/SimulationMetrics.cs
Runtime/Simulation/SimulationMetrics.cs.meta
Runtime/Simulation/SimulationSnapshot.cs
Runtime/Simulation/SimulationSnapshot.cs.meta
Runtime/Simulation/TrajectoryHistory.cs
Runtime/Simulation/TrajectoryHistory.cs.meta
Tests/EditMode/Mathematics.meta
Tests/EditMode/Mathematics/OptCon.Mathematics.EditModeTests.asmdef
Tests/EditMode/Mathematics/OptCon.Mathematics.EditModeTests.asmdef.meta
Tests/EditMode/Mathematics/PendulumDynamicsTests.cs
Tests/EditMode/Mathematics/PendulumDynamicsTests.cs.meta
Tests/EditMode/Mathematics/RolloutEngineTests.cs
Tests/EditMode/Mathematics/RolloutEngineTests.cs.meta
```

File preesistenti modificati, relativi alla root del progetto:

```text
Assets/OptimalControl/Editor/OptCon.Editor.asmdef
Docs/OPTCON_IMPLEMENTATION_PLAN.md
```

## F04 — Host Unity, comandi e gestione del tempo

- **Stato:** **completata**, 9 ottobre 2026; tutti i criteri sotto verificati. A03 e A04 risolti; A01 preservato, A02 non modificato. F05 non avviata nella consegna originale di F04.
- **Obiettivo:** collegare il Core al lifecycle della lezione, con un unico proprietario di input, configurazione, timeline e revisioni.
- **Prerequisiti:** caricamento lezione e rollout verificati; provider concreto del pendolo disponibile.
- **Dipendenze:** F02, F03.
- **File/cartelle:** creare `A/Runtime/Content/Models/PendulumModelDefinition.cs`, `A/Runtime/Application/ExperimentHost.cs`, `PlaybackController.cs`, `A/Runtime/Simulation/SimulationSession.cs`, comandi e `ICommandSink` puro; aggiornare preset/scenario di prototipo, LessonLoader e test PlayMode della sessione.
- **Sistemi Unity:** host MonoBehaviour, Update per il clock di presentazione, caricamento/scaricamento delle scene; la matematica non usa FixedUpdate o fisica Unity.
- **Risultato verificabile:** il workspace crea una sessione valida; un indicatore diagnostico espone revisione e timestep. I test possono eseguire play/pause/step/reset/seek e modifica input attraverso gli stessi comandi futuri della UI.
- **Criteri di completamento:** velocità di playback distinta da δ; rewind tramite storia; modifica di un input ricalcola il futuro; reset ricopia il preset; una sola pubblicazione coerente per revisione; chiusura della lezione elimina subscription.
- **Test:** EditMode su causalità del ricalcolo e reset; PlayMode su clock a diverse frequenze, terminale, riapertura sessione e asset mai modificati dagli override runtime.

### Consegna e verifica di F04

Il provider `PendulumModelDefinition` converte una volta l'authoring in configurazione pura. `SimulationSession` possiede configurazione richiesta, risultato valido, cursore e revisioni; implementa `ICommandSink` per play/pause/step avanti e indietro/seek/reset, input singolo o coda, stato iniziale, parametri, passo fisico e velocità di playback. `ExperimentHost` ne gestisce il lifetime; `PlaybackController.Update` passa soltanto secondi di presentazione. Il nuovo scenario generico contiene esclusivamente host e clock. Nessuna primitiva 3D, formula live, costo o Lesson Logic.

**A03:** la transizione conserva ID e lesson destinazione; le fasi osservabili sono `Requested → SceneLoaded → Composing → Binding → Ready`, oppure `Failed`. Il bootstrap compone e collega i consumer mediante chiamate esplicite. `Ready` viene pubblicato dopo il binding, con la sessione disponibile; il dispatch rifiuta navigazioni rientranti. Due lesson nello stesso workspace ricompongono la sessione senza dipendere da un nuovo evento nativo di caricamento scena.

**A04:** ogni `SimulationResult` contiene insieme `SimulationConfiguration` immutabile e `TrajectoryHistory` della stessa revisione. I consumer interpretano i campioni con `Result.Configuration`; configurazione richiesta e tentativo fallito sono espliciti e distinti. Un fallimento conserva l'intero ultimo risultato valido, inclusi parametri e passo fisico.

| Criterio / verifica | Esito |
| --- | --- |
| Compilazione finale Unity | **PASS**, Unity `6000.3.8f1`, exit code 0, nessun errore C#, dopo rimozione dei verificatori temporanei |
| Tutti gli EditMode | **115/115 PASS**, zero falliti/saltati: 60 catalogo, 26 pendolo, 13 rollout, 16 sessione |
| Tutti i PlayMode | **15/15 PASS**, zero falliti/saltati, inclusi sei nuovi casi lifecycle/sessione |
| Unico proprietario e revisioni coerenti | Sessione pura; una pubblicazione completa per modifica matematica; configurazione e storia associate alla stessa revisione |
| Velocità distinta da δ / terminale | Clock verificato a 30/60/144 aggiornamenti per secondo, due rate di playback, Update nativo e arresto al terminale; δ del pendolo resta 0.02 s |
| Rewind e causalità | Seek/rewind non chiamano Evaluate; la modifica di u₂ ricalcola solo le transizioni 2 e 3 nel caso T=4 e conserva il prefisso |
| Reset / asset | Ripristino del preset catturato all'apertura, cursore a zero e pausa; override runtime non scrivono gli asset |
| A03 | Ordine degli eventi, binding prima di Ready, rientranza da observer, due lesson nello stesso workspace, fallimento esplicito e chiusura/riapertura verificati |
| A04 | Risultati A/B con masse e campioni propri immutati; fallimento numerico successivo conserva configurazione e storia dell'ultimo risultato valido |
| A01 / regressione matematica F03 | Casi precedenti rieseguiti, legacy/round trip preservati, provider pendolo rifiuta passo assente/non valido, sessione index-only produce 2+3=5 con δ/τ assenti |
| Regressione F00–F02 | Tre cicli nativi menu/workspace/menu riusciti, incluso uno senza domain reload; URP, camera, pavimento, luce e Volume conservati |
| Verifica nativa/visiva F04 | Menu/workspace/comandi/ritorno/riapertura nell'Editor; Game View ispezionata con r2, t=2/100, δ=0.02 s, τ=0.04 s; zero errori runtime |
| Identità dei sorgenti verificati | **179 file di Assets/OptimalControl, zero differenze SHA-256** tra copia Unity verificata e workspace |

Rapporto completo, inventario di **34 file creati e 18 modificati**, API, compatibilità e procedura manuale: [F04_IMPLEMENTATION.md](REVIEWS/F04_IMPLEMENTATION.md). Nessun file preesistente eliminato. I report F00–F03 e A01 restano storici; il loro precedente stato di configurazione non eseguibile è superato dall'assegnazione del provider/scenario di F04.

**Decisioni di perimetro:** indicatore runtime limitato a revisione/timestep/tempo; comandi di verifica nell'Inspector editor, senza anticipare i controlli dello studente F05/F06. La verifica nativa è automatizzata su copia isolata e accompagnata da ispezione manuale della cattura; l'Editor dell'utente resta aperto. Nessun nuovo pacchetto. Il prerequisito «provider concreto disponibile» è soddisfatto dalla creazione prevista nella stessa F04, come già chiarito nella consegna F03.

Evidenze locali: `C:/Users/querz/AppData/Local/Temp/OptCon-F04-3ef1fc98d7284d97a1a460599d552509/`, XML EditMode/PlayMode, log, risultati sessione/smoke, `session-preview.png` e manifest dei sorgenti. A02 rimane da affrontare prima di Formula/HUD; F05–F30 non avviate nella consegna originale di F04.

## F05 — Visualization primitives e prima dinamica manipolabile

- **Stato:** **completata**, 9 ottobre 2026; tutti i criteri sotto verificati. Nella consegna originale F05 A01/A03/A04 sono preservati e A02 resta per F06, allora non avviata.
- **Obiettivo:** rendere visibili gli stati e il controllo usando componenti riutilizzabili, e permettere di manipolare la dinamica in Unity.
- **Prerequisiti:** sessione/command sink funzionanti e storia immutabile disponibile.
- **Dipendenze:** F04.
- **File/cartelle:** creare `A/Runtime/Visualization/TrajectoryRenderer.cs`, `StateMarker.cs`, `VectorRenderer.cs`, indicatore curvo di coppia, `StateProjection.cs`, `LayerController.cs`, `HighlightRegistry.cs`; creare `A/Prefabs/PendulumScenario.prefab`, marker/assi, materiali URP e `A/Data/Visualizations/`; aggiungere `A/Runtime/Application/LessonInputRouter.cs` e azioni di input dedicate.
- **Sistemi Unity:** LineRenderer, primitive/mesh semplici, Transform alimentati dagli snapshot, Input System; conversione double→float al confine visivo.
- **Risultato verificabile:** pendolo, piano di fase e traiettoria 3D `(θ,ω,τ)`; marker x_t/x_(t+1), coppia e assi. Controlli semplici modificano u e comandano play/pause/step/reset.
- **Criteri di completamento:** ogni punto deriva dalla storia; terzo asse etichettato tempo; freccia di coppia distinguibile da una forza; nessuna formula dinamica nel renderer; layer nascosti non cambiano il modello.
- **Test:** controllo manuale dei punti/assi/scale e dei controlli; PlayMode mirato che verifica proiezione di due campioni e aggiornamento dei marker dopo un comando. Nessun test sulle sole scelte cromatiche.

### Consegna e verifica di F05

Prima dell'implementazione sono stati controllati stato/evidenze di F04 e i suoi contratti: **179 file applicativi identici alla consegna verificata**, sessione/command sink funzionanti, storia e configurazione del risultato immutabili. Nessuna regressione concreta di A01/A03/A04 richiede di riaprire quelle correzioni.

`PendulumScenario` compone componenti riutilizzabili di Presentation: proiezione dello stato, traiettoria, marker, posa angolare, coppia curva, assi, layer e registry semantico. Corpo, piano di fase e stato-tempo leggono la stessa pubblicazione della sessione; la lunghezza appartiene a `Result.Configuration`, mai ai default editoriali o a un tentativo fallito. I renderer non valutano la dinamica. I buffer float servono soltanto al rendering.

Il corpo è planare, con angolo zero verso il basso e positivo antiorario; il grafico di fase usa `(θ,ω)`, quello 3D `(θ,ω,τ)`. Le scale sono dichiarate: corpo 1.5 unità/m, fase 4 e 1 nelle rispettive unità, stato-tempo 3 / 0.65 / 1.4. Il terzo asse indica tempo fisico in secondi, non posizione; una proiezione fisica rifiuta storie index-only, che possono usare un asse t esplicito. Linee come guide tra campioni, next/ghost assenti al terminale e simbolo di coppia curvo con dimensione fissa evitano significati matematici impliciti.

`LessonInputRouter` usa un clone locale dell'action asset dedicato e gli stessi comandi puri di F04: P play/pausa, frecce step, R reset, A/D variazione dell'input sulla coda, Z azzeramento, 1/2/3 layer. Componente/unità/incremento/policy sono configurabili; la legenda deriva da questi dati. Il binding avviene nell'host prima di Ready e la chiusura scollega consumer e azioni prima di disporre la sessione.

| Criterio / verifica | Esito |
| --- | --- |
| Compilazione finale Unity | **PASS**, Unity `6000.3.8f1`, exit 0, nessun errore/warning C#, dopo rimozione degli strumenti temporanei |
| Tutti gli EditMode | **122/122 PASS**, zero falliti/saltati; 115 regressioni e sette nuovi casi di proiezione |
| Tutti i PlayMode | **21/21 PASS**, zero falliti/saltati; 15 regressioni e sei nuovi casi F05 |
| Ogni punto dalla storia | Tutti i 101 punti di entrambe le curve confrontati con i campioni della stessa revisione; marker t/t+1 aggiornati dopo comandi |
| Coordinate deterministiche | Due campioni noti del pendolo e caso scalare 2+3=5 verificati; nessuna equazione Euler duplicata |
| Assi / tempo opzionale | Unità/scale/componenti omesse esplicite; asse fisico rifiutato senza secondi, asse t valido senza inventare δ/τ |
| Coppia / terminale | Freccia curva con segno verificato tramite eventi Keyboard nell'Input System, zero senza freccia, terminale senza u/next/ghost |
| Layer | Visibilità non altera risultato/cursore; i target nascosti ricevono i campioni aggiornati |
| Lifecycle/configurazione | Binding visuale/input prima di Ready, chiusura/riapertura, disable/enable e configurazione del risultato verificati; A03/A04 ancora validi |
| Pendolo / A01 | Preset matematico ed equazioni invariati, δ=0.02 s e τ=0.4 s a t=20; suite A01 legacy/round trip e index-only passata |
| Regressione nativa | **3/3 cicli menu/workspace/menu PASS**, incluso uno senza domain reload; root/camera/presenter unici, URP/floor/light/Volume conservati, zero script mancanti/errori runtime |
| Verifica visiva | Game View `1440×979` ispezionata: tre viste, current/next, coppia, assi/unità/scale e controlli leggibili |
| Identità dei file verificati | **240 file applicativi, zero differenze SHA-256** tra workspace e copia Unity verificata |

Rapporto completo, inventario di **62 file creati e 8 modificati**, API/dipendenze, controlli e procedura manuale: [F05_IMPLEMENTATION.md](REVIEWS/F05_IMPLEMENTATION.md). Il preset cambia solo scenario e descrizione; camera/pavimento e pannello editoriale sono adattati al viewport. Core, Simulation, Content, formule/metriche, altre specifiche/review, PDF, asset URP, ProjectSettings e pacchetti non sono modificati; nessun file preesistente eliminato o `.meta` preesistente cambiato.

**Modalità di verifica e limiti:** prove native su copia isolata, dispositivo Keyboard virtuale con eventi attraverso l'Input System nei PlayMode e ispezione manuale delle catture, senza usare l'Editor aperto dell'utente. Nessuna deviazione funzionale. Inquadratura/scale fisse e dichiarate: valori molto grandi possono uscire dal viewport, senza alterare i dati o introdurre saturazioni. Nessun Formula/HUD, resolver, costo, target, confronto o Lesson Logic anticipato; A02 resta aperto all'inizio di F06.

Evidenze locali: `C:/Users/querz/AppData/Local/Temp/OptCon-F05-b35a9c53a4fe482396eddd580fb8f583/`, XML finali EditMode/PlayMode, log, `visualization-preview.png`, risultati della scena/smoke e manifest SHA-256. **F06–F30 non avviate nella consegna originale F05.**

## F06 — Formula System e HUD live

- **Stato:** **completata**, 9 ottobre 2026; tutti i criteri sotto verificati. A02 risolto, A01/A03/A04 preservati. F07 non avviata nella consegna originale F06.
- **Obiettivo:** collegare formula, termini, numeri e target 3D alla stessa revisione/timestep.
- **Prerequisiti:** binding editoriali, metriche e registry dei target; primo esperimento visibile.
- **Dipendenze:** F01, F05.
- **File/cartelle:** creare `A/Runtime/UI/FormulaBindingResolver.cs`, `FormulaPresenter.cs`, `LessonHudPresenter.cs`, `TimelinePresenter.cs`, `ParameterPanelPresenter.cs`; creare `A/UI/LessonHud.uxml/.uss`, asset `A/Data/Formulas/DiscreteDynamics.asset` e specializzazione del pendolo; aggiornare Bootstrap e scenario.
- **Sistemi Unity:** UI Toolkit, selezione dei termini, highlight/materiali URP, pulsanti/slider collegati al command sink di F04.
- **Risultato verificabile:** formula `x_(t+1)=f_t(x_t,u_t)` e specializzazione leggibili; numeri live, descrizioni/unità; selezionare x_t/u_t/f_t/x_(t+1) evidenzia il significato nella scena. Timeline e pulsanti controllano la sessione.
- **Criteri di completamento:** `state.next` non subisce un secondo incremento temporale; valore/highlight risolvono lo stesso campione; f_t mostra modello/parametri; stato terminale «non applicabile» per input/next; formule in asset, non stringhe disperse nei MonoBehaviour. Unicode e lista di termini bastano al slice.
- **Test:** EditMode del resolver su current/next/terminal, binding assente e unità eterogenee; PlayMode su selezione→highlight e UI/3D della stessa revisione; controllo manuale di font, focus e leggibilità.

### Consegna e verifica di F06

Prerequisiti verificati prima dell'integrazione: F00–F05 completate, **240 file applicativi identici alla consegna F05**, sessione/risultato immutabile, binding editoriali e registry disponibili. Non è stato necessario modificare Core o Simulation.

`RuntimeMetricResolver` in Content è l'unica regola pura di lookup, tipo, forma, selezione e unità. Un vettore n=1 resta Vector; Scalar richiede una metrica scalare reale oppure una componente esplicita. Il binding u_t del prototipo usa ora component=0. `MetricResolution` conserva il risultato della revisione, validità/diagnostico e SampleIndex già risolto. Next non incrementa una seconda volta; δ/τ rimangono opzionali; input/next sono N/A al terminale. **A02 risolto**, con accesso tipizzato estendibile tramite provider senza trasformare gli scalari in vettori.

`FormulaBindingResolver` delega al resolver puro; `FormulaPresenter` legge gli asset e il risultato. Il HUD laterale mostra formula, termini, valori/unità, spiegazioni, selezione e legami semantici. Timeline/input/parametri inviano gli stessi comandi F04. `AppBootstrap` collega il HUD prima di Ready e lo scollega alla chiusura. `HighlightRegistry` riceve ruolo, indice assoluto e revisione; il nuovo segmento evidenzia la transizione salvata per f_t. Modello/parametri vengono da `Result.Configuration`, preservando A04. Formula e 3D sono consumer della stessa pubblicazione.

| Criterio / verifica | Esito |
| --- | --- |
| Compilazione finale senza verificatori temporanei | **PASS**, Unity 6000.3.8f1, exit 0, nessun errore/warning C# |
| Tutti gli EditMode | **150/150 PASS**, zero falliti/saltati: 122 regressioni e 28 nuovi casi resolver/formule |
| Tutti i PlayMode | **30/30 PASS**, zero falliti/saltati: 21 regressioni e nove nuovi casi HUD |
| Current/input/next, tipi e unità | Scalar reale, Vector n=1/2/4, selezione esplicita, diagnostici per componente/tipo/forma/unità/scope invalidi; unità eterogenee preservate |
| SampleIndex / terminale | next @t+1, senza secondo offset; a T input/next/ghost/transizione assenti, nessun valore fittizio |
| Formula e f_t | Asset discreto generico e specializzazione Euler, termini selezionabili; f_t mostra descrittore e parametri appartenenti al risultato |
| A01 / A04 | Pendolo δ=.02 s, τ=.4 s a t=20; index-only 2+3=5 senza δ/τ; A/B e fallimento mantengono configurazioni/campioni propri |
| A03 / revisione condivisa | HUD collegato prima di Ready, stessa sessione/risultato della visualizzazione; rientranza e chiusura/riapertura verificate dalle suite precedenti |
| Interazione UI e focus | Click Mouse/Input System su un termine, highlight corretto, seek/trasporto/input/parametri reali, guard dei campi e regressione tastiera F05 passati |
| Rebinding a orizzonte ridotto | Passaggio da T=100 terminale a T=1 mantiene la nuova sessione a t=0; il rendering dello slider non invia comandi |
| Verifica nativa e visiva | Pendolo/formula/dettaglio/terminale e index-only corrente/terminale: sei Game View 1440×979 ispezionate, zero errori runtime |
| Regressione nativa | **3/3 cicli menu/workspace/menu PASS**, incluso uno senza domain reload; URP/floor/light/Volume e unicità dei servizi conservati |
| Identità verificata | **274 file applicativi, zero differenze SHA-256** tra workspace e copia Unity verificata |

Rapporto completo, inventario di **37 file creati e 14 modificati**, contratto A02, copertura dei test, catture e procedura manuale: [F06_IMPLEMENTATION.md](REVIEWS/F06_IMPLEMENTATION.md). Nessun file preesistente eliminato o `.meta` preesistente modificato. Preset matematico del pendolo, Core/Simulation, altre specifiche/review, PDF, URP, SampleScene, Packages e ProjectSettings invariati.

**Modalità e decisioni di perimetro:** automazione nativa su copia isolata con ispezione manuale delle catture; Editor dell'utente preservato. Resolver nel layer superiore Content e template HUD condiviso, senza refactor delle API Core/Simulation. Il fixture index-only è editor/test-only; la conservazione A/B verifica il contratto e non introduce il confronto utente. Nessun costo, target F07, solver, layout matematico complesso o Lesson Logic anticipato. Nessun problema bloccante rimasto; precisione mostrata G6 e inquadratura F05 fissa sono limiti dichiarati.

Evidenze: `C:/Users/querz/AppData/Local/Temp/OptCon-F06-cea2f2e7b0f842608fb60bb30be99819/`, XML/log finali, risultati runtime/smoke e manifest. Due catture sono conservate in `Docs/REVIEWS/F06_FORMULA_HUD.png` e `F06_INDEX_ONLY_HUD.png`. **F07–F30 non avviate nella consegna originale F06.**

## F07 — Costo, target e confronto limitati al slice

- **Stato:** **completata**, 9 ottobre 2026; tutti i criteri sotto verificati. A01–A04 preservati; F08 non avviata.
- **Obiettivo:** aggiungere le anteprime di stage/terminal cost e il confronto A/B previsti dalla visione, usando gli stessi dati della dinamica.
- **Prerequisiti:** HUD live, parametri validati e selezione semantica funzionanti.
- **Dipendenze:** F06.
- **File/cartelle:** creare `A/Runtime/Core/Costs/QuadraticTrackingCost.cs`, `A/Runtime/Simulation/ComparisonSnapshot.cs`, `A/Runtime/Visualization/CostContributionVisualizer.cs`, `TargetVisualizer.cs`, `ComparisonTrajectoryRenderer.cs`; creare `A/Data/Formulas/AdditiveCost.asset`; estendere HUD, metriche/preset e test costo/confronto.
- **Sistemi Unity:** marker target, renderer di traiettoria riusato per A/B, grafico/barre semplici e controlli UI dei pesi/target/T; nessun solver.
- **Risultato verificabile:** tracking/effort/cumulativo/terminale cambiano con configurazione e input; selezione di ℓ_T evidenzia x_T; A resta visibile quando si modifica B.
- **Criteri di completamento:** terminale contato una volta; costo discreto identificato; parametri e orizzonti di A/B visibili; cambiando pesi, i costi sono attribuiti ai rispettivi obiettivi o ricalcolati sotto un obiettivo comune esplicito. Anteprima limitata a un solo preset matematico.
- **Test:** EditMode su somma dei contributi, cumulativo e terminale/T=1; immutabilità di A; PlayMode su highlight terminale mentre il cursore è altrove e modifica dei pesi senza alterare la dinamica.

### Consegna e verifica effettiva di F07

Costo opzionale data-driven, configurazione matematica pura immutabile, stage tracking/effort, terminale, prefissi C_t e J conservati nel medesimo `SimulationResult` della traiettoria. Il resolver F06 espone i valori senza ricalcolo nel HUD o nei renderer. Target, diamanti terminali e grafici riusano le primitive F05; selezionare ℓ_T/x_T evidenzia @T anche con cursore altrove. Controlli runtime per pesi/target/T e salvataggio A; ciascun confronto conserva modello, orizzonte, passo e obiettivo propri. Nessuna modifica alle equazioni del pendolo o al rollout, nessun solver o Lesson Logic.

| Criterio / verifica | Esito |
| --- | --- |
| Unity 6000.3.8f1, compilazione finale senza helper temporanei | **Exit 0**, zero errori/warning C# |
| Tutti gli EditMode | **180/180 PASS**, inclusi **30 nuovi test costi**; zero falliti/skipped |
| Tutti i PlayMode | **38/38 PASS**, inclusi **8 nuovi test costo/presentazione**; zero falliti/skipped |
| Stage, terminale e prefissi | Index-only x0=2, u0=3, T=1: stage=13, terminale=25, J=38, C=[0,13]; T=2: J=111, C=[0,13,47] |
| Termini e terminale | Formula data-driven selezionabile; x_T/ℓ_T @T, stage e u_T N/A al terminale; C_T esclude il terminale, J lo conta una volta |
| Costo discreto e A01 | Nessun fattore δ; pendolo δ=.02 s preservato; index-only senza δ/τ artificiali |
| Revisione e A04 | A mantiene configurazione/campioni/costi dopo B; errori conservano l'ultimo risultato valido completo |
| Modifica pesi, target e T | Pesi/target rivalutano i costi senza chiamare la dinamica; UI mostra parametri/orizzonti/obiettivi propri di A/B; T ricostruisce il rollout |
| Confronto e semantica | A viola spessa/B blu sottile; overlay sull'intervallo comune, anche con δ diversi; highlight terminale corretto con cursore altrove |
| A02 / A03 e regressioni | Resolver tipizzato e lifecycle precedenti passati; **3/3 cicli nativi menu/workspace/menu**, incluso uno senza domain reload |
| Runtime e verifica visiva | Pendolo/A-B/terminale e index-only stage+terminale concordi tra HUD, costo e grafico; **sei Game View 1440×979 ispezionate**, zero errori runtime |
| Identità della consegna | **308 file applicativi**, zero differenze SHA-256 rispetto alla copia verificata |

Report completo, inventario di **39 file creati e 17 modificati**, architettura, test, catture e procedura manuale: [F07_IMPLEMENTATION.md](REVIEWS/F07_IMPLEMENTATION.md). Nessun file preesistente eliminato o `.meta` preesistente cambiato. Altre specifiche/report, PDF, scene, asset URP, pacchetti e ProjectSettings invariati. Le API costo opzionale/risultato/comandi sono estensioni minime necessarie, documentate e testate; nessuna deviazione funzionale da F07.

**Modalità e limiti:** esecuzioni native su copia isolata e ispezione manuale delle catture; Editor dell'utente preservato. Preview con pesi diagonali, target costante e un solo preset; confronto esplicitamente sotto i rispettivi obiettivi, senza attribuzioni di ottimalità. T=1…10000, estensione ripetendo l'ultimo input; formato G6 e viewport F05 fisso. Nessun problema bloccante rimasto.

Evidenze: `C:/Users/querz/AppData/Local/Temp/OptCon-F07-8f89939fb9ab4940a05b9d63578afc04/`, XML/log finali, risultati runtime/smoke, sei Game View e manifest SHA-256. Quattro catture conservate in `Docs/REVIEWS/F07_*.png`. **F08–F30 non avviate.**

## F08 — Lesson Logic e completamento del primo vertical slice

- **Stato:** **completata**, 10 ottobre 2026; tutti i criteri sotto verificati. A01–A04 preservati; F09 non avviata.
- **Obiettivo:** validare l'intera pipeline attraverso un esperimento guidato con verifica concettuale, non soltanto una demo tecnica.
- **Prerequisiti:** dinamica, 3D, formula, HUD, costo/confronto e navigazione disponibili.
- **Dipendenze:** F02, F07.
- **File/cartelle:** creare `A/Runtime/Lessons/LessonSession.cs`, valutatori di condizioni e registro; completare `A/Runtime/Content/LessonStepDefinition.cs` e descrittori delle condizioni; aggiornare `A/Data/Lessons/Prototype/PendulumSlice.asset`, HUD e test end-to-end del slice.
- **Sistemi Unity:** passi guidati, abilitazione di controlli/layer, UI delle spiegazioni/risposte, eventi di sessione e selezione.
- **Risultato verificabile:** da Bootstrap lo studente prevede una transizione, cambia u, avanza, legge i valori e spiega i termini; poi esplora, confronta A/B e conclude una verifica. Questo è il primo vertical slice completo.
- **Criteri di completamento:** tutti i requisiti della matrice del slice passano; play/pause/step/reset più rewind; pipeline Model→Simulation→Visualization→Formula→HUD→Lesson Logic osservabile; nessun completamento automatico di C1.L2/L3; catalogo prototipo separato dal corso di produzione.
- **Test:** PlayMode end-to-end su modifica input e propagazione della stessa revisione a tutti i consumer; current/next/terminal; chiusura/riapertura; controllo manuale dell'intero percorso didattico e confronto con risultati Core.

### Consegna e verifica di F08

Prerequisiti verificati prima dell'implementazione: navigazione, sessione, dinamica, 3D, Formula/HUD, costi e confronto disponibili; **308 file applicativi identici alla consegna F07**. Il prototipo contiene ora sette step editoriali: stato → previsione/input → transizione/current-next → traiettoria → costo/peso → A/B → terminale/verifica concettuale.

`LessonPlan` congela i metadati, `LessonSession` orchestra i comandi esistenti, `LessonConditionRegistry` valuta azioni/selezioni/risposte/terminale/confronto. `LessonPresentationState` e `ILessonRuntime` sono contratti Content, senza dipendenza Presentation→Application. La stessa politica limita i comandi UI/tastiera e i controlli mostrati. Formule e highlight riusano resolver/semantic registry F06/F05; costi e risultati rimangono F07. **Core e Simulation invariati**, nessuna seconda source of truth o nuova dipendenza esterna.

| Criterio / verifica | Esito |
| --- | --- |
| Compilazione finale Unity | **PASS**, `6000.3.8f1`, exit code 0; zero errori e warning C#, dopo rimozione dei verificatori temporanei |
| Tutti gli EditMode | **206/206 PASS**, zero falliti/saltati; 26 nuovi casi Lesson Logic |
| Tutti i PlayMode | **50/50 PASS**, zero falliti/saltati; 12 nuovi casi di orchestrazione |
| Pipeline completa | Percorso nativo di tutti i sette step attraverso UI, dati reali e revisioni comuni; dieci Game View ispezionate |
| Play/pause/step/reset/rewind | Step di esplorazione e suite strumenti liberi; input/parametri consentiti per step, nessun bypass da tastiera |
| Current/next/terminal | Il nuovo current coincide con il precedente next; highlight con SampleIndex corretto; al terminale nessun input/stage/next fittizio |
| Costo e A/B | Modifica Q[0] cambia J senza cambiare stati; A conserva configurazione/campioni/costi propri dopo modifica B o fallimento successivo |
| A01–A04 | Suite precedenti rieseguite; pendolo δ=.02 s, τ_T=2 s; index-only 2+3=5/J=38 senza δ/τ; resolver tipizzato e lifecycle Ready preservati |
| Reset/back/lifecycle | Preset e condizioni ripristinati, A rimosso dalla lesson, selezioni/controlli/layer riapplicati; dispose e nuova sessione su riapertura/cambio lesson |
| Completamento | Soltanto prototipo corrente, dopo verifica esplicita; nessuna progressione persistente o completamento C1.L2/L3 |
| Smoke/regressioni | Tre cicli nativi menu/workspace/menu, due con domain reload e uno senza; scene, URP e asset matematici preservati |
| Identità sorgenti | **329 file applicativi, zero differenze SHA-256**, nessun file extra rispetto alla copia Unity verificata |

Report completo: [F08_IMPLEMENTATION.md](REVIEWS/F08_IMPLEMENTATION.md), con inventario di **29 file creati e 20 modificati**, test, catture e procedura manuale. Nessun file preesistente eliminato. **Decisioni:** Indietro riparte dal preset del passo; Reset guidato torna al primo step; esplorazione libera sblocca gli strumenti senza attribuire completamento e Reset ne conserva la modalità. Persistenza e validazione generale restano F09. Limiti F07 conservati: diagonali, target costante, viewport fisso; la Lesson Logic non li assume universali.

Evidenze: `C:/Users/querz/AppData/Local/Temp/OptCon-F08-ec9d91964e83410993697f4884d3ecac/`, XML/log finali, runtime/smoke, dieci Game View e manifest. Sette catture conservate in `Docs/REVIEWS/F08_*.png`. **F09–F30 non avviate.**

## F09 — Stabilizzazione, validazione e progressione persistente

- **Stato:** **completata**, 10 ottobre 2026; tutti i criteri sotto verificati. AF08-01/02/03 preservati; AF08-04/05/06/07 risolti. **F10 non iniziata.**
- **Obiettivo:** correggere i problemi emersi nel slice e rendere affidabili contenuti, transizioni e salvataggi.
- **Prerequisiti:** slice accettato e osservazioni d'uso raccolte; schema dei dati runtime/editor già distinto.
- **Dipendenze:** F08.
- **File/cartelle:** creare `A/Editor/CourseContentValidator.cs`, `BindingValidator.cs`, controlli scene/fonti e un riepilogo di validazione; creare `Runtime/Course/ProgressionService.cs`, `IProgressStore.cs`/store versionato; estendere sessione/lifecycle e test di dati/progresso.
- **Sistemi Unity:** editor tooling, serializzazione del progresso nel percorso applicativo, caricamento scene, UI dei prerequisiti e gestione delle sessioni.
- **Risultato verificabile:** contenuti invalidi producono errori localizzati; riavviando l'app il progresso valido resta; riordinare capitoli/lezioni conserva l'identità dei completamenti.
- **Criteri di completamento:** niente dipendenze circolari tra assembly; validazione di ID/cicli/capability/unità/metriche/scene/pagine; nessun listener residuo; dati non finiti mantengono identificabile l'ultima revisione valida. Versione dello schema e del contenuto impedisce di promuovere automaticamente il completamento del prototipo.
- **Test:** EditMode su validatori, migrazione del salvataggio e prerequisiti; PlayMode di transizioni ripetute e risultato obsoleto scartato se esistono lavori asincroni. Non introdurre asincronia solo per poterla testare.

### Consegna e verifiche F09

Progressione separata da catalogo e runtime: completamenti per courseId/lessonId/versione/schema, store JSON v1 nel percorso applicativo e store in memoria sostituibile. Prerequisiti orientativi come previsto dall'architettura; nessuna promozione del prototipo a C1.L1. Commit dopo pubblicazione educativa riuscita e fine del notification scope, verificando destinazione, lifetime, risultato e consumer disponibili. Reset/restart non duplicano il credito.

AF08-04: dominio di editing input indisponibile dopo richiesta fallita, con recupero/reset comune a HUD/tastiera/Inspector. AF08-05: target/tipo/unità confrontati col provider. AF08-06: resolver/condition registry composti e passati coerentemente a runtime e validatori. AF08-07: completion text neutro. CourseContentValidator/BindingValidator consolidano i controlli e diagnosticano capability non ancora eseguibili.

Unity 6000.3.8f1: **268/268 EditMode, 64/64 PlayMode**, zero falliti/saltati; compilazione finale exit 0 senza errori/warning C#. Percorso nativo F08, file/reload anche in nuovo processo Unity, seconda lesson/prerequisiti/identità, pendolo/index-only, NonRepresentable, Enter fallito, richiesta T1 fallita e recuperi: PASS. Tre smoke menu/workspace/menu, incluso senza domain reload: PASS. **359 file applicativi identici alla copia verificata**. Report, inventario e procedura: [F09_IMPLEMENTATION.md](REVIEWS/F09_IMPLEMENTATION.md). **READY FOR F10; F10 non iniziata.**

## F10 — Authoring formule e prova di estensibilità

- **Stato:** **completata**, 10 ottobre 2026. Contratto comune dinamico/statico, obiettivo quadratico tramite catalogo/host reale, layout strutturato e authoring verificati. F11 non era iniziata nella consegna F10; il suo stato corrente è riportato nella sezione successiva.
- **Obiettivo:** consolidare la UI e dimostrare che un esperimento diverso si aggiunge senza riscrivere i sistemi comuni.
- **Prerequisiti:** infrastruttura stabile e validatori; feedback reale sulla leggibilità delle formule del slice.
- **Dipendenze:** F09.
- **File/cartelle:** creare `A/Runtime/UI/IFormulaView.cs` e layout strutturato minimo per somme/frazioni/indici/matrici; strumenti `A/Editor/FormulaAuthoring/`; introdurre `A/Runtime/Core/Optimization/IObjectiveFunction.cs` inizialmente per il valore, `A/Runtime/Simulation/IExperimentSnapshot.cs`/contratto di sessione e relativo provider; adattare SimulationSnapshot/ExperimentHost al contratto comune; creare un piccolo obiettivo quadratico e asset/prefab di test in `A/Tests/Fixtures/`. Riutilizzare formula, marker, vettori e UI esistenti.
- **Sistemi Unity:** UI Toolkit/layout matematico, Inspector/editor di authoring, prefab comuni e font con simboli del corso.
- **Risultato verificabile:** formula di costo, matrice e integrale di esempio leggibili; un esperimento quadratico di test permette di variare z/pesi e vedere valore e rappresentazione cambiare.
- **Criteri di completamento:** nessuna modifica specifica per capitolo in CourseManager o TrajectoryRenderer; l'host accetta anche un esperimento senza dinamica tramite un provider, senza inventare una timeline fisica; layout e binding restano separati. Il fixture non è una nuova lezione del curriculum.
- **Test:** test dei binding/dimensioni del secondo esperimento; prova manuale con espressioni reali di I/O/K e selezione dei termini. Scegliere un'eventuale dipendenza per formule solo se il prototipo dimostra una necessità concreta.

**Verifica di completamento F10:** Unity 6000.3.8f1, compilazione finale exit 0 senza errori/warning C#; **296/296 EditMode e 74/74 PlayMode**, zero falliti/saltati. Conservati tutti i 268+64 test F09; aggiunti 28+10 test. Pendolo completo nativo (formula, traiettoria, costo, A/B, lesson, progresso, terminale, failure e recupero) e fixture statica tramite normale UI/catalogo/workspace: PASS. Oracoli statici 9→12→20, revisioni immutabili, scope Snapshot senza SampleIndex/tempo/trajectory, provider custom E2E, readiness, teardown, NonRepresentable ed Enter fallito verificati. Layout somma/frazione/indici/matrice/integrale e selezione ispezionati nella Game View; matrice solo di layout, nessun Matrix runtime. Nessun ramo per capitolo in CourseManager o TrajectoryRenderer; nessuna dipendenza esterna. Tutti i 416 file applicativi coincidono con la copia verificata. Report, inventario, XML e catture: [F10_IMPLEMENTATION.md](REVIEWS/F10_IMPLEMENTATION.md). **READY FOR CHAPTER 1 PRODUCTION / F11.**

## F11 — Chapter 1: lezione introduttiva completa

- **Stato:** **completata**, 11 ottobre 2026. Corso di produzione `optimal-control`, Chapter `C1`, lesson indipendente `C1.L1` versione/schema `1`, esperimento `c1.introduction.pendulum`; accesso dal Bootstrap salvato senza fixture. **F12 non iniziata.**
- **Obiettivo:** trasformare il laboratorio validato nella C1.L1 di produzione, includendo motivazione, stato/input/output e tempo continuo/discreto.
- **Prerequisiti:** esperienza del slice completa e authoring verificato su un secondo esperimento.
- **Dipendenze:** F08, F10.
- **File/cartelle:** creare `A/Data/Courses/OptimalControlCourse.asset`, `A/Data/Chapters/Chapter01.asset`, `A/Data/Lessons/Chapter01/C1_L1.asset` e formule/glossario necessari; riusare `PendulumScenario`/Experiment; aggiornare catalogo di Bootstrap e fonti/passaggi, mantenendo il prototipo separato.
- **Sistemi Unity:** course/lesson data, HUD, steps, progressione e scenario già presenti.
- **Risultato verificabile:** Chapter 1 appare nel menu e C1.L1 propone esperimento, motivazioni e verifica completa, con riferimenti I pp. 2–10 e pendolo/Euler dichiarati.
- **Criteri di completamento:** lo studente distingue x, u e y, legge t/τ/δ e riconosce che la posizione non è tutto lo stato; i domini motivanti sono contenuti contestuali, non scene decorative separate.
- **Test:** validazione asset e regressione del percorso del slice solo se il nuovo authoring modifica il wiring; prova manuale di prerequisiti, progressione e domande.

**Verifica di completamento F11:** Unity 6000.3.8f1, compilazione finale exit 0 senza errori/warning C#; **316/316 EditMode e 80/80 PlayMode**, zero falliti/saltati. Tutti i 296+74 casi F10 conservati e passanti; aggiunti 20+6 test. Catalogo e binding validati anche contro hash/pagine delle fonti. Percorso di produzione a undici passaggi verificato in runtime nativo tramite normale UI, con equazioni Euler prima della previsione, output puro h e campioni immutabili, stato distinto dalla posa, t/δ/τ, risposte errate bloccate e terminale senza input/next inventati. Completion soltanto `optimal-control/C1.L1`; ritorno/riapertura, restart Play e nuovo processo Unity confermano persistenza e sessione fresca. Pendolo/prototipo, index-only e fixture statica F10 riverificati nativamente; contratti A01–A04 e AF08-01–07 preservati. Tutti i **446 file applicativi** corrispondono alla copia verificata. Catture Game View ispezionate; interazioni native automatizzate, nessuno studio di usabilità dichiarato. Inventario, test, fonti, procedura e gate: [F11_IMPLEMENTATION.md](REVIEWS/F11_IMPLEMENTATION.md). **READY FOR F12.**

## F12 — Chapter 1: traiettorie, equilibri e linearizzazione

- **Obiettivo:** produrre C1.L2 con confronto numerico/visivo tra perturbazione nonlineare e modello linearizzato.
- **Prerequisiti:** C1.L1, confronto A/B e renderer comuni; nominale dinamicamente valida.
- **Dipendenze:** F11.
- **File/cartelle:** creare `A/Runtime/Core/Derivatives/IDifferentiableDynamics.cs`, Jacobiani del pendolo, `A/Runtime/Simulation/LinearizationComparison.cs`, visualizzazioni Δx/Δu/errore e `A/Data/Lessons/Chapter01/C1_L2.asset`; aggiungere formule e test derivate.
- **Sistemi Unity:** traiettorie/ghost/vettori/timeline riusati, layer e slider di perturbazione.
- **Risultato verificabile:** equilibrio immobile; aumentando una perturbazione si osservano previsione lineare, rollout vero ed errore. Fonti I pp. 10–13.
- **Criteri di completamento:** A/B valutati sulla nominale; Δx₀=0 per iniziale fissato e caso iniziale perturbato distinto; errore non confuso con residuo dinamico; formula mantiene la convenzione del Jacobiano.
- **Test:** differenze finite dei Jacobiani; equilibrio; test dell'ordine dell'errore locale per perturbazioni piccole; prova manuale dei layer e del peggioramento dell'approssimazione.

## F13 — Chapter 1: costo, vincoli e curva desiderata

- **Obiettivo:** produrre C1.L3–L4 raggruppando costo/vincoli e riferimento/traiettoria, senza anticipare un solver generale.
- **Prerequisiti:** costi scomposti, nominali/traiettorie e confronto; classificazione di uguaglianza/disuguaglianza definita.
- **Dipendenze:** F07, F12.
- **File/cartelle:** creare `A/Runtime/Core/Constraints/` e diagnostiche h/g, `A/Runtime/Visualization/ConstraintRegionVisualizer.cs`; aggiungere asset `C1_L3.asset`, `C1_L4.asset`, preset di riferimento a gradino, formule di vincolo/tracking e grafici necessari.
- **Sistemi Unity:** regioni e bordi campionati, marker di violazione, UI dei pesi/limiti e traiettorie desired/actual.
- **Risultato verificabile:** modificare pesi cambia i costi; stringere un limite cambia g/ammissibilità; un riferimento a gradino è distinguibile da un rollout valido. Fonti I pp. 14–25 e p. 35.
- **Criteri di completamento:** limite duro distinto da penalità; input richiesto/applicato distinti se si abilita un attuatore; fonti/notazione corrette; manual rollout e riferimenti identificati come tali. L'esperimento di ottimizzazione di C1.L4 resta assegnato a F28.
- **Test:** h su rollout e curva non ammissibile; g dentro/sul/fuori bordo; somma costi; prova manuale delle differenze desired/actual e degli highlight stage/terminal.

## F14 — Chapter 1: drone planare

- **Obiettivo:** produrre C1.L5 con un nuovo modello concreto a sei stati/due input e infrastruttura esistente.
- **Prerequisiti:** vincoli, costo, projection e binding multidimensionali; fonte I pp. 26–30 ricontrollata.
- **Dipendenze:** F13.
- **File/cartelle:** creare `A/Runtime/Core/Models/PlanarDroneDynamics.cs`, provider/preset drone, `A/Prefabs/PlanarDroneScenario.prefab`, projection di posizione/stato e `A/Data/Lessons/Chapter01/C1_L5.asset`; aggiungere formule/unità per le sei componenti e test del modello.
- **Sistemi Unity:** primitive planari nel viewport 3D, frecce delle spinte, LineRenderer/graph/HUD riusati e limiti di quota/input.
- **Risultato verificabile:** due spinte modificano posizione, assetto e velocità; costo e g dei limiti reagiscono nello stesso snapshot.
- **Criteri di completamento:** ordine `[p_x,v_x,p_y,v_y,θ,ω]`; J nominato momento d'inerzia; modello dichiarato planare; Euler applicato allo stato vecchio; nessun nuovo CourseManager, HUD o renderer di traiettoria.
- **Test:** equilibrio di hover `θ=ω=v_x=v_y=0, u₁=u₂=Mg/2`; segni di accelerazioni/momento, dimensioni, costi/limiti; prova manuale di stati diversi a pari posizione.

## F15 — Chapter 1: continuo e trasferimento del pendolo

- **Obiettivo:** produrre C1.L6 e mostrare la differenza tra problema continuo e sua discretizzazione numerica.
- **Prerequisiti:** pendolo, equilibri, riferimento a gradino e confronto; formula integrale leggibile.
- **Dipendenze:** F12, F14.
- **File/cartelle:** creare profili di riferimento/equilibrio, configurazione di quadratura del costo e confronto di discretizzazioni in `A/Runtime/Core/Costs/`/`Simulation/`; aggiungere `A/Data/Lessons/Chapter01/C1_L6.asset`, formule continue, preset e test di quadratura.
- **Sistemi Unity:** pendolo e piano di fase esistenti, grafici θ/ω/u, slider δ e confronto su durata fisica comune.
- **Risultato verificabile:** ridurre δ cambia l'errore numerico e la somma approssimante l'integrale; il riferimento tra equilibri e la traiettoria manuale restano distinti. Fonti I pp. 31–36.
- **Criteri di completamento:** `T_discreto·δ` distinto dalla durata continua; confronto a durata uguale; pesi/fattori ½ della specifica fonte dichiarati; terminale escluso dalla quadratura degli stage; esempio di trasferimento ottimizzato previsto in F28.
- **Test:** costo integrale noto su profilo semplice, raffinamento a durata fissata e correttezza dei due equilibri; validazione/controllo manuale delle sei lezioni Chapter 1 e delle funzionalità rinviate esplicitamente a F28.

## F16 — Chapter 2: minimi, derivate, superfici e QP

- **Obiettivo:** produrre C2.L1 e la rappresentazione comune di un obiettivo scalare su uno spazio delle decisioni.
- **Prerequisiti:** authoring multidimensionale e secondo esperimento di F10; Chapter 1 disponibile.
- **Dipendenze:** F10, F15.
- **File/cartelle:** estendere il contratto di obiettivo di F10 con capability di gradiente/Hessiano; creare preset delle funzioni O p. 53, `A/Runtime/Visualization/CostSurfaceVisualizer.cs`, contorni/curvature e `A/Data/Lessons/Chapter02/C2_L1.asset`, `A/Data/Chapters/Chapter02.asset` e formule.
- **Sistemi Unity:** mesh procedurale del costo, curve di livello, marker z, vettori e UI dei pesi/punto iniziale.
- **Risultato verificabile:** trascinare z aggiorna costo, gradiente e curvature; minimo, massimo e sella sono confrontabili. Fonti O pp. 3–9, 23–25 e 53.
- **Criteri di completamento:** asse dell'altezza identificato come costo; gradiente nello spazio z distinto dalla normale 3D; QP singolare non dato automaticamente per risolvibile; sella aggiuntiva etichettata come esempio progettuale.
- **Test:** gradienti/Hessiani contro differenze finite, QP con minimo noto e caso singolare non limitato; controllo manuale della superficie/contorni e della distinzione argmin/valore minimo.

## F17 — Chapter 2: convessità e limiti quadratici

- **Obiettivo:** produrre C2.L2 con geometria di convessità, μ/L e relative ipotesi.
- **Prerequisiti:** obiettivi, derivate e superfici verificati; convenzione μ/2 del curriculum.
- **Dipendenze:** F16.
- **File/cartelle:** creare visualizzatori di corda/piano di supporto/limiti quadratici e diagnostiche μ/L sui preset in `A/Runtime/Core/Optimization/`; aggiungere `A/Data/Lessons/Chapter02/C2_L2.asset`, formule e sottoesperimenti di monotonia/co-coercività.
- **Sistemi Unity:** due marker trascinabili, segmenti/piani/mesh di confronto e pannelli di approfondimento.
- **Risultato verificabile:** cambiare punti o autovalori del QP modifica corda, gradiente e limiti; μ/L descrivono la geometria e il condizionamento. Fonte O pp. 10–22.
- **Criteri di completamento:** Δz e Δgradiente hanno orientamento coerente; ipotesi del dominio visibili; distinguere convessità, stretta/forte convessità ed esistenza del minimo; enunciati sui vincoli seguono le rettifiche del curriculum.
- **Test:** disuguaglianze su QP noto con `μ=λ_min(Q)` e `L=λ_max(Q)`; controesempi alle equivalenze improprie delle fonti; verifica manuale dei pannelli.

## F18 — Chapter 2: iterazioni, discesa e scelta del passo

- **Obiettivo:** produrre C2.L3 e introdurre una storia delle iterazioni separata dal tempo della dinamica.
- **Prerequisiti:** costo/derivate/convessità e command/HUD comuni; direzioni di discesa definite.
- **Dipendenze:** F17.
- **File/cartelle:** creare `A/Runtime/Core/Optimization/SteepestDescent.cs`, strategie di passo/Armijo, `A/Runtime/Simulation/IterationHistory.cs` e sessione iterativa, `A/Runtime/UI/IterationPresenter.cs`; aggiungere tracce iterative, `A/Data/Lessons/Chapter02/C2_L3.asset` e formule.
- **Sistemi Unity:** controlli su k, una iterazione/play/pause/reset dell'algoritmo, vettore d, trace e grafici costo/gradiente/line search.
- **Risultato verificabile:** passi costanti/decrescenti e backtracking producono iterazioni osservabili; tentativi Armijo accettati e rifiutati sono distinti. Fonti O pp. 27–33, 38–46, 80–81.
- **Criteri di completamento:** k distinto da t; direzione e lunghezza separate; storia e reset deterministici; limiti di backtracking e fallimento espliciti; nessuna promessa di minimo globale fuori dalle ipotesi.
- **Test:** QP con gradiente noto, verifica della disuguaglianza Armijo e caso di passo eccessivo; clock iterativo indipendente dal framerate; storia/riproduzione; controllo manuale della notazione.

## F19 — Chapter 2: Newton e approssimazione quadratica

- **Obiettivo:** produrre C2.L4 e confrontare Newton/steepest descent sul medesimo problema.
- **Prerequisiti:** storia iterativa, Hessiani e line search funzionanti.
- **Dipendenze:** F18.
- **File/cartelle:** creare solver lineare per piccoli sistemi e `A/Runtime/Core/Optimization/NewtonMethod.cs`, esempio di root finding, confronto dell'Hessiano analitico/numerico e visualizzatore del modello quadratico; aggiungere `A/Data/Lessons/Chapter02/C2_L4.asset` e formule.
- **Sistemi Unity:** superfici locale/originale, tangente per radici, vettori e confronto delle tracce iterative.
- **Risultato verificabile:** Newton risolve il QP positivo definito in un passo pieno e differisce dal gradiente su problemi anisotropi. Fonti O p. 32 e pp. 34–37.
- **Criteri di completamento:** `HΔz=−∇ℓ`, update con `+Δz`; niente inverso numerico esplicito; singolarità/curvatura errata diagnosticate; Newton puro e smorzato identificati.
- **Test:** residuo del sistema lineare, QP a soluzione nota, zero scalare noto, Hessiano singolare/indefinito e backtracking; controllo manuale del modello locale.

## F20 — Chapter 2: proiezione e direzioni ammissibili

- **Obiettivo:** produrre C2.L5 con proiezioni calcolate dal modello su insiemi semplici.
- **Prerequisiti:** regioni/diagnostiche dei vincoli e sessione iterativa; insiemi scelti non vuoti, chiusi e convessi.
- **Dipendenze:** F13, F19.
- **File/cartelle:** creare `A/Runtime/Core/Optimization/IProjectionOperator.cs`, proiezioni box/disco/semispazio, projected gradient e metodo delle direzioni ammissibili; aggiungere `A/Data/Lessons/Chapter02/C2_L5.asset`, preset e formule.
- **Sistemi Unity:** regione, punto libero/proiettato, segmenti e controlli iterativi già presenti.
- **Risultato verificabile:** il passo libero può uscire dal set, quello proiettato resta ammissibile; un minimo al bordo mostra gradiente non nullo. Fonte O pp. 48–52.
- **Criteri di completamento:** proiezione numerica distinta da collisione; γ∈[0,1] per l'interpolazione dei punti ammissibili; condizione variazionale e ipotesi di sufficienza dichiarate.
- **Test:** proiezioni con risultato analitico dentro/sul/fuori set, ammissibilità dei passi e QP vincolato noto; controllo manuale della geometria.

## F21 — Chapter 2: KKT, moltiplicatori e QP di uguaglianza

- **Obiettivo:** produrre C2.L6 mostrando separatamente tutte le condizioni KKT e la curvatura tangente.
- **Prerequisiti:** gradienti dei vincoli, regioni/proiezioni e piccoli sistemi lineari.
- **Dipendenze:** F20.
- **File/cartelle:** creare `A/Runtime/Core/Optimization/KktDiagnostics.cs`, Lagrangiana/gradienti e QP di uguaglianza a piccola dimensione, visualizzatore di somma pesata/normali/tangenti; aggiungere `A/Data/Lessons/Chapter02/C2_L6.asset` e formule/preset analitici.
- **Sistemi Unity:** slider dei moltiplicatori, contorni/bordi/vettori, indicatori distinti di primal/dual/stationarity/complementarità.
- **Risultato verificabile:** nel semispazio lo studente bilancia i gradienti; passando il target nell'interno il moltiplicatore si annulla; un QP mostra il sistema a blocchi. Fonte O pp. 56–68.
- **Criteri di completamento:** h=0, g≤0 e μ≥0 distinti; attivo non implica μ>0; regolarità verificata sui preset; QP usa Ay=0 e curvatura ridotta appropriata; KKT non certificate come minimo generale.
- **Test:** esempio `g=z₁−c` per target interno/esterno/sul bordo, μ corretto e complementarità; residui del QP e rank dei gradienti; controllo manuale della curvatura lungo la tangente.

## F22 — Chapter 2: Newton vincolato, SQP e merito

- **Obiettivo:** produrre C2.L7 con un QP locale e line search sulla funzione di merito.
- **Prerequisiti:** Newton, KKT, solver piccolo e uguaglianze regolari; esempi con QP locale ben posto.
- **Dipendenze:** F19, F21.
- **File/cartelle:** creare `A/Runtime/Core/Optimization/EqualityNewton.cs`, `EqualitySqp.cs`, `L1MeritFunction.cs`, relativa line search e diagnostiche; aggiungere vista del vincolo linearizzato, `A/Data/Lessons/Chapter02/C2_L7.asset`, formule e preset.
- **Sistemi Unity:** frontiera/tangente, superficie locale, grafici ℓ/‖h‖/M₁, tentativi di passo e pannello di sistema a blocchi.
- **Risultato verificabile:** ogni iterazione mostra incremento, moltiplicatore del QP, violazione e merito; cambiare s modifica l'accettazione del passo. Fonte O pp. 70–77.
- **Criteri di completamento:** M₁ usa h; λ_QP distinto da Δλ Newton; update smorzato coerente; curvatura del QP dichiarata; ipotesi di esattezza del merito non omesse; nessun solver generale di grandi problemi richiesto.
- **Test:** residuo del sistema a blocchi, vincolo linearizzato, equivalenza Newton/QP sul preset, update λ e Armijo sul merito; prova manuale di progresso con costo e ammissibilità non coincidenti.

## F23 — Chapter 2: barriere e chiusura del capitolo

- **Obiettivo:** produrre C2.L8 e verificare la copertura delle otto lezioni di ottimizzazione.
- **Prerequisiti:** obiettivi/constraint/derivate, gestione delle iterazioni e solver di uguaglianza; preset con interno strettamente ammissibile.
- **Dipendenze:** F22.
- **File/cartelle:** creare `A/Runtime/Core/Optimization/LogBarrierObjective.cs`, strategia ε, diagnostiche del dominio e visualizzazione dei costi originale/barriera; aggiungere `A/Data/Lessons/Chapter02/C2_L8.asset`, formule e preset; aggiornare il catalogo Chapter 2.
- **Sistemi Unity:** superficie barriera, timeline delle iterazioni/ε, contorni/frontiera e confronto con soluzione nota.
- **Risultato verificabile:** riducendo ε il punto si avvicina alla soluzione vincolata; attraversare il bordo produce una diagnostica di dominio. Fonti O pp. 54 e 78.
- **Criteri di completamento:** log definito solo per g<0, con normalizzazione dichiarata se i residui hanno unità; uguaglianze conservate; `μ_jg_j=−ε` distinto da zero; tutte C2.L1–L8 hanno esperimento/fonti/verifica.
- **Test:** valore/gradiente barriera, dominio e soluzione scalare nota per diversi ε; complementarità perturbata; validazione degli asset e percorso manuale del capitolo.

## F24 — Chapter 3: curve, manifold e spazio tangente

- **Obiettivo:** produrre C3.L1 separando esplicitamente curve arbitrarie e traiettorie valide.
- **Prerequisiti:** linearizzazione/Jacobiani e diagnostiche dei vincoli; un esempio scalare a un passo verificabile.
- **Dipendenze:** F12, F23.
- **File/cartelle:** creare `A/Runtime/Simulation/StateInputCurve.cs`, valutazione dei residui dinamici di curve, `A/Runtime/Visualization/TangentSpaceVisualizer.cs`; aggiungere `A/Data/Chapters/Chapter03.asset`, `A/Data/Lessons/Chapter03/C3_L1.asset`, formule/preset di manifold e test.
- **Sistemi Unity:** catena temporale, curva e tangente nello spazio `(u₀,x₁)`, controlli per modificare uno stato e per eseguire rollout.
- **Risultato verificabile:** trascinare uno stato viola h; il rollout separato ripristina la dinamica; un passo tangente finito può lasciare il manifold nonlineare. Fonte K pp. 2–7.
- **Criteri di completamento:** nessuna sessione spaccia una curva editata per rollout; alto numero di dimensioni trattato come sezione/proiezione dichiarata; Δx₀=0 nel caso fissato; μ della curva distinto dal moltiplicatore.
- **Test:** h su curva e rollout, null-space/tangente del preset, errore del passo tangente nonlineare; controllo manuale di assi/etichette e modalità.

## F25 — Chapter 3: problema compatto e Lagrangiana temporale

- **Obiettivo:** produrre C3.L2 facendo vedere da dove provengono i termini della Lagrangiana e la sparsità.
- **Prerequisiti:** curve/rollout, costi e Lagrangiana/KKT generici già verificati.
- **Dipendenze:** F21, F24.
- **File/cartelle:** creare `A/Runtime/Core/OptimalControl/TrajectoryLagrangian.cs`, descrittori dei blocchi e `A/Runtime/Visualization/TimestepVisualizer.cs`/binding del grafo temporale; aggiungere `A/Data/Lessons/Chapter03/C3_L2.asset` e formule compatte/decomposte.
- **Sistemi Unity:** nodi x/u/costo, archi h e λ, pannello dei blocchi del Jacobiano e selezione dei termini.
- **Risultato verificabile:** selezionare x_t mostra i termini entranti/uscenti e −λ_tᵀx_t; terminale e iniziale hanno strutture diverse. Fonti K pp. 8–15.
- **Criteri di completamento:** x impilato esclude x₀ fissato; h=f−x_next; λ_(t+1) associata alla transizione t; nessuna stationarity su x₀ fissato; «unconstrained» spiegato nel senso del corso.
- **Test:** equivalenza L compatta/decomposta, dimensioni e dipendenze di uno stato intermedio/terminale; PlayMode mirato di selezione→nodi; revisione manuale dei segni.

## F26 — Chapter 3: adjoint e stationarity degli input

- **Obiettivo:** produrre C3.L3 con forward/backward autentici e gradiente ridotto rispetto agli input.
- **Prerequisiti:** Lagrangiana, derivate di dinamica/costo e algoritmi di discesa; preset lineare/quadratico con soluzione nota.
- **Dipendenze:** F12, F18, F25.
- **File/cartelle:** creare `A/Runtime/Core/OptimalControl/AdjointEvaluator.cs`, gradiente ridotto e provider del preset lineare, `A/Runtime/Visualization/AdjointVisualizer.cs`; aggiungere binding/metriche `(k,t)`, `A/Data/Lessons/Chapter03/C3_L3.asset`, formule e test sensitività.
- **Sistemi Unity:** layer forward/adjoint/stationarity, due selettori separati k/t, frecce/costate e grafico del costo.
- **Risultato verificabile:** λ parte dal terminale e propaga backward; una traiettoria valida arbitraria mostra ρ_u non nullo; aggiornando gli input cambia il costo.
- **Criteri di completamento:** `λ_T=∇ℓ_T`, ricorrenza e segno coerenti con h; ρ_u=∇₂ℓ+Bᵀλ_next; costate dichiarate sensitività, non forze; timeline backward non è semplice replay invertito. Fonti K pp. 14–15 e 17.
- **Test:** gradiente ridotto contro differenze finite del costo rispetto a ogni input su piccolo T; terminale e caso T=1; soluzione lineare/quadratica nota e distinzione t/k.

## F27 — Chapter 3: vincoli di percorso e di bordo

- **Obiettivo:** produrre C3.L4 estendendo le KKT temporali a limiti e condizioni agli estremi.
- **Prerequisiti:** adjoint, KKT/QP, SQP/barriere e capability di gradiente dei vincoli; riferimento lineare/quadratico a piccolo orizzonte verificabile.
- **Dipendenze:** F21, F22, F23, F26.
- **File/cartelle:** creare `A/Runtime/Core/OptimalControl/ConstrainedKktEvaluator.cs`, provider di bordo/percorso e soluzione di riferimento, estensioni della sessione per iniziale fissato/libero; aggiungere `A/Data/Lessons/Chapter03/C3_L4.asset`, formule/preset e test KKT temporali.
- **Sistemi Unity:** limiti sui nodi/input, active set per timestep, λ_d/λ_b/μ_t e pannelli dei quattro residui KKT.
- **Risultato verificabile:** stringere un limite cambia l'insieme attivo e i termini di stationarity; selezionare il bordo mostra le condizioni terminali/iniziali. Fonti K pp. 18–20 e O pp. 57–65.
- **Criteri di completamento:** x₀ modificabile come parametro distinto da x₀ libero come decisione; g indicizzato fino a T−1 salvo definizione esplicita; bordo separato e accoppiato correttamente distinti; uguaglianze/μ≥0/complementarità tutte verificate. Il riferimento non è un tracciato disegnato manualmente.
- **Test:** piccolo QP con limiti e soluzione analitica o active set finito verificato; condizioni di bordo, gradiente dei vincoli, regolarità e quattro residui; confronto barriera a ε>0 distinto dalla soluzione KKT esatta.

## F28 — Consolidamento e traiettorie ottimizzate del Chapter 1

- **Obiettivo:** completare le applicazioni rinviate e collegare gli algoritmi verificati agli esperimenti del Chapter 1.
- **Prerequisiti:** tutti i capitoli disponibili e gradiente ridotto verificato; problemi/preset e criteri di convergenza espliciti.
- **Dipendenze:** F14, F15, F26, F27.
- **File/cartelle:** comporre provider degli algoritmi esistenti per il controllo, estendere le derivate dei modelli applicativi dove necessarie, aggiornare `A/Data/Lessons/Chapter01/C1_L4.asset`, `C1_L5.asset`, `C1_L6.asset`, relativi preset/formule/passaggi e validazione di copertura; nessun nuovo CourseManager o renderer duplicato.
- **Sistemi Unity:** confronto desired/actual/candidate/soluzione verificata, selezione k/t, timeline/costi/vincoli e scenari pendolo/drone comuni.
- **Risultato verificabile:** C1.L4 e il trasferimento del pendolo possono confrontare input manuali e traiettorie ottenute con un metodo numerico; il drone riusa gli stessi strumenti. Le 18 lezioni hanno la loro esperienza finale.
- **Criteri di completamento:** capacità non disponibili non finte; convergenza/residui/ipotesi visibili; soluzione nonlineare identificata come candidato o risultato locale verificato; continuo indicato come approssimazione discretizzata; tutti i rinvii di F13/F15 chiusi e fonti/rettifiche rispettate.
- **Test:** gradienti dei modelli applicativi contro differenze finite e costi dei rollout; regressioni mirate ai wiring modificati; matrice di copertura delle 18 lezioni e prova manuale dei tre capitoli.

## F29 — Polish di leggibilità, accessibilità e performance

- **Obiettivo:** rendere fruibile l'esperienza completa sul dispositivo di destinazione senza indebolire il significato matematico.
- **Prerequisiti:** contenuti consolidati; target desktop iniziale e risoluzioni/budget prestazionali concordati in base alle misure.
- **Dipendenze:** F10, F28.
- **File/cartelle:** modificare `A/UI/` layout/stili/font, materiali/stili semantici, camera/picking/label e renderer/campionatori che il profiling individua come critici; aggiornare configurazioni di qualità pertinenti, senza upgrade automatici dei pacchetti.
- **Sistemi Unity:** UI Toolkit/focus, Input System, URP, Profiler, mesh/linee/cache; pannelli collassabili e gestione dell'area viewport.
- **Risultato verificabile:** formule/assi/unità e selezione restano leggibili, keyboard navigation funziona e il frame time rientra nel budget misurato sul target.
- **Criteri di completamento:** significato leggibile senza solo colore; scale dei vettori esplicite; nessuna perdita di precisione del Core per ottimizzare il rendering; superfici rigenerate ai cambi dei parametri, non inutilmente ogni frame; mobile/asset realistici solo se scelti come target.
- **Test:** controllo manuale di risoluzioni, pannelli, contrasto, simboli, focus e camera; profilo degli esperimenti più costosi; rieseguire test numerici solo per modifiche che toccano il calcolo o i dati.

## F30 — Build, verifica finale ed estensione del corso

- **Obiettivo:** consegnare una build riproducibile e dimostrare l'aggiunta di un capitolo tramite la pipeline documentata.
- **Prerequisiti:** polish verificato, 18 lezioni e salvataggi stabili; scene/riferimenti validi nel build.
- **Dipendenze:** F09, F29.
- **File/cartelle:** aggiornare Build Settings e configurazione di build, strumenti editor di validazione/authoring già presenti; generare build e rapporto di verifica; usare fixture di capitolo aggiuntivo nei test senza inventare contenuti di corso da PDF assenti.
- **Sistemi Unity:** build player, scene additive, serializzazione, UI/input runtime e visualizzazioni URP sul dispositivo finale.
- **Risultato verificabile:** eseguibile con percorso Course→Chapter→Lesson, progressi persistenti e fonti leggibili come metadati; capitolo di test aggiunto attraverso asset/registro senza cambiare CourseManager.
- **Criteri di completamento:** tutti i criteri di fase soddisfatti; nessun rinvio obbligatorio residuo; 18 lezioni tracciabili alle fonti; pipeline nuovo PDF→mappa→capitolo→lezioni→formule→esperimenti riproducibile; fixture di test esclusi dal catalogo di produzione.
- **Test:** suite EditMode/PlayMode necessaria all'intero prodotto e validatore dei dati; smoke test della build da avvio a chiusura, cambio lezione e riavvio; verifica manuale di fonte/notazione e prova di estensione isolata.

## Prossimo passo e limite di questa consegna

**F00–F11 sono completate; il prossimo intervento pianificato è F12, non avviato in questa consegna.** Il percorso fino a F08 valida la pipeline completa prima di produrre il corso; F09 consolida validazione, lifecycle e progresso; F10 verifica authoring formule e riuso tramite un vero esperimento statico; F11 avvia la produzione con C1.L1; F12–F27 proseguono i capitoli nell'ordine richiesto; F28 chiude le dipendenze applicative; F29–F30 completano polish e consegna.

La consegna corrente comprende esclusivamente F10, documentata in [F10_IMPLEMENTATION.md](REVIEWS/F10_IMPLEMENTATION.md). Preserva A01–A04 e AF08-01…07 e aggiunge il contratto comune, l'obiettivo statico di test e il layout matematico minimo, senza completare contenuti di produzione. Asset editoriali/scene/prefab precedenti, PDF, specifiche/review precedenti, URP, ProjectSettings e pacchetti invariati. Il catalogo di fixture è separato dal corso predefinito; nessun esperimento statico usa una timeline fittizia. Il lavoro termina con F10.
