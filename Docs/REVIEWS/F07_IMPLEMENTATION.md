# F07 — Costi discreti, target e confronto A/B

Data: 9 ottobre 2026. Ambito: esclusivamente F07, **completata e verificata**. Tutti i criteri soddisfatti; stato aggiornato nella roadmap. F08 non avviata.

## Prerequisiti e perimetro

Letti integralmente visione, curriculum, architettura, roadmap, mini-audit F00–F03, remediation A01 e report F04/F05/F06; esaminati sorgenti, assembly, asset e test delle fasi completate. Specifica operativa principale: F07 di `OPTCON_IMPLEMENTATION_PLAN.md`, con costo, target e confronto limitati al prototipo.

Prima delle modifiche, tutti i **274 file applicativi** coincidevano per SHA-256 con il manifest della consegna F06. Disponibili HUD live, validazione dei parametri, resolver tipizzato, selezione semantica e risultati immutabili. F06 risultava completata con 150 EditMode e 30 PlayMode passati. Nessuna regressione concreta richiedeva di ridisegnare A01–A04.

## Architettura e contratti

```text
ExperimentDefinition → ModelDefinition + CostDefinition
    → SimulationConfiguration { Model, x0, Controls, Cost }
    → SimulationSession / rollout
    → SimulationResult { Configuration, History, Costs }
         Costs = CostEvaluation(Configuration.Cost, History)
                       ↓
       SessionMetricSource → RuntimeMetricResolver F06
                       ↓
             FormulaPresenter / CostPanelPresenter

    SessionState → ExperimentVisualization
         → CostContributionVisualizer
         → TargetVisualizer
         → ComparisonTrajectoryRenderer
```

Core e Simulation rimangono C# puro con `noEngineReferences`; i rispettivi assembly non cambiano dipendenze. Content converte l'authoring in configurazione pura. Presentation legge risultati e invia comandi; Application conserva il lifecycle F04 senza modifiche. Nessun nuovo pacchetto o ciclo tra assembly.

`ICostModel` espone stage e terminale. `CostConfiguration` aggiunge il contratto immutabile di identità, unità, riferimento, parametri e validazione rispetto al modello. `QuadraticTrackingCost` è l'unica implementazione produttiva della forma quadratica; `CostContribution` conserva tracking, effort e totale finiti. Nessuna equazione di costo nel HUD o nei renderer.

`CostDefinition` è un provider editoriale opzionale. `QuadraticTrackingCostDefinition` congela pesi, riferimento input e target del preset, con dimensioni/unità del modello. Il nuovo `PendulumTracking.asset` contiene Q=[10,1], R=[0.1], QT=[20,2], u_des=[0]; il target resta [0,0]. Gli asset non vengono conservati nei risultati o modificati dai controlli runtime.

## Forma matematica e accumulo

La convenzione delle fonti I pp.20–22 e della roadmap è:

```text
ℓ_t = ½ Σ_i q_i (x_t[i] − x_des[i])²
    + ½ Σ_j r_j (u_t[j] − u_des[j])²
ℓ_T = ½ Σ_i qT_i (x_T[i] − x_des[i])²
C_j = Σ_(i=0)^(j−1) ℓ_i
J ≡ ℓ(x,u) = C_T + ℓ_T
```

Q e QT sono diagonali semidefinite positive; R è diagonale definita positiva. Il costo del prototipo è adimensionale `[1]`; i pesi delle componenti fisiche hanno unità inverse al quadrato, per esempio `1/(rad)²` e `1/(N·m)²`. Target/input desiderati conservano le unità dello stato/input. Non si sommano implicitamente quantità fisiche incompatibili.

È un **costo discreto per campione**, senza moltiplicazione per δ. Non è un'approssimazione dell'integrale continuo: la quadratura resta F15. Il modello di costo riceve t, x_t e u_t senza richiedere secondi.

`CostEvaluation` valuta esattamente T stage e un terminale, costruisce T+1 prefissi con C_0=0 e controlla finitezza dei contributi, prefissi e totale. Tutte le collezioni pubbliche sono immutabili. Il risultato è pubblicato soltanto dopo rollout e costi completi; non viene esposto un risultato parzialmente valutato.

### Caso deterministico

Fixture pura index-only: x_next=x+u, x0=2, u0=3, Q=R=QT=[2], target e input desiderato nulli.

