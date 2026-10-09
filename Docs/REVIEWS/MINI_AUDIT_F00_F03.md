# Mini-audit strutturale F00–F03

Data: 9 ottobre 2026. Ambito: implementazione attuale, rispetto alla roadmap operativa. **Verdetto: FIX BEFORE CONTINUING.**

Risultato: **0 CRITICAL, 1 MAJOR, 3 MINOR**. La separazione dei livelli è solida. Prima di consolidare F04 conviene correggere il contratto editoriale che identifica ogni dinamica discreta con una discretizzazione in secondi. Gli altri rilievi riguardano confini da stabilizzare nelle fasi indicate; non richiedono un refactor generale.

## Perimetro e metodo

Letti integralmente [visione](../OPTCON_PROJECT_VISION.md), [curriculum](../OPTCON_CURRICULUM.md), [architettura](../OPTCON_ARCHITECTURE.md) e [piano operativo](../OPTCON_IMPLEMENTATION_PLAN.md). Esaminati i sorgenti, gli assembly, i test, gli otto asset editoriali, le tre scene applicative e gli asset UI di `Assets/OptimalControl`, insieme ai Build Settings. Nel seguito **A = `Assets/OptimalControl`**; i numeri di riga si riferiscono ai file attuali.

Audit statico, senza avviare Unity, compilare, eseguire test o modificare file preesistenti. È stato creato soltanto questo rapporto. Le conseguenze future indicate sono rischi dedotti dai contratti attuali, distinti dai comportamenti già presenti.

Ricontrollate in lettura le evidenze locali della consegna F03:

- `EditMode-results.xml`: **66/66 passati**, inclusi 39 casi matematici F03 e 27 di catalogo F01.
- `PlayMode-results.xml`: **9/9 passati**, navigazione F02.
- `smoke-results.txt`: tre cicli nativi menu/workspace/menu riusciti, incluso un ciclo senza domain reload.
- `diagnostics-results.txt` e `diagnostics-preview.png`: tabella del pendolo e del caso scalare, terminali N/A, unità e indici coerenti; cattura riesaminata visivamente.
- `verified-source-manifest.json`: **146 file verificati, zero differenze SHA-256** rispetto al workspace attuale.

Questi sono risultati **precedenti**, non una nuova certificazione della compilazione o del comportamento runtime. Evidenze: `C:/Users/querz/AppData/Local/Temp/OptCon-F03-e4b0e4bd9b7346a9843c686979b627fc/`.

La roadmap assegna a **F03 il core matematico/simulativo**, a **F05 le primitive 3D**, a F06 Formula/HUD e a F08 Lesson Logic. La loro assenza attuale non è una regressione. Il provider concreto e la sessione runtime sono previsti in F04; `model` e `scenarioPrefab` ancora non assegnati nel prototipo sono dichiarati nel piano.

## Classificazione

| Gravità | Significato nell'audit |
| --- | --- |
| CRITICAL | Correttezza o integrità fondamentali già compromesse; blocco immediato. |
| MAJOR | Contratto strutturale incoerente che rischia di propagarsi nei livelli successivi; correggere prima di consolidarne i consumer. |
| MINOR | Disallineamento circoscritto o rischio d'integrazione ancora economico da risolvere nella fase indicata. |

| ID | Gravità | Problema | Quando intervenire |
| --- | --- | --- | --- |
| A01 | **MAJOR** | Il catalogo rende obbligatorio il tempo fisico anche per dinamiche discrete che non lo hanno | Prima di consolidare provider/sessione/clock di F04 |
| A02 | **MINOR** | Tipo del binding di `u_t` non allineato alla metrica prodotta; API metriche ancora limitata ai vettori | Prima del resolver/HUD di F06; confine estendibile prima dei costi F07 |
| A03 | **MINOR** | Fine transizione pubblicata prima del collegamento dei consumer della scena | Durante il wiring del lifecycle F04 |
| A04 | **MINOR** | La storia conserva i risultati ma non la configurazione matematica della revisione | Alla definizione del risultato di sessione in F04, prima di F06/F07 |

## A01 — MAJOR — Dinamica discreta e tempo fisico confluiscono nello stesso contratto editoriale

**File/classi coinvolti:** `A/Runtime/Content/Definitions/ExperimentDefinition.cs:20` e `:42` (`UsesDiscreteTime`, `DiscreteTimePreset`); `A/Editor/CatalogValidator.cs:277`; `A/Runtime/Core/Contracts/ModelDescriptor.cs:15`; `A/Tests/EditMode/CatalogValidatorTests.cs:244`; `A/Tests/EditMode/Mathematics/RolloutEngineTests.cs:35`.

