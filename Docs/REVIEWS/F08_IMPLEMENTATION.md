# F08 — Lesson Logic e primo vertical slice didattico

Data: 10 ottobre 2026. Ambito: esclusivamente F08, **completata e verificata**. Tutti i criteri soddisfatti; stato aggiornato nella roadmap. F09 e fasi successive non avviate.

## Prerequisiti

Letti integralmente visione, curriculum, architettura, roadmap, mini-audit F00–F03, remediation A01 e report F04/F05/F06/F07; esaminati implementazione, assembly, dati, prefab e test delle fasi completate. Specifica operativa principale: F08 della roadmap e richiesta dell'utente.

Prima delle modifiche, i **308 file applicativi** coincidevano per SHA-256 con la consegna F07 verificata. Baseline separata di **388 file** in Assets/Docs/Packages/ProjectSettings, per distinguere questa attività dalle modifiche F07 già presenti. F07 risultava completata: 180 EditMode, 38 PlayMode, prove native e manifest disponibili. Navigazione, sessione, dinamica, primitive, formule, costi e confronto erano presenti: tutti i prerequisiti F08 soddisfatti. Nessuna regressione concreta richiedeva di ridisegnare A01–A04.

## Architettura

```text
LessonDefinition / LessonStepDefinition
    → LessonPlan immutabile (metadati, comandi, condizioni)
    → LessonSession + LessonConditionRegistry [Application]
         → LessonPresentationState / ILessonRuntime [Content]
         → comandi consentiti → SimulationSession F04/F07
         ← SessionState / SimulationResult immutabile
                ↓
         LessonHudPresenter / LessonStepPresenter
           → FormulaPresenter F06 → resolver F06 → risultato
           → HighlightRegistry F05
           → Timeline / ParameterPanel / CostPanel esistenti
         ExperimentVisualization → LayerController F05
```

Core e Simulation **non sono modificati**. Nessuna nuova dipendenza esterna o dipendenza di matematica/simulazione da Unity, UI, renderer o Lesson Logic. `Runtime/Lessons/OptCon.Application.asmref` colloca gli orchestratori nell'assembly Application esistente; Presentation conosce soltanto l'interfaccia Content `ILessonRuntime`. Non introduce un riferimento Presentation → Application né un ciclo.

La Lesson Logic possiede indice educativo, modalità guidata/libera, risposte, feedback e fatti delle azioni dell'utente. Conserva un riferimento alla sessione; **non conserva copie di campioni, input, parametri, costi, metriche o risultati matematici**. Il test con modello contatore verifica che selezione, progressione, seek e confronto non chiamino la dinamica. I consumer continuano a leggere lo stesso `SimulationResult`.

## Modello dei passi e progressione

`LessonStepDefinition` resta incorporata nella `LessonDefinition`, con i campi preesistenti preservati. Aggiunge titolo, obiettivo, help, formula, termine selezionato, termini divulgati, layer, controlli, target dei parametri editabili, introduzione dei costi, azioni di ingresso, domanda e risposte con feedback. Non obbliga ogni lesson a utilizzare tutti i campi.

`LessonPlan` congela i metadati all'apertura: liste in sola lettura, ID della formula e comandi immutabili. Non rilegge dati editoriali durante la progressione. Verifica ID dei passi, riferimenti alle formule, termini, selezione divulgata, controlli e risposte; `LessonSession` verifica tutti i provider di condizioni, parametri e layer prima del binding/Ready, anche per i passi successivi. Una scena senza layer disponibili non accetta implicitamente layer richiesti.

Convenzioni esplicite:

- lista dei termini vuota: tutti i termini della formula disponibili;
- controlli e layer vuoti: nessun controllo opzionale e nessun layer visibile in modalità guidata;
- parametri editabili vuoti: nessun parametro modificabile in modalità guidata;
- nessuno step: esplorazione libera, senza inventare un completamento didattico;
- condizioni del passo combinate con AND; nessuna condizione: avanzamento esplicito immediatamente consentito;
- il raggiungimento delle condizioni abilita **Avanti**, senza cambiare automaticamente step;
- soltanto **Concludi verifica** nell'ultimo step completa il percorso della lesson corrente.