| Campione | Stato | Input | Stage | Cumulativo all'arrivo |
| --- | --- | --- | --- | --- |
| 0 | 2 | 3 | 4 tracking + 9 effort = 13 | 0 |
| 1=T | 5 | N/A | N/A | 13 |

Terminale=25; **J=13+25=38**. Con due input pari a 3: stati [2,5,8], stage [13,34], prefissi [0,13,47], terminale=64, J=111. Con Q cambiato da 2 a 4: J=140, senza cambiare i campioni.

## Revisioni, comandi e A04

`SimulationConfiguration` aggiunge il costo opzionale. Quando presente, `ReferenceState` proviene dalla stessa configurazione di costo; il target mostrato non è una copia indipendente. Le API precedenti restano utilizzabili con `cost=null`.

`SimulationResult.Costs` conserva i riferimenti alla configurazione di costo e alla storia della medesima revisione. Un risultato A trattenuto conserva obiettivo, campioni, contributi e prefissi propri dopo qualsiasi modifica a B. Un fallimento di validazione o overflow conserva l'intero ultimo risultato valido, inclusi i costi e la loro configurazione, insieme al diagnostico del tentativo.

`SetParameter` usa i target dichiarati dalla configurazione: parametri della dinamica al modello, parametri del costo al costo. I target quadratics sono `cost.q.i`, `cost.r.i`, `cost.qt.i`, `cost.target.i`, `cost.inputTarget.i`. Il cambio di soli pesi/target riusa il percorso F04 di ricalcolo dal confine T: nuova storia/revisione con gli stessi oggetti stato, **zero chiamate Evaluate della dinamica**. I costi vengono rivalutati una volta. Modificare input o parametri fisici continua a usare le regole causali esistenti.

`SetHorizon` accetta 1…10000 transizioni: ridurre T tronca gli input; estenderlo ripete l'ultimo input disponibile, come dichiarato nel pannello. Ricrea rollout e costi completi e limita il cursore al nuovo terminale. Reset ripristina il preset catturato, senza rileggere gli asset. Playback e δ restano separati.

`CaptureComparison` trattiene l'intero risultato immutabile B come A; `ClearComparison` lo rilascia. Non serve clonare nuovamente dati già immutabili. Salvare/rimuovere A non modifica la revisione matematica. Reset di B conserva A per il confronto; chiudere la sessione elimina il riferimento della sessione ad A e le subscription.

## Formula HUD e metriche

Il resolver F06 rimane **l'unica regola di tipizzazione, forma, componenti, unità e scope**. Viene esteso soltanto `SessionMetricSource`, che legge costi già valutati e parametri della configurazione del risultato. `RuntimeMetric.Number` aggiunge un argomento scope opzionale, conservando il comportamento precedente.

| Metrica | Tipo / campione / significato |
| --- | --- |
| `cost.stage` | Scalar @t, tracking+effort; N/A a T |
| `cost.tracking` / `cost.effort` | Scalar @t, contributi salvati; N/A a T |
| `cost.cumulative` | Scalar @t, C_t; include soltanto gli stage precedenti |
| `cost.terminal` | Scalar, scope Terminal, @T anche con cursore altrove |
| `cost.total` | Scalar globale, senza SampleIndex artificiale; intero J |
| `state.reference` | Vector @t con unità per componente; riferimento del risultato |
| `parameter.cost.…` | Scalar del parametro/unità della configurazione immutabile |
| `state.current` con scope Terminal | Vector @T, usato per x_T senza un nuovo resolver |

In assenza di un provider, le metriche di costo dichiarano NotApplicable con diagnostico; non restituiscono uno zero inventato. Il pannello dei costi viene nascosto. Scalar e Vector restano distinti, anche con dimensione 1; nessuna conversione implicita aggiunta.

`AdditiveCost.asset` conserva ID `cost.additive`, somma discreta, trascrizione quadratica, fonti, convenzioni e dieci termini selezionabili: J, stage, tracking, effort, cumulativo, x_t, u_t, terminale, x_T e target. È la terza formula del prototipo; le due formule F06 e i loro ID/termini iniziali sono preservati.

`CostPanelPresenter` riusa il resolver per i valori live e genera campi dai parametri runtime puri. Mostra contributi, C_t, terminale previsto, J, pesi/target/T e confronto. Le configurazioni A/B mostrano revisione, T, δ opzionale, modello, x0/u0, parametri e obiettivo propri. I due J sono esplicitamente attribuiti ai rispettivi obiettivi; non viene affermato che il costo minore indichi una soluzione ottima.

