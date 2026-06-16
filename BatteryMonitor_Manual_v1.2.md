# BatteryMonitor Windows — User Manual

## Overview

BatteryMonitor displays real-time battery data in two side-by-side charts and a two-row header. Controls are in the footer bar at the bottom.

---

## The TC66 USB Power Meter (Optional)

BatteryMonitor optionally integrates with the **MakerHawk TC66 USB Power Meter** (~$26.99 on Amazon: *MakerHawk USB Power Meter, TC66 USB Tester Type C, USB Voltage Meter and Current Tester, 0.96 Inch IPS Color LCD Display, PD Ammeter Voltmeter QC 2.0/3.0*). The TC66 measures voltage, current, and power independently of the BMS, enabling accurate DC-DC charging efficiency calculations. It connects via USB serial (typically COM3–COM6 on Windows) and displays live V/i data on its own color LCD while simultaneously streaming data to BatteryMonitor.

> **Note:** To reset the TC66's cumulative capacity (Qin) and energy (Ein) counters to zero, the physical button on the meter must be pressed. There is no software reset available.

---

## Footer Controls (left to right)

- **Start / Stop** — begins or ends a recording session. While recording, the system is kept awake.
- **Clear** — clears all chart data and resets all accumulators without stopping the session.
- **Load CSV** — loads a previously saved CSV file for review. Live recording is suspended.
- **Int: N s** — sample interval in seconds (1–60). Lower values give finer resolution but larger files.
- **Stop** *(red, TC66 section)* — disconnects the TC66 meter.
- **Span:** — time window shown in the charts (All, 1 h, 2 h, 4 h, 8 h). Does not affect recording.
- **Refr** — forces an immediate TC66 screen refresh.
- **TC66: COMx** — serial port selector for the TC66 USB meter.
- **Disc / Con** — connects or disconnects the TC66 meter on the selected port.
- **⇅** — flips the TC66 display orientation (useful when the meter is inverted).
- **Psys: N W** — system baseline power (W) subtracted from TC66 input power when computing charging efficiency CE. Typically 4–5 W.
- **LR pts: N** — minimum number of SOC points used for the auto linear regression (TT5 prediction).
- **Run time** — elapsed time since Start was pressed.
- **N samples** — total number of samples recorded in the current session.

---

## Header — Row 0 (BMS data)

Row 0 is always visible. The **status label** on the left shows the current battery state:

| State label | Meaning |
|-------------|---------|
| `CHRG` | Charging |
| `DCHRG` | Discharging |
| `IDLE` | No significant current flow |
| `FULL` | Fully charged (trickle only) |

### During discharge (DCHRG)

| Field | Description |
|-------|-------------|
| `U=` | Battery terminal voltage (V), 2 decimal places |
| `i=` | Discharge current (A), negative, 2 decimal places |
| `R=` | Internal resistance (Ω), integer |
| `P=` | Discharge power (W), negative; 2 decimals below 10 W, 1 decimal above |
| `Qc=` | Segment charge withdrawn since last state transition (Ah) |
| `Ec=` | Segment energy withdrawn since last state transition (Wh) |
| `Qt=` | Cumulative charge withdrawn since Start (Ah) |
| `Et=` | Cumulative energy withdrawn since Start (Wh) |
| `SOC=` | State of charge reported by BMS (%) |
| `SoH=` | State of health: ratio of current full-charge capacity to design capacity (%) |

### During charge (CHRG)

Same fields apply, with `i=`, `P=`, `Qc=`, `Ec=` positive; `Qt=` and `Et=` accumulate in the charging direction.

---

## Header — Row 1 (TC66 data + computed)

Row 1 is visible when the TC66 is connected **or** when the TT5 countdown is active.

### When TC66 is connected

| Field | Description |
|-------|-------------|
| `U=` | TC66 measured voltage (V) |
| `i=` | TC66 measured current (A) |
| `R=` | TC66 computed resistance (Ω), integer |
| `P=` | TC66 measured power (W) |
| `Qin=` | Cumulative charge delivered by TC66 since connection (Ah) |
| `Ein=` | Cumulative energy delivered by TC66 since connection (Wh) |
| `CE=` | Charging efficiency: `Ec / (Ein − Psys×t)` × 100 (%) |
| `T.TC=` | TC66 internal sensor temperature (°C) — **not** battery cell temperature |