`LessonEntryAction` converte dati in comandi già esistenti: pause, reset, seek, input, parametro, stato iniziale, rimozione confronto. Nessuna equazione incorporata. Azioni non supportate falliscono esplicitamente. Il prototipo usa un seek a zero negli ingressi che devono formulare una previsione o introdurre il costo sul campione iniziale.

## Presentation state e controllo dell'interazione

`LessonPresentationState` espone identità della lesson, step, generazione, obiettivo, condizioni soddisfatte, disponibilità dell'avanzamento, feedback e modalità. `LessonControls` definisce un vocabolario centrale per play, step, seek, input, parametri, stato iniziale, passo fisico, playback, orizzonte, confronto, layer e formula.

La politica viene applicata su due confini:

1. **`LessonSession.Execute`** rifiuta comandi non consentiti e parametri fuori dalla whitelist del passo. Tutti i comandi dello studente di HUD e tastiera transitano qui. Pause e reset rimangono disponibili.
2. **`LessonStepPresenter`** applica la stessa politica ai controlli UI dopo il rendering dei presenter esistenti. La disabilitazione visiva non sostituisce il controllo dei comandi. I presenter preesistenti ristabiliscono prima la disponibilità intrinseca, evitando controlli rimasti bloccati dopo un cambio step.

Il router Input System conserva le azioni clonate e il focus guard F06; inoltra i comandi alla lesson e consente toggle dei layer soltanto quando previsto. I suggerimenti da tastiera F05 rimangono visibili nell'esplorazione libera. Non è introdotto un secondo sistema di input.

## Formule, highlight e disclosure

La lesson richiede un ID formula, una lista di termini visibili e una selezione iniziale. `FormulaPresenter` continua a usare **l'unico resolver F06**, con kind, shape, componenti, unità, SampleIndex e disponibilità invariati. I termini nascosti non sono selezionabili.

L'evento `TermSelected(formulaId, termId)` distingue una selezione dell'utente dal focus automatico del passo e dai render successivi. La selezione editoriale aggiorna il semantic registry ma **non soddisfa** una condizione che richiede un'azione dell'utente.

Il collegamento 3D continua a essere formula → semantic binding → `HighlightRegistry`: nessuna ricerca di renderer concreti nella Lesson Logic. Valore e highlight usano l'indice risolto una sola volta; `state.next` non diventa t+2. Il terminale viene evidenziato a T anche se il cursore si trova altrove.

`LayerController` applica maschere di ID e supporta più root per un layer. I nuovi contenitori nel prefab impediscono che il normale `Render(...).SetActive(...)` renda visibile un elemento ancora nascosto dal percorso. Contenitori con trasformazione identità: nessuna modifica a proiezioni o matematica. Layer aggiunti: `input`, `next`, `trajectory`, `target`, `cost`, `comparison`; gli indici e gli ID dei tre layer F05 originali restano invariati. Anche il toggle generico agisce su tutti i root del layer.

In modalità guidata la maschera viene mantenuta dopo il rendering; in esplorazione libera si sbloccano inizialmente tutti i layer e i toggle successivi dello studente persistono durante gli aggiornamenti della simulazione.

## Integrazione dei costi

Lo step può introdurre il pannello F07 e abilitare singoli target di parametro, senza conoscere la formula del costo. La UI continua a leggere `Result.Costs` e il resolver F06; non calcola J.

Nel prototipo il quinto passo permette soltanto `cost.q.0`. La variazione del peso cambia J, conservando i campioni e senza rivalutare la dinamica. Nel sesto passo si salva A e si cambia l'input di B: i due risultati mantengono configurazione, storia e costi propri. Il fallimento di una nuova modifica conserva il risultato valido e la sua configurazione, anche in presenza di A.

