# Optimal Control — curriculum derivato dalle fonti

Data: 9 ottobre 2026. Proposta: **3 capitoli, 18 lezioni**, con sottoesperimenti e approfondimenti interni.

## Fonti, lettura e numerazione

Le fonti primarie sono i PDF presenti direttamente in `Docs/`, del corso di Giuseppe Notarstefano. Il testo è stato estratto localmente con `pdftotext -layout`; le formule ambigue sono state controllate anche tramite rendering locale delle pagine. Nessun programma di corso esterno sostituisce queste fonti.

| Codice | PDF | Pagine fisiche | Numerazione delle slide |
| --- | --- | --- | --- |
| I | [Introduction to Optimal Control](OPTCON-RL_introduction_optimal_control_2026_09_14.pdf) | 36 | Copertina + slide 1–35 |
| O | [Nonlinear Optimization](OPTCON-RL_optimization_2026_09_14.pdf) | 81 | Copertina, separatori e versioni progressive; numerazione fino a 73 |
| K | [Optimality Conditions for Optimal Control](OPTCON-RL_optimality_conditions_KKT_2026_10_08.pdf) | 20 | Copertina e una versione aggiuntiva della slide 7; numerazione fino a 18 |

**«p.» e «pp.» indicano sempre la pagina fisica, contando la copertina come pagina 1**. Il numero «slide» è il numero stampato nel footer. In O le pp. 38–40 sono versioni della slide 37, pp. 44–45 della slide 41, pp. 59–62 della slide 55 e pp. 64–65 della slide 57. In K le pp. 8–9 sono versioni della slide 7. Per O e K non usare un offset fisso tra pagina e slide.

Le espressioni seguenti sono trascrizioni matematiche normalizzate, non estratti letterali. Le proposte di esperimenti a bassa dimensione sono adattamenti didattici, identificati come tali. Il registro finale esplicita le rettifiche rispetto alle slide.

## Mappa concettuale e dipendenze

```text
C1: stato/input/output → traiettoria/equilibrio/linearizzazione
                        → decisione/costo/vincoli
                        → riferimento vs traiettoria → drone e pendolo
                                       ↓
C2: minima/derivate → convessità → discesa e Newton
                    → proiezione → Lagrangiana/KKT
                    → Newton vincolato/SQP e barriere
                                       ↓
C3: curve e manifold → problema compatto e Lagrangiana
                    → forward/adjoint/stationarity
                    → vincoli di bordo e di percorso
```

| Capitolo | Lezioni | Risultato atteso |
| --- | --- | --- |
| 1 — Introduction to Optimal Control | `C1.L1`–`C1.L6` | Costruire e valutare una traiettoria coerente con dinamica, obiettivi e vincoli |
| 2 — Nonlinear Optimization | `C2.L1`–`C2.L8` | Interpretare derivate, iterazioni, proiezioni e condizioni di ottimalità |
| 3 — Optimality Conditions / KKT for Optimal Control | `C3.L1`–`C3.L4` | Ricostruire le KKT sfruttando la struttura temporale del problema |

Prerequisiti principali: `C1.L2` prima di `C3.L1`; `C1.L3` prima di `C2.L1`; `C2.L1–L4` prima di `C2.L7`; `C2.L6` prima di `C3.L2–L4`. La numerazione segue il percorso consigliato, con barriere dopo KKT/SQP per facilitarne il confronto. Menu e progressione sono data-driven.

## Convenzioni comuni

- `x_t ∈ R^n`, `u_t ∈ R^m`; `t = 0,…,T−1` per transizioni e input, `t = 0,…,T` per stati.
- Con `x_0` fissato, `x = col(x_1,…,x_T) ∈ R^(nT)` e `u = col(u_0,…,u_(T−1)) ∈ R^(mT)`. Lo stato iniziale non fa parte del vettore delle decisioni libere.
- `ℓ(x,u) = Σ_(t=0)^(T−1) ℓ_t(x_t,u_t) + ℓ_T(x_T)`; `J` è un eventuale alias UI esplicito.
- `∇r(z)` per `r: R^d → R^p` è la matrice `d×p` con gradienti delle componenti in colonna, cioè il **trasposto del Jacobiano**. Quindi `A_t = ∇₁f_tᵀ`, `B_t = ∇₂f_tᵀ`.
- I vincoli di dinamica sono `h_t = f_t(x_t,u_t) − x_(t+1) = 0`. Questo segno fissa il segno delle costate in tutte le lezioni.
- `k` indica un'iterazione dell'ottimizzatore, non un timestep: `x_t^k` è lo stato al tempo `t` nella traiettoria candidata all'iterazione `k`.
- Unità, normalizzazione degli assi, precisione mostrata e tolleranze numeriche sono esplicite. Il display 3D può riscalare i vettori senza modificarne i valori.

## Capitolo 1 — Introduction to Optimal Control

### C1.L1 — Stato, input, output e tempo

**Fonti:** I pp. 2–10, «Optimal Control», «motivating domains», «Continuous-time control systems», «Discrete-time control systems and notation»; slide 1–9. Modello del primo laboratorio: I p. 33, slide 32; discretizzazione adattata da I p. 27, slide 26.

**Obiettivo e argomenti:** distinguere stato, controllo e output; capire perché un indice di prestazione e i vincoli aggiungono qualcosa al tracking; distinguere tempo continuo e successione discreta. Aeronautica, robotica, guida, droni e spazio sono motivazioni selezionabili all'interno della stessa lezione.

