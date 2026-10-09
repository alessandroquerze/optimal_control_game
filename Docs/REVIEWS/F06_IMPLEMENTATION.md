# F06 — Formula System e Live Mathematics HUD

Data: 9 ottobre 2026. **F06 completata e verificata. A02 risolto. F07 non iniziata.**

Specifica operativa: F06 di `OPTCON_IMPLEMENTATION_PLAN.md`. Letti integralmente visione, curriculum, architettura, roadmap, mini-audit, remediation A01 e report F04/F05; esaminati contratti, implementazione, test e asset delle fasi completate. I report storici restano invariati.

## Prerequisiti

F00–F05 risultano completate nel piano. Prima dell'implementazione sono stati confrontati i **240 file applicativi** con il manifest della verifica F05: **zero differenze SHA-256**. Le evidenze precedenti riportano 122 EditMode e 21 PlayMode passati. Sono effettivamente disponibili catalogo/formule editoriali, provider pendolo, sessione/command sink, risultato immutabile con configurazione, lifecycle con binding prima di Ready, primitive visuali e registry semantico. A02 è l'integrazione richiesta da F06; A01/A03/A04 non richiedono ridisegni.

## Architettura implementata

```text
Mathematical Model → SimulationSession
                         ↓
         SessionState { Result.Configuration, Result.History, Sample }
                    ↙                             ↘
       ExperimentVisualization              SessionMetricSource
       primitive F05 + transizione                 ↓
                                      RuntimeMetricResolver [Content]
                                                   ↓
                                      FormulaBindingResolver
                                                   ↓
                         FormulaPresenter → HUD / HighlightRegistry

TimelinePresenter / ParameterPanelPresenter → ICommandSink
AppBootstrap → binding esplicito di tutti i consumer → Ready
```

**Core e Simulation non sono modificati.** Il contratto necessario ad A02 è implementato nel layer superiore Content, dove già risiedono le definizioni dei binding. `RuntimeMetricResolver`, i tipi restituiti e il source sono C# puro, senza riferimenti a Unity, UI, renderer o asset editoriali durante il lookup. L'assembly Content contiene anche ScriptableObject, ma questi nuovi tipi non ne richiedono uno per risolvere dati runtime.

Il DAG rimane aciclico: Core senza Unity; Simulation → Core; Content → Core/Simulation; Presentation → Content/Simulation/Core; Application compone questi livelli. Il Formula system conosce il registry generico, senza cercare renderer concreti. Il resolver e il HUD non invocano `Evaluate` o `CreateDynamics`.

`LessonHudPresenter` possiede una sola subscription alla sessione e distribuisce la stessa pubblicazione ai tre presenter del pannello. `FormulaPresenter.DisplayedResult` e ogni `MetricResolution.Result` conservano il riferimento al risultato letto, senza copiare la configurazione. La visualizzazione F05 è un altro consumer dello stesso risultato. I campi richiesti servono soltanto a costruire comandi di modifica; i valori mostrati appartengono sempre al risultato valido.

## Contratto delle metriche e soluzione A02

L'unica regola di risoluzione/tipizzazione è **`RuntimeMetricResolver.Resolve`**. `FormulaBindingResolver` traduce il termine editoriale e delega, senza conversioni proprie. Anche la validazione di esecuzione usa il medesimo resolver.

| Dato esposto | Contratto |
| --- | --- |
| Identità/provenienza | `MetricId`, riferimento a `SimulationResult`, quindi configurazione e revisione esatte |
| Tipo | `FormulaValueKind`: Scalar, Vector, ModelDescriptor; Matrix resta riservato, senza valori o calcoli fittizi |
| Forma | Rows/Columns espliciti: scalare 1×1, vettore n×1, descrittore 0×0 |
| Componente | `ComponentSelection=-1` conserva il valore intero; indice ≥0 seleziona esplicitamente una componente vettoriale |
| Valore | Payload distinti `Scalar: double?`, `Vector: NumericVector`, `Model: ModelConfiguration`; nessun wrapping di scalari in vettori |
| Unità | Collezione immutabile per componente; la selezione eredita esattamente l'unità della componente scelta |
| Campione | `SampleIndex` assoluto già risolto, nullable quando non applicabile |
| Validità | Available, NotApplicable, Invalid e diagnostico esplicito |