### Always (when discharging)

| Field | Description |
|-------|-------------|
| `TT5:` | Estimated time remaining to 5% SOC, from auto linear regression on recent SOC data |
| `Cyc=` | Battery cycle count reported by BMS |

---

## Alerts

- **Charge complete:** when charging current drops to zero (CHRG→IDLE transition), "Battery full" is announced every 60 seconds for up to 10 minutes, signalling that a discharge cycle can be started. Dismissed by the 🔇 button that appears on the right of the footer, or automatically when discharge resumes.
- **Low SOC:** the SOC level is announced by name ("ten", "nine" … "one") at each 1% step from 10% down to 1% during discharge.

---

## Known BMS Limitations

- **SOC step size varies by model:** SOC is reported in discrete steps by the BMS; step size depends on the laptop model and SOC level, and is often smaller near full discharge than near full charge.
- **BMS holdback (model-specific):** on the ThinkPad T490, SOC was observed to hold artificially at 7% for 1–2 minutes before dropping sharply. This is a deliberate BMS algorithm to give the user time to save work, not a measurement artifact. It affects TT5 accuracy in this range. Other models may exhibit similar behavior at different SOC levels.
- **Battery cell temperature** is not accessible via any standard Windows API on the tested Lenovo models (Yoga 7 2-in-1, ThinkPad T490, ThinkPad X1 Carbon). `T.TC=` shows the TC66 meter's own sensor temperature, not the battery cell temperature.


---

## The Two Charts

BatteryMonitor displays two dual-axis charts side by side, updated in real time at the selected sample interval. Both charts share the same elapsed-time X axis. Curve colors switch automatically between charge (green tones), discharge (red/orange tones), and idle (gray) states.

![Charge cycle](Yoga_TC66_charge.jpg)
*Full charge cycle on a Lenovo Yoga 7 2-in-1 with TC66 USB meter. SOC rises from ~20% to 100% over ~2h 40min; TC66 current tapers near full charge. CE=65.4%.*



---

### Chart 1 — Voltage, Current & Capacity

**Left Y axis (U, V):** battery terminal voltage and, when the TC66 is connected, TC66 input voltage.

**Right Y axis (i, A & Qc, Ah):** current and cumulative segment capacity.

| Curve | Style | Description |
|-------|-------|-------------|
| `i, A` | Solid | BMS discharge/charge current (negative = discharge) |
| `i.TC, A` | Dashed | TC66 measured current |
| `U, V` | Solid | BMS battery terminal voltage |
| `U.TC, V` | Dashed | TC66 measured input voltage |
| `Qc, Ah` | Solid | Segment charge (resets on state transition) |
| `Qt, Ah` | Solid | Cumulative charge since Start |

TC66 curves are only shown when the meter is connected.

---

### Chart 2 — SOC, Power & Temperature

**Left Y axis (P, W & Energy, Wh):** power and cumulative energy curves.

**Right Y axis (SOC, % & T, °C):** state of charge and TC66 temperature.

| Curve | Style | Description |
|-------|-------|-------------|
| `SOC, %` | Solid | State of charge reported by BMS |
| `P, W` | Solid | BMS power (negative = discharge) |
| `P.TC, W` | Dashed | TC66 input power |
| `Ec, Wh` | Dashed | Segment energy since last state transition |
| `Et, Wh` | Solid | Cumulative energy since Start |
| `T.TC, °C` | Dashed | TC66 internal temperature |

---

## Interactive Chart Features

### Running Average (2-click)

Available on both charts. Click two points on any curve to display a running average line and value badge between them. Useful for characterizing steady-state power consumption or current during a specific interval.

- **Click 1:** sets the start point (shown as a crosshair)
- **Click 2:** sets the end point; the average value is displayed
- **Click to clear avg** (shown in top-left of chart): removes the average

### Linear Regression — TT5 Prediction (3-click, Chart 2 only)

The auto linear regression fits a line to recent SOC data and projects it forward to 5% SOC, giving the **TT5** (Time to 5%) estimate shown in the header.

A manual 3-click regression is also available for more control:

- **Click 1 & 2:** define the SOC data range to fit
- **Click 3:** sets the target SOC (default 5%); the regression line and intercept time are displayed
- **Click to clear LR** (shown in top-right of chart): removes the regression line

