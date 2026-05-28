# BatteryMonitor Windows

Windows desktop app for real-time battery monitoring, long-duration cycle logging, and DC-DC charging efficiency analysis using a TC66 USB meter.

## Features

**Two dual-axis charts** running simultaneously:

- **Chart 1 — Voltage & Current:** BMS voltage and current (solid), TC66 voltage and current (dashed, optional)
- **Chart 2 — SOC, Power & Temperature:** power, state of charge, cumulative energy (Ec/Et), and TC66 temperature

**Analysis tools:**
- 3-click linear regression on the SOC curve for time-to-empty prediction with R² confidence
- 2-click running average on any curve (BMS and TC66)
- DC-DC charging efficiency (η) with system baseline power (Psys) correction
- Charge/discharge/idle state segmentation with color-coded curves

**Header display:** two rows showing BMS fields (Status, V, i, R, P, SOC, Qc, Ec, Qt, Et, H=, Cyc) and TC66 fields (V, i, R, P, Qin, Ein, η, Ttc) with ETA and battery health

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
4. Use the SOC chart for linear regression time-to-empty (3 clicks); power chart for 2-click running average

## Battery Temperature — Known Limitation

The app includes WMI infrastructure to display battery cell temperature if exposed by the system. In practice, **none of the tested Lenovo laptops expose true battery temperature through any standard Windows API**:

| Machine | WMI Thermal Zone | IOCTL `BatteryTemperature` | Result |
|---------|-----------------|---------------------------|--------|
| Lenovo Yoga 7 | `TZ01` = 43°C (platform zone, not battery) | Not supported by driver | ❌ |
| Lenovo ThinkPad T490 | `THM0` = 81°C (CPU zone) | Not tested | ❌ |
| Lenovo ThinkPad X1 Carbon | `THM0` = 34°C (CPU zone at idle) | Not supported by driver | ❌ |

Lenovo Vantage displays a battery temperature (e.g. 35°C on the Yoga 7) that differs from all WMI thermal zones — it reads via a proprietary EC/SMBus interface not exposed to Windows. The standard `IOCTL_BATTERY_QUERY_INFORMATION` with `BatteryTemperature` returns "not supported" on all three machines even when run as Administrator.

The TC66 temperature (`Ttc=`) displayed in the header is the USB meter's own internal sensor, not the battery cell temperature. This is a hardware/driver limitation with no known workaround short of reverse-engineering the Lenovo EC interface.

## Version

Current release: **v42.8**

## License

MIT License
