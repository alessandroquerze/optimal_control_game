# Optimal Control — visione del progetto

Data dell'analisi: 9 ottobre 2026. Stato: progettazione della prima fase; nessuna implementazione dei capitoli.

## Obiettivo didattico

Realizzare un interactive textbook in Unity + URP nel quale una formula descrive un esperimento osservabile. Lo studente modifica una decisione, vede la conseguenza sul sistema e riconosce gli stessi valori nella notazione del corso. La scena 3D deve rendere leggibili relazioni matematiche: evoluzione, errore, costo, ammissibilità, approssimazione e ottimalità.

Il ciclo di apprendimento è **prevedere → intervenire → osservare → leggere la formula → spiegare il risultato**. Ogni lezione comprende un esperimento guidato, un'esplorazione libera e una breve verifica concettuale. Le spiegazioni testuali accompagnano l'esperimento; una schermata di testo da sola non costituisce una lezione completata.

Le fonti primarie sono i tre PDF locali, analizzati integralmente. La mappa con pagine, formule e osservazioni di correttezza è in [OPTCON_CURRICULUM.md](OPTCON_CURRICULUM.md). Le visualizzazioni e gli esempi aggiuntivi sono proposte progettuali esplicitamente distinte dai contenuti delle slide.

## Stato reale del workspace

| Elemento verificato | Situazione al momento dell'analisi | Conseguenza |
| --- | --- | --- |
| Unity | `6000.3.8f1`, da `ProjectSettings/ProjectVersion.txt` | Progettare sulla versione presente, senza aggiornamento del progetto |
| URP | `com.unity.render-pipelines.universal` `17.3.0` | Riutilizzare i materiali e gli asset URP del template |
| Scene | Solo `Assets/Scenes/SampleScene.unity` nei Build Settings | Creare in seguito un ingresso e uno spazio di lavoro per lezioni |
| Contenuto della scena | Main Camera, Directional Light, Global Volume | Nessuna logica educativa o simulazione da integrare |
| Script applicativi | Solo Readme e relativo editor del template in `Assets/TutorialInfo/` | L'architettura didattica parte da zero |
| Input/UI/test | Input System `1.18.0`, uGUI `2.0.0`, moduli UI Elements, Test Framework `1.6.0` | Utilizzare strumenti Unity già presenti; evitare nuovi pacchetti iniziali |
| Rendering | Asset PC e Mobile; pipeline PC referenziata da Graphics Settings | Primo prototipo desktop con mouse/tastiera; mobile da validare successivamente |
| Fonti | Tre PDF direttamente in `Docs/` | I link di questa fase rispettano il percorso effettivo |

`Docs/CourseSources/`, citata nella richiesta, non esiste attualmente. La cartella è proposta per nuove fonti; i tre PDF esistenti restano nei loro percorsi. `Library/`, `Temp/`, `Logs/`, file di soluzione e progetti C# generati non sono contenuto applicativo da progettare o modificare.

## Principi di rappresentazione

### Uno stesso modello per numeri e immagini

Il modello matematico produce stati e diagnostiche; la simulazione li raccoglie in snapshot; scena e HUD leggono lo stesso snapshot, identificato da revisione, timestep e, quando necessario, iterazione dell'algoritmo. Nessun renderer o slider ricalcola autonomamente una versione della dinamica.

```text
Parametro o input modificato
          ↓
Configurazione validata + modello matematico
          ↓
Simulazione / iterazione + diagnostiche
          ↓
Snapshot coerente
       ↙       ↘
Oggetti 3D    Formula, termini, valori e grafici
       ↘       ↙
Selezione dello stesso concetto tramite un ID semantico
```

L'identità centrale è **formula ↔ termine ↔ significato ↔ valore ↔ rappresentazione**. Selezionare `u_t` mostra il controllo applicato a quel timestep e il suo effetto visivo; selezionare `ℓ_T(x_T)` evidenzia lo stato finale e il contributo terminale, anche quando la timeline è su un altro istante.

### Separare gli spazi matematici

La posizione del corpo nello spazio fisico, lo stato completo `x_t`, la decisione `z` e il costo `ℓ(z)` appartengono a spazi diversi. Assi, unità e titoli devono indicare quale spazio è mostrato. Una superficie `height = ℓ(z₁,z₂)` rappresenta un grafico di costo; il suo asse verticale non è una coordinata dello stato.