**Evidenza e perché è un problema:** il Core consente `DiscretizationStepSeconds = null`. Il test scalare `x_(t+1)=x_t+u_t`, `2+3=5`, verifica esplicitamente che il tempo fisico sia assente. Nel catalogo, invece, ogni esperimento con `UsesDiscreteTime=true` deve avere `StepSeconds>0`, espresso in secondi. Il preset non distingue «transizioni indicizzate da t» da «discretizzazione di una dinamica fisica». Il test editoriale denominato `AnExperimentWithoutPhysicalTimeDoesNotNeedAnArtificialTimeline` disabilita completamente il tempo discreto e azzera le dimensioni: copre un esperimento statico, non questo caso dinamico.

**Conseguenze se non corretto:** un nuovo modello discreto senza durata fisica dovrebbe inventare δ/τ oppure dichiararsi non discreto, perdendo la normale validazione di orizzonte e vettori. Trasferire questo presupposto in sessione, playback, proiezioni e HUD renderebbe la correzione trasversale. È in contrasto con la distinzione tra indice e tempo fisico nella visione e con il tempo opzionale già implementato dal Core. Il pendolo attuale resta corretto: per esso δ è necessario.

**Soluzione consigliata:** separare nel preset la presenza delle transizioni dalla presenza del passo fisico, con un indicatore serializzabile esplicito. Convertire quest'ultimo in un valore opzionale puro; validare positività/finitezza di δ quando presente e mantenere sempre i controlli su T, x₀ e input delle dinamiche discrete. Il provider pendolo richiederà δ, altri provider potranno non richiederlo. Preservare il significato del preset pendolo già salvato. La cadenza di playback deve avere una configurazione distinta dal passo matematico. Non serve anticipare algoritmi o sessioni statiche di F10.

**Quando conviene correggerlo:** prima di consolidare le API di F04. Oggi coinvolge pochi dati/contratti; dopo F05–F08 coinvolgerebbe anche tutti i consumer temporali.

**Verifica consigliata:** caso editoriale con dinamica discreta, T=1, unità adimensionali, x₀=2, u₀=3 e passo fisico assente: catalogo valido, rollout x₁=5, δ/τ assenti. Conservare il caso pendolo con δ=0.02 e il rifiuto del suo passo mancante/non valido. Sono verifiche proposte, non eseguite durante l'audit.

## A02 — MINOR — Contratto delle metriche da allineare prima della formula live

**File/classi coinvolti:** `A/Data/Formulas/PrototypeDiscreteDynamics.asset:42`; `A/Runtime/Content/Bindings/FormulaBindings.cs`; `A/Runtime/Simulation/SimulationMetrics.cs:15`; `A/Runtime/Simulation/SimulationSnapshot.cs:22`; `A/Runtime/Simulation/TrajectoryHistory.cs:28`; `A/Editor/CatalogValidator.cs:328` e seguenti.

**Evidenza e perché è un problema:** il termine `u_t` dichiara `kind: 0` (**Scalar**) e `component: -1` (**valore intero**), mentre `input.applied` è sempre un `VectorMetric`, anche per un solo input. Non esiste ancora una regola esplicita di conversione. Il validatore controlla la forma interna della formula, ma non la compatibilità con le metriche del provider. Inoltre lo snapshot espone direttamente un dizionario di soli `VectorMetric`, costruito con tre voci fisse; costruttori di metriche e snapshot sono interni. È un'implementazione sufficiente per F03, ma non ancora un confine da fissare per tutti i consumer futuri.

**Conseguenze se non corretto:** un resolver rigoroso potrebbe rifiutare il binding corrente; uno permissivo potrebbe introdurre conversioni implicite diverse tra HUD, formule e Lesson Logic. Se questi sistemi assumessero che ogni metrica sia un vettore, costi scalari e Jacobiani/matrici imporrebbero modifiche diffuse oppure archivi paralleli di valori. Il rischio riguarda il consolidamento dell'API; non significa che F03 dovesse già implementare costi e matrici.

**Soluzione consigliata:** definire una sola regola di tipizzazione e selezione delle componenti. Allineare il termine alla metrica vettoriale oppure dichiarare esplicitamente la componente scalare selezionata. Prima di vincolare i presenter a `VectorMetric`, introdurre un accesso alle metriche che esponga tipo, forma, unità, validità e campione risolto, estendibile senza ramificazioni per capitolo. Aggiungere le forme concrete quando richieste dalle fasi, senza trasformare gli scalari in vettori fittizi. Conservare `SampleIndex` già risolto, specialmente per `state.next`.