Un vettore di dimensione 1 **resta Vector**. Per richiedere Scalar da un vettore serve una componente esplicita; dopo la selezione l'expected kind deve essere Scalar. Componente fuori range, tipo/forma/unità/scope incompatibili e metrica sconosciuta producono Invalid con diagnostico. Non si applicano conversioni diverse nei presenter.

Il vecchio binding `u_t` del prototipo ora dichiara **Scalar, component=0** invece di Scalar con valore vettoriale intero. La formula generica dichiara invece Vector per l'intero input, anche con m=1. Entrambi passano attraverso la stessa regola.

La collezione vettoriale F03 `SimulationSnapshot.Metrics` rimane disponibile per compatibilità. Il nuovo `SessionMetricSource` la adatta al contratto tipizzato e aggiunge scalari reali e descrittore. Nessun nuovo consumer Formula/HUD dipende dall'assunzione «ogni metrica è un vettore». `IRuntimeMetricSource` è il punto di estensione: un test aggiunge una vera metrica scalare tramite questo contratto, senza modificare il presenter o creare un vettore artificiale. Nuove forme richiederanno il relativo payload/formatter quando necessarie; non richiedono una nuova API di binding per ogni consumer.

**A02 è risolto strutturalmente**, senza modificare il Core o consolidare l'API vettoriale legacy come confine del Formula system.

### Metriche disponibili

| Identifier | Tipo | Provenienza / campione |
| --- | --- | --- |
| `state.current` | Vector | Stato salvato @t |
| `input.applied` | Vector | Input salvato @t; assente a T |
| `state.next` | Vector | Stato salvato @t+1; assente a T |
| `time.index` | Scalar, unità 1 | Indice discreto t, anche al terminale |
| `time.physical` | Scalar, s | `PhysicalTimeSeconds` del campione; assente senza tempo fisico |
| `time.step` | Scalar, s | Passo opzionale del descrittore del risultato |
| `dynamics.model` | ModelDescriptor | Configurazione matematica immutabile appartenente al risultato |
| `parameter.<target>` | Scalar | Parametro/unità della configurazione del risultato, non dell'asset o del tentativo corrente |

Lo scope Next identifica la metrica `state.next`, che possiede già @t+1: **non si seleziona prima uno snapshot t+1**. Un binding incompatibile fra valore e highlight è un errore esplicito. Terminal seleziona una volta lo snapshot T, consentendo ad esempio `state.current` @T; CurrentIteration non viene interpretato come un timestep.

A t=T, input e next hanno NotApplicable, payload e SampleIndex assenti. Il descrittore del modello resta consultabile come metadata, ma non ha un indice di transizione: selezionare f_t al terminale non evidenzia una transizione inesistente. Nessun u_T o x_(T+1) viene creato.

Nel pendolo δ resta **0.02 s**, τ=0.4 s a t=20 e τ=2 s a T=100. Nel fixture index-only t=0/1 esiste e δ/τ sono N/A in entrambi i campioni. Il resolver legge i tempi già forniti dalla storia, senza attribuire secondi al clock di playback. A01 rimane preservato.

## Formule e HUD

Le definizioni esistenti forniscono ID stabile, espressione/trascrizione, descrizione, ipotesi, fonti e lista ordinata dei termini. Ogni termine conserva simbolo, nome, significato, expected kind, forma/unità, selezione, scope e binding semantico opzionale. Un termine senza metrica può essere descrittivo: il resolver restituisce esplicitamente indisponibilità del valore runtime.

