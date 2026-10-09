# Remediation A01 — Tempo fisico opzionale

Data: 9 ottobre 2026. Ambito: esclusivamente A01, classificato MAJOR nel mini-audit F00–F03. **Esito: A01 RISOLTO.**

Letti integralmente `OPTCON_ARCHITECTURE.md`, `OPTCON_IMPLEMENTATION_PLAN.md` e `MINI_AUDIT_F00_F03.md`. La correzione allinea il catalogo al contratto già presente nel Core, senza introdurre provider, sessione o clock di F04.

## Causa

`UsesDiscreteTime=true` attivava un controllo unico che richiedeva sia `T>0` sia `StepSeconds>0`. Il preset editoriale non poteva quindi descrivere una dinamica indicizzata da t senza attribuirle secondi. Il Core supportava già correttamente `DiscretizationStepSeconds=null` e snapshot senza tempo fisico.

Il precedente test editoriale sul tempo assente disabilitava le transizioni e azzerava le dimensioni: verificava un esperimento statico, non una dinamica discreta senza durata fisica. È stato rinominato per chiarirne l'ambito e affiancato da un caso dinamico completo.

## Contratto corretto

| Elemento | Significato e validazione |
| --- | --- |
| `ExperimentDefinition.UsesDiscreteTime` | Presenza di transizioni indicizzate da t. Mantiene il nome e il campo serializzato esistenti; non implica secondi. |
| `DiscreteTimePreset.Horizon` | Numero di transizioni T, sempre strettamente positivo per una dinamica discreta. |
| `DiscreteTimePreset.HasPhysicalTimeStep` | Indicatore esplicito e serializzato della presenza del passo fisico. |
| `DiscreteTimePreset.StepSeconds` | Ora `double?`: restituisce il passo dichiarato oppure `null` se il tempo fisico è assente. Quando presente deve essere finito e strettamente positivo. |
| Stato iniziale, input e riferimento opzionale | I controlli di dimensione e finitezza rimangono attivi anche senza passo fisico. Restano validati dimensioni del modello e unità. |

Un valore serializzato inattivo di `stepSeconds` viene conservato, ma non esposto né interpretato come durata: la presenza dipende dall'indicatore, non da uno zero convenzionale. Il valore nullable può essere passato al `ModelDescriptor` esistente. Per le dinamiche senza tempo fisico, δ e τ rimangono assenti, anche al campione t=0.

Il pendolo Euler continua a richiedere il passo fisico: `PendulumRolloutDiagnostic.Generate` rifiuta esplicitamente il valore assente prima di costruire il modello. `PendulumDynamics` mantiene la propria validazione di positività e finitezza, senza modifiche alle equazioni. Il catalogo valida il contratto generico; i requisiti specifici del modello sono applicati dal consumer matematico esistente. Il provider runtime rimane previsto in F04.

Nessun campo di playback introdotto o riutilizzato. `StepSeconds` descrive esclusivamente la discretizzazione fisica; non governa la velocità dell'interfaccia. Core e Simulation rimangono indipendenti da Unity, UI e visualizzazione.

## Compatibilità degli asset

Il nuovo campo `hasPhysicalTimeStep` ha valore iniziale `true`: un preset legacy, che non contiene il campo e attribuiva sempre significato fisico a `stepSeconds`, conserva tale interpretazione. Per authoring senza tempo fisico occorre disabilitare esplicitamente l'indicatore.

Non sono stati rinominati campi serializzati né riscritti asset. L'importazione nativa di un YAML senza il nuovo campo e i round trip con indicatore sia attivo sia disattivo sono coperti dai test. Tutti gli otto asset editoriali e i relativi `.meta` risultano identici alla baseline SHA-256, incluso `Assets/OptimalControl/Data/Experiments/PendulumPrototype.asset` con `T=100` e `stepSeconds=0.02`. GUID e riferimenti sono preservati.

**Incompatibilità sorgente intenzionale:** la proprietà pubblica `DiscreteTimePreset.StepSeconds` passa da `double` a `double?`. Un consumer che richiede secondi deve verificarne la presenza prima di usare `.Value`; non deve sostituire l'assenza con una durata arbitraria. Tutti i consumer attuali sono stati verificati e quello produttivo coinvolto è stato aggiornato. Nessuna incompatibilità introdotta nel formato degli asset o nelle API di Core/Simulation.