**Quando conviene correggerlo:** all'inizio di F06, prima di implementare i binding; stabilire il punto d'estensione prima dei costi di F07, senza attendere la produzione dei capitoli.

**Verifica consigliata:** binding `u_t` a un input e a un vettore di più input, componente fuori range, tipo incompatibile, terminale e `state.next` senza secondo offset. Il caso incompatibile deve avere un diagnostico esplicito, non una conversione ad hoc nella UI.

## A03 — MINOR — Fine navigazione e scena collegata sono due momenti non protetti

**File/classi coinvolti:** `A/Runtime/Application/LessonLoader.cs:94` (`Finish`); `A/Runtime/Course/CourseManager.cs:56` (`Navigate`); `A/Runtime/Application/AppBootstrap.cs:50` (`BindScene`); `A/Runtime/Content/Navigation/ICourseNavigation.cs`.

**Evidenza e perché è un problema:** `Finish` imposta `IsLoading=false`, invoca il callback che aggiorna `ActiveLesson` e pubblica `Changed`, e soltanto dopo emette `SceneReady`, usato dal bootstrap per collegare i presenter. Un observer di `Changed` vede quindi la navigazione conclusa prima del wiring. Può anche chiedere una nuova navigazione sincrona mentre la notifica `SceneReady` precedente deve ancora essere emessa. L'evento contiene soltanto la scena, non l'identità della transizione. L'ordine è verificabile nel codice; non è stato indotto un guasto runtime durante questo audit.

**Conseguenze se non corretto:** aggiungendo host, sessione, HUD e Lesson Logic si rischiano dipendenze dall'ordine delle subscription o attese di un frame sparse nei consumer. Una lezione potrebbe risultare pronta mentre la sessione è ancora assente. Il riuso dello stesso workspace per due lezioni rende particolarmente importante distinguere identità della lezione e identità della scena. Oggi il presenter editoriale limita l'impatto.

**Soluzione consigliata:** definire in Application l'ordine esplicito di commit dell'identità, composizione della sessione e pubblicazione dello stato pronto; proteggere anche la fase di dispatch da richieste rientranti. Se «scena caricata» e «esperimento pronto» restano stati distinti, esporli chiaramente. Associare le notifiche alla destinazione della transizione, senza affidare l'identità a una lettura tardiva dello stato globale. Evitare un event bus generale o workaround nei singoli presenter.

**Quando conviene correggerlo:** durante F04, prima di collegare più consumer al lifecycle.

**Verifica consigliata:** ordine delle notifiche e disponibilità della sessione; richiesta di navigazione da un observer; cambio tra due lezioni nello stesso workspace; chiusura/riapertura senza sessioni o subscription residue. I test F02 attuali coprono concorrenza durante il caricamento, ma non la rientranza durante le notifiche finali.

## A04 — MINOR — Configurazione e risultati devono essere conservati insieme per revisione

**File/classi coinvolti:** `A/Runtime/Core/Contracts/ModelDescriptor.cs`; `A/Runtime/Core/Models/PendulumDynamics.cs:12`; `A/Runtime/Simulation/RolloutEngine.cs:51`; `A/Runtime/Simulation/TrajectoryHistory.cs:10`; `A/Runtime/Simulation/SimulationSnapshot.cs:11`.

**Evidenza e perché è un problema:** la storia conserva ID/versione del modello, dimensioni/unità, δ, stati e input, ma non massa/lunghezza/gravità. Questi parametri appartengono correttamente al modello immutabile, che non viene trattenuto dalla storia. Due pendoli con parametri diversi hanno lo stesso ID/versione del descrittore e possono avere lo stesso δ. La storia, da sola, non permette di ricostruire quale configurazione abbia prodotto i campioni. La sessione prevista in F04 è il punto appropriato per chiudere questo contratto; non è un errore nelle transizioni F03.

**Conseguenze se non corretto:** formula `f_t`, diagnostiche e confronto A/B potrebbero leggere i parametri correnti o i default dell'asset mentre mostrano risultati di una revisione precedente. I singoli consumer potrebbero iniziare a conservare copie indipendenti della configurazione o a conoscere `PendulumParameters`, compromettendo riuso e coerenza.