- `PrototypeDiscreteDynamics.asset`: ID/GUID e quattro termini iniziali preservati; u_t corretto; aggiunti t/τ/δ. È la formula iniziale del prototipo e mantiene la spiegazione di θ/ω.
- `DiscreteDynamics.asset`: formula riutilizzabile `x_(t+1)=f_t(x_t,u_t)`, con stati/input vettoriali e dimensioni/unità inferite dal provider. `rows=-1`, `columns=1`, unità editoriali vuote dichiarano esplicitamente questa inferenza. Non implica conversione a Scalar. Le forme/unità specificate negli altri asset sono invece validate esattamente.
- `PendulumEuler.asset`: specializzazione a due equazioni Euler; selezioni scalari esplicite di θ/ω/current/next, coppia, t/τ/δ, m/l/g e f_t. Fonti I p.27 e p.33, adattamento dichiarato; nessuna equazione viene eseguita dalla UI.

Le formule si scelgono tramite dati/ID; titoli uguali non confluiscono in una sola voce. Il formatter mostra valori e unità, preserva le componenti eterogenee e non valuta stringhe matematiche. La riga di f_t mostra il descrittore; selezionarlo mostra anche tutti i parametri della revisione. N/A e Invalid sono distinti; il dettaglio include la diagnostica.

Il pannello laterale UI Toolkit riutilizza il UIDocument del workspace attraverso il template `LessonHud.uxml`. Contiene trasporto play/pausa/step avanti/indietro/reset, scrub t, formula, termini selezionabili, dettaglio del termine e foldout per parametri, input e playback. Obiettivo e ipotesi sono espandibili. Il controllo diagnostico F04 rimane collegato e interrogabile dai test, ma è nascosto visivamente perché il nuovo HUD mostra gli stessi metadata.

`ParameterPanelPresenter` genera i parametri dai descrittori editoriali e gli input dalle dimensioni/unità del modello. Modifica singola/coda sono esplicite. I limiti/invarianti sono validati dalla sessione; un rifiuto conserva i valori del risultato valido e mostra l'errore. Al terminale i campi di input vengono nascosti con N/A, senza mostrare uno zero inventato. La velocità di playback è etichettata campioni/s e distinta da δ; un rate rifiutato ripristina il valore valido con diagnostico.

### Selezione e 3D

Il termine passa ruolo semantico, SampleIndex e revisione a `HighlightRegistry`. Current evidenzia corpo/marker correnti; next evidenzia ghost/marker successivi; input evidenzia la coppia; f_t evidenzia i segmenti della transizione corrente nei due grafici. `TransitionHighlightView` riceve soltanto i campioni salvati e la proiezione F05. Le linee restano guide tra campioni discreti, senza interpolazione o previsione matematica.

Il registry conserva l'API F05 e aggiunge il vincolo di revisione per la selezione Formula. I layer nascosti non vengono riattivati; il dettaglio spiega il ruolo e le rappresentazioni associate. Non sono introdotti picking generale, animazioni o Lesson Logic.

### Lifecycle, configurazione e regressioni corrette durante la verifica

`AppBootstrap.BindScene` collega direttamente il HUD alla lesson destinazione, sessione e registry prima di Ready. Release lo scollega prima della chiusura dell'host; disable/enable e rebinding rimuovono callback/subscription e aggiornano subito dalla pubblicazione corrente. Non ci sono polling del manager, frame di attesa o dipendenze dall'ordine accidentale delle subscription. A03 è preservato.

Il dettaglio di f_t e i parametri sono letti da `Result.Configuration.Model`. I test conservano A, generano B, visualizzano ancora A e inducono un successivo fallimento: valori, campioni, parametri e tempo di ciascun risultato mantengono la propria configurazione. A04 è preservato; non viene introdotto il confronto utente di F07.

La prova nativa ha individuato e fatto correggere un side effect UI: riducendo il limite dello SliderInt da T=100 a T=1 durante il rebinding, Unity poteva emettere un ChangeEvent e inviare un seek non richiesto. Ora l'aggiornamento del range è protetto e **Render non emette comandi**. Il test index-only parte appositamente dal terminale del pendolo e verifica l'identità della pubblicazione iniziale della nuova sessione.