**Formule:**

```text
ẋ(τ) = f(x(τ),u(τ),τ),  y(τ) = h(x(τ),u(τ),τ)
x_(t+1) = f_t(x_t,u_t),  x_0 = x⁰
x = col(x_1,…,x_T),      u = col(u_0,…,u_(T−1))
```

**Visualizzazione/interazione:** pendolo nel mondo e punto `[θ,ω]` nel piano di fase, coppia applicata, output `y=θ`, stato precedente e previsione successiva. Slider di coppia, stato iniziale, passo singolo, play/pause, timeline e formula con valori. È dichiarato l'adattamento discreto del pendolo continuo del PDF.

**Verifica:** prevedere quale componente cambia al primo passo Euler con `ω_0=0` e coppia non nulla; riconoscere che `θ` da sola non descrive tutto lo stato. Sistemi comuni: dinamica, timeline, marker, vettori, formula live.

### C1.L2 — Traiettorie, equilibri e linearizzazione

**Fonti:** I pp. 10–13, «System trajectories», «Linearization of a system about a trajectory», «Recall some tools»; slide 9–12.

**Obiettivo e argomenti:** una traiettoria è una coppia stato-input che soddisfa tutte le transizioni; un equilibrio è un caso particolare; la linearizzazione descrive perturbazioni rispetto a una traiettoria nominale.

**Formule:**

```text
x̄_(t+1) = f_t(x̄_t,ū_t)
x_e = f(x_e,u_e)                         [sistema tempo-invariante]
Δx_(t+1) ≈ A_t Δx_t + B_t Δu_t
A_t = ∂f_t/∂x,  B_t = ∂f_t/∂u           [valutati sulla nominale]
```

Il modello linearizzato impone l'uguaglianza `Δx_(t+1)=A_tΔx_t+B_tΔu_t`; il simbolo `≈` descrive il confronto con la perturbazione del sistema nonlineare. Con stato iniziale fissato si usa `Δx_0=0`; un esperimento che perturba l'iniziale lo dichiara separatamente.

**Visualizzazione/interazione:** catena `x_0 → … → x_T`, controllo su ogni arco, ghost nominali, rollout nonlineare e linearizzato, frecce `Δx`, `Δu`, errore tra previsioni. Slider di ampiezza perturbazione e scelta del punto nominale.

**Verifica:** trovare una coppia di equilibrio del pendolo e osservare il peggioramento dell'approssimazione lontano dalla nominale. Sistemi comuni: traiettorie, confronto, diagnostiche di Jacobiano, proiezione dello stato.

### C1.L3 — Decisioni, costi e vincoli

**Fonti:** I pp. 14–23, «Optimization: main ingredients», esempi scalari/vettoriali, «Examples of constraint sets», «Discrete-time optimal control: main ingredients» e problema completo; slide 13–22.

**Obiettivo e argomenti:** passare da una decisione `z` a una sequenza di decisioni nel tempo; separare costo di tracking, effort, stage cost, terminal cost, uguaglianze di dinamica, vincoli di bordo e di percorso.

**Formule:**

```text
min ℓ(z)   subject to h(z)=0, g(z)≤0
ℓ(z) = ½(z−z̄)ᵀW(z−z̄)
ℓ_t = ½‖x_t−x_t^des‖²_Q + ½‖u_t−u_t^des‖²_R
ℓ_T = ½‖x_T−x_T^des‖²_QT
ℓ(x,u) = Σ ℓ_t + ℓ_T
r(x_0,x_T)=0,  g_t(x_t,u_t)≤0
```

Per il tracking si scelgono `Q,Q_T` simmetriche semidefinite positive e `R` simmetrica definita positiva, coerentemente con I p. 22. I limiti sono distinti dai pesi del costo.

**Visualizzazione/interazione:** parabola/superficie con pesi e target, ellisse ammissibile `az₁²+bz₂²−c≤0` con `a,b,c>0`, uguaglianza disegnata come bordo, barre di costo sopra gli stati e grafico cumulativo. Input richiesto e limite ammesso sono visibili separatamente. Slider dei pesi, target, limiti e orizzonte; selezione di stage/terminal cost.

**Verifica:** costruire una traiettoria a costo minore ma non ammissibile e spiegare perché non è una soluzione del problema vincolato. Sistemi comuni: costi scomposti, regioni di vincolo, formula-term binding.

**Copertura effettiva dopo F13 e gate MR-01–05 (12 ottobre 2026):** C1.L3 di produzione insegna z impilato con x₀ fissato, tracking/effort, stage/terminale/cumulativo/J, pesi diagonali, limite simmetrico di coppia con residuo g e vincolo fra estremi r. L'attività sul costo confronta input manuali differenti sotto lo stesso modello, costo, reference e vincoli. C1.L4 distingue riferimento campionato, output desiderato e rollout reale; il salto desiderato può violare la dinamica. La regione mostrata è un intervallo di coppia, non un'ellisse o l'intero insieme delle traiettorie ammissibili.

