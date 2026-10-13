# Remediation MR-01–05 prima di F14

Data: **12 ottobre 2026**. Unity **6000.3.8f1**, Windows, URP. Perimetro: evidenze didattiche, authoring e allineamento documentale dopo la [mini-review F11–F13](MINI_REVIEW_F11_F13.md). **F14 non iniziata.**

**READY FOR F14**

| Rilievo | Stato | Esito |
| --- | --- | --- |
| MR-01 | **RESOLVED** | I quattro step verificano l'esito corrente autorevole, oltre alle risposte/azioni pertinenti. |
| MR-02 | **RESOLVED** | Due rollout manuali sotto lo stesso problema; costo inferiore con disuguaglianza ancora violata. |
| MR-03 | **DOCUMENTED** | Copertura reale C1.L3 e recuperi F16/F17/F20/F28 esplicitati. |
| MR-04 | **RESOLVED** | Ordine editoriale deterministico variato nelle quattro lesson; ID delle risposte invariati. |
| MR-05 | **RESOLVED** | Chiusura della roadmap aggiornata a F13 completata, review e remediation verificate, F14 non iniziata. |

## Causa e soluzione

Il runtime precedente valutava correttamente le condizioni dichiarate dagli asset. Il problema era la loro insufficienza didattica: un evento di modifica rimaneva accreditato anche dopo undo, una componente sbagliata o un esito contrario alla risposta. Non è stato corretto alterando la matematica.

È stato aggiunto **un solo provider di condizione `metric.evidence`**, parametrizzato negli asset. `LessonMetricEvidenceDefinition` descrive metrica, componente esplicita, scope, campione richiesto, unità, confronto, valore atteso, tolleranza e vincoli di identità della nominale/problema. `LessonMetricEvidencePlan` congela questi dati all'apertura. `LessonMetricEvidenceEvaluator` usa il `RuntimeMetricResolver` composto nei `CourseRuntimeServices`, condiviso con gli altri consumer. Non risolve dinamiche, costi, vincoli, Jacobiani o rollout nel layer educativo.

La risoluzione richiede **Scalar, shape 1×1**: un vettore viene letto soltanto mediante selezione esplicita di componente. Gli stati Invalid/Unavailable non concedono evidenza. Un binding staticamente errato viene diagnosticato sia dal validatore sia durante la composizione della lesson. Vengono controllati unità, componente, sample entro T, scope, confronto, finitezza di valore atteso/tolleranza e capability necessarie. Un valore noto ma non ancora disponibile, per esempio la linearizzazione prima delle entry actions, può essere validato e diventa verificabile soltanto a runtime.

I confronti ammessi sono `Unchanged`, `Changed`, `EqualTo`, `GreaterMagnitude`, `LessThanBaseline`. La tolleranza editoriale dei binding introdotti è **1e-9 assoluto nell'unità della metrica**. L'evaluator confronta numeri già salvati; non contiene formule del pendolo né switch per lesson/capitolo.

## Baseline, revisioni e lifecycle

`LessonActivityBaseline` conserva un riferimento alla **pubblicazione immutabile completa** dopo l'esecuzione riuscita di tutte le entry actions. Include StepId, generation e un token di lifetime privato per istanza di `LessonSession`. Nessun baseline statico, globale o ricostruito dai parametri correnti.

Per concedere evidenza devono coincidere attività, generation, lifetime e cursor previsto. Anche il baseline deve essere al campione richiesto. I due valori vengono risolti dalle rispettive pubblicazioni e devono avere SampleIndex compatibile. A resta interpretabile con configurazione e campioni A dopo B; una request fallita non può reinterpretarlo.

- `Previous` ripristina il preset e le entry actions dell'attività precedente, quindi crea un nuovo baseline; non conserva il credito delle azioni future.
- `Restart` e Reset guidato ripartono dal primo step; il baseline precedente viene sostituito o rimosso secondo lo step di destinazione.
- Esplorazione libera, dispose e fallimento della transizione invalidano il baseline didattico. Riaprire crea una nuova sessione/lifetime.
- Un errore matematico rende falsa la nuova evidenza. Restano inoltre i gate esistenti su Error, LastPublicationFailure, NonRepresentable, navigazione e completion receipt.
- Nessun nuovo evento o canale di completion: il progresso continua a passare esclusivamente da `LessonSession.Next` → completion validata → binding/receipt persistente esistente.