## File modificati e creati

File modificati, con prefisso `Assets/OptimalControl/`:

- `Runtime/Content/Definitions/ExperimentDefinition.cs`: indicatore serializzato, passo nullable e chiarimento semantico delle transizioni.
- `Editor/CatalogValidator.cs`: controlli separati su orizzonte e passo fisico opzionale; validazione dei vettori preservata.
- `Editor/PendulumRolloutDiagnostic.cs`: rifiuto esplicito del passo assente; utilizzo del valore fisico verificato.
- `Tests/EditMode/CatalogValidatorTests.cs`: nuovi casi A01, compatibilità legacy e round trip per entrambi i tipi di preset; rinomina del test statico.
- `Tests/EditMode/OptCon.Content.EditModeTests.asmdef`: riferimento a `OptCon.Simulation` esclusivamente nell'assembly dei test, per verificare catalogo e rollout insieme.

File creato: `Docs/REVIEWS/A01_REMEDIATION.md`, questo rapporto. Nessun file applicativo nuovo, asset nuovo o dipendenza esterna.

## Test aggiunti o modificati

La suite di catalogo passa da 27 a 54 casi eseguibili: incremento netto di **27 casi**, comprendente l'estensione del round trip esistente a due configurazioni. Il modello additivo è una fixture privata dei test, non una nuova implementazione produttiva.

| Copertura | Test e risultato verificato |
| --- | --- |
| Caso A | `IndexOnlyDiscreteCatalogProducesFiveWithoutPhysicalStepOrTime`: catalogo valido con transizioni attive, T=1, x0=2, u0=3, passo assente; rollout x1=5; δ/τ null in entrambi gli snapshot. |
| Caso B | `ExistingPendulumPresetPreservesPhysicalMeaningAndEveryRolloutSample`: passo legacy 0.02 presente; tutti i 101 stati identici al rollout con la configurazione precedente; τ finale=2 secondi; asset non mutato. La validità del catalogo originale rimane verificata da `PrototypeCatalogIsValidButNotExecutable`. |
| Caso C | `EulerPendulumRejectsMissingOrInvalidPhysicalStep`: rifiuto di passo assente, zero, negativo, NaN, +∞ e −∞. `ADeclaredPhysicalStepMustBePositiveAndFinite` verifica anche il rifiuto nel catalogo dei cinque valori presenti non validi. |
| Validazione senza secondi | `IndexOnlyDynamicsStillRequirePositiveHorizon`, `IndexOnlyDynamicsStillValidateVectorDimensions`, `IndexOnlyDynamicsStillRejectNonFiniteVectors`, `IndexOnlyDynamicsStillRequirePositiveStateDimension`: T, x0, input e dimensioni restano vincolati. |
| Indicatore assente/disattivato | `AnAbsentPhysicalStepDoesNotExposeOrValidateItsInactivePayload`: il payload inattivo non inventa durata. `LegacyPresetWithoutPresenceFlagImportsAsPhysicalTime`: il vecchio YAML viene importato con passo 0.02 e τ finale=2. |
| Serializzazione | `NativeAssetRoundTripPreservesGraphUnicodeDoubleDefaultsAndStableIds(true/false)`: presenza/assenza del passo, dati numerici, unità, testo Unicode, ID e riferimenti sopravvivono a salvataggio, rinomina e reimportazione. |

I 39 casi matematici preesistenti sono stati rieseguiti integralmente, inclusi quelli con risultati Euler noti, gli equilibri, la validazione fisica e l'assenza di tempo nei rollout generici.

## Verifiche eseguite

Esecuzione nativa con **Unity 6000.3.8f1**, su copia isolata del progetto, mantenendo aperto l'Editor del workspace. Gli strumenti aggiuntivi di verifica delle scene sono stati usati solo nella copia temporanea e rimossi prima della compilazione finale.