La lista di visualizzazioni sopra rimane l'obiettivo curriculare: gli esempi statici scalari/vettoriali di I pp.14–18, parabola/superficie generale, ellisse ammissibile e frontiera di uguaglianza **non sono ancora erogati da C1.L3** e non vanno assunti come conoscenze già acquisite. Recupero tracciato nella roadmap: **F16** per decisioni statiche e superfici; **F17** per geometria degli insiemi, ellisse e distinzione interno/frontiera; **F20** per proiezioni sui soli insiemi con operatore effettivamente implementato; **F28** per verifica della copertura e richiami nel Chapter 1. Nessuna proiezione dell'ellisse è promessa dal contratto corrente. La fixture statica F10 non accredita questa copertura; solver/ottimizzazione del tracking rimangono alle fasi successive e all'integrazione F28.

### C1.L4 — Curva desiderata e generazione di traiettorie

**Fonti:** I pp. 22–25, «Cost function: examples», «Trajectory generation», «Optimal control for trajectory generation»; slide 21–24. Richiamo applicativo: I p. 35, slide 34.

**Obiettivo e argomenti:** una curva desiderata rappresenta l'intenzione del progettista e può essere dinamicamente irrealizzabile; la traiettoria generata deve rispettare la dinamica mentre approssima l'output desiderato.

**Formule:**

```text
y_t = h_t(x_t,u_t),    y_t ≈ y_t^des
min_(x,u) Σ [½‖x_t−x_t^des‖²_Q + ½‖u_t−u_t^des‖²_R]
          + ½‖x_T−x_T^des‖²_QT
subject to x_(t+1)=f_t(x_t,u_t), x_0=x⁰
```

**Visualizzazione/interazione:** riferimento a gradino tra equilibri, traiettoria ottenuta da input manuali e candidato prodotto dal solver quando disponibile; residui dinamici sul riferimento. Modificare target, pesi, durata e profilo di input. La posizione desiderata e la velocità desiderata sono configurate esplicitamente.

**Verifica:** indicare una transizione della curva desiderata che viola la dinamica e confrontarla con una transizione del rollout. Il solver riusa i sistemi di ottimizzazione introdotti nel capitolo 2; nella produzione i primi esperimenti possono usare input manuali o una soluzione di riferimento verificata.

### C1.L5 — Drone planare: stato completo, costo e limiti

**Fonti:** I pp. 26–30, «Trajectory generation for an autonomous drone», esempi I–III e «Example: path constraints»; slide 25–29.

**Obiettivo e argomenti:** osservare come assetto, velocità e due spinte contribuiscono a un output di posizione; leggere costi componente per componente e limiti degli attuatori/stati.

**Formule:**

```text
x = [p_x,v_x,p_y,v_y,θ,ω]ᵀ,   u = [u₁,u₂]ᵀ,   y = [p_x,p_y]ᵀ
p̈_x = −(u₁+u₂) sinθ / M
p̈_y =  (u₁+u₂) cosθ / M − g
θ̈   = L(u₁−u₂)/(2J)
x_(t+1) = x_t + δ f_CT(x_t,u_t)
u_min ≤ u_t ≤ u_max,    p_y,min ≤ p_y,t ≤ p_y,max
```

`J` qui è il momento d'inerzia del drone, non l'alias del costo totale: la UI usa il nome esteso nel glossario. `Q=diag(q₁,…,q₆)`, `R=diag(r₁,r₂)`, `Q_T=diag(q_T1,…,q_T6)` scompongono il costo mostrato in I p. 29.

**Visualizzazione/interazione:** drone composto da primitive in un piano del viewport 3D, frecce delle spinte, orientamento, posizione e traiettoria; grafici separati delle sei componenti; corridoio di quota e indicatori delle saturazioni. Modificare spinte, profili desiderati e pesi; mostrare quale contributo cresce.

**Verifica:** spiegare perché la stessa posizione con velocità/assetto diversi produce futuri e costi diversi. È un **modello planare**, non una dinamica spaziale a sei gradi di libertà.

### C1.L6 — Tempo continuo e trasferimento del pendolo

**Fonti:** I pp. 31–36, «Continuous-time Optimal Control problem» I–II, «Example: trajectory generation for a pendulum» I–IV; slide 30–35.

**Obiettivo e argomenti:** distinguere costo funzionale continuo e somma discreta; riconoscere l'ottimizzazione su funzioni; costruire un riferimento tra equilibri del pendolo e capire perché il gradino non è una traiettoria.

**Formule:**

```text
ℓ(x(·),u(·)) = ∫₀^T ℓ_τ(x(τ),u(τ)) dτ + ℓ_T(x(T))
θ̈ = −(g/l) sinθ + u/(m l²)
x₁=θ, x₂=ω;  ẋ₁=x₂,  ẋ₂=−(g/l)sin x₁+u/(m l²)
x_e=[θ_e,0]ᵀ,  u_e=m g l sinθ_e
```

L'esempio di costo espanso di I p. 36 usa pesi senza fattori `½`; mantenere quella convenzione nella relativa formula, oppure dichiarare esplicitamente una riscalatura dei pesi.

**Visualizzazione/interazione:** pendolo fisico, piano di fase, coppia e target di equilibrio; grafici di `θ(τ)`, `ω(τ)`, `u(τ)`; confronto tra due passi di discretizzazione e campionamento del costo integrale. Due target/tempi del trasferimento selezionabili.

**Verifica:** ridurre `δ` e distinguere errore numerico, errore di tracking e costo. Il laboratorio numerico approssima un problema continuo; non ne certifica la soluzione esatta.

## Capitolo 2 — Nonlinear Optimization

### C2.L1 — Minimi, punti stazionari e curvatura