I contratti `IExperimentSession`, pubblicazione, `SessionNotificationScope`, generation/lifetime dei consumer, immutable result, progress receipt, storage e AF08-11 reference/cost **non sono stati modificati**. L'unica estensione del lifecycle educativo è il baseline locale alla sua attività.

## Condizioni editoriali e probe

Le risposte concettuali e le selezioni di termini richieste rimangono necessarie. I risultati seguenti sono misurati dopo aver soddisfatto tali requisiti: non si tratta semplicemente di non aver selezionato una risposta.

### C1.L1 — `state`

Baseline a t=0: x₀=[.35,0]. Due binding `state.current`, componenti 0 [rad] e 1 [rad/s]: angolo invariato, velocità cambiata. L'istruzione propone .5 rad/s come esempio; la condizione richiede il fenomeno, non quel numero esatto.

| Edit iniziale | CanAdvance |
| --- | --- |
| [.7,0], soltanto θ | **false** |
| [.35,0], nessun cambiamento | **false** |
| [.7,.5], θ e ω | **false** |
| [.35,.5], soltanto ω | **true** |

### C1.L2 — `amplitude`

Le entry actions catturano la nominale, applicano Δx₀[0]=.1 e portano a **t=1**. Il baseline legge l'errore autorevole `LinearizationComparison.Errors[1][1]` tramite `linearization.error`, componente 1 [rad/s]. La nuova condizione richiede un modulo maggiore sullo **stesso oggetto nominale** e allo stesso campione. Un secondo binding a `linearization.delta-initial`, componente 0 [rad], verifica che sia davvero aumentata anche la perturbazione richiesta. Non basta produrre errore con una modifica diversa.

| Perturbazione / campione | Errore ω al campione 1 [rad/s] | CanAdvance |
| --- | ---: | --- |
| .1 / t=1, baseline/no-op | 0.00003268365389231713 | **false** |
| .05 / t=1 | 0.000004086989092912108 | **false** |
| .8 / t=2, campione errato | La verifica richiede t=1 | **false** |
| .8 / t=1 | 0.01621473496551364 | **true** |

Ricatturare una nominale differente non può reinterpretare il baseline dell'attività. I valori noti sono verificati nei test; Jacobiani e traiettorie continuano a essere calcolati soltanto dal core/sessione esistente.

### C1.L3 — `weight`

L'istruzione richiede esplicitamente **Q[0]=4** a t=0. Oltre all'azione e alla risposta, viene risolta `parameter.cost.q.0`, Scalar [1/(rad)²], confrontata con 4 entro tolleranza.

| Sequenza | CanAdvance finale |
| --- | --- |
| 2→4 | **true** |
| 2→4→2 | **false** |
| 2→3 | **false** |
| 2→2 | **false** |

### C1.L3 — `cost-not-feasibility`, MR-02

L'attività è ora un confronto fra candidati del **medesimo problema**. Entrando: Reset, limite di coppia .5 N·m, cancellazione dell'eventuale confronto, salvataggio di A. A usa u=1 N·m su tutta la coda. Si può modificare soltanto l'input; pesi, modello, reference e vincoli non sono editabili nello step. A non può essere sovrascritto dai comandi di confronto guidati.

Il binding `cost.total` [1], a t=0, richiede `J_B < J_A` oltre la tolleranza; resta obbligatoria la condizione autorevole `constraint.path-violated` e la risposta sull'inammissibilità. `RequireSameProblem` controlla ExperimentId, T, x₀ e identità delle configurazioni immutabili Model/Cost/Constraints/Reference. Una modifica esterna alla UI che cambi criterio o vincolo non supera la condizione.