Formula, pannello numerico e label del grafico usano formato **G6**, cultura invariante; il calcolo resta `double` senza arrotondamento. Il confronto con la valutazione numerica è esatto prima della formattazione.

## Visualizzazioni e selezione

- `CostContributionVisualizer` riceve `SessionState.Result.Costs`: grafico stage e marker del contributo corrente, diamante terminale separato, grafico C_t e J. Due scale verticali indipendenti, esplicite e derivate dai valori salvati; ascissa t, non τ. Le linee sono guide fra campioni discreti. Non somma contributi e non calcola costi.
- `TargetVisualizer` riceve il riferimento del risultato e la storia: sfera ciano nello spazio proiettato e diamante x_T sempre @T. `StateProjection.ProjectState` aggiunge soltanto la conversione geometrica di un riferimento, con identica validazione delle unità; non crea una traiettoria artificiale del target né ne certifica l'equilibrio.
- `ComparisonTrajectoryRenderer` riusa `TrajectoryRenderer`: A viola spessa, B blu sottile, ciascuna dalla propria storia. Le curve dedicate al confronto coprono soltanto l'intervallo comune fisico, quando entrambi hanno secondi, oppure l'intervallo comune di indici. Con passi diversi selezionano campioni diversi appartenenti alla stessa durata comune. Domini diversi producono sovrapposizione N/A. Il tracciato ordinario F05 di B conserva comunque il suo intero rollout.

`ExperimentVisualization` distribuisce la stessa pubblicazione ai nuovi consumer mediante la subscription esistente. `LessonHudPresenter` compone il pannello costi nel medesimo UIDocument. Nessun consumer legge costi da un renderer o asset mutabile.

Il termine stage usa `cost.stage`; target usa `state.reference`; **ℓ_T e x_T usano `state.terminal`** con SampleIndex T e revisione del risultato. Il registry F05/F06 evidenzia i due marker finali dei grafici di stato e il contributo terminale, anche con il cursore in t=7 o t=20. Le visibilità dei layer restano rispettate.

### Terminale e tempo

A t=T: x_T, C_T, ℓ_T e J sono disponibili; stage/tracking/effort/input sono N/A, marker stage nascosto, nessuna valutazione di u_T o x_(T+1). C_T esclude ℓ_T; J lo aggiunge una sola volta.

Il pendolo mantiene δ=.02 s, τ_20=.4 s e τ_100=2 s. La fixture index-only conserva t=0/1 e δ/τ assenti; il suo J=38 non usa un passo artificiale. La velocità di playback non entra nella valutazione del costo.

## Test e verifiche

Nuovi test: **30 EditMode in `CostSystemTests` e otto PlayMode in `CostPresentationTests`**. Coprono casi noti, contributi/somma/prefissi/T=1, terminale senza stage/input, index-only, pendolo, unità, forma/scope, A/B, cambio peso/target senza rivalutare la dinamica, errore/overflow/NaN/∞, assenza di provider, immutabilità e reset/T.

I PlayMode verificano medesimo risultato tra HUD/grafico/target, tutte le ordinate dei grafici contro i valori salvati, label J con lo stesso G6, selezione terminale da un cursore diverso, controlli effettivi di pesi/target/input/T, asset invariati, confronto su durata comune con δ differenti, fallimento coerente, chiusura/riapertura e caso index-only J=38.

| Verifica finale | Risultato |
| --- | --- |
| Compilazione Unity senza strumenti temporanei negli Assets | **Exit code 0**; zero errori e warning C# |
| Intera suite EditMode | **180/180 PASS**, zero falliti/skipped: catalogo 60, costi 30, formule/resolver 28, pendolo 26, rollout 13, sessione 16, proiezione 7 |
| Intera suite PlayMode | **38/38 PASS**, zero falliti/skipped: costi 8, navigazione/lifecycle 15, Formula HUD 9, visualizzazione 6 |
| Runtime nativo | **PASS**, pendolo, modifica input/peso, A/B, seek, selezione terminale, terminale, index-only 13+25=38 e chiusura/riapertura; zero Error/Exception/Assert |
| Regressione nativa F00–F06 | **3/3 cicli menu → workspace → menu PASS**, due con domain reload e uno senza; servizi unici, URP/pavimento/luci/Volume preservati |
| Prova visiva | **Sei Game View 1440×979 ispezionate**, pendolo/pannello/formula/selezione/terminale e index-only corrente/terminale; quattro conservate nel repository |
| Identità dei sorgenti verificati | **308 file applicativi**, zero differenze SHA-256 fra workspace e copia verificata; nessun helper temporaneo consegnato |
| Integrità della consegna | **39 file creati, 17 modificati, zero eliminati** rispetto al baseline di 349 file; nessuna modifica estranea al perimetro dichiarato |