**Fonti:** O pp. 3–9, slide 2–8; pp. 23–25, slide 22–24; p. 53, slide 49 («Examples of cost functions»).

**Obiettivo e argomenti:** minimo locale/globale e stretto; differenza tra `argmin` e valore minimo; gradiente, Hessiano, sella, condizioni necessarie/sufficienti; QP come esempio controllabile.

**Formule:**

```text
z* ∈ argmin ℓ(z),             ℓ(z*) = min ℓ(z)
minimo locale + C¹ ⇒ ∇ℓ(z*)=0
minimo locale + C² ⇒ ∇²ℓ(z*) ⪰ 0
∇ℓ(z*)=0 e ∇²ℓ(z*) ≻ 0 ⇒ minimo locale stretto
ℓ(z)=½zᵀQz+qᵀz,  ∇ℓ=Qz+q,  ∇²ℓ=Q
Q ≻ 0 ⇒ z*=−Q⁻¹q
```

**Visualizzazione/interazione:** curve e superfici di costo, sezioni direzionali e curvature principali; scegliere `ℓ(z)=z²`, il polinomio `z⁴+2z³−12z²−2z+6` della p. 53 e un QP. Una sella `z₁²−z₂²` è un esempio progettuale aggiuntivo per il concetto della p. 8. Trascinare il punto e variare autovalori/rotazione di `Q`.

**Verifica:** mostrare un punto con gradiente nullo che non è un minimo e spiegare il ruolo dell'Hessiano. Per `Q ⪰ 0` singolare, l'esistenza del minimo richiede anche `q ∈ range(Q)`; non promettere un minimo per ogni `q`.

### C2.L2 — Convessità, regolarità e significato geometrico

**Fonti:** O pp. 10–22, slide 9–21: insiemi/funzioni convesse, monotonia del gradiente, convessità stretta/forte, Lipschitz, co-coercività e condizioni globali.

**Obiettivo e argomenti:** segmento ammissibile, corda sopra il grafico, piano di supporto, limiti quadratici; capire le garanzie aggiunte dalla convessità e le diverse costanti `μ` e `L`.

**Formule principali**, con `Δz=z_B−z_A`:

```text
θz_A+(1−θ)z_B ∈ Z                   [Z convesso, θ∈[0,1]]
ℓ(θz_A+(1−θ)z_B) ≤ θℓ(z_A)+(1−θ)ℓ(z_B)
ℓ(z_B) ≥ ℓ(z_A)+∇ℓ(z_A)ᵀ(z_B−z_A)
ℓ(z_B) ≥ ℓ(z_A)+∇ℓ(z_A)ᵀΔz + (μ/2)‖Δz‖²   [μ-forte convessità]
‖∇ℓ(z_A)−∇ℓ(z_B)‖ ≤ L‖z_A−z_B‖
ℓ(z_B) ≤ ℓ(z_A)+∇ℓ(z_A)ᵀΔz + (L/2)‖Δz‖²
```

**Approfondimento nella stessa lezione:** con `Δp=∇ℓ(z_B)−∇ℓ(z_A)` e lo stesso `Δz=z_B−z_A`, mostrare `ΔpᵀΔz≥0`, il rafforzamento `≥μ‖Δz‖²`, la co-coercività `≥‖Δp‖²/L` per una funzione convessa L-smooth e il limite combinato `≥μL‖Δz‖²/(μ+L)+‖Δp‖²/(μ+L)` nel caso fortemente convesso e L-smooth. Ipotesi e dominio vengono mostrati insieme al risultato.

**Visualizzazione/interazione:** due punti trascinabili, segmento, corda, piano tangente e due superfici quadratiche di confronto. Su un QP positivo definito usare `μ=λ_min(Q)`, `L=λ_max(Q)` e mostrare anisotropia e condizionamento.

**Verifica:** distinguere «locale implica globale» da «minimo unico»: la seconda garanzia richiede ulteriori ipotesi, come stretta convessità ed esistenza. Le affermazioni sui vincoli delle pp. 12–13 sono normalizzate nel registro di correttezza.

### C2.L3 — Discesa, passo e convergenza

**Fonti:** O pp. 27–33, slide 26–32; pp. 38–46, slide 37–42; notazione O pp. 80–81, slide 72–73.

**Obiettivo e argomenti:** distinguere direzione e lunghezza del passo; steepest descent e metrica `D^k`; passo costante, decrescente e Armijo; leggere le ipotesi dei risultati sui punti limite.

**Formule:**

```text
z^(k+1)=z^k+γ^k d^k,   ∇ℓ(z^k)ᵀd^k<0
d^k=−D^k∇ℓ(z^k),      D^k simmetrica ≻0
steepest descent: D^k=I
Armijo: ℓ(z^k+γd^k) ≤ ℓ(z^k)+cγ∇ℓ(z^k)ᵀd^k
γ ← βγ,  0<β,c<1
γ_k→0, Σγ_k=∞; esempio γ_k=1/(k+1)^α, ½<α≤1
```

**Visualizzazione/interazione:** punti iterativi sul costo e sulle curve di livello; vettore `d^k`; proposte di passo accettate/rifiutate; sezione `φ(γ)=ℓ(z^k+γd^k)` e soglia Armijo. Confrontare passi sul medesimo QP, con grafici del costo e norma del gradiente.

