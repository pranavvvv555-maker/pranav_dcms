# UNIT 2: Data Centre Electrical Infrastructure Engineering
### Comprehensive Master Examination Notes & Study Guide (6-Hour Curriculum)
**Course:** M.Tech Computer Science & Engineering (Data Centre Systems Engineering)  
**Institutions:** MIT World Peace University (MIT-WPU) × NIRVAA Solution Pvt. Ltd.  
**Academic Year:** 2026–2027  

---

## Table of Contents
1. [Unit Syllabus & Examination Weightage](#1-unit-syllabus--examination-weightage)
2. [Module 1: Utility Power Systems & Grid Connectivity](#2-module-1-utility-power-systems--grid-connectivity)
3. [Module 2: Backup Power Systems (DG Sets & UPS Topologies)](#3-module-2-backup-power-systems-dg-sets--ups-topologies)
4. [Module 3: End-to-End Power Distribution Architectures (SLD)](#4-module-3-end-to-end-power-distribution-architectures-sld)
5. [Module 4: Earthing, Grounding & Lightning Protection](#5-module-4-earthing-grounding--lightning-protection)
6. [Module 5: Electrical Design Methodology & Capacity Sizing](#6-module-5-electrical-design-methodology--capacity-sizing)
7. [Module 6: Energy Efficiency Metrics (PUE, DCiE) & Tier Standards](#7-module-6-energy-efficiency-metrics-pue-dcie--tier-standards)
8. [Module 7: Solved University Numerical Problems](#8-module-7-solved-university-numerical-problems)
9. [Module 8: High-Yield Comparison Tables (5-Mark Short Notes)](#9-module-8-high-yield-comparison-tables-5-mark-short-notes)
10. [Module 9: Master Question Bank & 15-Mark Model Answers](#10-module-9-master-question-bank--15-mark-model-answers)

---

## 1. Unit Syllabus & Examination Weightage

### Prescribed Syllabus (6 Contact Hours)
> **UNIT 2: Data Centre Electrical Infrastructure Engineering**  
> Utility Power System, Backup Power Systems, Power Distribution, Earthing and Lightning Protection, and Electrical Design.

### Exam Mark Distribution Blueprint
| Section | Topic Focus | Expected Question Types | Typical Marks |
|---|---|---|---|
| **Part A** | Utility & Grid Intake, Transformers, RMU, Power Quality | 5-Mark Short Notes, Definitions | 5 – 10 Marks |
| **Part B** | Backup Power, DG Sets, UPS Topologies, Battery Tech | 10-Mark Essay, Comparison Tables | 10 – 15 Marks |
| **Part C** | Power Distribution, SLD, PDUs, Overhead Busway | 15-Mark Comprehensive Architecture | 10 – 15 Marks |
| **Part D** | Earthing, TN-S Scheme, SRG Mesh, Lightning Protection | 5-Mark Short Note, 10-Mark Explanatory | 5 – 10 Marks |
| **Part E** | Electrical Sizing Calculations, PUE Numericals | 10-Mark Numerical Problem | 10 Marks |

---

## 2. Module 1: Utility Power Systems & Grid Connectivity

### 2.1 Utility Grid Intake Architecture
Data centres are among the most power-dense facilities on Earth, consuming anywhere from 5 MW to over 100 MW of continuous power. Therefore, utility power is delivered at **High Voltage (HV)** or **Medium Voltage (MV)**:
- **Voltage Levels:** Delivered at 132 kV, 66 kV, 33 kV, or 11 kV; stepped down on-site to 415 V (3-phase, 4-wire, 50 Hz).
- **Dual Utility Feeds (Dual Substation Feeds):**
  - High-availability data centres (Tier III and Tier IV) mandate **two independent utility feeds**.
  - **Diversity Requirement:** The feeds must originate from **two distinct utility transmission substations** fed by independent power generation grids.
  - **Diverse Physical Routing:** Utility underground cables must enter the property boundary from geographically opposite directions (> 20 meters physical separation) to prevent a single backhoe/civil excavation trench cut from severing both feeds.

### 2.2 Switchgear & Ring Main Units (RMU)
- **Ring Main Unit (RMU):** A factory-assembled, metal-enclosed switchgear unit used at the intake point. It connects the data centre to a ring/loop distribution feeder. If a fault occurs on one section of the utility loop, the RMU isolates the faulted section while continuously supplying the data centre from the alternate side of the ring.
- **Circuit Breaker Types:**
  - **Vacuum Circuit Breakers (VCB):** Arc extinction takes place in a high-vacuum bottle. Highly reliable, minimal maintenance, standard choice for 11 kV / 33 kV indoor switchgear.
  - **SF6 Gas Insulated Switchgear (GIS):** Uses Sulfur Hexafluoride gas for insulation. Reduces physical footprint by up to 70% compared to Air Insulated Switchgear (AIS); ideal for space-constrained urban multi-story data centres.

### 2.3 Substation Step-Down Transformers
Transformers step down the incoming voltage (e.g., 33 kV or 11 kV) to the utilization voltage (415 V line-to-line, 230 V line-to-neutral).

#### Cast Resin Dry-Type (CRT) vs. Oil-Immersed Transformers
| Feature | Cast Resin Dry-Type (CRT) | Oil-Immersed (ONAN / ONAF) |
|---|---|---|
| **Cooling Medium** | Air convection around epoxy resin cast coils | Mineral oil or synthetic ester oil |
| **Fire Safety** | **Non-flammable, self-extinguishing** (F1 fire rating). Can be installed directly indoors adjacent to IT whitespace. | **Flammable liquid.** Mandates outdoor placement with blast-proof firewall barriers and oil-containment sump pits. |
| **Environmental Hazard**| Zero toxic spills; zero soil contamination risk. | Risk of oil leakages, spills, and water-table contamination. |
| **Maintenance** | Virtually maintenance-free (periodic dusting/vacuuming). | Regular oil breakdown voltage (BDV) testing, dissolved gas analysis (DGA), silica gel breather replacement. |
| **Efficiency & Cost** | Slightly higher CAPEX; higher internal losses at heavy loads. | Lower initial cost; marginally higher efficiency at extreme mega-scale (> 5 MVA). |
| **Data Centre Preference**| **Standard choice for indoor DC substations.** | Used primarily in remote hyperscale outdoor sub-stations. |

#### Crucial Concept: K-Factor Rated Transformers
- Modern data centre IT equipment uses **Switch Mode Power Supplies (SMPS)**.
- SMPS are **non-linear loads** that draw current in short, sharp, non-sinusoidal pulses, generating intense harmonic currents (predominantly 3rd, 5th, 7th, 9th harmonics).
- **Eddy Current Losses** in transformer windings increase proportional to the **square of the frequency** ($P_{eddy} \propto f^2 \times I^2$). Standard commercial transformers overheat rapidly and suffer catastrophic insulation breakdown under harmonic loads.
- **K-Factor Definition:** A weighting factor indicating a transformer's ability to handle non-linear harmonic load currents without exceeding its thermal insulation temperature rating:
  $$\text{K-Factor} = \sum_{h=1}^{h_{max}} I_h^2 \times h^2$$
  *(where $h$ is the harmonic order and $I_h$ is the fraction of total RMS current).*
- **Data Centre Recommendation:**
  - **K-13:** Standard computer rooms and administrative offices.
  - **K-20:** High-density IT server whitespace and Power Distribution Units (PDUs).

### 2.4 Power Quality, Harmonics & Power Factor
1. **Voltage Sags and Swells:** Voltage sags (voltage drops between 10% and 90% for 0.5 cycles to 1 minute) account for over 80% of all utility power quality disturbances, typically caused by remote grid short-circuits or heavy industrial motor startups.
2. **Triplen Harmonics & Neutral Overheating:**
   - Triplen harmonics are odd multiples of the 3rd harmonic (3rd, 9th, 15th, 21st...).
   - In a balanced 3-phase 4-wire system, fundamental 50 Hz currents cancel out in the neutral. However, **triplen harmonics are in-phase (zero-sequence)** and **arithmetically add together in the neutral conductor**.
   - As a result, the neutral current can exceed the phase current ($I_{neutral} > 1.73 \times I_{phase}$).
   - **Mitigation:** Data centre engineers specify **200% rated double-sized neutral conductors** ($2 \times$ copper cross-section) and Dyn11 delta-wye isolation transformers to trap triplens inside the primary delta winding.
3. **Active Harmonic Filters (AHF):** Connected in parallel at main switchboards. Measures harmonic distortion in real time using DSPs and injects exact counter-phase cancelation currents, keeping Total Harmonic Distortion (THDi) **below 5%** in strict compliance with **IEEE 519**.
4. **Automatic Power Factor Correction (APFC):** Modern server power supplies have built-in active PFC (PF > 0.98). However, data centre mechanical equipment (chillers, pumps, fans) is inductive (PF $\approx$ 0.80–0.85). Detuned capacitor banks (with series reactors to prevent harmonic resonance) maintain the overall facility power factor at **0.95 to 0.99 lag**.

---

## 3. Module 2: Backup Power Systems (DG Sets & UPS Topologies)

### 3.1 Diesel Generator (DG) Systems
According to the **Uptime Institute**, the public utility grid is considered an "economic convenience", not a reliable power source. The on-site generator plant is the **primary sustained power provider** during any grid interruption.

#### DG Power Ratings (ISO 8528-1 Standard)
1. **Emergency Standby Power (ESP):** Max power for the duration of a utility outage. Max 200 hours/year; average load factor must not exceed 70%. *Not acceptable for Tier III/IV certifications!*
2. **Prime Power (PRP):** Unlimited hours of operation per year with variable load.
3. **Data Centre Continuous (DCC) Rating:** Specialized rating introduced by leading engine manufacturers (Cummins, Caterpillar, Kohler) specifically for data centres. Permits operation at **100% rated capacity for unlimited hours** with no average load restriction, qualifying for Uptime Institute Tier III and Tier IV without derating.

#### Key Subsystems of Data Centre Generators
- **Fast Black-Start Capability:** In accordance with **NFPA 110 Type 10**, generators must crank, fire, achieve nominal voltage and frequency, synchronize, and accept 100% block load within **10 seconds** of utility power failure.
- **Engine Jacket Water Pre-Heaters:** Maintains coolant water temperature at 40°C to 50°C 24/7 so the engine is immediately ready for instant full-load acceptance without thermal shock or combustion lag.
- **Dual Starting Battery Banks:** 24 V DC starter batteries arranged with redundant starter motors and dual battery chargers.
- **Fuel Autonomy Strategy:**
  - **Day Tank:** Located inside the generator acoustic room, gravity-feeding the engine. Sized for **1 to 2 hours** of runtime. Equipped with automated fill valves, level transmitters, and overflow return lines.
  - **Bulk Fuel Storage:** Massive underground or double-walled aboveground storage tanks storing sufficient diesel fuel for **48 to 72 hours of full continuous operation** without refuelling.
  - **Fuel Polishing System:** Continuous multi-stage filtration circulating diesel fuel through particulate filters and water coalescers to prevent fuel degradation, sludge, and bacterial contamination.

---

### 3.2 Uninterruptible Power Supply (UPS) Systems
The UPS serves as the zero-break energy bridge that powers the servers during the critical **10 to 15 seconds** it takes for the diesel generators to start, synchronize, and accept load.

#### Online Double Conversion UPS (VFI Topology)
Classified under **IEC 62040-3** as **VFI (Voltage and Frequency Independent)**:

```
[ Raw Utility / DG AC ] 
         │
         ▼
 ┌───────────────┐        DC Bus (400V - 600V DC)        ┌───────────────┐
 │   RECTIFIER   │──────────────────────────────────────►│    INVERTER   │────► [ Clean AC to IT Load ]
 │   / CHARGER   │                  ▲                    │   (PWM/IGBT)  │
 └───────────────┘                  │                    └───────────────┘
                                    │
                         ┌────────────────────┐
                         │    BATTERY BANK    │
                         │ (VRLA / Li-Ion)    │
                         └────────────────────┘
```

#### Operating Modes:
1. **Normal Mode:** Utility AC power enters the **Rectifier/Charger**, which converts AC to DC. The DC power float-charges the battery bank and simultaneously feeds the **Inverter**. The Inverter uses high-speed IGBT PWM switching to synthesize a flawless, pure sinusoidal 415/230V AC output to the server load.
2. **Battery Mode (Stored Energy Mode):** When utility AC fails, the DC bus is instantaneously powered by the battery bank. Because the battery is permanently connected across the DC bus, the transfer time to battery power is **identically 0.00 milliseconds (Zero Transfer Time)**!
3. **Static Bypass Mode:** If the inverter experiences an internal component fault, excessive overtemperature, or a downstream short-circuit draw, the solid-state **Static Bypass Switch (SCR thyristors)** transfers the load to raw utility bypass in **< 2 milliseconds** without breaking server operation.
4. **Maintenance Bypass Mode:** A manual mechanical wrap-around switch that completely routes power around the entire UPS cabinet, de-energizing internal electronics so technicians can safely service components without interrupting IT operations.

#### Rotary / Dynamic UPS (DRUPS)
- Integrates a **diesel engine**, a high-speed **kinetic energy flywheel**, and a **synchronous motor-generator** mounted along a single mechanical shaft.
- When mains power drops, the spinning flywheel releases its stored kinetic energy through an electromagnetic clutch, spinning the generator to sustain the electrical load for **15 to 30 seconds** while the diesel engine cranks and engages.
- **Pros:** Eliminates hazardous chemical battery rooms, requires no HVAC cooling for batteries, extremely durable (20+ year lifespan).
- **Cons:** High mechanical friction wear, periodic bearing maintenance, loud acoustic noise, expensive initial investment.

---

### 3.3 Battery & Energy Storage Technologies

| Evaluation Parameter | VRLA (Lead-Acid) | Lithium-Ion (LFP) | Kinetic Flywheel |
|---|---|---|---|
| **Storage Chemistry / Mechanism** | Lead plates in dilute sulfuric acid immobilized in AGM/Gel | Lithium Iron Phosphate ($LiFePO_4$) cathode with carbon anode | High-speed carbon-composite rotor in magnetic vacuum bearings |
| **Typical Service Lifespan** | **3 to 5 Years** | **12 to 15 Years** | **20+ Years** |
| **Weight & Floor Loading** | Extremely heavy (800 – 1000 kg/m²); requires structural reinforcement | **60% lighter**; can be installed on standard raised floors | Compact, but high point-load concentration |
| **Footprint Area** | Bulky; requires dedicated fire-separated battery rooms | **70% smaller footprint**; can be placed directly in IT room | Smallest footprint |
| **Operating Temperature** | Highly sensitive: **20°C – 25°C**. (Every 10°C rise cuts lifespan by 50%!) | Tolerates **up to 30°C** with negligible cycle degradation | Ambient tolerant (0°C – 40°C) |
| **Cycle Life (80% DoD)** | 200 to 400 cycles | 2,000 to 3,000 cycles | **> 1,000,000 cycles** |
| **Autonomy Duration** | 10 to 15 minutes | 10 to 15 minutes | **15 to 30 seconds** |
| **BMS Integration** | Optional external sensors; prone to undetected cell failures | **Integrated digital BMS** reporting cell voltage, Temp, SoH, SoC | Integrated digital monitoring |
| **Thermal Runaway Risk** | Moderate (acid drying, plate sulfation) | Mitigated via stable LFP chemistry + smart disconnects | **Zero risk** (no chemical storage) |
| **10-Year TCO** | High (requires 2 to 3 complete replacements) | **Lowest TCO** (zero cell replacement over 10-15 years) | Moderate (periodic bearing overhaul) |

---

### 3.4 Electrical Redundancy Topologies

```
N Capacity Topology:
[ Utility / Gen ] ──► [ UPS 1 ] ──► [ Critical IT Load ]

N+1 Parallel Redundant:
[ Utility / Gen ] ──┬──► [ UPS 1 ] ──┬──► [ Critical IT Load ]
                    ├──► [ UPS 2 ] ──┤
                    └──► [ UPS +1] ──┘ (Common Output Bus = SPOF)

2N System Redundant (Dual Path):
[ Utility A / DG A ] ──► [ UPS A ] ──► [ PDU A ] ──► [ Rack PDU A ] ──┐
                                                                       ├──► [ Server Dual PSUs ]
[ Utility B / DG B ] ──► [ UPS B ] ──► [ PDU B ] ──► [ Rack PDU B ] ──┘
```

#### Detailed Topologies Breakdown:
1. **N (Base Capacity):** The minimum equipment capacity required to power the IT design load. No redundancy. Any single equipment failure causes immediate IT downtime. *(Tier I)*
2. **N+1 (Component Redundancy):** $N$ units handle the load, plus $1$ extra unit is idle/sharing load to act as a reserve. If one UPS module fails, the remaining $N$ units seamlessly absorb the load.
   - *Limitation:* All units feed into a **single common paralleling bus**. A busbar fault or main breaker failure drops the entire data centre. *(Tier II)*
3. **2N (System Redundancy / Dual Path):** Two completely independent, physically isolated distribution systems (System A and System B). Each system is capable of supplying 100% of the maximum critical load alone. Under normal conditions, each system runs at **40% to 50% load**.
   - *Key Advantage:* **Concurrent Maintainability and Fault Tolerance.** Any component on Path A (transformer, switchboard, UPS, PDU) can be taken offline for maintenance without interrupting power on Path B. *(Tier IV)*
4. **Distributed Redundancy (3N/2 or 3-to-make-2):** Uses 3 UPS systems to feed 2 separate load buses. Each UPS operates at a maximum load of 66.7%. If any 1 UPS fails, Static Transfer Switches (STS) transfer load so the remaining 2 UPS units operate at 100%. Provides $N+1$ redundancy across distribution paths while reducing stranded, unutilized capacity compared to 2N. *(Tier III)*

---

## 4. Module 3: End-to-End Power Distribution Architectures (SLD)

### 4.1 Master Single Line Diagram (SLD) Flow
In an examination, you are expected to sketch the complete electrical single line diagram from the utility substation down to the server components:

```
[ Utility Grid Feed A (33kV) ]                  [ Utility Grid Feed B (33kV) ]
              │                                               │
    [ RMU & VCB Breaker A ]                         [ RMU & VCB Breaker B ]
              │                                               │
 [ Step-Down Transformer A (K-20) ]              [ Step-Down Transformer B (K-20) ]
              │                                               │
              ▼                                               ▼
     ┌─────────────────┐                             ┌─────────────────┐
     │  ATS Unit A     │◄── [ DG Plant A ]           │  ATS Unit B     │◄── [ DG Plant B ]
     └────────┬────────┘                             └────────┬────────┘
              │                                               │
     [ Main LV Switchgear A ]                        [ Main LV Switchgear B ]
              │                                               │
              ▼                                               ▼
     [ 2N UPS Plant A (VFI) ]                        [ 2N UPS Plant B (VFI) ]
              │ (Battery Bank A)                              │ (Battery Bank B)
              ▼                                               ▼
     [ Floor PDU System A ]                          [ Floor PDU System B ]
       (Isolation Tx & BCMS)                           (Isolation Tx & BCMS)
              │                                               │
              ▼                                               ▼
   [ Overhead Busway Run A ]                       [ Overhead Busway Run B ]
              │                                               │
    (Tap-Off Box A - 32A)                           (Tap-Off Box B - 32A)
              │                                               │
              ▼                                               ▼
   [ Intelligent Rack PDU A ]                      [ Intelligent Rack PDU B ]
              │                                               │
              └───────────────┬───────────────────────────────┘
                              │
                    ┌──────────────────┐
                    │ SERVER CABINET   │
                    │                  │
                    │ [PSU 1]  [PSU 2] │  <-- Dual-Corded Server
                    │ (Feed A) (Feed B)│
                    └──────────────────┘
```

---

### 4.2 Power Distribution Units (PDUs) vs. Overhead Track Busways

#### Traditional Floor-Standing PDUs with RPPs
- **Floor PDU:** Contains an input circuit breaker, an internal **Delta-Wye isolation transformer (K-20 rated)**, transient surge suppressors, and multi-circuit branch monitoring.
- **Remote Power Panel (RPP):** A secondary breaker panel fed from the PDU, placed at the end of server rows.
- **Underfloor Whips:** Flexible liquid-tight conduits containing copper wires run from the RPP beneath the raised floor to individual server racks.
- **Major Disadvantages:**
  - Cable congestion beneath the raised floor blocks the cold air plenum, starving server racks of cooling.
  - Adding or modifying server circuits requires pulling new cables under live floors, risking accidental disruption of adjacent cables.

#### Modern Overhead Track Busway Systems
- **Concept:** Continuous aluminum or copper busbars housed inside an extruded aluminum housing suspended from the structural ceiling directly above server rack aisles.
- **Plug-In Tap-Off Boxes:** Modular electrical tap-off units equipped with branch circuit breakers (e.g., 32A 3-phase) and drop cables (with IEC 309 or twist-lock connectors).
- **Hot-Swappable Installation:** Tap-off boxes can be mechanically inserted and turned 90 degrees to lock into the energized busbar **while the busway is completely live**, without interrupting adjacent racks!
- **Benefits for Data Centres:**
  - **Zero Floor Space:** Frees up expensive white space floor tiles for additional compute racks.
  - **Zero Airflow Obstruction:** Keeps the underfloor or overhead cold-air plenum completely clear, reducing fan power and improving PUE.
  - **Speed of Deployment:** Cuts tenant server provisioning time from days to minutes.

---

### 4.3 Rack Power Distribution (ePDUs) & Dual-Cording
- **Intelligent Rack PDUs (ePDUs):**
  - **Basic PDU:** Standard multi-outlet distribution strip.
  - **Metered PDU:** Digital local display showing total current; helps technicians balance phase currents and prevent breaker overloads.
  - **Monitored PDU:** Integrated network interface (SNMP, Modbus TCP, HTTP). Meters voltage, current, active power (kW), energy (kWh), and power factor per phase and per branch.
  - **Switched PDU:** Offers individual outlet-level power switching. Allows system administrators to reboot crashed servers remotely and lock unassigned outlets to prevent unauthorized plug-ins.
- **Dual-Corded Server Redundancy:**
  - Enterprise servers feature **two power supply units (PSU 1 and PSU 2)**.
  - PSU 1 connects to **Rack PDU A** (powered by UPS Path A).
  - PSU 2 connects to **Rack PDU B** (powered by UPS Path B).
  - Under normal conditions, both PSUs share the load (e.g., 50% each). If UPS Path A drops completely, PSU 2 instantly transitions to draw 100% of the load without causing a single CPU reset or packet drop!

---

## 5. Module 4: Earthing, Grounding & Lightning Protection

### 5.1 Objectives of Data Centre Grounding (IEEE 1100)
Grounding in data centres is governed by **IEEE 1100 (The Emerald Book)** and serves three distinct functions:
1. **Personnel Life Safety Grounding:** Provides a low-impedance path to ground so high fault currents quickly trigger upstream circuit breakers and protective relays within milliseconds, preventing hazardous touch/step voltages (> 50 V).
2. **System Neutral Grounding:** Establishes a stable reference point for the neutral conductor, eliminating phase-to-ground floating voltages.
3. **High-Frequency Signal Reference Grounding:** Shields delicate microelectronics (CMOS chips operating at 1.0 V – 3.3 V logic levels) from common-mode electrical noise, electromagnetic interference (EMI), radio frequency interference (RFI), and electrostatic discharge (ESD).

---

### 5.2 The TN-S Earthing System
In data centre electrical distribution, the **TN-S (Terre-Neutre Séparé)** scheme is strictly mandatory:

```
Substation Transformer Secondary (Dyn11)
  Neutral (N) ───┬──────────────────────────────────────────► (Separate Neutral N to Load)
                 │  <-- Single Point of N-PE Bond
  Earth (PE)  ───┴────────┬─────────────────────────────────► (Separate Protective Earth PE)
                          │
                   [ Earth Pit ]
```

- **Separate Conductors:** The Neutral ($N$) conductor and Protective Earth ($PE$) conductor are run physically separated throughout the entire installation.
- **Single Neutral-Earth Bond:** The neutral conductor is bonded to the protective earth system **at one single point only** (at the secondary neutral of the substation transformer or PDU isolation transformer).
- **Why TN-C (Combined PEN) is Strictly Prohibited:**
  - In a TN-C system, the Neutral and Earth are combined into a single PEN conductor.
  - Non-linear server triplen harmonic currents traveling along the PEN conductor create voltage drops ($V = I_{harmonic} \times Z_{PEN}$).
  - This turns every metal server chassis, cable tray, and rack into an active electrical conductor carrying stray return currents, creating **shock hazards**, ground loops, and continuous data packet corruption across shielded Ethernet cables!

---

### 5.3 Signal Reference Grid (SRG)
- **The High-Frequency Grounding Challenge:**
  - At 50 Hz power frequency, copper wire resistance dominates ($Z \approx R$). A standard green grounding wire works well.
  - At high frequencies (MHz/GHz microprocessor clock harmonics and lightning nanosecond rise-times), **inductive reactance dominates**:
    $$X_L = 2\pi f L$$
  - A 10-meter grounding conductor exhibits massive inductive impedance at 10 MHz ($X_L > 1000\ \Omega$), behaving as an open circuit and acting as an antenna that radiates noise into servers!
- **The SRG Solution:**
  - A prefabricated **2 ft × 2 ft (600 mm × 600 mm) welded copper strip or wire mesh** laid beneath the entire raised floor footprint.
  - **Physics Principle:** The mesh creates thousands of interconnected parallel current loops. Inductances in parallel divide:
    $$\frac{1}{L_{total}} = \frac{1}{L_1} + \frac{1}{L_2} + \dots + \frac{1}{L_n}$$
    This drives the high-frequency impedance down to near **zero ohms** across all radio-frequency bands!
- **Equipotential Bonding:** Every server rack, PDU chassis, raised floor pedestal, and overhead metal cable ladder is bonded to the nearest node of the SRG using short, wide, flat **braided copper bonding straps** (minimum width 25 mm).

---

### 5.4 Earth Pit Construction & Resistance Limits
- **Chemical Earth Pits:** Traditional charcoal-and-salt pits corrode and dry out. Data centres mandate maintenance-free **chemical earth electrodes** consisting of high-conductivity copper-bonded steel rods (minimum 250 microns copper molecularly bonded to high-tensile steel) surrounded by a non-shrink, hygroscopic **carbon/bentonite conductive backfill compound**.
- **Earth Resistance Standard:**
  - General Commercial Buildings: $< 5.0\ \Omega$
  - **Data Centres (IEEE 1100 / IEEE 142):** Strictly **$< 1.0\ \Omega$** (Gold standard design target: **$< 0.5\ \Omega$**).
- **Ring Earth Electrode:** Multiple chemical earth pits are interconnected in a continuous subterranean closed loop (ring) around the building perimeter using $50\text{ mm} \times 6\text{ mm}$ bare copper tape.

---

### 5.5 Lightning Protection System (LPS) & Cascaded SPDs
Governed by international standard **IEC 62305** (Protection against lightning):

#### External Lightning Protection
- **Air Termination Network:** Roof-mounted metallic mesh (Faraday cage conductor network) combined with vertical air terminal rods sized using the **Rolling Sphere Method** (Sphere radius $R = 20\text{ m}$ for Class I protection).
- **Down Conductors:** Heavy-gauge, low-inductance copper conductors routed around the building perimeter at regular intervals (< 10 meters apart) to conduct strike currents directly into the earth ring.

#### Internal Lightning Protection: 3-Stage Cascaded SPDs
Surge Protection Devices (SPDs) must be coordinated in three distinct stages:

```
[ External Strike ]
        │
        ▼
[ Main Substation Switchgear ] ──► [ Type 1 (Class I) SPD ]  (10/350 µs impulse; I_imp ≥ 25 kA)
        │
        ▼
[ Floor PDU & UPS Panels ]    ──► [ Type 2 (Class II) SPD ] (8/20 µs transient; I_n ≥ 20 kA)
        │
        ▼
[ Server Rack ePDUs ]          ──► [ Type 3 (Class III) SPD ] (Combined wave; U_oc ≤ 1.0 kV)
```

1. **Type 1 (Class I) SPD:** Installed at the main service intake switchgear. Tested with the **10/350 µs lightning current waveform**. Diverts high-energy direct lightning stroke currents ($I_{imp} \ge 25\text{ kA}$ per phase).
2. **Type 2 (Class II) SPD:** Installed at floor PDUs and UPS distribution panels. Tested with the **8/20 µs waveform**. Clamps inductive switching surges and residual transients passed by Type 1 units.
3. **Type 3 (Class III) SPD:** Installed directly inside Rack PDUs. Clamps fine transient overvoltages down to a residual let-through voltage **$< 1000\text{ V}$**, completely safe for delicate microprocessors.

---

## 6. Module 5: Electrical Design Methodology & Capacity Sizing

### 6.1 Complete Sizing Pipeline Algorithm
When designing or answering a numerical sizing question on data centre electrical infrastructure, follow this sequential 7-step pipeline:

```
[ Step 1: Compute IT Active Load (kW) ]
        │  P_IT = Rack Count × kW per Rack
        ▼
[ Step 2: Determine IT Apparent Power (kVA) ]
        │  S_IT = P_IT / Power Factor
        ▼
[ Step 3: Size UPS System (kVA) ]
        │  S_UPS = S_IT / (Inverter Efficiency × Max Load Factor)
        ▼
[ Step 4: Size UPS Battery Storage (Ah) ]
        │  Ah = (DC Watts / DC Voltage) × Hours × Derating Factor
        ▼
[ Step 5: Compute Total Facility Power (kW) ]
        │  P_Total = P_IT × Design PUE
        ▼
[ Step 6: Size Substation Transformers (kVA) ]
        │  S_Tx = (P_Total / Facility PF) × Growth Margin (1.25)
        ▼
[ Step 7: Size Standby Diesel Generators (kVA) ]
           S_DG = S_Tx × Generator Sizing Factor (1.30 to 1.50)
```

---

## 7. Module 6: Energy Efficiency Metrics (PUE, DCiE) & Tier Standards

### 7.1 PUE & DCiE Formulations
Established by **The Green Grid**:

$$\text{PUE} = \frac{\text{Total Facility Energy}}{\text{IT Equipment Energy}}$$

$$\text{DCiE} = \frac{\text{IT Equipment Energy}}{\text{Total Facility Energy}} \times 100\% = \frac{1}{\text{PUE}} \times 100\%$$

- **Total Facility Energy includes:**
  1. IT Equipment Power (Servers, Storage arrays, Core Network Switches, Routers).
  2. Mechanical Cooling (Chillers, Cooling Towers, CRAH/CRAC units, Water pumps).
  3. Electrical Infrastructure Losses (Transformer core losses, UPS double-conversion losses, PDU losses, cable resistance $I^2R$ drops).
  4. Auxiliary Loads (Lighting, Fire suppression systems, Security, BMS).
- **Physical Interpretation:**
  - $\text{PUE} = 1.0$: Absolute theoretical perfection (100% of incoming energy powers computing; zero energy wasted).
  - $\text{PUE} = 2.0$: 1 kW wasted on cooling and power losses for every 1 kW delivered to servers ($\text{DCiE} = 50\%$).
  - **Modern Enterprise Target:** $\text{PUE} = 1.25\text{ to }1.40$ ($\text{DCiE} = 71\%\text{ to }80\%$).
  - **Hyperscale Cloud (Google, AWS):** $\text{PUE} = 1.10\text{ to }1.15$ ($\text{DCiE} = 87\%\text{ to }91\%$).

---

### 7.2 Uptime Institute Tier Classification Summary

| Tier Level | Redundancy Topology | Distribution Paths | Concurrent Maintainability | Fault Tolerance | Maximum Allowable Downtime / Year | Target Availability |
|---|---|---|---|---|---|---|
| **Tier I** | **N** (No redundancy) | 1 Path | **No** | **No** | **28.8 Hours** | **99.671%** |
| **Tier II** | **N+1** (Components only) | 1 Path | **No** | **No** | **22.0 Hours** | **99.741%** |
| **Tier III**| **N+1** (Components & Paths)| 1 Active, 1 Alternate | **YES** | **No** (unplanned events cause drop) | **1.6 Hours** | **99.982%** |
| **Tier IV** | **2(N+1) or 2N** | 2 Simultaneously Active | **YES** | **YES** (survives any unplanned failure) | **26.3 Minutes** | **99.995%** |

---

## 8. Module 7: Solved University Numerical Problems

### Numerical Problem 1: UPS & Battery Sizing (10 Marks)
> **Problem Statement:**  
> A proposed Tier-III Data Centre has a total IT white space payload of **500 kW** with a measured operating Power Factor of **0.95 lag**. The facility specifies an Online Double Conversion static UPS with an inverter efficiency of **94%**. To prevent premature aging, the continuous operating load of the UPS must not exceed **75%** of its nominal capacity under normal conditions.  
> The backup battery bank operates on a nominal DC bus voltage of **480 V DC**. The required battery backup autonomy time is **15 minutes** at full load.  
> **Calculate:**  
> 1. Total IT Apparent Power in kVA.  
> 2. Minimum required UPS rating in kVA.  
> 3. Required Battery Bank capacity in Ampere-Hours (Ah), assuming an aging and temperature derating factor of **1.25**.

#### Step-by-Step Solution:
**Part 1: Calculate IT Apparent Power ($S_{IT}$)**
$$S_{IT} = \frac{P_{IT}}{\text{Power Factor}} = \frac{500\text{ kW}}{0.95} = \mathbf{526.32\text{ kVA}}$$

**Part 2: Calculate Minimum UPS Rating ($S_{UPS}$)**
The DC inverter input power required to deliver 500 kW at 94% efficiency is:
$$P_{DC} = \frac{P_{IT}}{\eta_{inverter}} = \frac{500\text{ kW}}{0.94} = 531.91\text{ kW}$$

Apparent power demanded by the inverter:
$$S_{inverter} = \frac{S_{IT}}{\eta_{inverter}} = \frac{526.32\text{ kVA}}{0.94} = 559.91\text{ kVA}$$

Applying the 75% maximum continuous loading rule:
$$S_{UPS\_minimum} = \frac{S_{inverter}}{0.75} = \frac{559.91\text{ kVA}}{0.75} = \mathbf{746.55\text{ kVA}}$$

*Engineering Selection:* In an $N+1$ configuration, specify **two 500 kVA UPS modules** operating in parallel (Total capacity = 1000 kVA). Normal load per unit = $\frac{559.91}{1000} = 56\%$, which is well below the 75% limit.

**Part 3: Calculate Battery Bank Capacity (Ah)**
Total power drawn from the DC bus during mains failure:
$$P_{battery} = 531.91\text{ kW} = 531,910\text{ Watts}$$

DC discharge current drawn from the 480V DC battery bank:
$$I_{discharge} = \frac{P_{battery}}{V_{DC}} = \frac{531,910\text{ W}}{480\text{ V}} = 1108.15\text{ Amperes}$$

Required autonomy time: $t = 15\text{ minutes} = \frac{15}{60}\text{ hours} = 0.25\text{ hours}$.
Theoretical battery capacity:
$$C_{theoretical} = I_{discharge} \times t = 1108.15\text{ A} \times 0.25\text{ h} = 277.04\text{ Ah}$$

Applying the 1.25 aging and temperature safety derating factor:
$$C_{design} = C_{theoretical} \times 1.25 = 277.04\text{ Ah} \times 1.25 = \mathbf{346.30\text{ Ah}}$$

*Final Recommendation:* Specify a **480V DC battery bank with a nominal capacity of at least 350 Ah** (or two parallel strings of 200 Ah batteries for redundancy).

---

### Numerical Problem 2: Total Facility Power, PUE, and Transformer Sizing (10 Marks)
> **Problem Statement:**  
> A high-density cloud colocation data centre contains **250 server racks**. Each rack has an average power draw of **6 kW**. The mechanical and electrical engineers have designed the facility to achieve an operational **PUE of 1.30**. Mechanical cooling represents **70%** of the non-IT overhead power, while electrical distribution losses and building services consume the remaining **30%**. The overall facility power factor is **0.90**.  
> **Calculate:**  
> 1. Total IT Load ($P_{IT}$) in kW.  
> 2. Total Facility Power ($P_{Total}$) in kW.  
> 3. Active power consumed by the cooling system in kW.  
> 4. Minimum required Substation Step-down Transformer rating in kVA, incorporating a **25% future growth margin**.  
> 5. Facility Data Centre Infrastructure Efficiency (DCiE) percentage.

#### Step-by-Step Solution:
**Part 1: Total IT Load ($P_{IT}$)**
$$P_{IT} = 250\text{ racks} \times 6\text{ kW/rack} = \mathbf{1,500\text{ kW}}\ (1.50\text{ MW})$$

**Part 2: Total Facility Power ($P_{Total}$)**
$$\text{PUE} = \frac{P_{Total}}{P_{IT}} \implies P_{Total} = P_{IT} \times \text{PUE}$$
$$P_{Total} = 1,500\text{ kW} \times 1.30 = \mathbf{1,950\text{ kW}}\ (1.95\text{ MW})$$

**Part 3: Overhead & Cooling Power Breakdown**
$$\text{Total Overhead Power} = P_{Total} - P_{IT} = 1,950\text{ kW} - 1,500\text{ kW} = 450\text{ kW}$$
$$\text{Cooling Power} = 70\% \times 450\text{ kW} = \mathbf{315\text{ kW}}$$
$$\text{Electrical Losses \& Auxiliary} = 30\% \times 450\text{ kW} = \mathbf{135\text{ kW}}$$

**Part 4: Transformer Sizing in kVA**
Facility apparent power:
$$S_{facility} = \frac{P_{Total}}{\text{Facility PF}} = \frac{1,950\text{ kW}}{0.90} = 2,166.67\text{ kVA}$$

Applying the 25% future growth margin ($1.25\times$):
$$S_{Tx\_required} = 2,166.67\text{ kVA} \times 1.25 = \mathbf{2,708.33\text{ kVA}}$$

*Standard Commercial Selection:* Select standard commercial rating of **3,000 kVA (3.0 MVA) 33kV/415V Cast Resin Dry-Type Transformer**. In a Tier-IV 2N architecture, install **two separate 3,000 kVA transformers** (one on Substation Feed A and one on Substation Feed B).

**Part 5: Data Centre Infrastructure Efficiency (DCiE)**
$$\text{DCiE} = \frac{1}{\text{PUE}} \times 100\% = \frac{1}{1.30} \times 100\% = \mathbf{76.92\%}$$

---

## 9. Module 8: High-Yield Comparison Tables (5-Mark Short Notes)

### Table 1: Automatic Transfer Switch (ATS) vs. Static Transfer Switch (STS)
| Comparison Factor | Automatic Transfer Switch (ATS) | Static Transfer Switch (STS) |
|---|---|---|
| **Operating Principle** | Electromechanical contactor / motorized circuit breaker mechanism with mechanical interlock. | Solid-state power electronic SCR (Silicon Controlled Rectifier) thyristors. |
| **Transfer Time** | **50 ms to 100 ms** (Break-before-make). | **2 ms to 4 ms** (Sub-cycle, break-before-make). |
| **Server Continuity** | Exceeds server PSU hold-up time (16–20 ms); causes server reboot unless backed by UPS. | Well within server PSU hold-up time; **completely seamless transfer with zero IT reboot**. |
| **Wear & Life** | Physical moving contacts; mechanical fatigue over switching operations. | Solid-state; zero moving parts; virtually infinite switching cycles. |
| **Thermal Dissipation**| Negligible heat loss during steady-state conduction. | Continuous forward voltage drop across SCRs generates constant heat (requires cooling). |
| **Deployment Location** | Upstream between Utility and Diesel Generator inputs. | Downstream between dual UPS output buses feeding single-corded racks. |

---

### Table 2: 2N Redundancy vs. N+1 Redundancy
| Parameter | N+1 Redundancy (Parallel Redundant) | 2N Redundancy (System Redundant) |
|---|---|---|
| **Architecture** | Single distribution path with $N$ active modules + $1$ standby module on a common bus. | Two completely independent, physically isolated distribution paths (Path A & Path B). |
| **Single Point of Failure (SPOF)**| **YES:** Common paralleling switchgear and distribution busbar are single points of failure. | **ZERO SPOF:** Total isolation from grid substation to server dual power supplies. |
| **Concurrent Maintainability**| No: Servicing the common switchgear or distribution board mandates a planned IT shutdown. | **YES:** Path A can be fully de-energized for maintenance while Path B carries 100% of load. |
| **Capital Cost (CAPEX)**| Approximately **1.25× to 1.35×** base cost. | Approximately **2.0× to 2.2×** base cost. |
| **Operating Load Factor** | High efficiency; units run at 70% to 85% load. | Units normally operate at 40% to 50% load under steady state. |
| **Tier Certification** | Uptime Institute **Tier II**. | Uptime Institute **Tier IV**. |

---

## 10. Module 9: Master Question Bank & 15-Mark Model Answers

### Question 1 (15 Marks - Long Essay)
> **"Draw and explain the comprehensive Single Line Diagram (SLD) of a Tier-IV Data Centre electrical distribution system from the incoming utility substation to the server cabinet. Discuss the role of each major component in ensuring fault tolerance."**

#### Model Answer Outline & Examination Strategy:
1. **Introduction (2 Marks):**
   - Define Tier-IV availability requirements (99.995%, max downtime 26.3 mins/year).
   - State the core architectural principle: **2(N+1) or 2N system redundancy** with complete physical and electrical compartmentalization, ensuring zero single points of failure (SPOF) and autonomous fault tolerance.
2. **Neat Labeled Single Line Diagram (5 Marks):**
   - Sketch dual incoming feeds (Utility A & Utility B at 33 kV/11 kV).
   - Draw VCB circuit breakers and dual step-down K-factor transformers.
   - Draw the ATS units backed by DCC-rated Diesel Generators.
   - Draw dual 2N Online Double Conversion UPS plants with dedicated battery strings.
   - Draw dual floor PDUs with K-20 isolation transformers and BCMS.
   - Draw overhead busways with hot-swappable tap-off boxes.
   - Draw server racks featuring dual intelligent ePDUs feeding redundant PSUs.
3. **Component Functionality & Critical Role (5 Marks):**
   - *RMU & VCB:* Provides automated loop fault isolation.
   - *K-Factor Transformer:* Traps triplen harmonics in delta primary; prevents core saturation.
   - *DCC Rated Generator:* Provides primary continuous power for 72 hours upon grid failure.
   - *Online Double Conversion UPS:* 0 ms transfer time, full VFI voltage/frequency regulation.
   - *PDU Isolation Transformer:* Provides clean local neutral-earth bond; eliminates common-mode noise.
   - *Overhead Busway:* Eliminates underfloor airflow obstruction; provides modular tap-off scaling.
4. **Fault Tolerance Analysis (3 Marks):**
   - Detail what happens if Path A suffers a direct short-circuit: protective breakers clear the fault on Path A, while Server PSU 2 on Path B seamlessly supports 100% of server compute without a single microsecond of downtime!

---

### Question 2 (10 Marks)
> **"With a neat schematic block diagram, explain the construction and working of an Online Double Conversion UPS. Explain the functions of the Rectifier, Inverter, Static Bypass Switch, and Maintenance Bypass."**

#### Model Answer Outline:
1. **Schematic Block Diagram (3 Marks):** Draw AC input $\rightarrow$ Rectifier $\rightarrow$ DC Bus (with Battery Bank) $\rightarrow$ Inverter $\rightarrow$ Output to load. Show parallel Static Bypass loop (SCRs) and external Manual Maintenance Bypass loop.
2. **Detailed Operating Sequence (4 Marks):**
   - *Double Conversion Process:* Explain AC $\rightarrow$ DC $\rightarrow$ AC synthesis via IGBT PWM.
   - *Normal Operation:* Rectifier powers inverter and maintains battery float charge.
   - *Outage Operation:* Inverter draws from battery; transfer time is strictly **0 ms**.
   - *Recharge:* Utility restores; rectifier powers inverter and recharges battery without disturbing load.
3. **Function of Bypass Loops (3 Marks):**
   - *Static Bypass:* Sub-cycle thyristor switch (< 2 ms) protecting against inverter faults or downstream short-circuits.
   - *Maintenance Bypass:* Manual mechanical interlocked switch allowing technicians to service or replace the entire UPS cabinet safely while raw utility power bypasses the unit.

---

### Question 3 (10 Marks)
> **"Explain the earthing and lightning protection architecture of a modern data centre. Why is a Signal Reference Grid (SRG) necessary under raised floors, and why is TN-S grounding preferred over TN-C?"**

#### Model Answer Outline:
1. **TN-S Grounding Architecture (3 Marks):**
   - Diagram showing separated Neutral ($N$) and Protective Earth ($PE$) throughout the data centre.
   - Single point of neutral-earth bond at transformer secondary.
   - Why TN-C is prohibited: Explain how triplen harmonic return currents in a shared PEN conductor flow through server chassis, causing touch-voltage electric shocks and severe packet corruption.
2. **Signal Reference Grid (SRG) Engineering (4 Marks):**
   - Definition: Prefabricated 2 ft × 2 ft welded copper mesh beneath raised floor tiles.
   - High-Frequency Inductive Physics: Contrast $50\text{ Hz}$ single-cable earthing with high-frequency microprocessor clock harmonics where inductive reactance ($X_L = 2\pi f L$) turns single cables into open circuits/antennas.
   - Explain how multiple parallel mesh paths drive impedance to near zero ($Z \rightarrow 0$), creating an equipotential ground plane that prevents ground loops and dissipates electrostatic discharge (ESD).
3. **Cascaded Surge Protection (3 Marks):**
   - Detail 3-stage SPD coordination according to **IEC 62305**:
     - *Type 1:* Main intake (10/350 µs direct strike impulse).
     - *Type 2:* Distribution & UPS boards (8/20 µs switching transients).
     - *Type 3:* Rack ePDU level (residual clamping to < 1.0 kV).
   - Chemical earth pits achieving earth resistance **$< 1.0\ \Omega$** as per **IEEE 1100**.

---

### Golden Examination Tips for High Scoring
1. **Always draw diagrams:** Even if a question does not explicitly say "draw", always sketch a clean, labeled block diagram or circuit for any question worth 5 marks or more.
2. **Include Standards:** Citing industry standards like **IEEE 1100 (Emerald Book)**, **ISO 8528-1 (DCC Ratings)**, **IEC 62305 (Lightning)**, **IEEE 519 (Harmonics)**, and **Uptime Institute Tier Standards** immediately signals master-level technical mastery to the examiner.
3. **State the Units:** In all numerical problems, explicitly show intermediate formulas, substitution steps with units (kW, kVA, V, Ah), and box your final answer.