| Verifica | Risultato |
| --- | --- |
| Compilazione Unity dei sorgenti e test; compilazione finale senza strumenti temporanei | **PASS**, processi conclusi con exit code 0, nessun errore C# di compilazione. |
| Tutti gli EditMode | **93/93 PASS**, zero falliti o saltati: 54 catalogo, 26 pendolo, 13 rollout. |
| Tutti i PlayMode disponibili e rilevanti | **9/9 PASS**, zero falliti o saltati; inclusi input reale del menu, navigazione ripetuta/concorrenza, bootstrap duplicato, lifecycle dei presenter, nuovi contenuti e confini degli assembly. |
| Regressioni F00–F02 aggiuntive | **3/3 cicli menu/workspace/menu PASS**, due con domain reload e uno senza: una radice persistente, una scena contenuto, una camera/presenter, testi degli asset, floor/light/Volume URP e assenza di script mancanti/errori runtime. |
| Diagnostica F03 nativa | **PASS**: menu e finestra sul preset pendolo originale, terminali N/A, asset invariato; caso fisico noto `[0,0] → [0,0.5] → [0.25,1]` e caso scalare `2+3=5` senza tempo fisico. Cattura riesaminata visivamente: δ/τ N/A nel caso scalare, unità e indici coerenti. |
| Identità dei sorgenti verificati | **146 file di `Assets/OptimalControl`, zero differenze SHA-256** tra copia verificata e workspace. |
| Integrità del workspace | Fra i 215 file della baseline in Assets/Docs/Packages/ProjectSettings, cambiano soltanto i cinque file elencati. Asset, scene, `.meta`, Core, Simulation, UI, Application, piano, architettura e vecchio mini-audit invariati. |

Durante la preparazione, l'avvio confinato non raggiungeva l'IPC del servizio licenze: le esecuzioni riuscite hanno usato l'accesso al servizio locale. Due tentativi dello smoke aggiuntivo sono stati interrotti da un'eccezione di `UnityEditor.Search.SearchDatabase` nell'indicizzazione all'avvio. È stata disabilitata esclusivamente tale opzione nei `UserSettings` della copia temporanea; lo smoke e la diagnostica successivi sono riusciti senza errori runtime. Nessun filtro sugli errori applicativi e nessuna modifica al progetto per aggirarla. Anche l'aggiornamento automatico dell'asset URP mobile effettuato da Unity nella copia è rimasto confinato alla copia.

Evidenze conservate in `C:/Users/querz/AppData/Local/Temp/OptCon-A01-40deb07a92554ff8ae01ffe67f82e336/`: `EditMode-results.xml`, `PlayMode-results.xml`, `editmode.log`, `playmode.log`, `final-compile.log`, `smoke-results.txt`, `regression-smoke.log`, `diagnostics-results.txt`, `diagnostics-preview.png`, `diagnostics.log`, `baseline.json`, `workspace-differences.json`, `verified-source-manifest.json` e log dei tentativi iniziali.

## Ripetizione manuale in Unity

1. Aprire il Test Runner ed eseguire tutte le suite EditMode e PlayMode; per A01 filtrare `CatalogValidatorTests`.
2. Selezionare `PrototypeCourse.asset` e usare `Tools/Optimal Control/Validate Selected Course/Catalog`: catalogo valido. La configurazione eseguibile mantiene i prerequisiti F04 ancora mancanti già documentati.
3. Selezionare `PendulumPrototype.asset` e usare `Tools/Optimal Control/F03/Inspect Selected Pendulum Preset`: T=100, 101 stati, 100 input, δ=0.02 secondi, τ finale=2 secondi e input/stato successivo N/A al terminale.
4. Eseguire `IndexOnlyDiscreteCatalogProducesFiveWithoutPhysicalStepOrTime` e i due round trip per verificare il caso senza secondi senza alterare gli asset originali.

## Esito e limiti dell'intervento

**A01 è risolto:** il catalogo ammette dinamiche discrete senza tempo fisico, conserva la validazione matematica essenziale e preserva significato e comportamento del pendolo esistente. Il Core non richiedeva modifiche.

**A02, A03 e A04 non sono stati corretti né modificati. F04 non è stata iniziata.** Nessuno stato di fase aggiornato e `MINI_AUDIT_F00_F03.md` lasciato integralmente invariato.