**Verifica:** provocare un aumento del costo con passo eccessivo e recuperare con backtracking. I teoremi delle pp. 42–45 riguardano punti limite sotto ipotesi specifiche; non garantiscono in generale un minimo globale o l'esistenza di un punto limite. Le ipotesi `d₁I⪯D^k⪯d₂I`, regolarità e, dove richiesta, Lipschitz/coercività restano visibili.

### C2.L4 — Newton: radici e modello quadratico

**Fonti:** O p. 32, slide 31; pp. 34–37, slide 33–36.

**Obiettivo e argomenti:** Newton come ricerca di uno zero, Newton per `∇ℓ=0`, modello locale quadratico, confronto con steepest descent e Newton con Hessiano approssimato.

**Formule:**

```text
r(z^k)+∇r(z^k)ᵀΔz^k=0,     z^(k+1)=z^k+Δz^k
∇²ℓ(z^k)Δz^k=−∇ℓ(z^k)
m_k(Δz)=ℓ(z^k)+∇ℓ(z^k)ᵀΔz+½Δzᵀ∇²ℓ(z^k)Δz
```

**Visualizzazione/interazione:** curva con tangente per il problema di radici, superficie originale e quadratica locale per l'ottimizzazione, direzioni Newton/gradiente e tracce A/B. Variare condizionamento e punto iniziale; scegliere un passo smorzato o puro; approfondimento con Hessiano stimato tramite differenze finite.

**Verifica:** osservare un passo Newton esatto su QP positivo definito e riconoscere che, su una funzione generale, Newton puro può non scendere. Il minimo del modello quadratico richiede curvatura appropriata; un sistema singolare produce una diagnostica esplicita.

### C2.L5 — Insiemi convessi, proiezione e direzioni ammissibili

**Fonti:** O pp. 48–52, slide 44–48.

**Obiettivo e argomenti:** ottimizzazione su un insieme non vuoto, chiuso e convesso, condizione variazionale, proiezione unica, projected gradient e interpolazione tra punti ammissibili.

**Formule:**

```text
∇ℓ(z*)ᵀ(z−z*) ≥ 0  per ogni z∈Z          [condizione necessaria]
P_Z(v)=argmin_(y∈Z) ‖y−v‖²
z^(k+1)=P_Z(z^k−γ^k∇ℓ(z^k))
z̃=argmin_(z∈Z) [∇ℓ(z^k)ᵀ(z−z^k)+½‖z−z^k‖²]
z^(k+1)=(1−γ^k)z^k+γ^k z̃,   0≤γ^k≤1
```

**Visualizzazione/interazione:** box, disco o semispazio con proiezione calcolabile; punto libero, punto proiettato e segmento ammissibile. Modificare punto iniziale, passo e confine; confrontare discesa libera e proiettata.

**Verifica:** un minimo al bordo può avere gradiente non nullo. La proiezione del disco è un esperimento progettuale; le garanzie di unicità non si estendono a insiemi non convessi. La condizione variazionale è anche sufficiente se l'obiettivo è convesso.

### C2.L6 — Lagrangiana, vincoli attivi e KKT

**Fonti:** O pp. 56–65, slide 52–57; pp. 66–68, slide 58–60 (QP con uguaglianze).

**Obiettivo e argomenti:** insieme attivo, regolarità/indipendenza dei gradienti, moltiplicatori, stationarity, ammissibilità, segno duale, complementary slackness e curvatura lungo direzioni tangenti.

**Formule:**

```text
A(z)={j | g_j(z)=0}
L(z,μ,λ)=ℓ(z)+Σ_j μ_j g_j(z)+Σ_i λ_i h_i(z)
∇ℓ(z*)+∇g(z*)μ*+∇h(z*)λ*=0
h(z*)=0,  g(z*)≤0,  μ*≥0,  μ*_j g_j(z*)=0
yᵀ∇²_zz L y≥0 per le direzioni tangenti indicate in O p. 65
```

Nel QP con `Az=b`, il sistema è `[Q Aᵀ; A 0][z;λ]=[−q;b]`; la curvatura si considera su `Ay=0`. Per un esempio con soluzione unica scegliere `A` a righe indipendenti e `Q` positiva definita sul suo nucleo.

**Visualizzazione/interazione:** curve di livello, frontiere, gradienti e loro somma pesata; slider dei moltiplicatori e indicatori separati dei residui. Esempio progettuale verificabile: `ℓ(z)=½‖z−d‖²`, `g(z)=z₁−c≤0`. Se `d₁>c`, `z*=[c,d₂]`, `μ*=d₁−c`; se `d₁<c`, `z*=d`, `μ*=0`. Il caso `d₁=c` mostra un vincolo attivo con moltiplicatore zero.

**Verifica:** bilanciare i gradienti al bordo, poi spostare il target nell'interno e spiegare `μ=0`. Le KKT sono necessarie per minimi locali regolari; un punto KKT non è automaticamente un minimo. L'approfondimento di secondo ordine delle slide usa lo spazio tangente agli attivi; non viene presentato come una trattazione completa dei coni critici.

### C2.L7 — Newton vincolato, SQP e funzione di merito

**Fonti:** O pp. 70–77, slide 62–69.

**Obiettivo e argomenti:** trovare radici del sistema KKT di uguaglianza, linearizzare i vincoli, risolvere un QP locale, distinguere incremento del moltiplicatore e moltiplicatore del QP; scegliere il passo sul merito.

**Formule:**