Al terminale rimangono x_T, C_T, costo terminale e J. Non vengono creati u_T, uno stage a T o x_(T+1). δ e τ continuano a essere opzionali: il pendolo conserva δ=0.02 s e τ_T=2 s con T=100; il caso index-only conserva t senza secondi.

## Condizioni di completamento

`LessonConditionRegistry` registra provider, senza switch su lesson, capitoli, scene o formule:

| Provider | Condizione |
| --- | --- |
| `action` | Fatto prodotto da un comando riuscito: input realmente cambiato, parametro realmente cambiato, avanzamento manuale, confronto salvato |
| `term` | Selezione esplicita del termine della formula attiva e divulgata |
| `answer` | Risposta dichiarata uguale all'ID atteso, con feedback editoriale |
| `terminal` | Campione attualmente terminale nella sessione reale; tornando indietro non rimane soddisfatta |
| `comparison.changed` | A salvato nello step, input modificato e risultati A/B distinti |

Input/parametro invariati o comando fallito non danno credito. I fatti sono locali allo step; le condizioni live vengono rivalutate sui dati reali. La registry permette di registrare nuovi valutatori tramite API; i provider sconosciuti falliscono durante la composizione. L'eventuale aggiunta di un nuovo tipo di provider richiede la sua registrazione nel punto di composizione/validazione; aggiungere lesson o capitoli che usano i provider disponibili richiede soltanto dati.

Non sono introdotti scoring, achievement, progressi su disco o completamenti delle lesson di produzione.

## Reset e lifecycle

`AppBootstrap` compone prima simulazione e LessonSession, poi host, consumer visuali, HUD e input; soltanto dopo viene pubblicato Ready dal lifecycle F04. L'identità della destinazione e le protezioni A03 restano intatte. Il dispatch educativo rifiuta comandi e avanzamenti rientranti.

- **Avanti:** pausa, nuovo step, fatti/risposta azzerati, azioni di ingresso; conserva l'esperimento corrente salvo comandi editoriali espliciti.
- **Indietro:** riapre il passo precedente dal preset, pausa, elimina A e i fatti delle attività successive; riapplica le azioni di ingresso. La scelta evita di attribuire al passo precedente azioni future.
- **Reset/R guidato o Ricomincia:** preset catturato dalla sessione, pausa, cursore zero, A rimosso, primo step e condizioni azzerati, formula/layer/highlight ripristinati. Nessuna rilettura degli asset.
- **Esplorazione libera:** sblocca gli strumenti senza attribuire completamento. Reset/R ripristina l'esperimento e rimuove A, mantenendo la modalità libera; Ricomincia torna alla guida.
- **Chiusura/cambio lesson:** unbind HUD/input/visualizzazione, dispose della lesson, poi dispose della simulazione. Riapertura crea nuove istanze; nessuna subscription o confronto precedente viene riutilizzato.

## Percorso didattico consegnato

| Step | Attività | Introduzione / controllo |
| --- | --- | --- |
| 1 — Leggi lo stato | Selezionare x_t e rispondere sul ruolo di angolo e velocità | Corpo e stato; input, tempo e confronto bloccati |
| 2 — Prevedi | Modificare u e prevedere la prima transizione Euler da velocità nulla | Input e coppia; nessun avanzamento prima della previsione |
| 3 — Confronta | Selezionare x_(t+1), eseguire Step e confrontarlo con il nuovo x_t | Ghost, dinamica, indice, tempo fisico, rewind |
| 4 — Esplora | Cambiare ancora u e avanzare osservando la traiettoria | Piano di fase, stato-tempo 3D, play/pause, seek e playback |
| 5 — Introduci il costo | Cambiare Q[0] e spiegare cosa cambia | Formula additiva, target, stage/cumulativo/terminale/J; una sola modifica di peso consentita |
| 6 — Confronta A/B | Salvare A, cambiare u in B, selezionare J | Confronto immutabile e relativi obiettivi |
| 7 — Verifica | Raggiungere T, selezionare il terminale, rispondere sull'assenza dello stage/input a T | Verifica concettuale e completamento locale del prototipo |