Il router F05 aggiunge soltanto un guard del focus: l'editing di campi numerici/testuali o dropdown blocca le scorciatoie dell'esperimento; uno slider focalizzato possiede le frecce senza bloccare P/R e gli altri comandi. La regressione completa da tastiera F05 e il nuovo test di editing passano. Questo evita sia modifiche durante la digitazione sia perdita delle scorciatoie esistenti.

## Verifiche finali

Unity **6000.3.8f1**, esecuzioni native su copia isolata con sorgenti/asset identici al workspace. L'Editor dell'utente è stato preservato. La copia accede al servizio licenze locale e disabilita soltanto la propria indicizzazione Search all'avvio; l'eventuale migrazione automatica URP nella copia non viene trasferita. Nessun pacchetto aggiunto.

| Verifica | Risultato |
| --- | --- |
| Compilazione finale senza strumenti temporanei | **PASS**, exit 0; nessun errore/warning C#, missing script o eccezione nel log finale |
| Tutti gli EditMode | **150/150 PASS**, zero falliti/saltati: 60 catalogo, 26 pendolo, 13 rollout, 16 sessione, 7 proiezioni, 28 resolver/formule |
| Tutti i PlayMode | **30/30 PASS**, zero falliti/saltati: 15 navigazione/lifecycle, 6 visualizzazione F05, 9 Formula/HUD |
| A02 / casi resolver | Scalar reale, Vector n=1/2/4, selezione valida, fuori range/negativa, expected kind incompatibile, forma/unità/scope errati, metrica assente/sconosciuta, provider scalare aggiuntivo, unità congelate |
| Campioni e terminali | Current/input @t, next @t+1 senza secondo offset, Terminal @T; input/next senza valore/indice a T |
| A01 | Tutti i test legacy/serializzazione/validazione precedenti passati; pendolo δ=.02 s e dinamica index-only senza δ/τ |
| A03 | Binding HUD prima di Ready e stessa sessione della destinazione; regressioni rientranza, stessa scena, chiusura/riapertura passate |
| A04 | A/B conservati con parametri e campioni propri; fallimento conserva risultato/configurazione validi; il tempo di A non usa il passo di B |
| Live e revisione comune | Seek e modifiche input/parametri aggiornano HUD e primitive della stessa revisione; rate di playback non modifica il risultato |
| UI reale | Click tramite Mouse virtuale → Input System → Button UI Toolkit → selezione next e highlight @1; submit dei controlli, focus numerico e tastiera F05 verificati |
| Prova runtime nativa | Pendolo con input=1, t=20, next @21; formula Euler; terminale 100; fixture index-only 2+3=5 e terminale 1; ritorno/riapertura/reset; zero Error/Exception/Assert |
| Regressione nativa F00–F05 | **3/3 cicli menu/workspace/menu PASS**, due con domain reload e uno senza; root/scena/camera/presenter unici, URP/floor/light/Volume conservati |
| Verifica visiva | **Sei Game View 1440×979 ispezionate**: formula/valori, dettaglio selezionato, Euler, terminale pendolo, index-only corrente/terminale |
| Identità dei sorgenti verificati | **274 file di Assets/OptimalControl, zero differenze SHA-256** fra workspace e copia verificata |
| Integrità | Nessun `.meta` preesistente modificato; preset matematico del pendolo, Core/Simulation, altre specifiche/review, PDF, URP, SampleScene, Packages e ProjectSettings invariati |

I 28 casi EditMode sono in `FormulaResolverTests`; i nove PlayMode in `FormulaHudTests`: readiness, ruoli/campioni, seek/comandi, parametri/specializzazione/A04, terminale, index-only/rebinding a orizzonte ridotto, disable/rebind/chiusura, focus e click reale.

Le verifiche native sono automatizzate nell'Editor, seguite da ispezione manuale delle catture; non si dichiara una sessione di click manuali nell'Editor dell'utente. I tentativi iniziali hanno evidenziato un problema nello strumento temporaneo di authoring JSON, un evento submit sintetico senza target nella fixture, contrasto/layout da correggere e i due casi UI di range/focus sopra descritti. Sono stati corretti prima delle verifiche finali; nessun errore applicativo filtrato.