```text
[B^k  ∇h; ∇hᵀ  0] [Δz;Δλ] = −[∇ℓ+∇h λ^k; h]
B^k=∇²_zz L(z^k,λ^k)
min_Δz ∇ℓ(z^k)ᵀΔz+½ΔzᵀB^kΔz
subject to h(z^k)+∇h(z^k)ᵀΔz=0
M₁(z;s)=ℓ(z)+s‖h(z)‖₁
z^(k+1)=z^k+γ^kΔz^k
λ^(k+1)=(1−γ^k)λ^k+γ^k λ_QP^k
```

**Visualizzazione/interazione:** frontiera curva e approssimazione tangente, superficie di costo e QP locale, residuo di ammissibilità, traccia delle iterazioni e grafici `ℓ`, `‖h‖`, `M₁`. Esempio aggiuntivo a due variabili con una uguaglianza regolare; mostrare il sistema a blocchi e il motivo di ogni blocco. Slider di `s` e backtracking.

**Verifica:** accettare un passo che migliora il merito e spiegare perché il solo costo non descrive il progresso verso l'ammissibilità. Usare esempi con QP locale ben posto; sufficienza delle KKT del QP ed esattezza della penalità richiedono ipotesi adeguate. `λ_QP^k` è il nuovo moltiplicatore locale, non `Δλ^k` del sistema Newton.

### C2.L8 — Barriere e avvicinamento alla frontiera

**Fonti:** O p. 54, slide 50; p. 78, slide 70.

**Obiettivo e argomenti:** incorporare disuguaglianze nel costo attraverso una barriera logaritmica, conservare le uguaglianze, partire strettamente all'interno e ridurre il parametro `ε`.

**Formula:**

```text
ℓ_ε(z)=ℓ(z)−ε Σ_j log(−g_j(z)),   dominio g_j(z)<0
min ℓ_ε(z) subject to h(z)=0       [se ci sono uguaglianze]
```

**Visualizzazione/interazione:** superficie barriera che cresce alla frontiera, traiettorie per diversi `ε`, confronto con soluzione vincolata di riferimento; grafici di distanza dal bordo e costo originale. Slider di `ε` e sequenza di valori decrescenti.

**Verifica:** distinguere obiettivo originale e obiettivo barriera. Se si mostrano moltiplicatori `μ_j=ε/(−g_j)`, il prodotto è `μ_jg_j=−ε`, non zero: il problema barriera a `ε>0` non soddisfa esattamente la complementarità del problema originale. Gli esempi devono avere un interno strettamente ammissibile.

## Capitolo 3 — Optimality Conditions / KKT for Optimal Control

### C3.L1 — Curve, manifold delle traiettorie e spazio tangente

**Fonti:** K pp. 2–7, slide 1–6; prerequisito I pp. 10–12.

**Obiettivo e argomenti:** separare una curva generica `(α,μ)` da una traiettoria `(x̄,ū)`; dinamica come vincolo impilato; manifold e perturbazioni ammissibili al primo ordine. Il simbolo `μ` della curva qui è la sequenza di input generica, non il moltiplicatore di disuguaglianza delle KKT; indicarlo nel glossario locale.

**Formule:**

```text
h(x,u)=col(f₀(x₀,u₀)−x₁,…,f_(T−1)(x_(T−1),u_(T−1))−x_T)
𝒯={(x,u) | h(x,u)=0}
T_(x̄,ū)𝒯={(Δx,Δu) | ∇₁hᵀΔx+∇₂hᵀΔu=0}
Δx_(t+1)=A_tΔx_t+B_tΔu_t,  Δx₀=0       [x₀ fissato]
```

**Visualizzazione/interazione:** vista temporale con residui su ogni arco e un esempio scalare a un passo nello spazio `(u₀,x₁)` con curva di ammissibilità e tangente. Spostare uno stato senza ricalcolare gli altri crea intenzionalmente una curva non ammissibile; un comando separato di rollout ristabilisce la dinamica.

**Verifica:** un incremento tangente soddisfa la dinamica linearizzata, ma un passo finito lungo la tangente può lasciare il manifold nonlineare. Per dimensioni maggiori usare una sezione/proiezione dichiarata e mantenere disponibili i residui completi.

### C3.L2 — Dal costo temporale alla Lagrangiana

**Fonti:** K pp. 8–15, slide 7–13, in particolare notazione compatta pp. 10–11, richiamo KKT p. 12 e decomposizione p. 14. La p. 13 è un separatore.

**Obiettivo e argomenti:** capire «unconstrained optimal control» nel senso del corso: assenza di vincoli di bordo/percorso aggiuntivi, con dinamica ancora vincolante. Assunzioni `C²`, `x₀` fissato, struttura sparsa e moltiplicatore `λ_(t+1)` per ciascuna transizione.

**Formule:**

```text
min_(x,u) ℓ(x,u) subject to h(x,u)=0
L(x,u,λ)=Σ_(t=0)^(T−1) ℓ_t + ℓ_T
          + Σ_(t=0)^(T−1) λ_(t+1)ᵀ[f_t(x_t,u_t)−x_(t+1)]
L₀=ℓ₀+λ₁ᵀf₀
L_t=ℓ_t+λ_(t+1)ᵀf_t−λ_tᵀx_t       [1≤t≤T−1]
L_T=ℓ_T−λ_Tᵀx_T
```

