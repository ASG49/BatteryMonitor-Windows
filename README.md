# BatteryMonitor Windows

Windows desktop app for real-time battery monitoring, long-duration cycle logging, and DC-DC charging efficiency analysis using a TC66 USB meter.

## Features

**Two dual-axis charts** running simultaneously:

- **Chart 1 — Voltage & Current:** BMS voltage and current (solid), TC66 voltage and current (dashed, optional)
- **Chart 2 — SOC, Power & Temperature:** power, state of charge, cumulative energy (Ec/Et), and TC66 temperature

**Analysis tools:**
- 3-click linear regression on the SOC curve for TT5 prediction (time to 5% SOC) with R² confidence
- 2-click running average on any curve (BMS and TC66)
- DC-DC charging efficiency (CE) with system baseline power (Psys) correction
- Charge/discharge/idle state segmentation with color-coded curves

**Header display:** two rows with uniform `symbol=value` notation — BMS fields (Status, U=, i=, R=, P=, SOC=, Qc=, Ec=, Qt=, Et=, SoH=, Cyc=) and TC66 fields (U=, i=, R=, P=, Qin=, Ein=, CE=, T.TC=) with TT5 countdown and auto-sizing font to fit any screen width

**Alerts:**
- **Charge-complete:** triple beep (880 Hz) every 60 seconds for 10 minutes when charging current drops to zero — signals time to start a discharge cycle; dismissed by red ✕ Beep button on the footer
- **Low SOC:** single beep (660 Hz) at 10% SOC during discharge, then at every 1% step down to 1% — warns that full discharge is approaching

**Data & UI:**
- CSV auto-recording toggle for long multi-hour cycles
- Cursor tooltip with nearest-point detection across all curves
- Moveable legend (click to cycle through 3 positions)
- DPI-aware scaling: 100%, 125%, 150%, 175%, 200% (PerMonitorV2)
- State-synchronized colors: axis labels and curves match charge/discharge state
- TC66 screen flip button (⇅)
- Smart CSV export: TC66 columns included only if TC66 was connected

## Requirements

- Windows 10/11
- .NET Framework 4.0 or later
- TC66 USB meter (optional — for external V/i measurement and efficiency analysis)
- Administrator privileges (required for WMI battery data — see below)

## Installation

Download the latest release from the [Releases](../../releases) page.  
No installation required — run the executable directly.

The included `app.manifest` requests administrator privileges (`requireAdministrator`). This is necessary to query the `root\WMI` namespace for battery health (`BatteryStaticData`) and cycle count (`BatteryCycleCount`), which are inaccessible to standard user accounts. The manifest also sets `PerMonitorV2` DPI awareness for correct scaling on high-DPI displays.

## Usage

1. Launch `BatteryMonitor.exe` (UAC prompt will appear — accept it)
2. Optionally connect a TC66 USB meter and click **Connect**
3. Click **Start** to begin recording; CSV data is auto-saved to the application folder
4. Use the SOC chart for linear regression TT5 prediction (3 clicks); power chart for 2-click running average
5. At end of charge, a triple beep alerts you to start a discharge cycle; single beeps warn of low SOC during discharge

## Battery Temperature — Known Limitation

The app includes WMI infrastructure to display battery cell temperature if exposed by the system. In practice, **none of the tested Lenovo laptops expose true battery temperature through any standard Windows API**:

| Machine | WMI Thermal Zone | IOCTL `BatteryTemperature` | Result |
|---------|-----------------|---------------------------|--------|
| Lenovo Yoga 7 | `TZ01` = 43°C (platform zone, not battery) | Not supported by driver | ❌ |
| Lenovo ThinkPad T490 | `THM0` = 81°C (CPU zone) | Not tested | ❌ |
| Lenovo ThinkPad X1 Carbon | `THM0` = 34°C (CPU zone at idle) | Not supported by driver | ❌ |

Lenovo Vantage displays a battery temperature (e.g. 35°C on the Yoga 7) that differs from all WMI thermal zones — it reads via a proprietary EC/SMBus interface not exposed to Windows. The standard `IOCTL_BATTERY_QUERY_INFORMATION` with `BatteryTemperature` returns "not supported" on all three machines even when run as Administrator.

The TC66 temperature (`T.TC=`) displayed in the header is the USB meter's own internal sensor, not the battery cell temperature. This is a hardware/driver limitation with no known workaround short of reverse-engineering the Lenovo EC interface.

## Version

Current release: **v45.9**

## License

MIT License