Per il pendolo, il corpo visualizza l'angolo mentre il piano di fase visualizza `x = [θ, ω]`. Due corpi nella stessa posizione possono avere velocità differenti. Per il drone planare lo stato ha sei componenti, anche se la posizione visibile ne usa due. Per una traiettoria in `R^(nT+mT)`, una curva 2D è un esempio o una sezione dichiarata, mai una rappresentazione completa implicita.

### Rendere confrontabili le traiettorie

| Significato | Codifica proposta | Informazione associata |
| --- | --- | --- |
| Riferimento desiderato | Ciano, tratteggio, label `desired` | Può non soddisfare la dinamica |
| Rollout effettivo | Blu, linea continua, marcatori di timestep | Soddisfa la dinamica del modello simulato |
| Candidato di un algoritmo | Ambra, linea sottile e punti | Revisione/iterazione e residui disponibili |
| Soluzione di riferimento verificata | Verde, linea continua più spessa | Ipotesi e criterio di verifica dichiarati |
| Confronto A salvato | Viola, tratto alternato | Configurazione e orizzonte di A |
| Violazione | Rosso + simbolo/label | Vincolo e valore del residuo |

I colori sono una proposta iniziale: stile, etichette e marcatori devono conservare il significato anche senza distinguere i colori. Tratteggi e frecce saranno componenti comuni. Una traiettoria con costo inferiore non acquisisce automaticamente l'etichetta «ottima».

### I vettori devono dichiarare cosa misurano

Vettori di stato, variazioni, controllo, errore, gradiente e costate hanno significati e unità differenti. Ogni freccia espone origine, direzione, valore, unità e scala grafica. Una coppia del pendolo usa una freccia curva o un indicatore di coppia; non viene disegnata come una forza lineare. Le costate sono sensitività rappresentate sulla catena temporale o nel relativo spazio duale; non sono forze sul corpo.

Per una superficie di costo, la discesa è una direzione nello spazio delle decisioni. Il gradiente visualizzato sul piano dei parametri non coincide con la normale 3D al grafico della funzione.

### Mostrare approssimazione e ammissibilità

La linearizzazione include traiettoria nominale, perturbazioni `Δx`, `Δu`, previsione lineare e risultato nonlineare. Un indicatore mostra l'errore di approssimazione. L'utente deve vedere l'errore crescere con perturbazioni maggiori, senza una promessa di accuratezza globale.

I vincoli mostrano insieme geometria e residuo: `h = 0`, `g ≤ 0`. La regione disegnata è una rappresentazione del vincolo del modello. La collisione di un collider Unity non certifica l'ammissibilità matematica. Una penalizzazione di costo non equivale a un vincolo duro, e la saturazione dell'attuatore va distinta dal controllo richiesto.

## Tempo e interazione

Sono necessari play/pause, passo avanti, passo indietro, reset, cursore temporale, velocità di riproduzione, parametri modificabili, salvataggio di un confronto A e toggle dei layer. La velocità di riproduzione cambia quanto rapidamente si osservano i campioni, non il passo della dinamica.

Il tempo discreto del corso è `t = 0,…,T`. Quando il modello deriva da una discretizzazione con passo `δ`, il tempo fisico è `τ_t = tδ`; entrambi compaiono nell'interfaccia. L'indice `k` dell'ottimizzatore è un'altra coordinata, con una storia separata. Nelle lezioni del capitolo 3 l'utente potrà selezionare sia `k` sia `t`.

Il ritorno indietro legge campioni salvati. Modificare una sequenza di input genera una nuova revisione della traiettoria e ne ricalcola il futuro. I confronti conservano configurazione e dati propri. I layer nascosti continuano a esistere nel modello; una loro disattivazione grafica non cambia il problema.

## Matematica leggibile e corretta

La UI mantiene `x_t`, `u_t`, `f_t`, `ℓ_t`, `ℓ_T`, `z^k`, `λ`, `μ` e la convenzione dei gradienti vettoriali delle slide. La lettera `J` può essere un alias esplicativo del costo totale `ℓ(x,u)`, senza sostituire silenziosamente la notazione del corso.