Evidenze locali: `C:/Users/querz/AppData/Local/Temp/OptCon-F06-cea2f2e7b0f842608fb60bb30be99819/`. File principali: `EditMode-results-final.xml`, `PlayMode-results-final.xml`, `editmode-final.log`, `playmode-final.log`, `formula-runtime-final.log`, `formula-runtime-results.txt`, `smoke-results.txt`, `regression-smoke.log`, `final-compile.log`, relativi exit code, manifest SHA-256 e inventario. Gli strumenti di verifica sono esterni al progetto e rimossi dalla copia prima della compilazione finale.

Due catture sono conservate anche nel repository documentale:

- [Formula, valori, spiegazione e highlight nel pendolo](F06_FORMULA_HUD.png).
- [Caso index-only: 2+3=5, t disponibile e δ/τ N/A](F06_INDEX_ONLY_HUD.png).

## File creati

**37 file:** i seguenti 16 file applicativi, ciascuno con il proprio `.meta` (32), due `.meta` di cartelle, questo rapporto e due PNG. Percorsi applicativi relativi ad `Assets/OptimalControl/`:

```text
Runtime/Content/Bindings/RuntimeMetricResolver.cs
Runtime/UI/FormulaBindingResolver.cs
Runtime/UI/FormulaPresenter.cs
Runtime/UI/LessonHudPresenter.cs
Runtime/UI/TimelinePresenter.cs
Runtime/UI/ParameterPanelPresenter.cs
Runtime/Visualization/TransitionHighlightView.cs
UI/LessonHud.uxml
UI/LessonHud.uss
Data/Formulas/DiscreteDynamics.asset
Data/Formulas/PendulumEuler.asset
Tests/EditMode/Formula/FormulaResolverTests.cs
Tests/EditMode/Formula/OptCon.Formula.EditModeTests.asmdef
Tests/Fixtures/IndexOnlyFixture.cs
Tests/Fixtures/OptCon.TestFixtures.asmdef
Tests/PlayMode/FormulaHudTests.cs
```

Cartelle: `Assets/OptimalControl/Tests/EditMode/Formula.meta`, `Assets/OptimalControl/Tests/Fixtures.meta`. Documentazione: `Docs/REVIEWS/F06_IMPLEMENTATION.md`, `F06_FORMULA_HUD.png`, `F06_INDEX_ONLY_HUD.png`.

## File modificati

**14 file**, relativi alla root del progetto; nessun file preesistente eliminato:

```text
Assets/OptimalControl/Runtime/Content/Definitions/FormulaDefinition.cs
Assets/OptimalControl/Runtime/Application/AppBootstrap.cs
Assets/OptimalControl/Runtime/Application/LessonInputRouter.cs
Assets/OptimalControl/Runtime/Visualization/ExperimentVisualization.cs
Assets/OptimalControl/Runtime/Visualization/HighlightRegistry.cs
Assets/OptimalControl/Editor/CatalogValidator.cs
Assets/OptimalControl/UI/CourseMenu.uxml
Assets/OptimalControl/UI/CourseMenu.uss
Assets/OptimalControl/Scenes/LessonWorkspace.unity
Assets/OptimalControl/Prefabs/PendulumScenario.prefab
Assets/OptimalControl/Data/Formulas/PrototypeDiscreteDynamics.asset
Assets/OptimalControl/Data/Lessons/Prototype/PendulumSlice.asset
Assets/OptimalControl/Tests/PlayMode/OptCon.Navigation.PlayModeTests.asmdef
Docs/OPTCON_IMPLEMENTATION_PLAN.md
```

## Procedura manuale in Unity

