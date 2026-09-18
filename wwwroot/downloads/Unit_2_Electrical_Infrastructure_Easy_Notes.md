# UNIT 2: Data Centre Electrical Infrastructure Engineering
## 📘 Easy-to-Understand Master Exam Study Guide & Notes (6-Hour Syllabus)
**Target Course:** M.Tech CSE (Data Centre Systems Engineering)  
**Institutions:** MIT World Peace University (MIT-WPU) × NIRVAA Solution  
**Format:** Plain English Explanations + Real-World Analogies + Solved Numericals + Ready Exam Answers  

---

## 🧭 Quick Navigation
1. [Unit Overview & High-Yield Exam Blueprint](#1-unit-overview--high-yield-exam-blueprint)
2. [Topic 1: Utility Power Systems (Grid Intake & Transformers)](#2-topic-1-utility-power-systems-grid-intake--transformers)
3. [Topic 2: Backup Power Systems (Generators & UPS Topologies)](#3-topic-2-backup-power-systems-generators--ups-topologies)
4. [Topic 3: Power Distribution (From Substation to Server Chip)](#4-topic-3-power-distribution-from-substation-to-server-chip)
5. [Topic 4: Earthing, Grounding & Lightning Protection](#5-topic-4-earthing-grounding--lightning-protection)
6. [Topic 5: Electrical Sizing & Solved Exam Numericals](#6-topic-5-electrical-sizing--solved-exam-numericals)
7. [Topic 6: Energy Efficiency Metrics (PUE & DCiE)](#7-topic-6-energy-efficiency-metrics-pue--dcie)
8. [Master Comparison Cheat Sheets (5-Mark Questions)](#8-master-comparison-cheat-sheets-5-mark-questions)
9. [15-Mark Master Model Answers (Ready to Write in Exam)](#9-15-mark-master-model-answers-ready-to-write-in-exam)

---

## 1. Unit Overview & High-Yield Exam Blueprint

### 🎯 What is this Unit About?
In simple words: **A modern data centre is a factory that turns electricity into computing power.**
If power stops for even **15 milliseconds** (less than the blink of an eye), thousands of servers crash, banks lose money, cloud services drop, and millions of users get disconnected.

This unit teaches you **how to design an electrical system that NEVER fails**:
1. **Utility Power:** Taking huge amounts of raw electricity from the city power grid safely.
2. **Backup Power:** What happens when the city grid dies? (Generators + UPS batteries).
3. **Power Distribution:** Delivering electricity safely from the roof/substation down to the tiny chip inside a server rack.
4. **Earthing & Lightning:** Preventing electric shocks, static sparks, and lightning strikes from destroying millions of dollars of silicon chips.
5. **Electrical Sizing & PUE:** How to calculate how many megawatts (MW) of power, generators, and UPS batteries you need to buy, and how green/efficient your facility is.

---

## 2. Topic 1: Utility Power Systems (Grid Intake & Transformers)

### 💡 The Big Picture Analogy:
Think of the electrical grid like a high-pressure water dam:
- The water coming from the dam is at insane pressure (132,000 Volts). If you hook your kitchen sink directly to it, your house explodes.
- You need huge reduction valves (**Step-down Transformers**) to bring the pressure down to safe drinking tap water (415V / 230V).
- And because water pipes can break during road construction, you need **two completely different pipes coming from two different water reservoirs** (**Dual Utility Feeds**).

---

### 2.1 Dual Utility Feeds (Why One Grid Connection is Never Enough)
* **What is it?** A Tier III or Tier IV data centre does NOT take power from just one power pole on the street. It connects to **two separate electrical substations**.
* **Rule of Physical Diversity:**
  - The two power cables must enter the data centre campus from **opposite sides of the property** (at least 20 meters apart).
  - *Why?* If a construction worker digging a road with a backhoe accidentally cuts one cable, the second cable is completely safe on the other side of the road!
* **The "Economic Convenience" Principle (Uptime Institute):**
  - In data centre design, the public power company is treated as an *economic convenience* (cheap power while it works), but **NOT** as a 100% reliable life support system. The data centre must be ready to run completely independently on its own generators.

---

### 2.2 Switchgear: VCB vs. GIS
When high-voltage power enters the building, you need heavy-duty switches to turn it ON or OFF, especially during a short circuit.

1. **VCB (Vacuum Circuit Breaker):**
   - *How it works:* The electrical contacts open inside an empty sealed glass tube (a vacuum). Because there is no air, no fire or spark (electric arc) can survive.
   - *Where used:* Standard indoor 11 kV or 33 kV switchrooms. Cheap, robust, and lasts 20+ years.
2. **GIS (Gas Insulated Switchgear):**
   - *How it works:* The switch is sealed inside a tank filled with **SF6 (Sulfur Hexafluoride)** gas. SF6 has super-insulating powers (3 times better than air).
   - *Why use it:* It reduces the size of the switchroom by **70%**.
   - *Analogy:* Like buying an ultra-compact laptop instead of a huge desktop tower. Essential for expensive real estate in crowded cities (like Mumbai, Singapore, London).

---

### 2.3 Transformers: Why Data Centres Use Cast Resin Dry-Type (CRT)
Transformers step down voltage from **11,000 V (11 kV) down to 415 V** (3-phase).
In the exam, you will almost certainly be asked: *Why not use normal oil transformers inside a data centre?*

| Feature | Cast Resin Dry-Type (CRT) | Oil-Cooled Transformer |
|---|---|---|
| **What cools it?** | Air circulating around solid epoxy-resin cast coils | Flammable mineral oil |
| **Fire Danger** | **Zero Fire Risk** (Self-extinguishing, F1 rated) | **High Fire Risk** (Oil can explode & burn) |
| **Can you put it indoors?** | **YES!** Right next to the computer room | **NO!** Must be outside behind concrete blast walls |
| **Environmental Risk** | Clean, dry, no leaks | Dangerous oil spills can poison groundwater |
| **Maintenance** | Just clean dust once a year | Test oil quality, check acid levels, replace silica gel |
| **Exam Verdict** | **The Industry Standard for Indoor Data Centres** | Used only in outdoor utility substations |

---

### 2.4 The K-Factor: Why Computer Servers Murder Normal Transformers
* **The Problem:** 
  Normal electrical loads (like light bulbs or electric heaters) drink electricity smoothly like drinking water through a straw (**Linear Load** - pure sine wave).  
  **Computer servers are different.** Every server has a Switch Mode Power Supply (SMPS) that takes quick, violent "bites" or "gulps" of electricity thousands of times a second (**Non-Linear Load**).
* **Harmonics:** These choppy bites create electrical noise called **Harmonics** (3rd, 5th, 7th, 9th harmonic frequencies).
  - Normal frequency = 50 Hz.
  - 3rd harmonic = 150 Hz.
  - 5th harmonic = 250 Hz.
* **Why Normal Transformers Burn Up:**
  In a transformer, heat losses (Eddy currents) increase with the **SQUARE of the frequency** ($Loss \propto f^2$). A normal building transformer exposed to server harmonics will overheat and catch fire!
* **The Solution = K-Factor Rated Transformers:**
  - K-Factor measures a transformer's ability to handle harmonic heat without burning.
  - **K-1:** Standard office building (heaters, lights).
  - **K-13:** General computer labs / office PCs.
  - **K-20:** **High-density Data Centre Server Rooms** (Thicker copper coils, double-insulated windings, oversized neutral).

---

### 2.5 Triplen Harmonics & The 200% Neutral Problem
* **What are Triplen Harmonics?** Odd multiples of the 3rd harmonic: **3rd (150 Hz), 9th (450 Hz), 15th (750 Hz)**.
* **The Danger:**
  In a normal 3-phase building, the 3 phase currents cancel each other out in the return neutral wire (Phase A + Phase B + Phase C = 0 Amps).
  **BUT Triplen harmonics DO NOT cancel out!** They march in lockstep and **ADD UP directly in the neutral wire**.
* **Exam Formula to Remember:**
  $$I_{	ext{neutral}} pprox 1.73 	imes I_{	ext{phase}}$$
  The neutral wire can carry **almost TWICE as much current as the phase wires!**
* **Engineering Solution:**
  1. Specify a **200% Oversized Neutral Conductor** (the neutral wire is twice as thick as the phase wire so it won't melt).
  2. Install **Active Harmonic Filters (AHF)** that inject counter-frequencies to cancel out distortion, keeping THD (Total Harmonic Distortion) below 5% (IEEE 519 standard).

---

## 3. Topic 2: Backup Power Systems (Generators & UPS Topologies)

### 💡 The Lifesaving Handshake Analogy:
When grid power dies:
1. The **UPS (Uninterruptible Power Supply)** is the **Sprinter**: It reacts in **0 milliseconds**, feeding servers from batteries for 10 to 15 minutes.
2. The **Diesel Generator (DG)** is the **Marathon Runner**: It takes 10 to 15 seconds to crank up, roar to speed, and take over. Once running, it can power the data centre for days or weeks as long as you pour diesel into the tank.

```
[City Grid Fails] ---> [0ms: UPS Battery Powers Servers Instantly]
                              |
                              v (takes 10-15 seconds)
                       [DG Starts & Stabilizes Voltage]
                              |
                              v (ATS Switches Over)
                       [DG Powers UPS & Coolers Continuously]
```

---

### 3.1 Diesel Generator (DG) Ratings: Why "Standby" is Banned in Tier III/IV
If you buy a cheap backup generator for an apartment complex, it comes with an **ESP (Emergency Standby Power)** rating.
- ESP rating means: "Use me for max 200 hours a year, and do not load me above 70%."
- **Uptime Institute REJECTS ESP generators** for Tier III and Tier IV data centres! Why? Because if a major storm cuts power for a week, an ESP generator will overheat and fail.
- **The Correct Rating = DCC (Data Centre Continuous):**
  - Certified by Cummins, Caterpillar, MTU, Kohler.
  - Can run at **100% full capacity for UNLIMITED hours** without stopping.
- **Fuel Storage Rule:** Data centres must store **at least 48 to 72 hours of diesel on-site** in underground bulk storage tanks, equipped with automated **fuel polishers** (filters that remove algae, water, and sludge from stored diesel).

---

### 3.2 UPS Topologies: Why "Online Double Conversion" is King
In exams, you must distinguish between the 3 UPS types:

#### 1. Standby (Offline) UPS:
- Sits asleep. Power passes straight through from the wall.
- When power fails, a mechanical relay clicks over to battery in **8 to 12 milliseconds**.
- *Verdict:* Fine for your home desktop PC. **UNACCEPTABLE for data centres** (servers may crash or reboot during the 10ms hiccup).

#### 2. Line-Interactive UPS:
- Has a small voltage booster (buck/boost transformer).
- Transfer time is **4 to 6 milliseconds**.
- *Verdict:* Used for small office network racks. Not suitable for mission-critical IT whitespace.

#### 3. Online Double Conversion (VFI - Voltage and Frequency Independent):
- **THE GOLD STANDARD FOR DATA CENTRES (0 ms Transfer Time).**
- *How it works:*
  1. **Rectifier (AC $	o$ DC):** Converts incoming dirty wall AC power into DC power.
  2. **DC Bus:** The battery bank sits permanently connected right on this DC highway.
  3. **Inverter (DC $	o$ AC):** Converts DC back into perfect, pure, brand-new 50 Hz sine-wave AC power for the servers.
- *Why is transfer time ZERO?*  
  Because the battery is **always floating on the DC line**. When the grid dies, the rectifier stops, and the battery continues feeding the inverter immediately with **no switch, no relay, and zero interruption (0.00 seconds)!**
- **Static Bypass Switch:** A built-in lightning-fast electronic switch. If the UPS inverter suffers an internal short circuit or an overload, the static bypass automatically shunts the server load directly to raw utility power in less than 2 milliseconds without rebooting the servers.

---

### 3.3 Battery Battle: VRLA (Lead-Acid) vs. Lithium-Ion (LiFePO4)

| Feature | VRLA (Valve Regulated Lead-Acid) | Lithium-Ion (LFP / NMC) |
|---|---|---|
| **Analogy** | Heavy old car battery | Sleek modern smartphone battery |
| **Weight & Floor Space** | Very heavy (requires reinforced concrete floor slabs) | **60% lighter & 50% smaller footprint** |
| **Lifespan** | 3 to 5 years (must replace often) | **12 to 15 years** (lasts lifetime of UPS) |
| **Room Temperature Need**| Must be kept cold at strictly 20°C–25°C (heat kills lead acid fast!) | Can easily handle 30°C–35°C (saves huge AC cooling money) |
| **Charge Speed** | Slow (takes 12 to 24 hours to recharge) | **Ultra-fast (recharges in 1 to 2 hours)** |
| **Smart Monitoring** | Hard to measure internal cell health | Built-in **BMS (Battery Management System)** tracks every cell |
| **Cost** | Low initial purchase price (High long-term cost) | Higher initial cost (Much lower total cost of ownership) |

---

### 3.4 Redundancy Topologies Made Super Simple

* **N (Base Capacity):**
  - Exactly what is needed to power the load. 0 spares.
  - *Analogy:* A car with 4 wheels and **no spare tire in the trunk**. If 1 tire punctures, the car stops immediately.
* **N+1 (Parallel Redundant):**
  - If your servers need 3 UPS modules of 500 kVA each ($N = 3$), you buy $3 + 1 = 4$ modules.
  - If any 1 module fails or needs oil/filter service, the other 3 carry the load without downtime.
  - *Analogy:* A car with 4 wheels + **1 spare tire in the trunk**.
* **2N (System + System / Fully Fault Tolerant):**
  - Two 100% independent electrical systems running side-by-side (Path A and Path B).
  - If your servers need 1000 kW, Path A has a 1000 kW UPS, and Path B has another 1000 kW UPS.
  - You can literally take a sledgehammer and destroy the entire Path A room, and the data centre will not drop a single packet!
  - *Analogy:* A twin-engine airplane with two separate cockpits, two fuel tanks, and two engines.
  - **Required for Uptime Institute Tier IV!**
* **2(N+1):**
  - The ultimate paranoid design. Path A has $N+1$ units, and Path B has $N+1$ units.

---

## 4. Topic 3: Power Distribution (From Substation to Server Chip)

### 💡 The Power Highway: Step-by-Step Flow
Here is the exact journey of an electron from the power station to the CPU:

```
[132 kV / 33 kV Utility Grid]
         |
         v
[Substation Transformer (11 kV -> 415 V)]
         |
         v
[Main Low Voltage Switchboard (MLVS)] <--- [Diesel Generator (DG)]
         |                                         |
         +-------------> [ ATS Switch ] <----------+
                               |
                               v
               [ Online Double Conversion UPS ]
                               |
                               v
                     [ Sub-Distribution ]
                               |
        +----------------------+----------------------+
        |                                             |
[Traditional Floor PDU]                       [Overhead Busway]
  (Heavy transformer, copper whips)             (Open track overhead)
        |                                             |
        v                                             v
[Remote Power Panel (RPP)]                    [Plug-in Tap-off Box]
        |                                             |
        +----------------------+----------------------+
                               |
                               v
                     [ Rack PDU (ePDU) ]
                     (Mounted inside rack)
                               |
            +------------------+------------------+
            | (Feed A)                            | (Feed B)
            v                                     v
       [Server PSU 1]                        [Server PSU 2]
```

---

### 4.1 ATS vs. STS: Don't Confuse These in the Exam!
This is a classic 5-mark trap question. Memorize this difference:

| Feature | ATS (Automatic Transfer Switch) | STS (Static Transfer Switch) |
|---|---|---|
| **What is inside?** | Heavy mechanical motorized switches / contactors | Solid-state **Silicon SCRs (Thyristors)** |
| **Switching Speed** | **Slow: 50 to 100 milliseconds** | **Ultra-Fast: 2 to 4 milliseconds** (1/4 cycle) |
| **Where is it placed?** | Upstream between Utility Grid and Diesel Generator | Downstream right before single-corded server racks |
| **Why can't ATS feed servers directly?** | Servers crash if power is lost for > 15ms. ATS takes 50ms! | STS switches so fast that server power supplies never notice! |
| **Maintenance** | Mechanical parts wear out; annual lubrication | Zero moving parts; solid-state electronic reliability |

---

### 4.2 Floor PDU vs. Modern Overhead Track Busway
How do you run electricity inside the white space room where thousands of servers sit?

* **Old Method: Floor-Mounted PDU with Underfloor Cables:**
  - Giant steel cabinets sit on the floor eating up expensive server space.
  - Hundreds of thick black copper cables ("whips") run underneath the raised floor tiles.
  - *Problem:* Underfloor becomes a "cable snake pit" blocking cold air circulation from reaching servers.
* **Modern Method: Overhead Open-Channel Track Busway (Starline / Schneider):**
  - Aluminum busbar tracks hang from the ceiling above the server aisles.
  - Whenever you add a new rack, an electrician simply twists a **Tap-Off Box** into the overhead track with one hand ("plug-and-play")!
  - *Advantages:* 
    - Frees up 100% of white space floor for servers.
    - Zero air obstruction under the floor.
    - Up to 40% faster deployment time.

---

### 4.3 Dual-Corded Servers (A-Feed and B-Feed)
* Every enterprise server (Dell, HP, Cisco) has **two power supplies (PSU-1 and PSU-2)** built into its back.
* PSU-1 is plugged into **Rack PDU A** (powered by UPS A).
* PSU-2 is plugged into **Rack PDU B** (powered by UPS B).
* During normal operation, each power supply shares the load: **50% from A and 50% from B**.
* **The 50% Loading Golden Rule:**
  - Neither UPS A nor UPS B must ever be loaded beyond **40% to 45%** of its capacity during normal days!
  - *Why?* If UPS A catches fire, **100% of the entire data centre load instantly dumps onto UPS B**. If UPS B was already running at 60%, it will immediately overload and trip, crashing the whole data centre!

---

## 5. Topic 4: Earthing, Grounding & Lightning Protection

### 💡 Why Earthing in Data Centres is Different:
In your house, earthing is only meant to prevent you from getting an electric shock if a toaster wire touches the metal chassis.  
In a data centre, earthing does TWO jobs:
1. **Safety (Life Protection):** Trip circuit breakers during faults so people don't die.
2. **Signal Reference Ground (Digital Health):** High-speed microprocessors switch billions of times a second. Stray high-frequency noise or static electricity (ESD) can flip binary bits ($0 	o 1$), corrupt database transactions, or fry CPU gates.

---

### 5.1 The TN-S Earthing Scheme (The Only Acceptable Scheme for IT)
* **What does TN-S stand for?**
  - **T (Terre):** The transformer neutral is directly connected to Earth.
  - **N (Neutral):** Exposed metal equipment chassis are connected to Earth via a conductor.
  - **S (Separate):** **Neutral (N) and Protective Earth (PE) are kept STRICTLY SEPARATE throughout the entire facility!**
* **Why is TN-C (Combined Neutral + Earth) strictly BANNED?**
  In TN-C, Neutral and Earth share the same wire (PEN). Harmonic currents travelling back on the neutral wire will flow across server metal chassis, shocking technicians and injecting deadly electromagnetic interference (EMI) directly into Ethernet cables!
* **Standard Ground Resistance:**
  - Normal domestic building: $< 5\ \Omega$.
  - **Tier III/IV Data Centre whitespace: $< 1\ \Omega$** (as per **IEEE 1100 Emerald Book**).

---

### 5.2 Signal Reference Grid (SRG)
* **What is it?** A giant welded grid of flat copper strips or bare copper wire laid in a **2-foot by 2-foot (60 cm $	imes$ 60 cm)** mesh across the entire concrete floor directly beneath the raised floor pedestals.
* **Why a grid instead of a single wire?**
  At high frequencies (10 MHz to 1 GHz - server speeds), a round wire has high impedance (skin effect). A 2ft $	imes$ 2ft copper mesh provides thousands of parallel paths, offering near-zero impedance ($Z pprox 0\ \Omega$) to bleed away high-frequency static noise.
* Every single server rack, cable tray, and air-conditioning unit is bolted to this SRG with short copper bonding straps.

---

### 5.3 Lightning Protection (IEC 62305 Standard)
Lightning carries up to **200,000 Amperes** and millions of Volts. If lightning hits a data centre roof, the magnetic pulse alone can erase hard drives and destroy server motherboard chips.

1. **Roof Protection (Faraday Cage & Air Terminals):**
   - Copper lightning rods (Franklin rods) placed on the roof.
   - Designed using the **Rolling Sphere Method** (imagining a 20-meter sphere rolling over the building; any point the ball touches must have a lightning conductor).
2. **Surge Protection Devices (SPDs): 3-Stage Defense Cascade:**
   - **Type 1 SPD (At Main Substation Intake):** Absorbs massive direct lightning energy surges (10/350 $\mu$s waveform).
   - **Type 2 SPD (At Distribution Panels / UPS Inputs):** Clamps residual switching surges (8/20 $\mu$s waveform).
   - **Type 3 SPD (Inside Server Rack PDUs):** Ultra-fine voltage clamping right at the server power supply input.

---

## 6. Topic 5: Electrical Sizing & Solved Exam Numericals

### 🧮 Numerical Formula Toolkit (Keep on Your Fingertips):

1. **3-Phase Apparent Power (kVA):**
   $$S = rac{\sqrt{3} 	imes V_L 	imes I_L}{1000}$$
   *(Where $V_L = 415	ext{ V}$, $\sqrt{3} pprox 1.732$)*

2. **3-Phase Real Power (kW):**
   $$P = S 	imes 	ext{Power Factor} = rac{\sqrt{3} 	imes V_L 	imes I_L 	imes \cos\phi}{1000}$$

3. **Current per Phase (Amperes):**
   $$I = rac{P	ext{ (in Watts)}}{\sqrt{3} 	imes V_L 	imes 	ext{PF}} = rac{P	ext{ (in kW)} 	imes 1000}{1.732 	imes 415 	imes 	ext{PF}}$$

4. **Facility Power from PUE:**
   $$	ext{Total Facility Power (kW)} = 	ext{IT Load (kW)} 	imes 	ext{PUE}$$

---

### 📝 Solved Problem 1: Complete Capacity Sizing for a 500-Rack Data Centre
*(Standard 10-Mark or 15-Mark University Examination Problem)*

**Problem Statement:**  
A newly planned Tier III data centre has **500 server racks**. Each rack has an average rated IT power consumption of **6 kW**.  
The target facility **PUE is 1.40**.  
The data centre uses a **2N redundant UPS architecture** with an operating efficiency of **95%** and a load power factor of **0.90 lag**.  
The backup diesel generator (DG) system must support the total facility load with **N+1 redundancy** using standard **1500 kVA generators (0.80 PF)**.  
The on-site diesel storage must sustain full facility operation for **48 continuous hours**, with a specific fuel consumption of **0.25 Litres/kWh**.

**Calculate:**
1. Total IT Whitespace Load (in kW).
2. Total Facility Power Demand (in kW).
3. Sizing and number of UPS modules required for both Path A and Path B.
4. Total number of Diesel Generators required.
5. Total capacity of the on-site bulk diesel fuel tank (in Litres).

---

#### 💡 Step-by-Step Solution:

**Step 1: Calculate Total IT Load ($P_{IT}$)**
$$P_{IT} = 	ext{Number of Racks} 	imes 	ext{Power per Rack}$$
$$P_{IT} = 500 	imes 6	ext{ kW} = \mathbf{3000	ext{ kW}}\ (3.0	ext{ MW})$$

**Step 2: Calculate Total Facility Power ($P_{	ext{Facility}}$)**
Using the PUE formula:
$$	ext{Total Facility Power} = P_{IT} 	imes 	ext{PUE}$$
$$P_{	ext{Facility}} = 3000	ext{ kW} 	imes 1.40 = \mathbf{4200	ext{ kW}}\ (4.2	ext{ MW})$$
*(Note: The extra $4200 - 3000 = 1200	ext{ kW}$ is consumed by chillers, pumps, fans, lighting, and electrical losses).*

**Step 3: Size the UPS System (2N Redundant)**
- The UPS only protects the IT equipment plus critical cooling fans, but let us size the UPS for the IT load ($3000	ext{ kW}$):
- Considering UPS efficiency $\eta = 0.95$:
  $$	ext{UPS Input Power (kW)} = rac{3000	ext{ kW}}{0.95} = 3157.9	ext{ kW}$$
- Converting kW to kVA using $	ext{PF} = 0.90$:
  $$	ext{UPS Capacity Needed (kVA)} = rac{3157.9}{0.90} pprox \mathbf{3508.8	ext{ kVA}}$$
- **Because this is a 2N architecture**, we must build **TWO completely separate systems**:
  - **Path A Capacity:** Must be able to carry the full $3509	ext{ kVA}$.
  - **Path B Capacity:** Must be able to carry the full $3509	ext{ kVA}$.
- *Module Selection:* If we select standard **1000 kVA UPS modules**:
  - For Path A: $rac{3509}{1000} = 3.51 	o$ **Four (4) $	imes$ 1000 kVA modules**.
  - For Path B: $rac{3509}{1000} = 3.51 	o$ **Four (4) $	imes$ 1000 kVA modules**.
  - **Total UPS installed = 8 modules of 1000 kVA each.**

**Step 4: Size the Diesel Generator (DG) Plant (N+1 Redundancy)**
- The generators must power the **ENTIRE facility** ($4200	ext{ kW}$), because chillers and water pumps must keep running so servers don't boil in 3 minutes!
- Each generator rating: $1500	ext{ kVA}$ at $0.80	ext{ PF}$:
  $$	ext{Power per Generator (kW)} = 1500	ext{ kVA} 	imes 0.80 = \mathbf{1200	ext{ kW}}$$
- Number of running generators required ($N$):
  $$N = rac{	ext{Total Facility Power}}{	ext{Power per Generator}} = rac{4200	ext{ kW}}{1200	ext{ kW}} = 3.5 	o \mathbf{4	ext{ Generators}}$$
- Since **N+1 redundancy** is mandated:
  $$	ext{Total Generators} = N + 1 = 4 + 1 = \mathbf{5	ext{ Generators of 1500 kVA each}}.$$

**Step 5: Calculate Bulk Diesel Fuel Tank Capacity**
- Total energy generated over 48 hours running at full $4200	ext{ kW}$ facility load:
  $$	ext{Total kWh} = 4200	ext{ kW} 	imes 48	ext{ Hours} = 201,600	ext{ kWh}$$
- Diesel needed at $0.25	ext{ Litres/kWh}$:
  $$	ext{Net Fuel Required} = 201,600	ext{ kWh} 	imes 0.25	ext{ L/kWh} = 50,400	ext{ Litres}$$
- **Safety Margin (10% Unusable Tank Sludge/Dead Bottom):**
  $$	ext{Gross Fuel Storage} = rac{50,400}{0.90} = \mathbf{56,000	ext{ Litres}}$$
- *Conclusion:* Install two $28,000	ext{ Litre}$ or three $20,000	ext{ Litre}$ underground double-walled diesel tanks.

---

### 📝 Solved Problem 2: Server Rack Breaker & Cable Sizing
**Problem Statement:**  
A high-density blade server rack draws **10 kW** of continuous power from a standard 3-phase 415 V supply at **0.95 PF**.  
As per electrical standards (NEC / IEC), continuous loads must be derated to **80% of circuit breaker rating**.  
Calculate the required 3-phase circuit breaker rating and cable size.

**Solution:**
1. Calculate full-load running current ($I$):
   $$I = rac{P}{\sqrt{3} 	imes V_L 	imes 	ext{PF}} = rac{10,000}{1.732 	imes 415 	imes 0.95} = rac{10,000}{682.8} = \mathbf{14.64	ext{ Amperes}}$$
2. Apply 80% continuous duty safety rule:
   $$	ext{Breaker Minimum Rating} = rac{14.64	ext{ A}}{0.80} = \mathbf{18.3	ext{ Amperes}}$$
3. *Breaker Selection:* Round up to the next standard commercial industrial breaker size = **32 Amp 3-Pole MCB**.
4. *Cable Selection:* A 5-core (3 Phases + 1 Double Neutral + 1 Ground) **$6	ext{ mm}^2$ Copper XLPE cable**.

---

## 7. Topic 6: Energy Efficiency Metrics (PUE & DCiE)

### 💡 What is PUE (Power Usage Effectiveness)?
PUE answers the question: **"For every 1 Watt of electricity my computer servers eat, how many Watts does the whole building waste on air conditioning, lights, and power losses?"**

$$\mathbf{PUE} = rac{	ext{Total Facility Energy (kWh)}}{	ext{IT Equipment Energy (kWh)}} = rac{P_{	ext{Facility}}}{P_{	ext{IT}}}$$

- **The Perfect PUE is 1.0:** That means 100% of the electricity goes into computers, and ZERO power is wasted on fans, chillers, or transformers.
- In reality, PUE is always greater than 1.0:
  - **PUE = 2.0 (Poor/Old):** For every 1000 W used by servers, another 1000 W is wasted on cooling!
  - **PUE = 1.5 (Average modern commercial enterprise DC).**
  - **PUE = 1.15 to 1.25 (Modern Indian Cloud DC - e.g., Navi Mumbai hyperscalers).**
  - **PUE = 1.08 (Google / Microsoft hyperscale with liquid cooling).**

### 💡 What is DCiE (Data Centre Infrastructure Efficiency)?
DCiE is simply PUE expressed in reverse as a percentage:
$$\mathbf{DCiE} = rac{1}{	ext{PUE}} 	imes 100\% = rac{	ext{IT Equipment Energy}}{	ext{Total Facility Energy}} 	imes 100\%$$

- If PUE = 2.0 $	o$ $	ext{DCiE} = rac{1}{2.0} 	imes 100\% = \mathbf{50\%}$ (Half the power is wasted).
- If PUE = 1.25 $	o$ $	ext{DCiE} = rac{1}{1.25} 	imes 100\% = \mathbf{80\%}$ (80% useful computing).

---

## 8. Master Comparison Cheat Sheets (5-Mark Questions)

### Cheat Sheet 1: ATS vs. STS
- **ATS:** Mechanical, slow (50-100ms), used between Grid and DG.
- **STS:** Solid-state SCRs, super-fast (2-4ms), used at server racks.

### Cheat Sheet 2: K-Factor Transformers vs. Normal Transformers
- **Normal Transformer:** Built for pure 50 Hz sine waves. Severely overheats and can catch fire when feeding non-linear server loads.
- **K-Factor (K-20):** Specially engineered with thicker copper windings, electrostatic shields between windings, and a 200% oversized neutral to survive harmonic Eddy current heating.

### Cheat Sheet 3: VRLA vs. Lithium-Ion Batteries
- **VRLA:** 3-5 yr life, heavy, must cool to 20°C, 12-hour recharge, cheap upfront.
- **Lithium-Ion:** 15 yr life, 60% lighter, happy at 30°C, 1-hour fast recharge, smart BMS monitoring.

### Cheat Sheet 4: Redundancy Topologies
- **N:** 0 spares. Any failure causes a blackout.
- **N+1:** 1 spare module. Safe for scheduled maintenance.
- **2N:** 2 completely independent duplicate paths (A & B). Fully fault tolerant (Tier IV).

---

## 9. 15-Mark Master Model Answers (Ready to Write in Exam)

### 🏆 Question 1:
> **"Draw the comprehensive Single Line Diagram (SLD) of a modern Tier III/IV Data Centre electrical infrastructure from utility grid intake to the server rack. Explain the role and functioning of each major component in the power path." (15 Marks)**

#### Model Answer Structure to Write in Exam:

**1. Neat Labeled Block Diagram (Draw this first!):**
```
[132 kV / 33 kV Substation A]            [132 kV / 33 kV Substation B]
             |                                        |
             v                                        v
   [RMU & MV VCB Switchgear A]              [RMU & MV VCB Switchgear B]
             |                                        |
             v                                        v
 [Step-Down Transformer A (CRT)]          [Step-Down Transformer B (CRT)]
   (11 kV -> 415 V, Dyn11, K-20)            (11 kV -> 415 V, Dyn11, K-20)
             |                                        |
             v                                        v
   [Main Switchboard 415V - A]              [Main Switchboard 415V - B]
             ^                                        ^
             |======== [ ATS / Tie Breaker ] =========|
             |                                        |
   [Backup Generator Plant A]               [Backup Generator Plant B]
      (DCC Rated, 48h Fuel)                    (DCC Rated, 48h Fuel)
             |                                        |
             v                                        v
   [Online Double Conversion                [Online Double Conversion
        UPS System - Path A]                     UPS System - Path B]
   (Rectifier -> DC -> Inverter)            (Rectifier -> DC -> Inverter)
             |                                        |
             v                                        v
  [Overhead Busway Track A]                [Overhead Busway Track B]
             |                                        |
             v                                        v
   [Tap-off Box -> ePDU A]                  [Tap-off Box -> ePDU B]
             |                                        |
             +-------------> [ SERVER RACK ] <--------+
                        Dual Power Supplies
                        (PSU 1 on Feed A)
                        (PSU 2 on Feed B)
```

**2. Step-by-Step Component Explanations:**
1. **Dual Utility Grid Feeds:** Sourced from two independent electrical substations with diverse physical trench routes (> 20m separation) to eliminate single points of failure.
2. **Ring Main Unit (RMU) & VCB Switchgear:** Provides ring loop connectivity, fault isolation, and circuit breaking via vacuum interrupters.
3. **Cast Resin Dry-Type Step-Down Transformers:** Step down 11 kV to 415 V / 230 V. Cast in non-flammable epoxy resin (F1 fire rated) for safe indoor whitespace installation; K-20 rated to safely absorb server harmonic currents.
4. **Automatic Transfer Switch (ATS):** Automatically cranks and transfers the facility load from utility grid to Diesel Generators within 10 to 15 seconds during a blackout.
5. **DCC-Rated Diesel Generators:** Standby sustained power source with 48–72 hours of on-site fuel polishing storage, capable of running continuously at 100% load.
6. **Online Double Conversion UPS (VFI):** Eliminates all voltage sags, spikes, and surges by converting AC $	o$ DC $	o$ AC. Provides **0 ms transfer time** using battery banks (VRLA or Li-ion) to sustain IT loads while generators start.
7. **Overhead Open Track Busways & PDUs:** Distribute 415V power above server racks with plug-and-play tap-off boxes, avoiding underfloor airflow blockages.
8. **Rack PDUs & Dual-Corded Servers:** Mounted vertically in server racks; feed dual power supply units (PSU-1 and PSU-2), operating in a 50/50 load-sharing configuration.

---

### 🏆 Question 2:
> **"Explain the earthing and lightning protection requirements for a Tier IV data centre. Why is the TN-S scheme mandated and what is the role of a Signal Reference Grid (SRG)?" (10 to 15 Marks)**

#### Model Answer Structure to Write in Exam:
1. **Introduction:** Earthing in data centres is dual-purpose: life safety and high-frequency digital signal reference (IEEE 142 & IEEE 1100).
2. **The TN-S Earthing Scheme:**
   - Draw the diagram showing Transformer Neutral bonded to Earth at only ONE point (the source).
   - Show how the **Neutral (N)** wire and **Protective Earth (PE)** wire are kept strictly separated throughout the whole building.
   - Explain why **TN-C is prohibited**: Combined neutral-earth allows non-linear harmonic currents to flow through server metal cabinets, creating shock hazards and electromagnetic interference (EMI) that corrupts network packets.
3. **Signal Reference Grid (SRG):**
   - Explain the 2ft $	imes$ 2ft copper mesh laid under the raised floor.
   - Contrast DC resistance vs. High-Frequency Impedance ($Z = R + j\omega L$). At 100 MHz, a long ground wire acts as an open antenna; the SRG mesh provides low impedance to instantly dissipate static charges (ESD) and radio-frequency noise.
4. **Lightning Protection & Surge Protection Cascade:**
   - Mention **IEC 62305** and the Rolling Sphere method.
   - Detail the 3-stage SPD cascade: Type 1 at intake (direct strike absorption), Type 2 at panels (switching surge protection), and Type 3 at server rack PDUs (delicate microchip clamping).