Le formule hanno una definizione condivisa e termini selezionabili. Descrizioni, unità, dimensioni, fonti e ipotesi appartengono ai dati. I valori live appartengono alla sessione. Una formula valida al punto ottimo mostra un residuo se applicata a un candidato; non deve essere presentata come già soddisfatta.

La fedeltà alle fonti comprende il controllo dei refusi. Il curriculum registra le rettifiche motivate. Necessità, sufficienza, regolarità, convessità e tolleranza numerica devono rimanere distinguibili. Una simulazione discretizzata è descritta come tale; una campionatura finita non verifica tutti gli istanti di un vincolo continuo.

## Esperienza UI

| Zona | Contenuto |
| --- | --- |
| Top | Capitolo, lezione, obiettivo corrente, accesso al corso |
| Centro | Viewport 3D, assi, legenda, marcatori e selezione nel mondo |
| Lato | Formula corrente, termini, descrizione, valori live, fonte e ipotesi |
| Fondo | Timeline, play/pause, passi avanti/indietro, reset e velocità |
| Pannello parametri | Input, stato iniziale, target, pesi, orizzonte e opzioni pertinenti |

I pannelli sono collassabili. Le istruzioni guidate introducono pochi controlli per volta; la modalità libera rende disponibili gli altri. Un passaggio di lezione può cambiare lo spazio visualizzato senza cambiare l'impostazione del HUD.

Per la prima implementazione si propone UI Toolkit per i pannelli runtime e componenti URP semplici per il mondo. Il prototipo deve verificare leggibilità di simboli, selezione dei termini e input. Il layout di formule complesse resta un adattatore sostituibile: non si presume un motore LaTeX integrato.

## Progressione del corso

Il corso contiene tre capitoli e 18 lezioni concettuali: 6 introduzione, 8 ottimizzazione, 4 condizioni di ottimalità per il controllo. Sottopassaggi e approfondimenti restano all'interno delle lezioni; le pagine dei PDF non diventano scene individuali.

I prerequisiti orientano il percorso ma lasciano possibile l'esplorazione. I criteri di completamento verificano un'azione e una comprensione: prevedere uno stato, distinguere riferimento e traiettoria, spiegare un costo, riconoscere un vincolo attivo o motivare un bilanciamento KKT. La progressione usa ID stabili, così aggiungere o riordinare un capitolo non invalida i progressi salvati.

## Primo vertical slice proposto

**«Una coppia cambia il futuro del pendolo»**: laboratorio iniziale di `C1.L1`, con anticipazioni guidate di `C1.L2` e `C1.L3`. Il modello fisico viene da I pp. 33–36; la forma discreta applica al pendolo la procedura Euler illustrata in I p. 27. Questa trasposizione è un esempio progettuale dichiarato.

Il laboratorio usa un pendolo primitivo, un piano di fase, la sequenza degli stati e un target di equilibrio. Lo studente modifica la coppia, avanza un passo e vede aggiornarsi `x_t`, `u_t`, `x_(t+1)`, il movimento, la traccia e il costo. Salva una configurazione A e la confronta con B. Stage cost e terminal cost evidenziano i rispettivi campioni. Questo primo esperimento valuta input scelti dallo studente; la ricerca di una soluzione ottima viene introdotta nelle fasi successive.

Il completamento del slice dimostra l'intero collegamento modello → simulazione → visualizzazione → formula → verifica didattica, oltre alla navigazione tra menu e laboratorio. I confini e i criteri numerici sono in [OPTCON_IMPLEMENTATION_PLAN.md](OPTCON_IMPLEMENTATION_PLAN.md).

## Criterio di successo e limiti della fase iniziale

L'esperienza riesce quando uno studente può spiegare con formula e scena perché una modifica ha prodotto un certo effetto. Ogni visualizzazione deve rispondere a una domanda didattica esplicita; elementi puramente ornamentali hanno priorità bassa.

Questa fase consegna visione, curriculum, architettura e piano. Rendering avanzato, asset realistici, tutti i solver, tutti i capitoli, mobile e impaginazione matematica completa sono lavoro successivo. La scelta del dispositivo finale e il budget prestazionale vanno fissati prima del polish; non impediscono un primo prototipo desktop.

Documenti collegati: [curriculum](OPTCON_CURRICULUM.md), [architettura](OPTCON_ARCHITECTURE.md), [piano](OPTCON_IMPLEMENTATION_PLAN.md).