Il prototipo resta nel catalogo separato di F01/F02. Nessuna delle 18 lesson di produzione viene completata o popolata. Il percorso rappresenta l'intera pipeline Model → Simulation → Visualization → Formula → HUD → Lesson Logic, senza implementare il corso completo.

## Test aggiunti e adeguamenti

**26 nuovi casi EditMode** in `LessonSessionTests`: asset reale/sette step; piano congelato; sequenza e avanzamento esplicito; lesson vuota; comandi vietati; selezione utente contro focus automatico; modifica effettiva/no-op/fallimento; risposte; terminale live index-only; fatti locali; back/reset; modalità libera; nuovo provider e provider sconosciuto; A/B e candidato fallito; preflight parametri/layer; round trip nativo dell'asset; quattro configurazioni invalide; rientranza; dispose; modello contatore senza ricalcolo.

**12 nuovi PlayMode** in `LessonOrchestrationTests`: composizione prima di Ready; disclosure e comandi vietati; percorso completo attraverso UI; peso costo e revisione comune; terminale/highlight; back/reset; uscita/rientro; due lesson nello stesso workspace; tastiera reale; disable/enable HUD; toggle di tutti i root di un layer; percorso index-only con costi F07.

Le fixture F05–F07 che verificano gli strumenti liberi richiedono esplicitamente `Lesson.Explore()` all'apertura: mantengono le verifiche esistenti; i nuovi test verificano separatamente la modalità guidata. Aggiunta diagnostica di coordinate/focus alle prove reali di puntatore/tastiera. Le fixture del validatore che sostituiscono formule/esperimento azzerano gli step della copia, evitando riferimenti al grafo originale; la validazione del catalogo originale e il nuovo round trip continuano a verificare tutti i sette step reali. Nessuna asserzione matematica preesistente rimossa.

## Risultati delle verifiche

| Verifica | Esito |
| --- | --- |
| Tutti gli EditMode | **206/206 PASS**, zero falliti/saltati; `EditMode-results-5.xml` |
| Tutti i PlayMode | **50/50 PASS**, zero falliti/saltati; `PlayMode-results-5.xml` |
| Compilazione finale senza verificatori temporanei | **PASS**, Unity 6000.3.8f1, exit code 0; zero errori e zero warning C# nei log finali di compilazione, suite e prove native |
| Percorso nativo completo | PASS su Unity 6000.3.8f1; sette step attraverso controlli UI, costi, A/B, terminale, conclusione, reset, uscita e riapertura |
| Index-only nativo | PASS: x0=2, u0=3, x1=5, stage=13, terminale=25, J=38; HUD e grafico leggono lo stesso risultato; δ/τ assenti |
| Smoke F00–F02 | PASS: tre cicli Bootstrap/menu/workspace/menu; due con domain reload e uno senza; camera, URP, luce, pavimento e Volume preservati |
| A01 | Suite catalogo/core/sessione e fixture guidata: passo fisico opzionale; pendolo δ=.02 s; index-only senza δ/τ |
| A02 | Suite resolver/binding/F06 invariata e rieseguita; tipi, componenti, unità, SampleIndex e N/A; nessuna conversione UI aggiunta |
| A03 | Suite lifecycle e nuovi test: lesson/sessione/consumer disponibili prima di Ready, identità e rientranza, stesso workspace, cleanup |
| A04 | Suite sessione/costi e nuovi test: A/B/configurazioni proprie, campioni immutati, fallimento conserva l'ultimo valido; piano educativo congelato separatamente |
| Verifica visiva | Dieci Game View 1440×979 ispezionate: disclosure, controlli, ghost coincidente al primo Euler con spiegazione, traiettorie, costi, highlight terminale, completamento e reset |
| Source of truth | Modello contatore e assert di identità tra sessione/HUD/visualizzazione; nessun calcolo di dinamica/costo nella Lesson Logic |
| Identità dei sorgenti verificati | **329 file applicativi, zero differenze SHA-256 e nessun file applicativo extra** tra workspace e copia Unity verificata |