Per il preset T=20, x₀=[.35,0], δ=.02 s, Q=[2,.5], R=[.2], Q_T=[5,1], reference zero:

| Input su tutta la coda | J | g=abs(u)−.5 | CanAdvance |
| --- | ---: | ---: | --- |
| A: 1 | 5.294120943466966 | .5 | **false**, nessun miglioramento |
| B: 2 | 10.691159780282874 | 1.5 | **false**, J maggiore |
| B: 0 | 4.445472864026721 | −.5 | **false**, nessuna violazione di coppia |
| B: .75 | 4.655611848439829 | .25 | **true**, J minore e vincolo violato |
| .75→1, undo | 5.294120943466966 | .5 | **false** |

Cambiare Q a 4, incluso il controesempio originale della review, non accredita lo step. Sono testati anche Q=0, reference differente e limite differente. Tutti i numeri live provengono da CostEvaluation/ConstraintEvaluation dei risultati; i numeri nelle istruzioni aiutano l'osservazione e non alimentano il confronto runtime.

Il presenter espone il riepilogo **Configurazioni A/B in sola lettura** quando la condizione richiede un problema fisso, mantenendo nascosti i comandi che sovrascrivono A. È una modifica minima di disclosure del confronto esistente; nessun solver o nuovo sistema visuale.

## MR-04 — Ordine delle risposte

Riordinati deterministicamente i blocchi delle risposte nei quattro asset C1.L1–L4, alternando la posizione nei diversi step. ID, ExpectedAnswerId, testi e feedback non sono stati rigenerati. Il test `ProductionAnswerOrderVariesDeterministicallyAndExpectedIdsArePreserved` verifica per **ciascuna lesson di produzione** che ogni ExpectedAnswerId esista e che ci siano risposte corrette sia in prima posizione sia in posizione successiva. Nessuna randomizzazione.

La scelta contestuale iniziale di L1 resta engagement non valutativo (`answer.selected`); non viene trasformata in domanda a risposta corretta.

## MR-03 / MR-05 — Documentazione e compatibilità

Il curriculum distingue ora ciò che C1.L3 insegna realmente — z impilato, tracking/effort, stage/terminale/prefisso/J, pesi diagonali, intervallo di coppia, h/g/r — dagli esempi generali ancora da produrre. **Ellisse ammissibile, parabola/superficie generale e frontiera di uguaglianza non sono dichiarate acquisite.**

Recuperi documentati: F16 decisioni statiche/superfici; F17 geometria interno/frontiera ed ellisse; F20 proiezioni soltanto sui set con operatore effettivamente disponibile; F28 copertura e richiami nel Chapter 1. La fixture F10 non conta come contenuto C1.L3. Solver del tracking resta F28; non è promessa una proiezione sull'ellisse.

Il riepilogo finale del piano registra F13 completata, mini-review eseguita, remediation verificata e **F14 non iniziata**. Le verifiche storiche e i report precedenti, inclusa la mini-review, rimangono invariati.

**C1.L1, C1.L2, C1.L3: contentVersion 1→2, schemaVersion 1 invariata; stable ID invariati.** È necessario distinguere i completamenti ottenuti con le vecchie condizioni permissive da quelli validi per il nuovo assessment. I record v1 restano nel salvataggio ma non accreditano la versione 2. Nessuna migrazione, cancellazione di profilo o modifica dello storage. C1.L4 cambia soltanto l'ordine delle risposte e conserva versione 1: i suoi completamenti restano validi. Un test con quattro record legacy conferma entrambe le policy.

## Test e verifiche eseguite