**Regressioni A01–A04:** A01 preservato: pendolo δ=.02 s, τ coerente e fixture index-only senza δ/τ; A02 preservato: stesso resolver, Scalar/Vector/componenti/unità/scope espliciti; A03 preservato: suite lifecycle completa e cicli nativi con/senza domain reload; A04 preservato: A mantiene configurazione, storia e costi propri dopo B e dopo un tentativo fallito. Tutte le suite precedenti passano, senza test disabilitati.

Catture consegnate: [panoramica costo e A/B](F07_COST_OVERVIEW.png), [formula e contributi live](F07_COST_FORMULA.png), [selezione terminale con cursore a t=20](F07_COST_TERMINAL_SELECTION.png), [index-only al terminale, J=38](F07_INDEX_ONLY_TERMINAL.png).

Esecuzioni nell'Editor Unity **6000.3.8f1** su copia isolata, mantenendo aperto l'Editor dell'utente. L'indicizzazione Search è disabilitata soltanto nella copia; nessuna preferenza del workspace o migrazione automatica URP della copia viene trasferita. Le prove runtime sono automatizzate nell'Editor reale e le catture sono ispezionate manualmente; non si dichiara una sessione di click nell'Editor dell'utente.

Evidenze locali: `C:/Users/querz/AppData/Local/Temp/OptCon-F07-8f89939fb9ab4940a05b9d63578afc04/`. Conservati XML, log, exit code, strumenti esterni, risultati runtime/smoke, sei Game View e manifest SHA-256. Gli strumenti esterni non fanno parte degli Assets consegnati.

## Inventario

File applicativi creati: i seguenti **16 file**, ognuno con il proprio `.meta` (32), più i due metadati di cartella `Runtime/Core/Costs.meta` e `Data/Costs.meta`: **34 file applicativi nuovi**. Percorsi relativi ad `Assets/OptimalControl/`:

```text
Runtime/Core/Costs/CostConfiguration.cs
Runtime/Core/Costs/QuadraticTrackingCost.cs
Runtime/Simulation/CostEvaluation.cs
Runtime/Simulation/ComparisonSnapshot.cs
Runtime/Content/Definitions/CostDefinition.cs
Runtime/Content/Definitions/QuadraticTrackingCostDefinition.cs
Runtime/UI/CostPanelPresenter.cs
Runtime/Visualization/CostContributionVisualizer.cs
Runtime/Visualization/TargetVisualizer.cs
Runtime/Visualization/ComparisonTrajectoryRenderer.cs
Data/Costs/PendulumTracking.asset
Data/Formulas/AdditiveCost.asset
Materials/ComparisonA.mat
Materials/ReferenceTarget.mat
Tests/EditMode/Formula/CostSystemTests.cs
Tests/PlayMode/CostPresentationTests.cs
```

Documentazione creata: questo rapporto e quattro catture `F07_COST_OVERVIEW.png`, `F07_COST_FORMULA.png`, `F07_COST_TERMINAL_SELECTION.png`, `F07_INDEX_ONLY_TERMINAL.png` in `Docs/REVIEWS/`: **39 file creati complessivamente**.

File modificati: **16 applicativi** sotto `Assets/OptimalControl/`, più `Docs/OPTCON_IMPLEMENTATION_PLAN.md`, quindi **17 complessivi**:

```text
Runtime/Simulation/SimulationConfiguration.cs
Runtime/Simulation/SimulationResult.cs
Runtime/Simulation/SimulationCommands.cs
Runtime/Simulation/SimulationSession.cs
Runtime/Content/Definitions/ExperimentDefinition.cs
Runtime/Content/Definitions/ModelDefinition.cs
Runtime/Content/Bindings/RuntimeMetricResolver.cs
Runtime/UI/LessonHudPresenter.cs
Runtime/Visualization/ExperimentVisualization.cs
Runtime/Visualization/StateProjection.cs
Runtime/Visualization/TrajectoryRenderer.cs
Editor/CatalogValidator.cs
UI/LessonHud.uxml
Prefabs/PendulumScenario.prefab
Data/Experiments/PendulumPrototype.asset
Data/Lessons/Prototype/PendulumSlice.asset
```