Prime iterazioni: corretti due nomi API nei nuovi test e i riferimenti incoerenti nelle fixture del catalogo. Una prima suite PlayMode ha avuto tre fallimenti di interazione puntatore/tastiera non riprodotti nel gruppo isolato né nelle successive suite complete; conservati XML/log originali e aggiunta diagnostica, senza rimuovere gli assert. La suite successiva da 49 casi e quella finale da 50 sono entrambe passate. Le prove native e gli smoke hanno prodotto zero Error/Exception/Assert.

Le verifiche usano una copia isolata del progetto, con identità dei sorgenti verificata tramite manifest: nessun verificatore temporaneo o fixture aggiunto al progetto consegnato. La prima apertura della copia è stata interrotta perché il trasferimento iniziale non era ancora terminato; l'authoring riuscito e tutte le verifiche riportate sono successivi al trasferimento completo.

## File creati

Percorsi relativi ad `Assets/OptimalControl/`:

```text
Runtime/Content/Lessons/LessonPlan.cs
Runtime/Content/Lessons/LessonPresentationState.cs
Runtime/Lessons/LessonConditionRegistry.cs
Runtime/Lessons/LessonSession.cs
Runtime/Lessons/OptCon.Application.asmref
Runtime/UI/LessonStepPresenter.cs
Tests/EditMode/Lessons/LessonSessionTests.cs
Tests/EditMode/Lessons/OptCon.Lessons.EditModeTests.asmdef
Tests/PlayMode/LessonOrchestrationTests.cs
```

Creati anche i nove `.meta` corrispondenti e i tre `.meta` delle nuove cartelle `Runtime/Content/Lessons`, `Runtime/Lessons`, `Tests/EditMode/Lessons`: **21 file applicativi nuovi**.

Documentazione: questo report e sette catture `Docs/REVIEWS/F08_STATE.png`, `F08_TRANSITION.png`, `F08_TRAJECTORY.png`, `F08_COST.png`, `F08_TERMINAL.png`, `F08_COMPLETED.png`, `F08_INDEX_TERMINAL.png`. Totale della consegna: **29 file creati**.

## File modificati

Percorsi relativi ad `Assets/OptimalControl/`:

```text
Data/Lessons/Prototype/PendulumSlice.asset
Editor/CatalogValidator.cs
Prefabs/PendulumScenario.prefab
Runtime/Application/AppBootstrap.cs
Runtime/Application/ExperimentHost.cs
Runtime/Application/LessonInputRouter.cs
Runtime/Content/Definitions/LessonStepDefinition.cs
Runtime/UI/CostPanelPresenter.cs
Runtime/UI/FormulaPresenter.cs
Runtime/UI/LessonHudPresenter.cs
Runtime/UI/TimelinePresenter.cs
Runtime/Visualization/ExperimentVisualization.cs
Runtime/Visualization/LayerController.cs
Tests/EditMode/CatalogValidatorTests.cs
Tests/PlayMode/CostPresentationTests.cs
Tests/PlayMode/FormulaHudTests.cs
Tests/PlayMode/VisualizationTests.cs
UI/LessonHud.uss
UI/LessonHud.uxml
```

Più `Docs/OPTCON_IMPLEMENTATION_PLAN.md`: **20 file modificati**, nessuno eliminato. Nessun `.meta` preesistente modificato. Core, Simulation, formule, configurazione matematica del pendolo/costo, scene, PDF, altre specifiche/review, pacchetti e ProjectSettings conservati.

## Procedura manuale in Unity