1. Aprire Bootstrap, attendere compilazione e avviare Play; aprire il prototipo. Il pannello deve mostrare r0/t=0, formula discreta e valori live. Scorrere il pannello quando necessario; la parte introduttiva non sostituisce l'esperimento.
2. Selezionare x_t, u_t, f_t e x_(t+1). Leggere significato, valore, unità, disponibilità e campione nel dettaglio. Current/next evidenziano corpo e grafici; input la coppia; f_t i segmenti correnti e mostra modello/parametri. I layer 1/2/3 restano sotto il controllo dell'utente.
3. Espandere Input e parametri, impostare u[0]=1 e scegliere Solo transizione oppure Coda da t. Modificare massa/lunghezza entro i limiti: nuova revisione e aggiornamento coerente di formula, corpo e curve. Provare un valore rifiutato: ultimo risultato valido conservato e diagnostico visibile. Gli asset non devono risultare modificati.
4. Usare timeline t=20: current @20, next @21, δ=.02 s, τ=.4 s. Play/pausa/step/rewind/reset agiscono sugli stessi comandi F04. Cambiare playback in campioni/s non cambia δ o la revisione matematica. Durante l'editing dei campi le scorciatoie non alterano l'esperimento.
5. Scegliere Pendolo · Euler esplicito. Leggere le due equazioni e selezionare θ/ω/current/next, u, m/l/g: i valori appartengono allo stesso risultato, non a un secondo calcolo della UI.
6. Seek a 100: input e next N/A, nessun ghost/transizione, τ=2 s; Play e step avanti disabilitati. Selezionare f_t: metadata consultabile, nessun arco futuro inventato.
7. Tornare al corso e riaprire: nuova sessione r0, t=0 e preset originale; nessuna selezione o subscription residua.
8. Eseguire tutte le suite dal Test Runner: **150 EditMode, 30 PlayMode**. Per index-only eseguire `IndexOnlyHudUsesTheSameGenericFormulaWithoutPhysicalTime`: il medesimo HUD viene collegato al fixture puro T=1, x0=2, u0=3, senza registrare una nuova lesson di produzione. Verifica anche il passaggio dal precedente T=100 terminale al cursore iniziale 0 della nuova sessione. Le catture e il probe nativo esterno documentano lo stesso caso con una proiezione F05 generica, current=2 e next=5; al terminale current=5, input/next assenti e δ/τ sempre N/A.
9. Selezionare PrototypeCourse e validare Catalog ed Execution Configuration: entrambi validi. Un binding Scalar dell'intero input vettoriale deve produrre un errore RuntimeBinding, mentre la componente 0 esplicita è valida.

## Deviazioni, compatibilità e problemi aperti

Nessuna deviazione funzionale dal perimetro F06. Il resolver puro in Content è il supporto minimo ad A02 e consente di lasciare Core/Simulation invariati. Il template condiviso evita un secondo UIDocument e mantiene il menu esistente. Il piccolo target della transizione completa il legame di f_t richiesto dal piano, senza introdurre un nuovo sistema di primitive o animazione.

Il validatore editor estende soltanto la compatibilità delle formule effettivamente introdotte; i validatori generali di F09 non vengono anticipati. Il fixture index-only è editor/test-only e non aggiunge capitoli o contenuti di produzione. La conservazione A/B è verificata come contratto, senza funzionalità di confronto utente.

Le API esistenti del Core/Simulation e la selezione a due argomenti del registry sono preservate. Il formato editoriale mantiene campi/GUID/ID preesistenti, aggiungendo la convenzione esplicita del vettore generico; il binding u_t cambia intenzionalmente in selezione scalare esplicita. Nessuna migrazione del tempo fisico.

Limiti previsti: rendering Unicode/lista selezionabile, scorrimento per formule lunghe, precisione di display G6 e viewport F05 fisso. Matrici/Jacobiani e layout matematici strutturati richiedono i rispettivi payload/formatter nelle fasi previste; non sono simulati tramite vettori. Il modello resta in double. Nessun problema bloccante rimasto per F06.

**A02 risolto, A01/A03/A04 preservati e verificati. Nessun costo, target di F07, ottimizzazione, confronto utente o Lesson Logic avanzata implementato. Il lavoro termina con F06.**