**Visualizzazione/interazione:** grafo di timestep con nodi stato, controlli e costi; vincoli sugli archi e costate associate. Selezionare un termine della Lagrangiana evidenzia esattamente nodi e transizioni coinvolti. Vista a blocchi del Jacobiano con i blocchi `−I` della dinamica.

**Verifica:** ricostruire quali termini dipendono da uno stato intermedio e dal terminale, e spiegare perché non si impone stationarity su `x₀` quando è fissato.

### C3.L3 — Forward, backward/adjoint e stationarity degli input

**Fonti:** K pp. 14–15 e p. 17, slide 12–13 e 15.

**Obiettivo e argomenti:** interpretare la fattorizzazione temporale delle KKT; propagazione forward dello stato, condizione terminale, ricorrenza backward delle costate e residuo di stationarity per gli input.

**Formule, con il segno di h fissato sopra:**

```text
x_(t+1)=f_t(x_t,u_t)                          [0≤t≤T−1]
λ_T=∇ℓ_T(x_T)
λ_t=∇₁ℓ_t(x_t,u_t)+A_tᵀλ_(t+1)              [1≤t≤T−1]
ρ_(u,t)=∇₂ℓ_t(x_t,u_t)+B_tᵀλ_(t+1)=0         [0≤t≤T−1, all'ottimo]
```

**Visualizzazione/interazione:** tre layer temporali: stati forward, costate backward e residui input. Su una traiettoria candidata calcolare le costate e visualizzare `ρ_u` prima dell'ottimizzazione; dopo una modifica degli input confrontare costo e sensitività. Esempio iniziale lineare a bassa dimensione con costo quadratico e soluzione verificabile, poi pendolo.

**Verifica:** mostrare una traiettoria dinamicamente valida che ha stationarity non nulla. Confrontare il gradiente ridotto del costo sugli input con una differenza finita: l'animazione backward deve rappresentare la derivazione, non solo invertire il playback degli stati.

### C3.L4 — Vincoli di bordo e percorso nelle KKT temporali

**Fonti:** K pp. 18–20, slide 16–18; per il sistema completo KKT/complementarità O pp. 57–65.

**Obiettivo e argomenti:** aggiungere `r(x₀,x_T)=0`, `g_t(x_t,u_t)≤0`, moltiplicatori di dinamica `λ_d`, di bordo `λ_b` e di percorso `μ_t`; distinguere iniziale fissato e iniziale libero. La decomposizione delle slide assume `r=col(r₀(x₀),r_T(x_T))`.

**Formule principali per bordo separato e x₀ libero:**

```text
L=ℓ+Σ λ_(d,t+1)ᵀ(f_t−x_(t+1))+λ_bᵀr+Σ μ_tᵀg_t
λ_(d,t)=∇₁ℓ_t+A_tᵀλ_(d,t+1)+∇₁g_t μ_t       [1≤t≤T−1]
0=∇₂ℓ_t+B_tᵀλ_(d,t+1)+∇₂g_t μ_t             [0≤t≤T−1]
λ_(d,T)=∇ℓ_T+∇r_T λ_(b,T)
0=∇₁ℓ₀+A₀ᵀλ_(d,1)+∇r₀ λ_(b,0)+∇₁g₀ μ₀   [solo se x₀ libero]
h=0, r=0, g_t≤0, μ_t≥0, μ_(t,j)g_(t,j)=0
```

Con `x₀` fissato la relativa equazione di stationarity viene rimossa. Per un bordo accoppiato usare le derivate parziali di `r(x₀,x_T)` nelle equazioni iniziale/terminale, senza fingere che `r` sia separato. I vincoli di percorso del problema delle slide arrivano a `T−1`; limiti addizionali su `x_T` vanno definiti esplicitamente.

**Visualizzazione/interazione:** catena temporale con limiti sugli input/stati e vincolo terminale; colore/stile degli attivi, moltiplicatori e gradienti sul timestep selezionato; residui primal, dual, stationarity e complementarità separati. Primo esperimento: sistema lineare con costo quadratico, limiti semplici e piccolo orizzonte, risolto con un riferimento verificabile.

**Verifica:** stringere un limite e spiegare quali termini nuovi compaiono nella stationarity. Un'attività numerica usa tolleranze dichiarate; un vincolo attivo può avere `μ=0`. L'etichetta «ottimo globale» richiede una classe di problema e una verifica che la giustifichino.

## Registro di correttezza delle fonti

Queste osservazioni derivano dal confronto tra le formule delle pagine indicate e la loro algebra/dimensioni. I refusi visivi citati sono stati controllati anche nelle pagine renderizzate. Le correzioni devono accompagnare l'authoring delle formule; non modificano i PDF originali.