1. Aprire `Assets/OptimalControl/Scenes/Bootstrap.unity` e avviare Play. Aprire il prototipo dal menu. Verificare primo step, solo corpo, A assente, play/step/input disabilitati. Il pannello laterale è scrollabile: formula e dettagli si trovano sotto istruzioni e trasporto.
2. Scorrere alla formula, cliccare x_t, leggere angolo/velocità e rispondere «Servono angolo e velocità angolare». Tornare ad Avanti: l'evidenziazione iniziale da sola non deve sbloccarlo.
3. Nel secondo step aprire **Input e parametri**, cambiare u[0] da 0 a 1 e scegliere la risposta sulla velocità. La selezione **Modifica** mantiene i due scope F06, campione o coda. Avanzare.
4. Selezionare x_(t+1), annotare le componenti, premere Step → e confrontarle con il nuovo x_t. Al primo Euler l'angolo del ghost può coincidere con quello corrente: velocità e spiegazione mostrano la distinzione.
5. Avanzare, modificare ancora u e fare Step. Provare Play/Pausa, rewind e seek; il playback cambia soltanto la presentazione, δ rimane .02 s. Osservare piano di fase e stato-tempo, con τ come terzo asse.
6. Introdurre il costo: aprire **Costo discreto e confronto A/B → Pesi, target e orizzonte**. Modificare Q[0] da 10 a 30. Gli altri parametri sono bloccati; J cambia, gli stati della stessa sequenza no. Scegliere la risposta sul criterio e proseguire.
7. Salvare B come A, cambiare u in B, selezionare J. Aprire le configurazioni A/B e osservare risultati/obiettivi separati. Avanzare.
8. Portare t a 100 con seek. Selezionare il costo terminale: highlight @100, stage/input N/A, C_T più terminale uguale a J. Scegliere «C_T + costo terminale, senza stage né input a T» e **Concludi verifica**.
9. Verificare il messaggio limitato al prototipo. **Ricomincia** deve ripristinare step 1, preset e A vuoto. Ripetere un tratto, usare **Indietro**: passo precedente dal preset, azioni future azzerate. **Torna al corso** e riaprire: nuova sessione e primo step.
10. Provare **Esplorazione libera**, tasti A/D/Z, frecce, P/R e 1/2/3 fuori dai campi di testo. Reset rimane in modalità libera, Ricomincia torna al percorso. In modalità guidata i comandi vietati devono restare inefficaci anche da tastiera.
11. Test Runner: eseguire tutti gli EditMode e PlayMode. Il caso index-only è nelle fixture `LessonSessionTests` e `LessonOrchestrationTests`; la cattura nativa mostra anche il grafico generico di costo. Non è aggiunta una lesson fittizia al catalogo reale.

## Limiti e decisioni di perimetro

Nessuna deviazione funzionale da F08. Il modello dei passi resta incorporato, anziché creare asset separati per ogni obiettivo/azione; le condizioni usano provider registrati. La selezione semantica riusa il termine della formula, senza aggiungere un secondo sistema di highlight. La validazione aggiunta è limitata a rendere eseguibili i nuovi contratti; framework di validazione generale e progressi persistenti restano F09.

Scelte esplicite: Indietro riparte dal preset; esplorazione libera non completa la guida; reset della lesson elimina A, mentre il comando Simulation F07 mantiene il proprio comportamento originario. Titoli/risposte/help sono dati del prototipo, non stringhe specifiche nei manager generici.

Limiti F07 conservati: pesi diagonali, target costante, viewport fisso. Il pannello richiede scroll; alcune etichette 3D restano ravvicinate nel viewport fisso con tutti i layer attivi. La Lesson Logic non assume dimensioni/pesi/target o proiezioni universali e non li generalizza in questa fase. Nessun blocco architetturale nuovo individuato; non esiste una validazione automatica del contenuto pedagogico arbitrario dei testi/risposte.

Evidenze locali: `C:/Users/querz/AppData/Local/Temp/OptCon-F08-ec9d91964e83410993697f4884d3ecac/`, XML/log delle suite, log/exit code di compilazione, `lesson-runtime-results.txt`, `smoke-results.txt`, dieci catture e manifest SHA-256. I verificatori restano fuori dal progetto consegnato. Il mini-audit storico e la remediation A01 non sono modificati.