| Verifica finale | Risultato |
| --- | --- |
| Compilazione Unity conclusiva senza helper | **PASS, exit 0**, nessun errore/warning C# |
| Tutti gli EditMode | **425/425 PASS**, 0 failed/skipped/inconclusive; exit 0 |
| Tutti i PlayMode | **101/101 PASS**, 0 failed/skipped/inconclusive; exit 0 |
| Confronto nomi test con F13 | Conservati tutti i **388 EditMode + 97 PlayMode** precedenti; nessuno rimosso |
| Nuovi casi | **37 EditMode + 4 PlayMode** |
| Native Run / nuovo processo Reload | **PASS / PASS**, exit 0 / 0 |
| Mirror Assets/Packages/ProjectSettings | **605 file identici**, nessun C# helper aggiuntivo dopo pulizia |
| Dipendenze assembly | **13 assembly, grafo aciclico**; nessuna nuova assembly o dipendenza esterna |
| Integrità rispetto alla baseline iniziale di 738 file | Nessun file eliminato; Core/Simulation/Visualization, Packages/ProjectSettings e vecchi report invariati |

EditMode finale: 12 ottobre 2026, **13:49:36–13:49:41 UTC**. PlayMode finale: **13:49:57–13:50:48 UTC**. Risultati completi: [EditMode.xml](MR01_EVIDENCE/EditMode.xml), [PlayMode.xml](MR01_EVIDENCE/PlayMode.xml), [verification.json](MR01_EVIDENCE/verification.json).

I 37 nuovi EditMode coprono: quattro casi L1, quattro casi L2 e nominale ricatturata, quattro casi weight, quattro casi costo, quattro alterazioni del problema, quattro lifecycle delle attività interessate, generation/step/lifetime errati e revisione conservata, ordering, versioni legacy, request fallita e reset, injection del resolver custom fino a validator/runtime, sette binding invalidi (componente/unità/metrica/sample/tolleranza). Il codice di test non replica rollout, costo o Jacobiani: verifica esiti e alcuni valori noti.

I quattro nuovi PlayMode attraversano UI reale per L1, L2, weight e costo; coprono blocco/recupero, Previous, confronto A/B, completion e riapertura. I percorsi esistenti mantengono le loro asserzioni; sono aggiornati soltanto gli input dell'attività costo sostituita e le versioni attese dei contenuti corretti. Nessun test ignorato, disabilitato o eliminato.

Le suite complete conservano le regressioni F00–F13, A01/index-only e tempo fisico del pendolo, readiness A03, immutabilità A04, metric typing A02, AF08-01/02/03 e successive remediation, receipt/progress, Enter fallito, teardown/navigation, Presentation Error/NonRepresentable e separazione catalogo produzione/prototipo/fixture. Non sono stati introdotti percorsi alternativi per far passare le completion.

### Prova runtime nativa

Eseguita nell'Editor Unity nativo in una copia temporanea del progetto, partendo dal **Bootstrap e catalogo di produzione salvati**, senza sostituzione di lesson/asset. L'unica sostituzione è lo store verso un profilo file temporaneo isolato, per evitare di modificare il progresso personale. Un helper Editor temporaneo invoca i veri callback di navigazione, campi e pulsanti; è rimosso prima della compilazione finale. **Questa è automazione della UI nativa, non una sessione manuale umana, una Player build o uno studio di usabilità.**

Eseguiti i quattro percorsi completi: L1 11 step, L2 12, L3 9, L4 7. Ai quattro controesempi obbligatori, e agli altri negativi elencati, sono verificati **CanAdvance=false, pulsante disabilitato, Next=false e assenza di completion persistente**. Ogni corrispondente positivo produce CanAdvance=true. Il profilo acquisisce i quattro record soltanto dopo la conclusione corretta di ciascun percorso.

Riprodotti anche request matematica fallita con mantenimento dell'ultimo risultato, editing bloccato, reference finito ma non rappresentabile in float con Presentation **NonRepresentable**, identità della pubblicazione fra HUD/vista, nessun progresso e recupero con Reset/Restart. Menu/dispose cancella il baseline. Un **secondo processo Unity** rilegge il profilo e apre tutte le lesson: progresso presente, runtime nuovo non completato, primo step, nessun baseline/confronto residuo.

Tracce: [runtime](MR01_EVIDENCE/native-runtime-results.txt), [reload](MR01_EVIDENCE/native-reload-results.txt), [profilo risultante](MR01_EVIDENCE/native-progress.json). Catture archiviate, con campioni rappresentativi ispezionati visivamente:

| Attività | Negativo | Positivo |
| --- | --- | --- |
| L1 state | [θ modificato](MR01_EVIDENCE/negative-L1-state.png) | [soltanto ω](MR01_EVIDENCE/positive-L1-state.png) |
| L2 amplitude | [errore ridotto](MR01_EVIDENCE/negative-L2-amplitude.png) | [errore aumentato](MR01_EVIDENCE/positive-L2-amplitude.png) |
| L3 weight | [undo](MR01_EVIDENCE/negative-L3-weight.png) | [Q=4](MR01_EVIDENCE/positive-L3-weight.png) |
| L3 cost-not-feasibility | [J crescente](MR01_EVIDENCE/negative-L3-cost-not-feasibility.png) | [J minore, g positivo](MR01_EVIDENCE/positive-L3-cost-not-feasibility.png) |

## Procedura manuale in Unity

1. Aprire `Assets/OptimalControl/Scenes/Bootstrap.unity`, avviare Play e usare il corso predefinito `optimal-control` → Chapter 1. I prerequisiti rimangono consultivi. Le quattro lesson di produzione sono distinte dal prototipo e dalla fixture statica.
2. **C1.L1, step 3/11 «La posizione non è tutto lo stato».** Selezionare il termine x_t e la risposta sulla velocità oltre all'angolo. Aprire **Input e parametri**: con x₀[0]=.7 e x₀[1]=0 Avanti deve restare disabilitato; anche [.7,.5] e [.35,0] devono fallire. Impostare [.35,.5]: Avanti si abilita; posa invariata, stato differente.
3. **C1.L2, step 10/12 «Quanto lontano vale l'approssimazione?».** Le entry actions preparano nominale e baseline .1 a t=1. Selezionare la risposta sul peggioramento, portare Δx₀[0] a .05: errore non nullo ma inferiore, Avanti disabilitato. Portarlo a .8: errore maggiore e Avanti abilitato a t=1. Spostarsi a t=2: Avanti torna disabilitato; tornare a t=1 per proseguire. Non ricatturare la nominale.
4. **C1.L3, step 3/9 «Prevedi e modifica Q».** Selezionare Q0 e la risposta sul criterio. Impostare Q[0]=4: credito; riportare a 2: credito rimosso. Anche 3 e no-op 2 non bastano. Ripristinare 4 e proseguire normalmente.
5. **C1.L3, step 8/9 «Costo minore non basta».** A è già salvato, il limite è .5, t=0. Aprire **Input e parametri**, scegliere **Modifica=Coda**. Con input 2 leggere J≈10.69116 e g=1.5: Avanti disabilitato anche con risposta concettuale corretta. Con input 0 leggere J≈4.44547 e g=−.5: ancora disabilitato. Con .75 leggere J≈4.65561 contro J_A≈5.29412 e g=.25: Avanti abilitato. Riportare a 1 rimuove il credito. Il riepilogo A/B è leggibile scorrendo il pannello; non permette di sovrascrivere A in questa attività.
6. Usare **Indietro** dall'attività seguente: la precedente riparte dal proprio baseline, senza accreditare gli edit futuri. **Ricomincia/Reset** ripartono dal primo step. Ritornare al corso e riaprire: nuova attività, nessun baseline residuo.
7. Completare tutti gli step correttamente, tornare al corso, riaprire e riavviare Play/app: lo stato persistente è separato dalla sessione corrente, che riparte dal primo step. C1.L4 conserva il percorso manuale/reference precedente; le risposte corrette non sono sistematicamente la prima opzione.

Le catture confermano che alcuni controlli richiedono scroll, come già riportato in MR-06. Non interpretare il solo feedback della risposta «Corretto» come completion dell'attività: conta anche lo stato delle condizioni e Avanti.

## File creati e modificati

Percorsi relativi a `Assets/OptimalControl/`, salvo quelli sotto `Docs/`.

**Creati:**