**Soluzione consigliata:** associare a ogni risultato pubblicato una configurazione pura e immutabile della stessa revisione, conservata con la storia dalla sessione. Può essere un contenitore di risultato, senza aggiungere dati editoriali Unity o un dizionario arbitrario al Core. Formula e confronto devono usare questa configurazione, non rileggere gli asset o lo stato mutabile della sessione. La configurazione corrente modificabile e quella congelata di un risultato hanno lifetime diversi, con una sola provenienza autorevole.

**Quando conviene correggerlo:** nella progettazione della sessione di F04, prima del binding del descrittore in F06 e del confronto in F07. È coerente con quanto già previsto dall'architettura per sessioni e confronti.

**Verifica consigliata:** conservare un risultato A, cambiare un parametro e produrre B; parametri e risultati di A devono restare associati. Se B fallisce, l'ultimo risultato valido deve conservare anche la propria configurazione, non soltanto i campioni.

## Esito dei dieci controlli richiesti

| Controllo | Esito |
| --- | --- |
| 1. Separazione dei livelli | Core e Simulation sono puri e senza riferimenti Unity; Content contiene authoring; UI contiene presentazione; Application compone navigazione e scene. Lesson Logic eseguibile ancora prevista in F08. |
| 2. Duplicazione di stato/matematica | Una sola implementazione produttiva delle equazioni del pendolo. Snapshot e metriche referenziano gli stessi campioni immutabili della storia. Preset editoriali e copie runtime non sono una seconda simulazione. Attenzione alla configurazione per revisione: A04. |
| 3. Hardcoding dei capitoli/lezioni | Nessuno switch sul capitolo, conteggio fisso di lezioni o ID del prototipo nella navigazione produttiva. Dimensioni/unità del pendolo nel suo modello e nel suo asset sono legittime; il menu diagnostico è esplicitamente dedicato al pendolo. Presupposto generico troppo restrittivo sul tempo: A01. |
| 4. Nuovi PDF/capitoli | Catalogo ordinato tramite asset/ID; fonti registrate con percorso/hash/pagine, sottocartelle Docs ammesse. I test includono un nuovo capitolo/fonte e una seconda voce navigabile. L'aggiunta di nuovi modelli ha i confini da chiarire in A01/A02/A04. |
| 5. Responsabilità delle classi | Nessuno script unisce modello, renderer e interazione. RolloutEngine calcola; TrajectoryHistory conserva; Snapshot espone; CourseManager risolve identità; Loader gestisce scene; Presenter mostra dati. Il presenter comune di menu/anteprima è una scelta limitata alla F02, da non trasformare nell'host del futuro esperimento. |
| 6. Dipendenze improprie | DAG attuale: Core → nessuno; Simulation → Core; Content → Core; Presentation → Content; Application → Content/Presentation. Editor e test separati. L'asmref di CourseManager lo colloca correttamente in Application. Nessun ciclo rilevato. |
| 7. Coerenza architetturale | Precisione double, snapshot immutabili, provider distinti dall'authoring e ID stabili rispettati. A01 è un disallineamento tra authoring e Core; A02–A04 sono confini da stabilizzare prima dei nuovi consumer. |
| 8. Regressioni/deviazioni F00–F03 | Nessuna regressione funzionale identificata dalla lettura e dalle evidenze precedenti, con i limiti di un audit statico. F02 sostituisce l'ingresso diretto F00 con il menu come pianificato; F03 mantiene invariati scene/navigazione/catalogo. Diagnostica editor F03 documentata, senza anticipare renderer runtime. |
| 9. Semantica delle visualizzazioni | La tabella legge gli snapshot, distingue t/τ, conserva unità per componente e non inventa input/next al terminale. Casi noti coerenti. Non ci sono ancora traiettorie, frecce di coppia, regioni o superfici matematiche 3D da certificare; questa verifica resta a F05 e alle fasi pertinenti. |
| 10. Rischio di grosso refactor | I punti da non propagare sono tempo fisico obbligatorio, assunzione «ogni metrica è un vettore», readiness basata soltanto sulla scena e configurazione separata dalla revisione. Non emerge una necessità di riscrivere Core, catalogo o navigazione. |

## Decisione

**FIX BEFORE CONTINUING.** Correggere A01 prima di consolidare F04, per evitare che un presupposto editoriale contrario al Core diventi un vincolo di sessione, clock e visualizzazione. Risolvere A03/A04 nel lavoro di F04 e A02 prima di F06/F07. Sono interventi delimitati sui contratti, non un invito ad anticipare solver, matrici, renderer o Lesson Logic.

Nessuna correzione implementata, nessuno stato di fase aggiornato, nessuna nuova funzionalità introdotta.