| Fonte | Osservazione | Regola per il progetto |
| --- | --- | --- |
| I p. 16, slide 15 | Nell'espansione con `W=diag(w₁,w₂)` il secondo termine usa nuovamente `w₁` | Usare `½w₁(z₁−z̄₁)²+½w₂(z₂−z̄₂)²`, come impone il prodotto con W |
| I p. 34, slide 33 | La prima condizione di equilibrio riporta `0=x₁,e` | Da `ẋ₁=x₂` segue `x₂,e=0`; `x₁,e=θ_e` può essere non nullo |
| I p. 26, slide 25 | Il testo finale parla di spazio 3D, ma equazioni e output sono planari | Presentare il drone come planare immerso in un viewport 3D |
| I pp. 21, 23, 30 | Il testo dell'esempio cambia intervalli dei vincoli rispetto alla formulazione generale | Ogni vincolo dichiara il proprio dominio temporale; nessun `u_T` implicito |
| O p. 12, slide 11 | La quasi-convessità caratterizza tutti i sottolivelli, non solo quello a livello zero | Usare la sufficienza: componenti g convesse/quasi-convesse danno sottolivelli convessi; la convessità di un solo sottolivello non prova il viceversa |
| O p. 13, slide 12; K p. 4, slide 3 | Un'uguaglianza affine dà un insieme convesso, ma un'uguaglianza nonlineare non implica sempre un insieme non convesso | Usare «può essere non convesso»; distinguere spazi affini e lineari |
| O p. 16, slide 15; p. 19, slide 18 | Il coefficiente della forte convessità non è normalizzato allo stesso modo nelle due pagine | Adottare `μ/2` nel limite quadratico e nella forma con la corda; mostrare la convenzione |
| O p. 36, slide 35; p. 37, slide 36 | Il minimo del QP produce un incremento negativo del gradiente, ma la p. 36 poi lo sottrae | Con `HΔz=−∇ℓ`, usare sempre `z_next=z+Δz` |
| O p. 68, slide 60 | Per `A∈R^(m×d)` la pagina riporta `Aᵀy=0` con `y∈R^d` | Lo spazio tangente di `Az=b` soddisfa `Ay=0`; verificare tutte le dimensioni |
| O p. 73, slide 65 | Sufficienza delle condizioni del QP senza esplicitare la curvatura | Richiedere positività semidefinita sullo spazio ammissibile; usare positività definita ridotta negli esempi con minimo unico |
| O p. 75, slide 67 | Per uguaglianze `h=0`, la formula del merito riporta `‖g(z)‖₁` | Usare `M₁=ℓ+s‖h‖₁`; non attribuire esattezza a ogni punto KKT senza le ulteriori ipotesi di minimo/curvatura |
| O p. 78, slide 70 | Il problema con barriera è chiamato unconstrained anche mentre conserva `h=0` | Restano le uguaglianze; il log richiede `g<0` e non produce complementarità esatta a `ε>0` |
| K pp. 8–9, slide 7; p. 15, slide 13 | Alcune dimensioni compatte omettono T e il terminale usa `z_T` | Usare `R^(nT)×R^(mT)` e `x_T`; mai inventare una nuova variabile terminale |
| K pp. 19–20, slide 17–18 | Alcuni indici `g_t` al tempo zero, `λ_d/λ` e un doppio `+` sono incoerenti | Espandere con `g₀`, `λ_d` per la dinamica e `λ_b` per il bordo, mantenendo il segno di h |

Due controesempi verificabili chiariscono gli enunciati sulla convessità senza introdurre nuovi argomenti di corso: `h(z)=z₁²` ha insieme degli zeri `z₁=0`, convesso, pur non essendo affine; `g(z)=z²[(z²−1)²+0.1]` ha sottolivello zero `{0}`, ma non è quasi-convessa perché `g(0.5)=0.165625>max(g(0),g(1))=0.1`.

## Copertura e confini

Le sezioni teoriche di I pp. 2–36 sono coperte dalle sei lezioni; le motivazioni sono raggruppate in `C1.L1`. Le sezioni di O sono coperte da `C2.L1–L8`; copertina/separatori non generano lezioni e le versioni progressive sono riunite. Gli esempi di O p. 53 sono preset di funzioni e la notazione finale O pp. 80–81 è una regola trasversale. K pp. 2–20 è coperto dalle quattro lezioni, includendo sia controllo senza vincoli aggiuntivi sia controllo vincolato.

Nei documenti attuali non si aggiungono capitoli su reinforcement learning, Bellman, MPC, Pontryagin continuo o Riccati: il titolo del corso non basta a considerarli già presenti nelle tre fonti. Possono entrare attraverso nuovi PDF e una nuova mappa concettuale. Le ricorrenze adjoint qui derivano direttamente dalle KKT discrete del PDF K.

## Pipeline per un nuovo PDF

1. Conservare la fonte, assegnare un `sourceId`, registrare nome, versione/data, hash e numero di pagine. Per nuove fonti usare preferibilmente `Docs/CourseSources/`; risolvere i percorsi tramite il registro, senza spostare automaticamente quelle esistenti.
2. Estrarre testo mantenendo le pagine e controllare visivamente formule, grafici, ipotesi e indici dubbi.
3. Creare una mappa di concetti, prerequisiti, notazione nuova e collegamenti alle lezioni esistenti. Raggruppare per domanda didattica, non per pagina.
4. Aggiungere `ChapterDefinition`, `LessonDefinition` e riferimenti precisi `sourceId/page/slide/section`, con provenienza delle rettifiche.
5. Definire le formule e i loro termini, binding a valori, unità e oggetti; dichiarare gli adattamenti a bassa dimensione.
6. Comporre un esperimento con modelli e visualizzazioni esistenti. Aggiungere una capability o un provider solo se il nuovo concetto lo richiede.
7. Verificare matematica, interazione e criterio di completamento; registrare il capitolo nel `CourseDefinition` senza modificare il CourseManager.

Per le responsabilità tecniche vedere [OPTCON_ARCHITECTURE.md](OPTCON_ARCHITECTURE.md); per l'ordine di produzione vedere [OPTCON_IMPLEMENTATION_PLAN.md](OPTCON_IMPLEMENTATION_PLAN.md).