Nessun file preesistente eliminato. Le modifiche agli asset esistenti sono il collegamento del provider di costo, la terza formula e i componenti visuali del prefab; i parametri fisici, T=100, δ=.02, x0/input/target del preset rimangono invariati. Nessun `.meta` preesistente, assembly, pacchetto, ProjectSettings, scena, asset URP, SampleScene, PDF, altra specifica o report precedente modificato.

## Procedura manuale in Unity

1. Aprire Bootstrap, attendere compilazione, avviare Play e aprire il prototipo. Espandere «Costo discreto e confronto A/B»: stage iniziale=.6125, effort=0, C_0=0; terminale e J appartengono all'intero rollout. Controllare i due grafici e le loro scale distinte.
2. Nel selettore scegliere «Costo additivo discreto». Selezionare J, stage, tracking, effort, x_t e u_t: valori/significati/unità live provengono dalla stessa revisione. Muovere la timeline e confrontare C_t e contributo corrente; il rewind legge costi salvati.
3. Da t=7 o t=20 selezionare ℓ_T o x_T. Il cursore resta fermo; tre target semantici @100 vengono evidenziati. Selezionare x_des per il target ciano. Il riferimento non è un vincolo né una certificazione di equilibrio.
4. Salvare B come A. Modificare input, poi Q[0] da 10 a 30: A resta viola con la propria configurazione; il cambio del solo peso modifica i costi di B senza modificare gli stati. Espandere «Configurazioni A/B e rispettivi obiettivi» per leggere pesi, target, parametri, δ e T di entrambi.
5. In «Pesi, target e orizzonte» modificare x_des[0] e T, per esempio T=50. Il target e i costi seguono la nuova revisione; A conserva T=100, il confronto colorato usa l'intervallo comune. Provare R[0]=0 o un peso negativo: rifiuto esplicito, risultato valido conservato e campi ripristinati.
6. Seek al terminale: nessuno stage/input inventato; C_T+ℓ_T=J; x_T e terminale disponibili, next/ghost assenti. Cambiare playback non altera δ o J. Reset ripristina il preset di B; «Rimuovi A» rilascia il confronto.
7. Tornare al corso e riaprire: r0, pesi originali, nessun confronto o listener residuo. Catalog ed Execution Configuration del corso devono risultare validi.
8. Eseguire tutti i test dal Test Runner. `CostSystemTests` riproduce 13+25=38 e [0,13,47]+64=111; `IndexOnlyCostHudDisplaysThirtyEightAndTerminalWithoutSeconds` verifica il medesimo HUD senza durata fisica. Le catture native mostrano anche il grafico index-only al terminale, senza aggiungere un nuovo capitolo o asset di lezione di produzione.

## Compatibilità, decisioni e limiti

Nessuna deviazione funzionale da F07. Sono necessarie estensioni minime ai contratti precedenti: costo opzionale nel risultato/configurazione, confronto nei dati di sessione e comandi costo/T/A-B. RolloutEngine e le equazioni del pendolo restano invariati; non viene introdotto un secondo simulatore o resolver. L'assenza del nuovo campo costo mantiene il comportamento degli asset legacy.

I costi sono conservati nel risultato, invece di copiarli nei singoli `SimulationSnapshot`: è la stessa provenienza immutabile condivisa, con lookup per indice nel resolver. Il controllo T è limitato a 10000 per evitare richieste runtime accidentali eccessive; tale limite e la policy di estensione sono espliciti. I riferimenti costanti, pesi diagonali e confronto sotto obiettivi propri sono il perimetro della preview, non un sistema generale di ottimizzazione.

Viewport F05 fisso, layout Unicode/lista e scorrimento restano limiti dichiarati. I grafici dei costi adattano soltanto le scale geometriche; valori grandi di stato/target possono uscire dal viewport F05 senza saturazioni matematiche. Nessun problema architetturale bloccante individuato.

**F08, Lesson Logic, solver, superfici di ottimizzazione e contenuti dei capitoli successivi non implementati.**