The regression line color indicates fit quality:
- **Green:** R² ≥ 0.90 — reliable prediction
- **Orange:** R² < 0.90 — poor fit, treat estimate with caution

### Cursor Tooltip

Hovering over either chart displays a tooltip showing the time and value of the nearest data point across all visible curves. Useful for reading exact values at any point in the recording.

### Legend

The legend is moveable — click it to cycle through three positions (top-left, center, bottom-left). It lists all visible curves with their line style and color.

### Time Window (Span)

The **Span** selector in the footer controls the time window displayed in both charts simultaneously. Options: All, 1 h, 2 h, 4 h, 8 h. Narrowing the span zooms into the most recent data without affecting recording.

![Combined charge and discharge cycle](Yoga_TC66_charge-discharge.jpg)
*Combined charge+discharge session (~5h 26min). TT5 linear regression predicts 6h 11m to 0% SOC at 10.3%/h. Running average shows -0.421 A mean discharge current. Qt=2.59 Ah and Et=41.7 Wh cumulative across both segments.*




---

## Installation & Requirements

### Requirements

- **Operating system:** Windows 10 or Windows 11
- **.NET Framework 4.0** or later (included in Windows 10/11 by default)
- **Administrator privileges** — required for WMI battery queries (health, cycle count)
- **TC66 USB meter** (optional) — for external V/i measurement and charging efficiency analysis

### Installation

1. Download the latest release from the [Releases](../../releases) page on GitHub
2. Run the downloaded `BatteryMonitor_XX.Y.exe` directly — no installer or ZIP extraction required
3. Ensure `app.manifest` is in the same folder as `BatteryMonitor.exe` (included in the release). To compile from source locally, `build.bat`, `app.manifest`, and the `.cs` source file must all be in the same folder
4. Run `BatteryMonitor.exe` — a UAC prompt will appear; accept it to grant administrator access
5. If Windows Smart App Control or Defender blocks the executable, you may need to right-click → Properties → Unblock, or disable Smart App Control in Windows Security settings

### First Run

- The app starts in idle state; click **Start** to begin recording
- To use a TC66 meter, select the correct COM port and click **Con** before or after starting

---

## CSV File Format

CSV files are saved automatically to the application folder when CSV recording is active (click **Load CSV** button to start). Files are named `BatteryLog_YYYYMMDD_HHMMSS.csv`. A new row is written at each sample interval; the file is auto-saved every 10 seconds.

### Columns

| Column | Unit | Format | Description |
|--------|------|--------|-------------|
| `Timestamp` | — | `yyyy-MM-dd HH:mm:ss` | Wall-clock time of sample |
| `Elapsed_s` | s | F1 | Elapsed time since Start |
| `Voltage_V` | V | F3 | BMS battery terminal voltage |
| `Current_A` | A | F3 | BMS current (negative = discharge) |
| `Power_W` | W | F2 | BMS power (negative = discharge) |
| `SOC_Pct` | % | F1 | State of charge |
| `State` | — | text | `Charging`, `Discharging`, `Idle`, or `Full` |
| `Qt_Ah` | Ah | F6 | Cumulative charge since Start |
| `Et_Wh` | Wh | F3 | Cumulative energy since Start |
| `Qc_Ah` | Ah | F6 | Segment charge since last state transition |
| `Ec_Wh` | Wh | F3 | Segment energy since last state transition |
| `TC66_V` | V | F4 | TC66 measured voltage (blank if no TC66) |
| `TC66_A` | A | F5 | TC66 measured current (blank if no TC66) |
| `TC66_W` | W | F4 | TC66 measured power (blank if no TC66) |
| `TC66_Temp` | °C | integer | TC66 internal temperature (blank if no TC66) |
| `TC66_Ah` | Ah | F6 | TC66 cumulative charge (blank if no TC66) |
| `TC66_Wh` | Wh | F3 | TC66 cumulative energy (blank if no TC66) |

TC66 columns are always present in the header but left blank for rows where no TC66 data was available (e.g. meter was disconnected during part of the session).


---

## About

**Author:** Tony Gozdz (tgozdz@gmail.com)  
**Development assistance:** Claude (Anthropic)  
**Repository:** [https://github.com/ASG49/BatteryMonitor-Windows](https://github.com/ASG49/BatteryMonitor-Windows)  
**License:** MIT License  
**Manual version:** v1.2 — June 2026