- `Runtime/Lessons/LessonMetricEvidenceEvaluator.cs` e relativo `.meta`.
- `Tests/EditMode/Formula/LessonEvidenceRemediationTests.cs` e relativo `.meta`.
- `Docs/REVIEWS/MR01_REMEDIATION_BEFORE_F14.md`, questo report.
- `Docs/REVIEWS/MR01_EVIDENCE/`: `EditMode.xml`, `PlayMode.xml`, `verification.json`, `native-runtime-results.txt`, `native-reload-results.txt`, `native-progress.json`, otto PNG negative/positive delle quattro attività elencate sopra. Nessun helper eseguibile consegnato.

**Modificati:**

- `Runtime/Content/Definitions/LessonStepDefinition.cs` — descriptor editoriale opzionale dell'evidenza.
- `Runtime/Content/Lessons/LessonPlan.cs` — piano immutabile e validazione del descriptor.
- `Runtime/Lessons/LessonConditionRegistry.cs` — provider e contesto baseline.
- `Runtime/Lessons/LessonSession.cs` — cattura/invalidation locale all'attività; valutazione del contesto.
- `Runtime/Application/CourseRuntimeServices.cs` — stesso resolver composto per le condizioni.
- `Editor/CatalogValidator.cs` — preflight del binding con provider reale.
- `Runtime/UI/LessonStepPresenter.cs` — confronto A/B in sola lettura senza concedere comandi di overwrite.
- `Data/Lessons/Chapter01/C1_L1.asset`, `C1_L2.asset`, `C1_L3.asset` — evidenze, istruzioni, versioni 2, ordine risposte; entry/actions del confronto a criterio fisso in L3.
- `Data/Lessons/Chapter01/C1_L4.asset` — soltanto ordine risposte.
- `Tests/EditMode/Formula/ProductionIntroductionTests.cs`, `ProductionLinearizationTests.cs`, `ProductionCostReferenceTests.cs` — versioni aggiornate e percorso corretto dell'attività costo.
- `Tests/PlayMode/ProductionLinearizationRuntimeTests.cs`, `ProductionCostReferenceRuntimeTests.cs` — quattro nuove prove UI e aggiornamento del percorso costo esistente.
- `Docs/OPTCON_CURRICULUM.md` — copertura reale e recuperi MR-03.
- `Docs/OPTCON_IMPLEMENTATION_PLAN.md` — riepilogo/gate corrente MR-05 e note di recupero nei punti pianificati F16/F17.

Totale: **18 file esistenti modificati**, **4 nuovi file di codice/metadati**, questo report e **14 file di evidenza**. Confronto eseguito contro lo stato iniziale di questa remediation, che conteneva già il lavoro F00–F13, non contro l'intero diff Git preesistente. Nessuna scena, prefab, formula matematica, preset o modello modificato; GUID degli asset esistenti preservati.

## Limiti residui e gate

- `SameProblem` usa identità delle configurazioni immutabili e uguaglianza di x₀/T: è deliberatamente conservativo. Ricostruire un criterio equivalente con nuovi oggetti non dà credito; si deve rientrare nell'attività per creare un baseline coerente. Nessun motore generale di equivalenza dei problemi.
- L'evaluator introdotto copre solo confronti scalari/componenti su traiettorie richiesti qui. Non aggiunge assessment generale su esperimenti statici o matrici. Le altre condizioni d'azione rimangono nei loro scope di engagement/operazione già previsti.
- **MR-06** leggibilità/scroll, **MR-07** costi e geometrie oltre lo scope, **MR-08** Player/profiling/usabilità restano aperti nei gate dedicati. Nessuna correzione di questi rilievi è dichiarata.
- Nessun solver, drone, modello/capability/lesson F14 o implementazione delle geometrie future. Le annotazioni F16/F17 sono documentazione di recupero, non avanzamento di quelle fasi.

MR-01: **RESOLVED**  
MR-02: **RESOLVED**  
MR-03: **DOCUMENTED**  
MR-04: **RESOLVED**  
MR-05: **RESOLVED**

**READY FOR F14**
