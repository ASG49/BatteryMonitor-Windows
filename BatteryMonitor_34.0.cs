//=============================================================================
// BatteryMonitor.cs - Windows Battery Monitor v34.0
// May 20, 2026 - Tony Gozdz
//
// Target: .NET Framework 4.0+ / Windows 10/11
// Compiler: Visual Studio 2022 Build Tools (C# 12)
//
// Professional battery characterization tool with dual-axis charts, TC66 USB
// meter integration, and comprehensive data logging for capacity analysis.
//
// Two dual-axis charts with comprehensive battery characterization:
//
//   Chart 1: "Voltage & Current"
//     Left Y-axis (Voltage):
//       - Voltage (V) - BMS, solid darker blue/magenta, 3.5pt
//       - Voltage (V) (TC66) - TC66, dashed lighter blue/magenta, 2.5pt [optional]
//     Right Y-axis (Current):
//       - Current (A) - BMS, solid darker green/red, 3.5pt
//       - Current (A) (TC66) - TC66, dashed lighter green/red, 2.5pt [optional]
//
//   Chart 2: "Power, SOC & T"
//     Left Y-axis (Power):
//       - Power (W) - BMS, solid darker cyan/amber, 3.5pt
//       - Power (W) (TC66) - TC66, dashed lighter cyan/amber, 2.5pt [optional]
//     Right Y-axis (SOC, Temperature):
//       - SOC (%) - BMS, solid darker forest green/rust, 3.5pt
//       - Temperature (TC66) - dashed bright red, 2.5pt [optional]
//       - Temperature (BMS) - dashed dark orange, 2.5pt [optional, rarely available]
//
// Color coding: Charge=cool colors, Discharge=warm colors, Idle=4 gray shades
// BMS curves: Solid, darker (primary data)
// TC66 curves: Dashed, lighter (reference/comparison)
//
// Key Features v31.2:
//   - Single AppVersion constant — update only one line for each new release
//   - 3-click Linear Regression on SOC chart for time-to-empty prediction
//   - 2-click running average on all curves (BMS and TC66)
//   - Click legend box to reposition (cycles through 3 optimal positions)
//   - Export CSV with smart TC66 column inclusion (only if TC66 was connected)
//   - Chart Refresh button to reset time window to "All"
//   - TC66 USB meter: voltage, current, power, temp, mAh, mWh, efficiency
//   - System baseline power (Psys) correction for true charger efficiency
//   - BMS temperature detection via WMI (auto-charts if available on system)
//   - Thermal diagnostics button (Desktop report of all WMI temperature sources)
//   - Dynamic Y2 axis title (shows "& T" only when temperature data exists)
//   - Four distinct idle gray shades for voltage/current curve differentiation
//   - Removed resistance curves from Chart2 (cleaner, less artificial data)
//   - BMS header: 10 fields (Status, V, i, R, P, SOC, Qc, Ec, Qt, Et)
//   - TC66 header: 9 fields (TC66, V, i, R, P, T, Qin, Ein, η)
//   - DPI-aware scaling: works at 100%, 125%, 150%, 175%, 200%
//   - Cursor tooltip with nearest-point detection and distance formula
//   - Segment-connecting lines across time gaps (no broken curves)
//   - Auto-hiding unavailable fields (TC66, temperature, BMS temp)
//   - State-synchronized colors: axis labels match curve charge/discharge state
//   - Visual polish: legend click independence, proper event propagation
//   - TC66 screen flip (⇅) button - working correctly
//   - TC66 curves display properly during all states (charge/discharge/idle)
//
// Version History (Major Milestones):
//   v34.0 (May 23, 2026): SOC label: yellow background highlight with horizontal padding
//   v33.9 (May 23, 2026): TC66 status label: firmware version removed, trailing space added for column alignment
//   v33.8 (May 22, 2026): SOC moved to end of BMS row (aligns E values with TC66); P smart format: X.XX if <10W, XX.X if >=10W
//   v33.7 (May 22, 2026): BMS and TC66 P= format F2→F1 (XX.X W)
//   v33.6 (May 22, 2026): Header: T= moved to end of TC66 row; BMS P= precision F1→F2; CHRG/TC66 labels MinimumSize aligned; Y1 bottom tick label no longer clipped
//   v33.5 (May 22, 2026): Y1 axis: skip TC66 voltage values <= 0 — idle/disconnected TC66 (V=0.00) no longer drags voltage axis down to 0
//   v33.4 (May 22, 2026): OnFormClosing: dispose clockTimer, csvTimer, controlFont — second review pass
//   v33.3 (May 22, 2026): Code review fixes: GetU32 bounds check; TransformBlock return verified; bmsBaseEnergy_Wh dead field removed; isCsvLoaded cleared in OnClear; CSV 50MB size guard; legend position cached (recomputes every 60 pts or on resize)
//   v33.2 (May 22, 2026): Removed unused tc66EfficiencyActive field (CS0414 warning)
//   v33.1 (May 22, 2026): Chart titles Bold→Regular
//   v33.0 (May 21, 2026): Chart 1 voltage Y-axis: proportional 12% padding instead of flat ±2V — eliminates spurious 0V floor on battery voltage
//   v32.9 (May 21, 2026): η TC66 side restored to (current−baseline)/1000 — handles un-zeroed TC66; BMS side keeps segEnergy_Wh (Ec)
//   v32.8 (May 21, 2026): η uses segEnergy_Wh (Ec) directly + raw TC66 counter (user zeros TC66 before run) — eliminates baseline drift; cumulEnergy_Wh never used for η
//   v32.7 (May 21, 2026): η suppressed (shows n/a) when CSV loaded — TC66 delta vs replayed BMS is meaningless; re-enabled on live recording start
//   v32.6 (May 21, 2026): Legend lineHeight = S(21) — original 17 was 2px gap, +4px gives comfortable spacing; reverted GenericTypographic complexity
//   v32.5 (May 21, 2026): Legend uses GenericTypographic throughout — lineHeight=tightHeight+3px, DrawString aligned; fixes negative centering offset
//   v32.4 (May 21, 2026): Legend lineHeight now uses GenericTypographic (true glyph height ×1.15) — eliminates Segoe UI internal leading bloat
//   v32.3 (May 21, 2026): Legend lineHeight multiplier 1.1→1.05
//   v32.2 (May 21, 2026): Legend lineHeight multiplier 1.2→1.1
//   v32.1 (May 21, 2026): Legend lineHeight multiplier 1.5→1.2 (20% leading, standard typography)
//   v32.0 (May 21, 2026): Legend lineHeight derived from font.GetHeight×1.5 instead of hardcoded 17px — proper line spacing at all DPI
//   v31.9 (May 21, 2026): Unified left-click handler; SOC/LR radius 44px, avg radius 22px; result fonts 20f, tooltip 16f; LR hints restored to Click
//   v31.8 (May 21, 2026): LR right-click uses unlimited radius (no competing curves); avg/LR result fonts restored to 22f; only hover tooltip stays at 15f
//   v31.7 (May 21, 2026): Click radius capped at 2×'W' width (22×DpiScale px); hover tooltip font 23→15pt
//   v31.6 (May 21, 2026): Chart 2 click routing: left-click=avg (Power/Temp), right-click=LR (SOC) — eliminates trace competition; added Temperature avg (EnableAvgTC66_2)
//   v31.5 (May 20, 2026): Header rows + footer all items +10% (header 20→22/23→25pt, controlFont SF14, Diag SF20, elapsed SF19, MakeButton SF19)
//   v31.4 (May 20, 2026): Font rebalance at 100% DPI — chart/header -20% (28→23, 26→22, 24→20, 20→17pt); footer +20% (controlFont SF13, Diag SF18, elapsed SF17); added FontScale const — change one line to scale all fonts
//   v31.3 (May 20, 2026): Doubled all base font sizes (14→28pt, 13→26pt, 12→24pt) for better 100% DPI readability
//   v31.2 (May 20, 2026): Single AppVersion constant in MainForm — all window title references use it; update only one line
//   v31.1 (May 19, 2026): LR 16pt+Run 15pt+avg 12pt→all 13pt; hardcoded 12f→ScaleFont; badge offsets S()-scaled
//   v31.0 (May 19, 2026): Fix loaded filename in window title: ParentForm was null in panel context; now uses this.Text
//   v30.9 (May 19, 2026): Timestamp quoted in CSV output — Excel can no longer split it on the space character
//   v30.8 (May 19, 2026): Tab delimiter detection; error message warns about Excel timestamp space corruption
//   v30.7 (May 19, 2026): Load CSV: SplitCSVLine() handles Excel 2003 quoted fields; auto-detects comma/semicolon delimiter
//   v30.6 (May 15, 2026): SubTitle removed entirely; window title bar only for filename — clean and definitive
//   v30.3 (May 15, 2026): Filename moved to window title bar — eliminates chart title collision entirely
//   v29.8 (May 15, 2026): Recording filename shown in Chart 2 title (Rec CSV active); loaded filename in Chart 1
//   v29.7 (May 15, 2026): Chart title font auto-shrinks (14pt→7pt min) to fit width — handles long loaded filenames
//   v29.6 (May 15, 2026): Loaded filename shown in Chart 1 title (no popup); restored to default on Clear
//   v29.5 (May 14, 2026): BMS Y1/Y2 primary curve thickness 4.0f→3.0f; discharge red curves less visually dominant
//   v29.4 (May 14, 2026): dt capped at 10s — 13-hour sleep gap was integrating to 20 Ah in one step; now correctly skipped
//   v29.3 (May 12, 2026): OnMouseClick checks legend box hit directly (not just flag) — eliminates marker activation on legend click
//   v29.2 (May 12, 2026): Legend LR/avg penalty narrowed: left 65% × top 25% only — top-right corner now available
//   v29.1 (May 12, 2026): Fix CS1501: leftover 3-arg Controls.Add for TC66Eff (FlowLayoutPanel takes 1 arg)
//   v29.0 (May 12, 2026): Header TableLayoutPanel→FlowLayoutPanel; AutoSize labels — no clipping at any DPI or value width
//   v28.4 (May 11, 2026): Load CSV: BOM, headerless file, and trim fixes; full CSV replay with all curves and TC66
//   v28.0 (May 10, 2026): Load CSV button introduced — major new feature replacing Stack button
//   v27.7 (May 10, 2026): Header column widths rebalanced (wider Qc/Ec/Qt/Et cols); labels left-aligned to prevent value clipping
//   v27.6 (May 10, 2026): Chart titles updated: "Voltage, Current & Capacity" and "SOC, Power & Temperature"
//   v27.5 (May 10, 2026): Tick labels=GetNiceTicks round numbers; pixel span=padded data range; zero line drawn when axis crosses zero
//   v27.4 (May 09, 2026): Tick range clamped to exact padded data range (14-20V + 2V = 12-22V, not 5-25V)
//   v27.3 (May 09, 2026): Y1/Y2 tick min/max clamped post-GetNiceTicks to ±1 step of data (fixes 0-30V and -6 to 6A excess padding)
//   v27.2 (May 08, 2026): Fix η=--%: auto-activates efficiency on first valid TC66 reading if lastTC66Reading was null at Start
//   v27.1 (May 06, 2026): Current on Chart1 now signed: positive=charging, negative=discharging (matches Qc convention)
//   v27.0 (May 06, 2026): Y2 SOC axis: clamp visY2Max to 105 before GetNiceTicks AND y2tMax to 110 after — stops rounding to 150
//   v26.9 (May 05, 2026): Qc corner badge precision F3→F2
//   v26.8 (May 05, 2026): Y2 max clamp changed 100→105% — 5% headroom prevents SOC curve overlapping top frame
//   v26.7 (May 04, 2026): Qc/Ec badge: pa.Bottom-sz.Height-38 (mirrors T/SOC formula exactly, stops dynamic-calc oscillation)
//   v26.6 (May 04, 2026): Restored v26.1 dynamic badge stacking (v26.5 wrongly reverted to hardcoded 38px)
//   v26.5 (May 04, 2026): Qc/Ec badge uses hardcoded 38px offset (matches TC66 temp/SOC pair); MeasureString padding was causing gap
//   v26.4 (May 04, 2026): TC66 curves (Y1, Y2, Temp) added to axis arrows at x=75%
//   v26.3 (May 04, 2026): Fix build error CS0748: mixed explicit/implicit lambda params in drawArrow (Color col → col)
//   v26.2 (May 04, 2026): Axis arrows on BMS curves at x=75%: Y1 curves→left arrow, Y2 curves→right arrow, color-matched, DPI-scaled
//   v26.1 (May 04, 2026): Qc/Ec badge offset changed from hardcoded 38px to actual font height (DPI-safe stacking)
//   v26.0 (May 04, 2026): TC66 draw/legend thickness 2.0f→0.5f hairline (illustration only, not primary data)
//   v25.9 (May 03, 2026): Legend placement: stable tie-breaking (score*10+preference) with top-left=0 priority; correct clean threshold
//   v25.8 (May 03, 2026): Y2 tick labels vertically centered; Qc moved to right side of Chart1; Qc/Ec badges at temp display height
//   v25.7 (May 03, 2026): Y2 never goes below 0 for non-negative data; Qc badge on Chart1 left, Ec badge on Chart2 left
//   v25.6 (May 02, 2026): Fix Qc/Ec sign root cause: Current_mA/Power_W stored as Math.Abs; now sign applied from State during accumulation
//   v25.5 (May 02, 2026): Fix build errors: duplicate totalEnergyColor; StateOfCharge_Pct→SOC_Percent
//   v25.4 (May 02, 2026): Qt/Et restored as secondary thinner curves (2.5f solid); shown only when SOC≥99% or ≤6% at session start
//   v25.3 (May 02, 2026): Fix Qc/Ec sign: segment accumulation now uses BMS natural sign (was force-positive)
//   v25.2 (Apr 30, 2026): Y1/Y2 axis padding never forces range below zero unless actual data is negative
//   v25.1 (Apr 29, 2026): Post-cleanup audit: removed Et/Qt orphaned data lists, methods, feed calls, scoring, stale conditions
//   v25.0 (Apr 29, 2026): Full Y3/resistance removal - all data lists, methods, draw calls, legend, tooltip, avg, init properties
//   v24.9 (Apr 29, 2026): Y2 padding replaced 12% with fixed units: A/Ah±0.5, W/°C/%%±2 (no physical basis for 12%)
//   v24.8 (Apr 29, 2026): Et/Qt removed from display; Ec/Qc shown with natural sign; V axis 2V fixed padding
//   v24.7 (Apr 29, 2026): Legend placement: penalized positions (score≥5000) excluded from cycle; sampling 400→1000 pts/trace
//   v24.6 (Apr 28, 2026): Qc=cycle capacity (always fed), Qt=cumulative total (always fed); tooltip labels clarified
//   v24.5 (Apr 28, 2026): Qc fed only during Charging, Qt only during Discharging (fixes overlap); tooltip labels match legend
//   v24.4 (Apr 28, 2026): Footer button widths now use BW() - measured from text at runtime, always fit label at any DPI
//   v24.3 (Apr 28, 2026): Refr button widened S(52)→S(60) to fit label at 100% DPI
//   v24.2 (Apr 27, 2026): Fix Qc/Qt/Et/Ec sparse rendering - DrawTraceSingleColor maxGap was 3s (TC66 rate); BMS curves now use 10s
//   v24.1 (Apr 27, 2026): TC66 current/power header display no longer negated during discharge; TC66 reports correct sign itself
//   v24.0 (Apr 26, 2026): All TC66 curve draw/legend thickness 2.5f→2.0f (Y1, Y2, Y3 resistance, Temp)
//   v23.9 (Apr 26, 2026): Fix build error - Qc/Qt tooltip used y2tMin/y2Span (OnPaint locals); corrected to py2Low/py2Span (cached fields)
//   v23.8 (Apr 26, 2026): Cursor tooltip now detects Qc/Qt curves; shows "Charge/Discharge Capacity" in teal/orange
//   v23.7 (Apr 26, 2026): "R" button renamed "Refr", moved to directly after Span dropdown
//   v23.6 (Apr 26, 2026): Qc/Qt both fed always using |abs| values; fixes Qt invisible (was negative, below axis)
//   v23.5 (Apr 26, 2026): TC66 draw/legend thickness 3.0f→2.5f; Y2 capacity title state-specific "& Qc/Qt (Ah)" in teal/orange
//   v23.4 (Apr 25, 2026): Legend placement rewritten - tests actual box pixel overlap for all 9 candidates, cycles all 9 by score
//   v23.3 (Apr 25, 2026): Run: line appends ";  Tot: H:MM h" (run + LR remaining, rounded to 10 min) when LR is active
//   v23.2 (Apr 24, 2026): Added Qc (teal) and Qt (orange) capacity curves to Chart1 Y2 axis
//   v23.1 (Apr 24, 2026): TC66 header row height matched to main header row (both S(40))
//   v23.0 (Apr 23, 2026): Y1 title fixed: "Power (W) & Energy (Wh)" with "& Energy (Wh)" in purple (matches Et/Ec curves)
//   v22.9 (Apr 23, 2026): Normalized legend/draw thicknesses to two groups: BMS=3.5f, TC66=1.5f (Et,Ec,BmsTemp→3.5f; Temp(TC66)→1.5f)
//   v22.8 (Apr 23, 2026): Legend TC66 paired entries drop "(TC66)" suffix; left/right axis borders and tick marks state-colored
//   v22.7 (Apr 23, 2026): Legend entry renamed "Power (W)" (not "Power & Energy"); energy curves brighter purple, 6.5pt
//   v22.6 (Apr 22, 2026): CSV always includes TC66 column headers (prevents missing titles bug)
//   v22.5 (Apr 22, 2026): TC66 header font matches first row, legend font reduced 30% (10pt)
//   v22.4 (Apr 22, 2026): CSV outputs A and Ah instead of mA and mAh (standard units)
//   v22.3 (Apr 21, 2026): Clamped Y2 axis (SOC) to 0-100% range
//   v22.2 (Apr 21, 2026): Increased bottom margin (mB=95) to prevent footer/X-axis overlap at high DPI
//   v22.1 (Apr 21, 2026): Header font conditional: 12pt at 100% DPI, 14pt at 150%/200%
//   v22.0 (Mar 17, 2026): Y1 axis title at 5px (final position, clear of all tick labels)
//   v21.9 (Mar 17, 2026): Y1 axis title at 12px
//   v21.8 (Mar 17, 2026): Y-axis titles corrected direction (Y1: 20px, Y2: Width-8)
//   v21.7 (Mar 17, 2026): Wrong direction - moved too far (Y1: 38px, Y2: Width-36)
//   v21.6 (Mar 17, 2026): Header font reduced to 14pt to prevent unit truncation
//   v21.5 (Mar 17, 2026): Fixed Y1 axis title position (28px left), legend spacing (28px)
//   v21.4 (Mar 17, 2026): Header font 15pt, chart titles bold
//   v21.3 (Mar 17, 2026): Reduced header height to S(40) for tight single-row alignment
//   v21.2 (Mar 17, 2026): Fixed header label vertical alignment - balanced top/bottom padding
//   v21.1 (Mar 17, 2026): Attempt 1 - removed padding (too low)
//   v21.0 (Mar 17, 2026): Reduced header/footer font sizes (16pt/11pt) to match v20.0 baseline
//   v20.9 (Mar 17, 2026): Fixed X-axis title positioning - now relative to plot area, not form height
//   v20.8 (Mar 17, 2026): Fixed overlaps - X-axis title spacing, legend margin to avoid value badges
//   v20.7 (Mar 17, 2026): Increased legend spacing further - lineHeight 35px, gap 12px for clarity
//   v20.6 (Mar 17, 2026): Fixed legend spacing - increased line height and padding for 14pt font
//   v20.5 (Mar 17, 2026): Increased chart font base sizes for better readability at 100% DPI
//   v20.4 (Mar 17, 2026): Reduced chart font base sizes by 30-40% to work properly with DPI scaling
//   v20.3 (Mar 17, 2026): All chart fonts DPI-scaled (titles, labels, legend, tooltip, LR, avg)
//   v20.2 (Mar 17, 2026): Rebuilt control bar - unified 13pt font, proper DPI scaling, consistent sizing
//   v20.1 (Mar 17, 2026): Fixed font scaling - now uses Windows DPI setting (not screen resolution)
//   v20.0 (Mar 17, 2026): Y1 axis includes energy data in range calc, clamped min to 0
//   v19.9 (Mar 17, 2026): Energy curves increased to 5.0f for visual parity (anti-alias compensation)
//   v19.8 (Mar 17, 2026): Energy curves thickness 4.0f (match Power/SOC), added tooltip support
//   v19.7 (Mar 17, 2026): Added energy curves to Chart2 (Et & Ec on Y1 with Power, purple)
//   v19.6 (Mar 17, 2026): Added capacity/energy columns to CSV (Qt, Et, Qc, Ec)
//   v19.5 (Mar 17, 2026): Added run time display below LR prediction (like ViRecorder)
//   v19.4 (Mar 17, 2026): Continuous CSV recording - auto-saves every 10s until stopped
//   v19.3 (Mar 17, 2026): Chart1 visibility: brighter BMS voltage, thicker BMS curves, 
//                         lighter TC66 current, voltage curves drawn on top
//   v19.2 (Mar 8, 2026): Fixed legend click bug completely using flag approach
//   v19.1 (Mar 8, 2026): Fixed Refresh button + partial legend click fix
//   v19.0 (Mar 8, 2026): Added 2-click averaging to BMS and TC66 voltage curves
//   v18.8 (Mar 5, 2026): Fixed Y2 axis label - no unnecessary "T (°C)" without data
//   v18.7 (Mar 1, 2026): Stable working version - TC66 curves and screen flip work
//   v18.0-18.6: BMS temp support, cosmetic improvements, UI polish
//   v17.0-17.9: TC66 averages, legend repositioning, CSV export
//   v16.0-16.7: Auto-legend positioning, diagnostics, real-time CSV removed
//   v15.0-15.3: Refresh button, major crash fixes
//   v11.0-14.0: Core dual-chart system with TC66 integration
//
// Author: Tony Gozdz (tonygozdz@gmail.com)
// Repository: https://github.com/TonyGozdz/Battery-Monitoring-Apps
// Copyright (c) 2026 Tony Gozdz. All rights reserved.
//
// Compile: double-click build.bat (no IDE or SDK needed)
// Requires: Windows 10/11 with .NET Framework 4.x (pre-installed)
//=============================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace BatteryMonitor
{
    // ========================================================================
    // Data Structures
    // ========================================================================
    public enum BatteryState { Charging, Discharging, Idle }

    public class BatteryReading
    {
        public DateTime Timestamp;
        public double ElapsedSeconds;
        public double Voltage_V;
        public double Current_mA;
        public double Power_W;
        public double SOC_Percent;
        public BatteryState State;
        public double Temperature_C;  // BMS battery temperature (if available)
        public bool HasTemperature;   // Whether temperature reading is valid
        
        // Cumulative capacity and energy (from app start)
        public double TotalCapacity_mAh;
        public double TotalEnergy_Wh;
        public double SegmentCapacity_mAh;
        public double SegmentEnergy_Wh;
        
        // TC66 USB power meter data (if connected)
        public double TC66_V;
        public double TC66_A;
        public double TC66_W;
        public int TC66_Temp_C;
        public int TC66_mAh;
        public int TC66_mWh;
        public bool HasTC66Data;
    }

    // ========================================================================
    // Battery Reader
    // ========================================================================
    public class BatteryReader
    {
        [DllImport("PowrProf.dll")]
        private static extern uint CallNtPowerInformation(
            int InformationLevel, IntPtr lpInputBuffer, int nInputBufferSize,
            IntPtr lpOutputBuffer, int nOutputBufferSize);

        private const int SystemBatteryState = 5;

        [StructLayout(LayoutKind.Explicit)]
        private struct SYSTEM_BATTERY_STATE
        {
            [FieldOffset(0)]  public byte AcOnLine;
            [FieldOffset(1)]  public byte BatteryPresent;
            [FieldOffset(2)]  public byte Charging;
            [FieldOffset(3)]  public byte Discharging;
            [FieldOffset(8)]  public uint MaxCapacity;
            [FieldOffset(12)] public uint RemainingCapacity;
            [FieldOffset(16)] public int  Rate;
            [FieldOffset(20)] public uint EstimatedTime;
            [FieldOffset(24)] public uint DefaultAlert1;
            [FieldOffset(28)] public uint DefaultAlert2;
        }

        private double cachedVoltage_mV = 0;
        private double cachedTemperature_C = 0;
        private bool hasTemperature = false;
        private DateTime lastWmiQuery = DateTime.MinValue;
        private const double WMI_QUERY_INTERVAL_SEC = 10;

        public BatteryReading Read(double elapsedSeconds)
        {
            var reading = new BatteryReading();
            reading.Timestamp = DateTime.Now;
            reading.ElapsedSeconds = elapsedSeconds;

            try
            {
                int size = Marshal.SizeOf(typeof(SYSTEM_BATTERY_STATE));
                IntPtr ptr = Marshal.AllocHGlobal(size);
                try
                {
                    uint status = CallNtPowerInformation(SystemBatteryState,
                        IntPtr.Zero, 0, ptr, size);

                    if (status == 0)
                    {
                        var state = (SYSTEM_BATTERY_STATE)Marshal.PtrToStructure(
                            ptr, typeof(SYSTEM_BATTERY_STATE));

                        if (state.BatteryPresent == 0)
                        {
                            reading.State = BatteryState.Idle;
                            return reading;
                        }

                        reading.State = state.Charging != 0 ? BatteryState.Charging :
                                       state.Discharging != 0 ? BatteryState.Discharging :
                                       BatteryState.Idle;

                        reading.SOC_Percent = state.MaxCapacity > 0 ?
                            (double)state.RemainingCapacity / state.MaxCapacity * 100.0 : 0;

                        reading.Power_W = Math.Abs(state.Rate) / 1000.0;

                        double now_sec = (DateTime.Now - lastWmiQuery).TotalSeconds;
                        if (now_sec >= WMI_QUERY_INTERVAL_SEC || cachedVoltage_mV == 0)
                        {
                            double v = GetVoltageFromWMI();
                            if (v > 0)
                            {
                                cachedVoltage_mV = v;
                                lastWmiQuery = DateTime.Now;
                            }
                            
                            // Also query battery temperature
                            double temp = GetBatteryTemperatureFromWMI();
                            if (temp > -100)  // Valid temperature range check
                            {
                                cachedTemperature_C = temp;
                                hasTemperature = true;
                            }
                        }

                        reading.Voltage_V = cachedVoltage_mV / 1000.0;
                        reading.Temperature_C = cachedTemperature_C;
                        reading.HasTemperature = hasTemperature;

                        if (reading.Voltage_V > 0)
                            reading.Current_mA = Math.Abs(state.Rate) / reading.Voltage_V;
                        else
                            reading.Current_mA = 0;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BatteryReader: " + ex.Message);
            }

            return reading;
        }

        private double GetVoltageFromWMI()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("root\\WMI",
                    "SELECT Voltage FROM BatteryStatus WHERE Voltage > 0"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        object v = obj["Voltage"];
                        if (v != null)
                        {
                            double val = Convert.ToDouble(v);
                            if (val > 0) return val;
                        }
                    }
                }
            }
            catch { }

            try
            {
                using (var searcher = new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT DesignVoltage FROM Win32_Battery"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        object v = obj["DesignVoltage"];
                        if (v != null)
                        {
                            double val = Convert.ToDouble(v);
                            if (val > 0) return val;
                        }
                    }
                }
            }
            catch { }

            return 0;
        }

        private double GetBatteryTemperatureFromWMI()
        {
            try
            {
                // Try MsAcpi_ThermalZoneTemperature with battery-related instance names
                // Common patterns: BATZ, BAT0, BAT1, TZ00, TZ01
                string[] patterns = { "BATZ", "BAT0", "BAT1", "BAT_", "_BAT" };
                
                using (var searcher = new ManagementObjectSearcher("root\\WMI",
                    "SELECT * FROM MSAcpi_ThermalZoneTemperature"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        object instanceName = obj["InstanceName"];
                        object currentTemp = obj["CurrentTemperature"];
                        
                        if (instanceName != null && currentTemp != null)
                        {
                            string name = instanceName.ToString().ToUpper();
                            
                            // Check if instance name matches battery thermal zone patterns
                            foreach (string pattern in patterns)
                            {
                                if (name.Contains(pattern))
                                {
                                    double tempKelvin = Convert.ToDouble(currentTemp);
                                    // Convert from tenths of Kelvin to Celsius
                                    double tempC = (tempKelvin - 2732.0) / 10.0;
                                    
                                    // Sanity check: battery temp should be between -20°C and 80°C
                                    if (tempC >= -20 && tempC <= 80)
                                        return tempC;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BatteryReader GetTemperature: " + ex.Message);
            }
            
            return -999;  // Indicates no valid temperature found
        }

        public bool IsBatteryPresent()
        {
            try
            {
                int size = Marshal.SizeOf(typeof(SYSTEM_BATTERY_STATE));
                IntPtr ptr = Marshal.AllocHGlobal(size);
                try
                {
                    uint status = CallNtPowerInformation(SystemBatteryState,
                        IntPtr.Zero, 0, ptr, size);
                    if (status == 0)
                    {
                        var state = (SYSTEM_BATTERY_STATE)Marshal.PtrToStructure(
                            ptr, typeof(SYSTEM_BATTERY_STATE));
                        return state.BatteryPresent != 0;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            catch { }
            return false;
        }

        public void WriteThermalDiagnostics(string filepath)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(filepath))
                {
                    sw.WriteLine("=============================================================================");
                    sw.WriteLine("Battery Monitor - Thermal Zone Diagnostics");
                    sw.WriteLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    sw.WriteLine("=============================================================================");
                    sw.WriteLine();
                    
                    // 1. Enumerate all MSAcpi_ThermalZoneTemperature instances
                    sw.WriteLine("--- MSAcpi_ThermalZoneTemperature (root\\WMI) ---");
                    try
                    {
                        using (var searcher = new ManagementObjectSearcher("root\\WMI",
                            "SELECT * FROM MSAcpi_ThermalZoneTemperature"))
                        {
                            int count = 0;
                            foreach (ManagementObject obj in searcher.Get())
                            {
                                count++;
                                object instanceNameObj = obj["InstanceName"];
                                string instanceName = (instanceNameObj != null) ? instanceNameObj.ToString() : "null";
                                object tempObj = obj["CurrentTemperature"];
                                string tempStr = "null";
                                if (tempObj != null)
                                {
                                    double tempKelvin = Convert.ToDouble(tempObj);
                                    double tempC = (tempKelvin - 2732.0) / 10.0;
                                    tempStr = tempKelvin + " (tenth-Kelvin) = " + tempC.ToString("F1") + " °C";
                                }
                                sw.WriteLine("  Thermal Zone " + count + ":");
                                sw.WriteLine("    InstanceName: " + instanceName);
                                sw.WriteLine("    CurrentTemperature: " + tempStr);
                                sw.WriteLine();
                            }
                            if (count == 0)
                                sw.WriteLine("  No thermal zones found");
                        }
                    }
                    catch (Exception ex)
                    {
                        sw.WriteLine("  Error: " + ex.Message);
                    }
                    sw.WriteLine();
                    
                    // 2. Check BatteryStatus (root\WMI)
                    sw.WriteLine("--- BatteryStatus (root\\WMI) ---");
                    try
                    {
                        using (var searcher = new ManagementObjectSearcher("root\\WMI",
                            "SELECT * FROM BatteryStatus"))
                        {
                            foreach (ManagementObject obj in searcher.Get())
                            {
                                sw.WriteLine("  Available properties:");
                                foreach (PropertyData prop in obj.Properties)
                                {
                                    string val = (prop.Value != null) ? prop.Value.ToString() : "null";
                                    sw.WriteLine("    " + prop.Name + " = " + val);
                                }
                                sw.WriteLine();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        sw.WriteLine("  Error: " + ex.Message);
                    }
                    sw.WriteLine();
                    
                    // 3. Check Win32_Battery (root\CIMV2)
                    sw.WriteLine("--- Win32_Battery (root\\CIMV2) ---");
                    try
                    {
                        using (var searcher = new ManagementObjectSearcher("root\\CIMV2",
                            "SELECT * FROM Win32_Battery"))
                        {
                            foreach (ManagementObject obj in searcher.Get())
                            {
                                sw.WriteLine("  Available properties:");
                                foreach (PropertyData prop in obj.Properties)
                                {
                                    string val = (prop.Value != null) ? prop.Value.ToString() : "null";
                                    sw.WriteLine("    " + prop.Name + " = " + val);
                                }
                                sw.WriteLine();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        sw.WriteLine("  Error: " + ex.Message);
                    }
                    sw.WriteLine();
                    
                    // 4. Check Win32_TemperatureProbe (root\CIMV2)
                    sw.WriteLine("--- Win32_TemperatureProbe (root\\CIMV2) ---");
                    try
                    {
                        using (var searcher = new ManagementObjectSearcher("root\\CIMV2",
                            "SELECT * FROM Win32_TemperatureProbe"))
                        {
                            int count = 0;
                            foreach (ManagementObject obj in searcher.Get())
                            {
                                count++;
                                sw.WriteLine("  Temperature Probe " + count + ":");
                                foreach (PropertyData prop in obj.Properties)
                                {
                                    string val = (prop.Value != null) ? prop.Value.ToString() : "null";
                                    sw.WriteLine("    " + prop.Name + " = " + val);
                                }
                                sw.WriteLine();
                            }
                            if (count == 0)
                                sw.WriteLine("  No temperature probes found");
                        }
                    }
                    catch (Exception ex)
                    {
                        sw.WriteLine("  Error: " + ex.Message);
                    }
                    sw.WriteLine();
                    
                    // 5. Check BatteryFullChargedCapacity (root\WMI)
                    sw.WriteLine("--- BatteryFullChargedCapacity (root\\WMI) ---");
                    try
                    {
                        using (var searcher = new ManagementObjectSearcher("root\\WMI",
                            "SELECT * FROM BatteryFullChargedCapacity"))
                        {
                            foreach (ManagementObject obj in searcher.Get())
                            {
                                foreach (PropertyData prop in obj.Properties)
                                {
                                    string val = (prop.Value != null) ? prop.Value.ToString() : "null";
                                    sw.WriteLine("    " + prop.Name + " = " + val);
                                }
                                sw.WriteLine();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        sw.WriteLine("  Error: " + ex.Message);
                    }
                    sw.WriteLine();
                    
                    sw.WriteLine("=============================================================================");
                    sw.WriteLine("Diagnostics complete");
                    sw.WriteLine("=============================================================================");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("WriteThermalDiagnostics error: " + ex.Message);
            }
        }
    }

    // ========================================================================
    // TC66 USB Meter Data Structure
    // ========================================================================
    public class TC66Reading
    {
        public double Voltage_V;
        public double Current_A;
        public double Power_W;
        public double Resistance_Ohm;
        public int Temperature_C;
        public int Group0_mAh;
        public int Group0_mWh;
        public int Group1_mAh;
        public int Group1_mWh;
        public double DpVoltage_V;
        public double DmVoltage_V;
        public string FirmwareVersion;
        public uint SerialNumber;
        public bool IsValid;
    }

    // ========================================================================
    // TC66 USB Meter Reader (Serial over USB)
    // ========================================================================
    public class TC66Reader : IDisposable
    {
        private SerialPort port;
        private Aes aes;
        
        // AES-256-ECB key for TC66 protocol (from sigrok documentation)
        private static readonly byte[] AES_KEY = new byte[] {
            0x58, 0x21, 0xfa, 0x56, 0x01, 0xb2, 0xf0, 0x26,
            0x87, 0xff, 0x12, 0x04, 0x62, 0x2a, 0x4f, 0xb0,
            0x86, 0xf4, 0x02, 0x60, 0x81, 0x6f, 0x9a, 0x0b,
            0xa7, 0xf1, 0x06, 0x61, 0x9a, 0xb8, 0x72, 0x88
        };

        public bool IsConnected { get { return port != null && port.IsOpen; } }
        public string PortName { get { return port != null ? port.PortName : ""; } }

        public TC66Reader()
        {
            aes = Aes.Create();
            aes.Key = AES_KEY;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
        }

        public static string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }

        public bool Connect(string portName)
        {
            try
            {
                Disconnect();
                port = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
                port.ReadTimeout = 2000;
                port.WriteTimeout = 1000;
                port.Open();
                
                // Test communication with a poll
                var test = Poll();
                if (!test.IsValid)
                {
                    Disconnect();
                    return false;
                }
                return true;
            }
            catch
            {
                Disconnect();
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (port != null)
                {
                    if (port.IsOpen) port.Close();
                    port.Dispose();
                    port = null;
                }
            }
            catch { }
        }

        public TC66Reading Poll()
        {
            var reading = new TC66Reading { IsValid = false };
            if (port == null || !port.IsOpen) return reading;

            try
            {
                // Clear any pending data
                port.DiscardInBuffer();
                
                // Send poll command
                byte[] cmd = Encoding.ASCII.GetBytes("getva");
                port.Write(cmd, 0, cmd.Length);

                // Read 192 bytes response
                byte[] encrypted = new byte[192];
                int totalRead = 0;
                int attempts = 0;
                while (totalRead < 192 && attempts < 50)
                {
                    int toRead = Math.Min(port.BytesToRead, 192 - totalRead);
                    if (toRead > 0)
                    {
                        int read = port.Read(encrypted, totalRead, toRead);
                        totalRead += read;
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(20);
                    }
                    attempts++;
                }

                if (totalRead < 192) return reading;

                // Decrypt — verify all 192 bytes were written
                byte[] decrypted = new byte[192];
                using (var decryptor = aes.CreateDecryptor())
                {
                    int written = decryptor.TransformBlock(encrypted, 0, 192, decrypted, 0);
                    if (written != 192) return reading;
                }

                // Verify pac1 header
                if (decrypted[0] != 'p' || decrypted[1] != 'a' || 
                    decrypted[2] != 'c' || decrypted[3] != '1')
                    return reading;

                // Parse pac1 (bytes 0-63)
                reading.FirmwareVersion = GetString(decrypted, 8, 4);
                reading.SerialNumber = GetU32(decrypted, 12);
                reading.Voltage_V = GetU32(decrypted, 48) * 1e-4;
                reading.Current_A = GetU32(decrypted, 52) * 1e-5;
                reading.Power_W = GetU32(decrypted, 56) * 1e-4;

                // Parse pac2 (bytes 64-127)
                reading.Resistance_Ohm = GetU32(decrypted, 68) * 1e-2;
                reading.Group0_mAh = (int)GetU32(decrypted, 72);
                reading.Group0_mWh = (int)GetU32(decrypted, 76);
                reading.Group1_mAh = (int)GetU32(decrypted, 80);
                reading.Group1_mWh = (int)GetU32(decrypted, 84);
                int tempSign = (int)GetU32(decrypted, 88);
                int tempVal = (int)GetU32(decrypted, 92);
                reading.Temperature_C = tempSign != 0 ? -tempVal : tempVal;
                reading.DpVoltage_V = GetU32(decrypted, 96) * 1e-2;
                reading.DmVoltage_V = GetU32(decrypted, 100) * 1e-2;

                reading.IsValid = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("TC66 Poll error: " + ex.Message);
            }

            return reading;
        }

        public bool RotateScreen()
        {
            if (port == null || !port.IsOpen) return false;

            try
            {
                // Clear any pending data
                port.DiscardInBuffer();
                
                // Send rotate command
                byte[] cmd = Encoding.ASCII.GetBytes("rotate");
                port.Write(cmd, 0, cmd.Length);
                
                System.Threading.Thread.Sleep(100);  // Brief delay for command processing
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("TC66 RotateScreen error: " + ex.Message);
                return false;
            }
        }

        private uint GetU32(byte[] buf, int offset)
        {
            if (buf == null || offset < 0 || offset + 3 >= buf.Length) return 0;
            return (uint)(buf[offset] | (buf[offset + 1] << 8) |
                         (buf[offset + 2] << 16) | (buf[offset + 3] << 24));
        }

        private string GetString(byte[] buf, int offset, int maxLen)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < maxLen && buf[offset + i] != 0; i++)
                sb.Append((char)buf[offset + i]);
            return sb.ToString();
        }

        public void Dispose()
        {
            Disconnect();
            if (aes != null) { aes.Dispose(); aes = null; }
        }
    }

    // ========================================================================
    // Nice tick calculator
    // ========================================================================
    public static class TickCalc
    {
        public static void GetNiceTicks(double dataMin, double dataMax, int maxTicks,
            out double tickMin, out double tickMax, out double tickStep)
        {
            if (dataMax - dataMin < 1e-9)
            {
                dataMin -= 1; dataMax += 1;
            }

            double range = NiceNum(dataMax - dataMin, false);
            tickStep = NiceNum(range / (maxTicks - 1), true);
            tickMin = Math.Floor(dataMin / tickStep) * tickStep;
            tickMax = Math.Ceiling(dataMax / tickStep) * tickStep;

            if (tickMax <= tickMin) { tickMax = tickMin + tickStep; }
        }

        private static double NiceNum(double range, bool round)
        {
            if (range <= 0) range = 1;
            double exponent = Math.Floor(Math.Log10(range));
            double fraction = range / Math.Pow(10, exponent);
            double nice;

            if (round)
            {
                if (fraction < 1.5) nice = 1;
                else if (fraction < 3) nice = 2;
                else if (fraction < 7) nice = 5;
                else nice = 10;
            }
            else
            {
                if (fraction <= 1) nice = 1;
                else if (fraction <= 2) nice = 2;
                else if (fraction <= 5) nice = 5;
                else nice = 10;
            }

            return nice * Math.Pow(10, exponent);
        }

        public static string SmartFormat(double val, double step)
        {
            if (step >= 1) return val.ToString("F0");
            int decimals = Math.Max(0, -(int)Math.Floor(Math.Log10(step)));
            decimals = Math.Min(decimals, 4);
            return val.ToString("F" + decimals);
        }
    }

    // ========================================================================
    // Helper class for legend entries
    // ========================================================================
    internal class LegendEntry
    {
        public string Text;
        public Color Color;
        public float LineWidth;
        public bool Dashed;
        
        public LegendEntry(string text, Color color, float lineWidth, bool dashed)
        {
            Text = text;
            Color = color;
            LineWidth = lineWidth;
            Dashed = dashed;
        }
    }

    // ========================================================================
    // DualAxisChartPanel - Y1 (left) + Y2 (right), with LR and cursor tooltip
    // ========================================================================
    public class DualAxisChartPanel : Panel
    {
        public string ChartTitle = "";
        public string Y1Title = "";
        public string Y2Title = "";
        public string Y1LegendTitle = "";  // Optional separate legend title for Y1 (defaults to Y1Title if empty)
        public string Y2LegendTitle = "";  // Optional separate legend title for Y2 (defaults to Y2Title if empty)
        public string Y1Unit = "";
        public string Y2Unit = "";
        public double TimeWindow = 3600;
        
        // DPI scaling factor for fonts (set by parent form)
        public float DpiScale = 1.0f;
        public float FontScale = 1.0f;  // Global font scale multiplier (set by parent form)
        
        // Helper method to scale font sizes based on DPI
        private float ScaleFont(float baseSize)
        {
            return baseSize * FontScale * DpiScale;
        }
        
        // Helper method to scale integer values based on DPI
        private int S(int baseValue)
        {
            return (int)(baseValue * DpiScale);
        }
        public int MaxTicks = 6;
        
        // Run time display (updated from main form)
        public double RunTimeSeconds = 0;

        // Enable 3-click LR on Y2 axis (used for SOC chart)
        public bool EnableLR = false;

        // Y1 data
        private List<double> xData1 = new List<double>();
        private List<double> yData1 = new List<double>();
        private List<BatteryState> stateData1 = new List<BatteryState>();
        private double y1Min = double.MaxValue, y1Max = double.MinValue;

        // Y2 data
        private List<double> xData2 = new List<double>();
        private List<double> yData2 = new List<double>();
        private List<BatteryState> stateData2 = new List<BatteryState>();
        private double y2Min = double.MaxValue, y2Max = double.MinValue;

        // TC66 data (displayed as dashed/thin lines overlaid on Y1/Y2)
        private List<double> xDataTC66_1 = new List<double>();
        private List<double> yDataTC66_1 = new List<double>();
        private List<BatteryState> stateDataTC66_1 = new List<BatteryState>();
        private List<double> xDataTC66_2 = new List<double>();
        private List<double> yDataTC66_2 = new List<double>();
        private List<BatteryState> stateDataTC66_2 = new List<BatteryState>();

        // Temperature data (TC66 - red curve on Y2 axis)
        private List<double> xDataTemp = new List<double>();
        private List<double> yDataTemp = new List<double>();

        // BMS Temperature data (if available - darker orange curve on Y2 axis)
        private List<double> xDataBMSTemp = new List<double>();
        private List<double> yDataBMSTemp = new List<double>();

        // Energy data (Ec on Y1 axis - segment energy, natural sign)
        private List<double> xDataEnergySegment = new List<double>();
        private List<double> yDataEnergySegment = new List<double>();
        // Et on Y1 axis - cumulative total energy, shown only when session started from known SOC
        private List<double> xDataEnergyTotal = new List<double>();
        private List<double> yDataEnergyTotal = new List<double>();

        // Capacity data (Qc on Y2 axis - cycle capacity, natural sign)
        private List<double> xDataCapCharge = new List<double>();
        private List<double> yDataCapCharge = new List<double>();
        // Qt on Y2 axis - cumulative total capacity, shown only when session started from known SOC
        private List<double> xDataCapDischarge = new List<double>();
        private List<double> yDataCapDischarge = new List<double>();

        // LR state: 0=no clicks, 1=first point set, 2=LR line drawn
        private int lrClickState = 0;
        private int lrIdx1 = -1;       // first selected data index (Y2)
        private int lrIdx2 = -1;       // second selected data index (Y2)
        private double lrSlope, lrIntercept;
        private double lrZeroTime = -1;  // elapsed seconds where LR hits SOC=0
        private double lrRemainingSeconds = -1;  // predicted seconds remaining (from LR calc)
        private string lrPrediction = "";

        // Callback to report LR prediction to main form
        public Action<string> OnLRPrediction;

        // Average calculation state: 0=no clicks, 1=first point set, 2=avg calculated
        public bool EnableAvgY1 = false;  // Enable avg on Y1 trace
        public bool EnableAvgY2 = false;  // Enable avg on Y2 trace
        public bool EnableAvgTC66_1 = false;  // Enable avg on TC66 Y1 trace (voltage)
        public bool EnableAvgTC66_2 = false;  // Enable avg on TC66 Y2 trace (current on Chart1, power on Chart2)
        private int avgClickState = 0;
        private int avgTrace = 0;         // Which trace: 1=Y1, 2=Y2, 5=TC66_1, 6=TC66_2
        private int avgIdx1 = -1, avgIdx2 = -1;
        private double avgValue = 0;
        private string avgResult = "";

        // Plot area (cached for hit-testing)
        private Rectangle plotArea;
        
        // Legend position tracking (for click-to-reposition)
        private float legendBoxX, legendBoxY, legendBoxWidth, legendBoxHeight;
        private bool legendWasClicked = false;
        private int legendPositionIndex = 0;
        // Legend position cache — recomputed only when layout or data window changes
        private float cachedLegendX = -1, cachedLegendY = -1;
        private float cachedLegendBoxW = -1, cachedLegendBoxH = -1;
        private Rectangle cachedLegendPlotArea;
        private int cachedLegendDataCount = -1;
        private double cachedLegendXFirst = double.NaN, cachedLegendXRange = double.NaN;
        
        // Current BMS state (for axis tick colors)
        private BatteryState currentBatteryState = BatteryState.Idle;
        public BatteryState CurrentBatteryState 
        { 
            get { return currentBatteryState; }
            set { currentBatteryState = value; }
        }
        private double pxFirst, pxRange;
        private double py2Low, py2Span;
        
        // Cursor tracking for tooltip
        private Point cursorPos = Point.Empty;
        private bool cursorInChart = false;
        
        // Cached axis scales for tooltip calculation
        private double cachedY1Low, cachedY1Span;

        // Theme (light)
        private static readonly Color BgColor       = Color.FromArgb(255, 255, 255);
        private static readonly Color GridColor      = Color.FromArgb(210, 210, 220);
        private static readonly Color AxisFrameColor = Color.FromArgb(20, 20, 20);
        private static readonly Color TextColor      = Color.Black;
        private static readonly Color TitleColor     = Color.FromArgb(30, 80, 180);

        // Chart-specific colors (set per instance)
        public Color Y1ChargeColor     = Color.FromArgb(46, 140, 50);
        public Color Y1DischargeColor  = Color.FromArgb(211, 47, 47);
        public Color Y1IdleColor       = Color.FromArgb(120, 120, 130);
        public Color Y2ChargeColor     = Color.FromArgb(0, 151, 167);
        public Color Y2DischargeColor  = Color.FromArgb(142, 36, 170);
        public Color Y2IdleColor       = Color.FromArgb(120, 120, 130);
        public Color TempColor         = Color.Red;
        public Color BMSTempColor      = Color.FromArgb(204, 102, 0);
        
        // TC66-specific colors (lighter than BMS for visual differentiation)
        public Color TC66Y1ChargeColor     = Color.FromArgb(102, 187, 106);
        public Color TC66Y1DischargeColor  = Color.FromArgb(244, 67, 54);
        public Color TC66Y1IdleColor       = Color.FromArgb(158, 158, 158);
        public Color TC66Y2ChargeColor     = Color.FromArgb(38, 198, 218);
        public Color TC66Y2DischargeColor  = Color.FromArgb(171, 71, 188);
        public Color TC66Y2IdleColor       = Color.FromArgb(158, 158, 158);

        private static readonly Color Y1AxisColor = Color.Black;
        private static readonly Color Y2AxisColor = Color.Black;

        private static readonly Color LRLineColor   = Color.FromArgb(255, 0, 128);
        private static readonly Color LRMarkerColor = Color.FromArgb(255, 0, 128);

        private const int TickLen = 10;

        public DualAxisChartPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.ResizeRedraw, true);
            this.MouseMove += OnMouseMoveHandler;
            this.MouseLeave += OnMouseLeaveHandler;
        }
        
        private void OnMouseMoveHandler(object sender, MouseEventArgs e)
        {
            cursorPos = e.Location;
            cursorInChart = plotArea.Contains(e.Location);
            this.Invalidate();
        }
        
        private void OnMouseLeaveHandler(object sender, EventArgs e)
        {
            cursorInChart = false;
            this.Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            // Check if click is inside legend box FIRST (before calling base)
            if (e.Button == MouseButtons.Left &&
                e.X >= legendBoxX && e.X <= legendBoxX + legendBoxWidth &&
                e.Y >= legendBoxY && e.Y <= legendBoxY + legendBoxHeight)
            {
                // Cycle to next best position (top 3 only)
                legendPositionIndex++;
                cachedLegendX = -1;  // Force reposition on next paint
                legendWasClicked = true;  // Set flag to prevent marker in OnMouseClick
                this.Refresh();  // Force immediate repaint of ONLY this chart
                return;  // Don't call base - prevent event propagation
            }
            
            legendWasClicked = false;  // Not a legend click
            base.OnMouseDown(e);
        }

        public void AddY1Point(double elapsed, double value, BatteryState state)
        {
            xData1.Add(elapsed);
            yData1.Add(value);
            stateData1.Add(state);
            if (value < y1Min) y1Min = value;
            if (value > y1Max) y1Max = value;
            this.Invalidate();
        }

        public void AddY2Point(double elapsed, double value, BatteryState state)
        {
            xData2.Add(elapsed);
            yData2.Add(value);
            stateData2.Add(state);
            if (value < y2Min) y2Min = value;
            if (value > y2Max) y2Max = value;
            this.Invalidate();
        }

        public void AddTC66Y1Point(double elapsed, double value, BatteryState state)
        {
            xDataTC66_1.Add(elapsed);
            yDataTC66_1.Add(value);
            stateDataTC66_1.Add(state);
            if (value < y1Min) y1Min = value;
            if (value > y1Max) y1Max = value;
            this.Invalidate();
        }

        public void AddTC66Y2Point(double elapsed, double value, BatteryState state)
        {
            xDataTC66_2.Add(elapsed);
            yDataTC66_2.Add(value);
            stateDataTC66_2.Add(state);
            if (value < y2Min) y2Min = value;
            if (value > y2Max) y2Max = value;
            this.Invalidate();
        }

        public void AddTempPoint(double elapsed, double value)
        {
            xDataTemp.Add(elapsed);
            yDataTemp.Add(value);
            // Update Y2 min/max to include temperature
            if (value < y2Min) y2Min = value;
            if (value > y2Max) y2Max = value;
            this.Invalidate();
        }

        public void AddBMSTempPoint(double elapsed, double value)
        {
            xDataBMSTemp.Add(elapsed);
            yDataBMSTemp.Add(value);
            // Update Y2 min/max to include BMS temperature
            if (value < y2Min) y2Min = value;
            if (value > y2Max) y2Max = value;
            this.Invalidate();
        }

        public void AddEnergySegmentPoint(double elapsed, double value)
        {
            xDataEnergySegment.Add(elapsed);
            yDataEnergySegment.Add(value);
            if (value < y1Min) y1Min = value;
            if (value > y1Max) y1Max = value;
            this.Invalidate();
        }

        public void AddEnergyTotalPoint(double elapsed, double value)
        {
            xDataEnergyTotal.Add(elapsed);
            yDataEnergyTotal.Add(value);
            if (value < y1Min) y1Min = value;
            if (value > y1Max) y1Max = value;
            this.Invalidate();
        }

        public void AddCapacityChargePoint(double elapsed, double value)
        {
            xDataCapCharge.Add(elapsed);
            yDataCapCharge.Add(value);
            if (value < y2Min) y2Min = value;
            if (value > y2Max) y2Max = value;
            this.Invalidate();
        }

        public void AddCapacityDischargePoint(double elapsed, double value)
        {
            xDataCapDischarge.Add(elapsed);
            yDataCapDischarge.Add(value);
            if (value < y2Min) y2Min = value;
            if (value > y2Max) y2Max = value;
            this.Invalidate();
        }

        public void ClearData()
        {
            xData1.Clear(); yData1.Clear(); stateData1.Clear();
            xData2.Clear(); yData2.Clear(); stateData2.Clear();
            xDataTC66_1.Clear(); yDataTC66_1.Clear(); stateDataTC66_1.Clear();
            xDataTC66_2.Clear(); yDataTC66_2.Clear(); stateDataTC66_2.Clear();
            xDataTemp.Clear(); yDataTemp.Clear();
            xDataBMSTemp.Clear(); yDataBMSTemp.Clear();
            xDataEnergySegment.Clear(); yDataEnergySegment.Clear();
            xDataEnergyTotal.Clear(); yDataEnergyTotal.Clear();
            xDataCapCharge.Clear(); yDataCapCharge.Clear();
            xDataCapDischarge.Clear(); yDataCapDischarge.Clear();
            y1Min = double.MaxValue; y1Max = double.MinValue;
            y2Min = double.MaxValue; y2Max = double.MinValue;
            ClearLR();
            ClearAvg();
            this.Invalidate();
        }

        public void ClearLR()
        {
            lrClickState = 0;
            lrIdx1 = -1; lrIdx2 = -1;
            lrZeroTime = -1;
            lrRemainingSeconds = -1;
            lrPrediction = "";
            if (OnLRPrediction != null) OnLRPrediction("");
            this.Invalidate();
        }

        public void ClearAvg()
        {
            avgClickState = 0;
            avgTrace = 0;
            avgIdx1 = -1; avgIdx2 = -1;
            avgValue = 0;
            avgResult = "";
            this.Invalidate();
        }

        public void ResetLegendPosition()
        {
            legendPositionIndex = 0;  // Reset to optimal (emptiest) position
        }

        // --- Mouse click handler for LR and Average ---
        // Single left-click routes by nearest trace within the active radius (22×DpiScale px).
        // LR (SOC/Y2) and avg (Power/Temp/Y1) never compete: each uses its own axis distance,
        // and the radius cap prevents a distant curve from stealing the click.
        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            // Block any processing if legend was clicked, OR if click is inside current legend box
            if (legendWasClicked ||
                (e.X >= legendBoxX && e.X <= legendBoxX + legendBoxWidth &&
                 e.Y >= legendBoxY && e.Y <= legendBoxY + legendBoxHeight))
            {
                legendWasClicked = false;
                return;
            }

            base.OnMouseClick(e);

            // Clear states on third click
            if (avgClickState == 2) { ClearAvg(); return; }
            if (EnableLR && lrClickState == 2) { ClearLR(); return; }

            // Find nearest point across all enabled traces, each with its own radius cap
            int nearestTrace = 0;
            int nearestIdx   = -1;
            double nearestDist = double.MaxValue;

            if (EnableAvgY1 && xData1.Count >= 2)
            {
                int idx; double dist;
                FindNearestPoint(xData1, yData1, cachedY1Low, cachedY1Span, e.X, e.Y, out idx, out dist);
                if (idx >= 0 && dist < nearestDist) { nearestDist = dist; nearestIdx = idx; nearestTrace = 1; }
            }

            // Y2 (SOC): used for LR or avg — larger radius since it is the only Y2 trace
            if ((EnableLR || EnableAvgY2) && xData2.Count >= 2)
            {
                int idx; double dist;
                double lrRadius = (int)(44 * DpiScale);  // 2× avg radius — SOC is sole Y2 curve
                FindNearestPoint(xData2, yData2, py2Low, py2Span, e.X, e.Y, out idx, out dist, lrRadius);
                if (idx >= 0 && dist < nearestDist) { nearestDist = dist; nearestIdx = idx; nearestTrace = 2; }
            }

            if (EnableAvgTC66_1 && xDataTC66_1.Count >= 2)
            {
                int idx; double dist;
                FindNearestPoint(xDataTC66_1, yDataTC66_1, cachedY1Low, cachedY1Span, e.X, e.Y, out idx, out dist);
                if (idx >= 0 && dist < nearestDist) { nearestDist = dist; nearestIdx = idx; nearestTrace = 5; }
            }

            if (EnableAvgTC66_2 && xDataTC66_2.Count >= 2)
            {
                int idx; double dist;
                FindNearestPoint(xDataTC66_2, yDataTC66_2, py2Low, py2Span, e.X, e.Y, out idx, out dist);
                if (idx >= 0 && dist < nearestDist) { nearestDist = dist; nearestIdx = idx; nearestTrace = 6; }
            }

            if (nearestIdx < 0) return;

            // Route: Y2 nearest → LR (if enabled), else avg
            if (EnableLR && nearestTrace == 2 && !EnableAvgY2)
            {
                HandleLRClick(nearestIdx);
                return;
            }

            bool avgEnabled = (nearestTrace == 1 && EnableAvgY1) ||
                              (nearestTrace == 2 && EnableAvgY2) ||
                              (nearestTrace == 5 && EnableAvgTC66_1) ||
                              (nearestTrace == 6 && EnableAvgTC66_2);
            if (avgEnabled)
                HandleAvgClick(nearestTrace, nearestIdx);
        }

        // maxDist: click is ignored if nearest point is farther than this (pixels).
        // Default ~2 'W' character widths at current DPI — prevents click stealing by distant curves.
        private void FindNearestPoint(List<double> xData, List<double> yData,
            double yLow, double ySpan, int mx, int my, out int idx, out double dist,
            double maxDist = -1)
        {
            idx = -1;
            dist = double.MaxValue;
            if (plotArea.Width < 1 || pxRange < 1e-9) return;
            if (maxDist < 0) maxDist = (int)(22 * DpiScale);  // 2 × 'W' width at current DPI

            for (int i = 0; i < xData.Count; i++)
            {
                int px = plotArea.Left + (int)((xData[i] - pxFirst) / pxRange * plotArea.Width);
                int py = plotArea.Bottom - (int)((yData[i] - yLow) / ySpan * plotArea.Height);
                double d = Math.Sqrt((px - mx) * (px - mx) + (py - my) * (py - my));
                if (d < dist) { dist = d; idx = i; }
            }
            // Reject if beyond active radius
            if (dist > maxDist) { idx = -1; dist = double.MaxValue; }
        }

        private void HandleLRClick(int idx)
        {
            if (lrClickState == 0)
            {
                lrIdx1 = idx;
                lrClickState = 1;
                this.Invalidate();
            }
            else if (lrClickState == 1)
            {
                lrIdx2 = idx;
                if (lrIdx1 == lrIdx2) return;
                if (lrIdx1 > lrIdx2) { int tmp = lrIdx1; lrIdx1 = lrIdx2; lrIdx2 = tmp; }
                ComputeLR();
                lrClickState = 2;
                this.Invalidate();
            }
        }

        private void HandleAvgClick(int trace, int idx)
        {
            if (avgClickState == 0)
            {
                avgTrace = trace;
                avgIdx1 = idx;
                avgClickState = 1;
                this.Invalidate();
            }
            else if (avgClickState == 1)
            {
                // Must be same trace
                if (trace != avgTrace) return;
                avgIdx2 = idx;
                if (avgIdx1 == avgIdx2) return;
                if (avgIdx1 > avgIdx2) { int tmp = avgIdx1; avgIdx1 = avgIdx2; avgIdx2 = tmp; }
                ComputeAvg();
                avgClickState = 2;
                this.Invalidate();
            }
        }

        private void ComputeAvg()
        {
            List<double> yData = avgTrace == 1 ? yData1 : 
                                 avgTrace == 2 ? yData2 : 
                                 avgTrace == 5 ? yDataTC66_1 : yDataTC66_2;
            string unit = avgTrace == 1 ? Y1Unit : 
                         avgTrace == 2 ? Y2Unit : 
                         avgTrace == 5 ? Y1Unit : Y2Unit;
            string name = avgTrace == 1 ? Y1Title : 
                         avgTrace == 2 ? Y2Title : 
                         avgTrace == 5 ? "TC66 " + Y1Title : "TC66 " + Y2Title;

            double sum = 0;
            int count = 0;
            for (int i = avgIdx1; i <= avgIdx2; i++)
            {
                sum += yData[i];
                count++;
            }
            avgValue = sum / count;
            avgResult = name + " avg = " + avgValue.ToString("F2") + " " + unit;
        }

        private void ComputeLR()
        {
            // Determine the battery state for this LR segment
            // Use the state at the first selected point
            if (lrIdx1 < 0 || lrIdx2 < 0 || lrIdx1 >= stateData2.Count || lrIdx2 >= stateData2.Count) return;
            
            BatteryState targetState = stateData2[lrIdx1];
            
            // If first point is Idle, use second point's state
            if (targetState == BatteryState.Idle && lrIdx2 < stateData2.Count)
                targetState = stateData2[lrIdx2];
            
            // Only proceed if we have a valid charge or discharge state
            if (targetState == BatteryState.Idle) return;

            // Linear regression using staircase corners only
            // Discharge: lower-left corners (first point after value drops)
            // Charge: upper-right corners (last point before value rises)
            double sumX = 0, sumY = 0, sumXX = 0, sumXY = 0;
            int n = 0;
            
            for (int i = lrIdx1; i <= lrIdx2; i++)
            {
                // Only include points that match our target state (charge or discharge)
                if (i >= stateData2.Count || stateData2[i] != targetState) continue;
                
                bool includePoint = false;
                
                if (targetState == BatteryState.Discharging)
                {
                    // Discharge: include lower-left corners
                    // First point of segment OR value just dropped from previous
                    if (i == lrIdx1)
                        includePoint = true;
                    else if (i > 0 && i < yData2.Count && yData2[i] < yData2[i-1])
                        includePoint = true;
                }
                else if (targetState == BatteryState.Charging)
                {
                    // Charge: include upper-right corners
                    // Last point of segment OR value about to rise at next point
                    if (i == lrIdx2)
                        includePoint = true;
                    else if (i < yData2.Count - 1 && yData2[i] < yData2[i+1])
                        includePoint = true;
                }
                
                if (!includePoint) continue;
                
                double x = xData2[i];
                double y = yData2[i];
                sumX += x; sumY += y;
                sumXX += x * x; sumXY += x * y;
                n++;
            }
            
            if (n < 2) return;  // Need at least 2 corner points

            double denom = n * sumXX - sumX * sumX;
            if (Math.Abs(denom) < 1e-12) return;

            lrSlope = (n * sumXY - sumX * sumY) / denom;
            lrIntercept = (sumY - lrSlope * sumX) / n;

            // Find where SOC hits target: 0% for discharging, 100% for charging
            if (Math.Abs(lrSlope) > 1e-12 && lrSlope < 0)
            {
                // Discharging: SOC -> 0%: 0 = slope * t + intercept => t = -intercept / slope
                lrZeroTime = -lrIntercept / lrSlope;

                double currentElapsed = xData2[xData2.Count - 1];
                double remaining = lrZeroTime - currentElapsed;

                if (remaining > 0)
                {
                    TimeSpan ts = TimeSpan.FromSeconds(remaining);
                    double socPerHour = Math.Abs(lrSlope) * 3600;
                    lrPrediction = string.Format("LR: {0}h {1}m to 0% ({2:F1}%/h)",
                        (int)ts.TotalHours, ts.Minutes, socPerHour);
                    lrRemainingSeconds = remaining;
                }
                else
                {
                    lrPrediction = "LR: already past predicted 0%";
                    lrRemainingSeconds = -1;
                }
            }
            else if (Math.Abs(lrSlope) > 1e-12 && lrSlope > 0)
            {
                // Charging: SOC -> 100%: 100 = slope * t + intercept => t = (100 - intercept) / slope
                lrZeroTime = (100.0 - lrIntercept) / lrSlope;

                double currentElapsed = xData2[xData2.Count - 1];
                double remaining = lrZeroTime - currentElapsed;

                if (remaining > 0)
                {
                    TimeSpan ts = TimeSpan.FromSeconds(remaining);
                    double socPerHour = Math.Abs(lrSlope) * 3600;
                    lrPrediction = string.Format("LR: {0}h {1}m to 100% ({2:F1}%/h)",
                        (int)ts.TotalHours, ts.Minutes, socPerHour);
                    lrRemainingSeconds = remaining;
                }
                else
                {
                    lrPrediction = "LR: already past predicted 100%";
                    lrRemainingSeconds = -1;
                }
            }
            else
            {
                lrZeroTime = -1;
                lrRemainingSeconds = -1;
                lrPrediction = "LR: insufficient slope";
            }

            if (OnLRPrediction != null) OnLRPrediction(lrPrediction);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (var brush = new SolidBrush(BgColor))
                g.FillRectangle(brush, this.ClientRectangle);

            int mL = 100, mR = 100, mT = 48, mB = 95;  // Increased bottom margin for X-axis title clearance
            Rectangle pa = new Rectangle(mL, mT, this.Width - mL - mR, this.Height - mT - mB);
            if (pa.Width < 20 || pa.Height < 20) return;
            plotArea = pa;
            
            // Compute state-dependent axis colors early (used for frame, ticks, labels, and titles)
            Color y1AxisCol = CurrentBatteryState == BatteryState.Charging ? Y1ChargeColor :
                              CurrentBatteryState == BatteryState.Discharging ? Y1DischargeColor : Y1IdleColor;
            Color y2AxisCol = CurrentBatteryState == BatteryState.Charging ? Y2ChargeColor :
                              CurrentBatteryState == BatteryState.Discharging ? Y2DischargeColor : Y2IdleColor;

            // Draw plot frame: top/bottom in neutral dark, left/right in current axis colors
            using (var topBotPen = new Pen(AxisFrameColor, 3))
            using (var y1Pen = new Pen(y1AxisCol, 3))
            using (var y2Pen = new Pen(y2AxisCol, 3))
            {
                g.DrawLine(topBotPen, pa.Left,  pa.Top,    pa.Right, pa.Top);    // top
                g.DrawLine(topBotPen, pa.Left,  pa.Bottom, pa.Right, pa.Bottom); // bottom
                g.DrawLine(y1Pen,     pa.Left,  pa.Top,    pa.Left,  pa.Bottom); // left  = Y1 color
                g.DrawLine(y2Pen,     pa.Right, pa.Top,    pa.Right, pa.Bottom); // right = Y2 color
            }

            // Chart title - fixed 14pt centered, never modified
            using (var font = new Font("Segoe UI", ScaleFont(23f), FontStyle.Regular))
            using (var brush = new SolidBrush(TitleColor))
            {
                SizeF ts = g.MeasureString(ChartTitle, font);
                float titleX = pa.Left + (pa.Width - ts.Width) / 2;
                float titleY = (mT - ts.Height) / 2;
                g.DrawString(ChartTitle, font, brush, titleX, titleY);
            }

            if (yData1.Count < 2 && yData2.Count < 2)
            {
                using (var font = new Font("Segoe UI", ScaleFont(23f)))
                using (var brush = new SolidBrush(TextColor))
                {
                    string msg = EnableLR ? "Waiting for data... (click SOC for LR)" :
                                            "Waiting for data...";
                    g.DrawString(msg, font, brush,
                        pa.Left + pa.Width / 2 - 120, pa.Top + pa.Height / 2 - 10);
                }
                return;
            }

            // X range
            double xLast = 0;
            if (xData1.Count > 0) xLast = Math.Max(xLast, xData1[xData1.Count - 1]);
            if (xData2.Count > 0) xLast = Math.Max(xLast, xData2[xData2.Count - 1]);
            double xFirst = Math.Max(0, xLast - TimeWindow);
            double xRange = xLast - xFirst;
            if (xRange < 1) xRange = 1;
            pxFirst = xFirst;
            pxRange = xRange;

            // Calculate min/max for visible data only (within time window)
            double visY1Min = double.MaxValue, visY1Max = double.MinValue;
            double visY2Min = double.MaxValue, visY2Max = double.MinValue;
            
            for (int i = 0; i < xData1.Count; i++)
            {
                if (xData1[i] >= xFirst)
                {
                    if (yData1[i] < visY1Min) visY1Min = yData1[i];
                    if (yData1[i] > visY1Max) visY1Max = yData1[i];
                }
            }
            for (int i = 0; i < xData2.Count; i++)
            {
                if (xData2[i] >= xFirst)
                {
                    if (yData2[i] < visY2Min) visY2Min = yData2[i];
                    if (yData2[i] > visY2Max) visY2Max = yData2[i];
                }
            }
            
            // Include TC66 data in min/max — skip zero/negative values (TC66 idle/disconnected)
            for (int i = 0; i < xDataTC66_1.Count; i++)
            {
                if (xDataTC66_1[i] >= xFirst && yDataTC66_1[i] > 0)
                {
                    if (yDataTC66_1[i] < visY1Min) visY1Min = yDataTC66_1[i];
                    if (yDataTC66_1[i] > visY1Max) visY1Max = yDataTC66_1[i];
                }
            }
            for (int i = 0; i < xDataTC66_2.Count; i++)
            {
                if (xDataTC66_2[i] >= xFirst)
                {
                    if (yDataTC66_2[i] < visY2Min) visY2Min = yDataTC66_2[i];
                    if (yDataTC66_2[i] > visY2Max) visY2Max = yDataTC66_2[i];
                }
            }
            
            // Include temperature data in Y2 min/max
            for (int i = 0; i < xDataTemp.Count; i++)
            {
                if (xDataTemp[i] >= xFirst)
                {
                    if (yDataTemp[i] < visY2Min) visY2Min = yDataTemp[i];
                    if (yDataTemp[i] > visY2Max) visY2Max = yDataTemp[i];
                }
            }
            
            // Include BMS temperature data in Y2 min/max
            for (int i = 0; i < xDataBMSTemp.Count; i++)
            {
                if (xDataBMSTemp[i] >= xFirst)
                {
                    if (yDataBMSTemp[i] < visY2Min) visY2Min = yDataBMSTemp[i];
                    if (yDataBMSTemp[i] > visY2Max) visY2Max = yDataBMSTemp[i];
                }
            }
            
            // Include Segment Energy (Ec) in Y1 min/max - natural sign
            for (int i = 0; i < xDataEnergySegment.Count; i++)
            {
                if (xDataEnergySegment[i] >= xFirst)
                {
                    if (yDataEnergySegment[i] < visY1Min) visY1Min = yDataEnergySegment[i];
                    if (yDataEnergySegment[i] > visY1Max) visY1Max = yDataEnergySegment[i];
                }
            }

            // Include Total Energy (Et) in Y1 min/max if shown
            for (int i = 0; i < xDataEnergyTotal.Count; i++)
            {
                if (xDataEnergyTotal[i] >= xFirst)
                {
                    if (yDataEnergyTotal[i] < visY1Min) visY1Min = yDataEnergyTotal[i];
                    if (yDataEnergyTotal[i] > visY1Max) visY1Max = yDataEnergyTotal[i];
                }
            }

            // Include charge capacity (Qc) in Y2 min/max - natural sign
            for (int i = 0; i < xDataCapCharge.Count; i++)
            {
                if (xDataCapCharge[i] >= xFirst)
                {
                    if (yDataCapCharge[i] < visY2Min) visY2Min = yDataCapCharge[i];
                    if (yDataCapCharge[i] > visY2Max) visY2Max = yDataCapCharge[i];
                }
            }

            // Include total capacity (Qt) in Y2 min/max if shown
            for (int i = 0; i < xDataCapDischarge.Count; i++)
            {
                if (xDataCapDischarge[i] >= xFirst)
                {
                    if (yDataCapDischarge[i] < visY2Min) visY2Min = yDataCapDischarge[i];
                    if (yDataCapDischarge[i] > visY2Max) visY2Max = yDataCapDischarge[i];
                }
            }
            
            // Fall back to global if no visible data
            if (visY1Min > visY1Max) { visY1Min = y1Min; visY1Max = y1Max; }
            if (visY2Min > visY2Max) { visY2Min = y2Min; visY2Max = y2Max; }

            // Add padding to Y1 range
            bool y1DataHasNegative = visY1Min < 0;
            double y1Range = visY1Max - visY1Min;
            if (y1Range > 0)
            {
                double padding = y1Range * 0.12;
                if (Y1Unit == "V")
                {
                    // Voltage: tight proportional padding — do NOT floor at 0 (battery is never 0V)
                    visY1Min -= padding;
                    visY1Max += padding;
                }
                else
                {
                    if (!y1DataHasNegative) visY1Min = 0;  // Non-negative data: floor at 0
                    else visY1Min -= padding;               // Negative data: expand downward
                    visY1Max += padding;
                }
            }
            // Only allow below zero if actual data went there
            if (!y1DataHasNegative && visY1Min < 0) visY1Min = 0;
            
            bool y2DataHasNegative = visY2Min < 0;
            double y2Range = visY2Max - visY2Min;
            if (y2Range > 0)
            {
                double y2Pad = (Y2Unit == "A" || Y2Unit == "Ah") ? 0.5 :
                               (Y2Unit == "W") ? 2.0 : 2.0;
                if (!y2DataHasNegative) visY2Min = 0;  // Non-negative data: always floor at 0
                else visY2Min -= y2Pad;                // Negative data: expand downward
                visY2Max += y2Pad;
            }
            if (visY2Max > 105) visY2Max = 105;  // Cap before tick calc

            // Y1 ticks: GetNiceTicks gives round label positions; axis SPAN = padded data range
            double y1tMin, y1tMax, y1tStep;
            TickCalc.GetNiceTicks(visY1Min, visY1Max, MaxTicks, out y1tMin, out y1tMax, out y1tStep);
            // Axis pixel span is the padded data range (tight fit), not the rounded tick extremes
            double y1AxisMin = visY1Min;
            double y1AxisMax = visY1Max;
            if (y1AxisMin < 0 && !y1DataHasNegative) y1AxisMin = 0;
            double y1Span = y1AxisMax - y1AxisMin;
            if (y1Span < 1e-9) y1Span = 1;
            cachedY1Low = y1AxisMin;
            cachedY1Span = y1Span;

            // Y2 ticks: same approach
            double y2tMin, y2tMax, y2tStep;
            TickCalc.GetNiceTicks(visY2Min, visY2Max, MaxTicks, out y2tMin, out y2tMax, out y2tStep);
            double y2AxisMin = visY2Min;
            double y2AxisMax = visY2Max;
            if (y2AxisMin < 0 && !y2DataHasNegative) y2AxisMin = 0;
            if (Y2Unit == "%" && y2tMax > 110) { y2tMax = 110; y2tStep = 10; }
            double y2Span = y2AxisMax - y2AxisMin;
            if (y2Span < 1e-9) y2Span = 1;
            py2Low = y2AxisMin;
            py2Span = y2Span;

            // Tick label colors reuse the axis colors computed above (y1AxisCol / y2AxisCol)
            Color y1TickCol = y1AxisCol;
            Color y2TickCol = y2AxisCol;

            // Y1 axis (left) - round tick labels, but pixels mapped to tight axis span
            using (var pen = new Pen(y1TickCol, 2))
            using (var gridPen = new Pen(GridColor, 2) { DashStyle = DashStyle.Dot })
            using (var zeroPen = new Pen(y1TickCol, 1) { DashStyle = DashStyle.Dot })
            using (var font = new Font("Segoe UI", ScaleFont(23f)))
            using (var brush = new SolidBrush(y1TickCol))
            {
                for (double v = y1tMin; v <= y1tMax + y1tStep * 0.01; v += y1tStep)
                {
                    if (v < y1AxisMin - y1tStep * 0.01 || v > y1AxisMax + y1tStep * 0.01) continue;
                    int py = pa.Bottom - (int)((v - y1AxisMin) / y1Span * pa.Height);
                    py = Math.Max(pa.Top, Math.Min(pa.Bottom, py));
                    g.DrawLine(pen, pa.Left, py, pa.Left + TickLen, py);
                    g.DrawLine(pen, pa.Right - TickLen, py, pa.Right, py);
                    g.DrawLine(gridPen, pa.Left + 1, py, pa.Right - 1, py);
                    string lbl = TickCalc.SmartFormat(v, y1tStep);
                    SizeF sz = g.MeasureString(lbl, font);
                    float labelTop = py - sz.Height / 2;
                    if (labelTop + sz.Height <= pa.Bottom + 2)  // Skip if clipped below plot area
                        g.DrawString(lbl, font, brush, pa.Left - sz.Width - 6, labelTop);
                }
                // Zero line if zero is within range but not already a tick
                if (y1AxisMin < 0 && y1AxisMax > 0)
                {
                    int py0 = pa.Bottom - (int)((0 - y1AxisMin) / y1Span * pa.Height);
                    py0 = Math.Max(pa.Top, Math.Min(pa.Bottom, py0));
                    using (var zeroPenSolid = new Pen(y1TickCol, 1.5f))
                        g.DrawLine(zeroPenSolid, pa.Left, py0, pa.Right, py0);
                    SizeF sz0 = g.MeasureString("0", font);
                    g.DrawString("0", font, brush, pa.Left - sz0.Width - 6, py0 - sz0.Height / 2);
                }
            }

            // Y2 axis (right) - round tick labels, pixels mapped to tight axis span
            using (var pen = new Pen(y2TickCol, 2))
            using (var font = new Font("Segoe UI", ScaleFont(23f)))
            using (var brush = new SolidBrush(y2TickCol))
            {
                for (double v = y2tMin; v <= y2tMax + y2tStep * 0.01; v += y2tStep)
                {
                    if (v < y2AxisMin - y2tStep * 0.01 || v > y2AxisMax + y2tStep * 0.01) continue;
                    int py = pa.Bottom - (int)((v - y2AxisMin) / y2Span * pa.Height);
                    py = Math.Max(pa.Top, Math.Min(pa.Bottom, py));
                    g.DrawLine(pen, pa.Right - TickLen, py, pa.Right, py);
                    g.DrawLine(pen, pa.Left, py, pa.Left + TickLen, py);
                    string lbl = TickCalc.SmartFormat(v, y2tStep);
                    SizeF sz2 = g.MeasureString(lbl, font);
                    g.DrawString(lbl, font, brush, pa.Right + 6, py - sz2.Height / 2);
                }
                // Zero line if zero is within range but not already a tick
                if (y2AxisMin < 0 && y2AxisMax > 0)
                {
                    int py0 = pa.Bottom - (int)((0 - y2AxisMin) / y2Span * pa.Height);
                    py0 = Math.Max(pa.Top, Math.Min(pa.Bottom, py0));
                    using (var zeroPenSolid = new Pen(y2TickCol, 1.5f))
                        g.DrawLine(zeroPenSolid, pa.Left, py0, pa.Right, py0);
                    SizeF sz0 = g.MeasureString("0", font);
                    g.DrawString("0", font, brush, pa.Right + 6, py0 - sz0.Height / 2);
                }
            }

            // X axis - nice round time ticks
            using (var pen = new Pen(TextColor, 2))
            using (var font = new Font("Segoe UI", ScaleFont(23f)))
            using (var brush = new SolidBrush(TextColor))
            {
                // Choose a nice round interval targeting ~6-10 ticks
                double[] niceIntervals = { 10, 15, 30, 60, 120, 300, 600, 900, 1800, 3600, 7200, 14400, 28800 };
                double rawStep = xRange / 8.0;
                double xStep = niceIntervals[niceIntervals.Length - 1];
                for (int ni = 0; ni < niceIntervals.Length; ni++)
                {
                    if (niceIntervals[ni] >= rawStep) { xStep = niceIntervals[ni]; break; }
                }

                // Start at first round multiple >= xFirst
                double xTickStart = Math.Ceiling(xFirst / xStep) * xStep;
                if (xTickStart < xFirst + xStep * 0.01) xTickStart = xFirst < 1 ? 0 : xTickStart;

                // Always draw tick at origin
                {
                    int px0 = pa.Left;
                    g.DrawLine(pen, px0, pa.Bottom - TickLen, px0, pa.Bottom);
                    g.DrawLine(pen, px0, pa.Top, px0, pa.Top + TickLen);
                    string lbl0 = FormatTime(xFirst);
                    SizeF sz0 = g.MeasureString(lbl0, font);
                    g.DrawString(lbl0, font, brush, px0 - sz0.Width / 2, pa.Bottom + S(6));  // More spacing
                }

                for (double tv = xTickStart; tv <= xFirst + xRange + xStep * 0.01; tv += xStep)
                {
                    if (tv < xFirst) continue;
                    int px = pa.Left + (int)((tv - xFirst) / xRange * pa.Width);
                    if (px < pa.Left + 30 || px > pa.Right) continue;
                    g.DrawLine(pen, px, pa.Bottom - TickLen, px, pa.Bottom);
                    g.DrawLine(pen, px, pa.Top, px, pa.Top + TickLen);
                    string lbl = FormatTime(tv);
                    SizeF sz = g.MeasureString(lbl, font);
                    g.DrawString(lbl, font, brush, px - sz.Width / 2, pa.Bottom + S(6));  // More spacing
                }
            }

            // X axis title
            using (var font = new Font("Segoe UI", ScaleFont(22f)))  // Axis title
            using (var brush = new SolidBrush(TextColor))
            {
                string xTitle = "Elapsed Time";
                SizeF sz = g.MeasureString(xTitle, font);
                // Position below tick labels: pa.Bottom + tick_offset + tick_height + spacing
                g.DrawString(xTitle, font, brush,
                    pa.Left + (pa.Width - sz.Width) / 2, pa.Bottom + S(32));  // Clear space below tick labels
            }

            // Y axis titles - use current state color (already calculated above)
            Color energyTitleColor = Color.FromArgb(0x8E, 0x24, 0xAA);  // Vivid deep purple (matches Ec curves)
            if (xDataEnergySegment.Count > 0)
            {
                DrawVerticalAxisTitleTwoColor(g, Y1Title, y1TickCol,
                    " & Energy (Wh)", energyTitleColor,
                    5, pa.Top, pa.Height, true);
            }
            else
            {
                DrawVerticalAxisTitle(g, Y1Title, y1TickCol,
                    5, pa.Top, pa.Height, true);
            }
            // Y2 axis title - add temperature in red if data exists
            if (xDataTemp.Count > 0)
            {
                // Temperature data exists - show "SOC (%)" in normal color and "& T (°C)" in red
                DrawVerticalAxisTitleTwoColor(g, "SOC (%)", y2TickCol,
                    " & T (°C)", Color.Red,
                    this.Width - 8, pa.Top, pa.Height, false);  // Close to right edge, clear of tick labels
            }
            else if (xDataCapCharge.Count > 0)
            {
                // Capacity data exists on Y2 (V-i chart) - "& Qc (Ah)" in teal
                DrawVerticalAxisTitleTwoColor(g, Y2Title, y2TickCol,
                    " & Qc (Ah)", Color.FromArgb(0x00, 0xAC, 0xC1),
                    this.Width - 8, pa.Top, pa.Height, false);
            }
            else
            {
                // No temperature data - show only SOC part
                DrawVerticalAxisTitle(g, Y2Title, y2TickCol,
                    this.Width - 8, pa.Top, pa.Height, false);  // Close to right edge, clear of tick labels
            }

            // Clip to plot area
            g.SetClip(pa);

            // Data traces - Draw TC66 first (background), then BMS on top (foreground)
            // TC66 traces (dashed lines, lighter colors, thinner for jagged data)
            if (xDataTC66_1.Count >= 2)
            {
                DrawTraceDashed(g, pa, xDataTC66_1, yDataTC66_1, stateDataTC66_1, xFirst, xRange,
                    y1AxisMin, y1Span, TC66Y1ChargeColor, TC66Y1DischargeColor, TC66Y1IdleColor, 0.5f);
            }
            if (xDataTC66_2.Count >= 2)
            {
                DrawTraceDashed(g, pa, xDataTC66_2, yDataTC66_2, stateDataTC66_2, xFirst, xRange,
                    y2AxisMin, y2Span, TC66Y2ChargeColor, TC66Y2DischargeColor, TC66Y2IdleColor, 0.5f);
            }
            
            // BMS traces (solid lines, thicker, drawn on top for visibility)
            DrawTrace(g, pa, xData1, yData1, stateData1, xFirst, xRange,
                y1AxisMin, y1Span, Y1ChargeColor, Y1DischargeColor, Y1IdleColor, 3.0f);
            DrawTrace(g, pa, xData2, yData2, stateData2, xFirst, xRange,
                y2AxisMin, y2Span, Y2ChargeColor, Y2DischargeColor, Y2IdleColor, 3.0f);

            // Energy traces (on Y1 axis with Power) - purple colors
            Color totalEnergyColor = Color.FromArgb(0x8E, 0x24, 0xAA);   // Vivid deep purple
            Color segmentEnergyColor = Color.FromArgb(0xBA, 0x68, 0xC8); // Bright medium purple
            
            // Segment Energy (Ec) - dashed line, natural sign
            if (xDataEnergySegment.Count >= 2)
            {
                DrawTraceSingleColor(g, pa, xDataEnergySegment, yDataEnergySegment, xFirst, xRange,
                    y1AxisMin, y1Span, segmentEnergyColor, 3.5f, true, 10.0);
            }

            // Total Energy (Et) - solid thinner line, shown only from known SOC start
            if (xDataEnergyTotal.Count >= 2)
            {
                DrawTraceSingleColor(g, pa, xDataEnergyTotal, yDataEnergyTotal, xFirst, xRange,
                    y1AxisMin, y1Span, totalEnergyColor, 2.5f, false, 10.0);
            }

            // Capacity curves (on Y2 axis with Current, for V-i chart)
            Color capChargeColor = Color.FromArgb(0x00, 0xAC, 0xC1);  // Teal (Qc)
            Color capTotalColor  = Color.FromArgb(0x00, 0x6E, 0x7F);  // Darker teal (Qt)

            // Qc - cycle capacity, natural sign
            if (xDataCapCharge.Count >= 2)
            {
                DrawTraceSingleColor(g, pa, xDataCapCharge, yDataCapCharge, xFirst, xRange,
                    y2AxisMin, y2Span, capChargeColor, 3.5f, false, 10.0);
            }

            // Qt - total capacity, solid thinner, shown only from known SOC start
            if (xDataCapDischarge.Count >= 2)
            {
                DrawTraceSingleColor(g, pa, xDataCapDischarge, yDataCapDischarge, xFirst, xRange,
                    y2AxisMin, y2Span, capTotalColor, 2.5f, false, 10.0);
            }

            // Temperature trace - TC66 (bright red, dashed line on Y2 axis)
            if (xDataTemp.Count >= 2)
            {
                DrawTraceSingleColor(g, pa, xDataTemp, yDataTemp, xFirst, xRange,
                    y2AxisMin, y2Span, TempColor, 0.5f, true);  // TC66 hairline
            }

            // Temperature trace - BMS (dark orange, dashed line on Y2 axis)
            if (xDataBMSTemp.Count >= 2)
            {
                DrawTraceSingleColor(g, pa, xDataBMSTemp, yDataBMSTemp, xFirst, xRange,
                    y2AxisMin, y2Span, BMSTempColor, 3.5f, true);  // BMS group thickness
            }

            // --- LR overlay ---
            if (EnableLR && lrClickState >= 1)
            {
                // Draw marker on first selected point
                DrawLRMarker(g, pa, lrIdx1, xFirst, xRange, y2AxisMin, y2Span);

                if (lrClickState == 2)
                {
                    // Draw marker on second selected point
                    DrawLRMarker(g, pa, lrIdx2, xFirst, xRange, y2AxisMin, y2Span);

                    // Draw LR line from first point to target SOC intercept (or edge)
                    double x1_lr = xData2[lrIdx1];
                    double y1_lr = lrSlope * x1_lr + lrIntercept;
                    double x2_lr = lrZeroTime > 0 ? lrZeroTime : xData2[lrIdx2] + xRange * 0.3;
                    double y2_lr = lrSlope * x2_lr + lrIntercept;
                    double targetSOC = lrSlope < 0 ? 0 : 100;

                    int px1 = pa.Left + (int)((x1_lr - xFirst) / xRange * pa.Width);
                    int py1 = pa.Bottom - (int)((y1_lr - y2AxisMin) / y2Span * pa.Height);
                    int px2 = pa.Left + (int)((x2_lr - xFirst) / xRange * pa.Width);
                    int py2 = pa.Bottom - (int)((y2_lr - y2AxisMin) / y2Span * pa.Height);

                    using (var pen = new Pen(LRLineColor, 2.5f) { DashStyle = DashStyle.Dash })
                        g.DrawLine(pen, px1, py1, px2, py2);

                    // Draw target SOC intercept marker (X)
                    if (lrZeroTime > 0)
                    {
                        int pxZ = pa.Left + (int)((lrZeroTime - xFirst) / xRange * pa.Width);
                        int pyZ = pa.Bottom - (int)((targetSOC - y2AxisMin) / y2Span * pa.Height);
                        using (var pen = new Pen(LRLineColor, 2))
                        {
                            g.DrawLine(pen, pxZ - 8, pyZ - 8, pxZ + 8, pyZ + 8);
                            g.DrawLine(pen, pxZ - 8, pyZ + 8, pxZ + 8, pyZ - 8);
                        }
                    }

                    // LR prediction text inside chart
                    if (lrPrediction.Length > 0)
                    {
                        using (var font = new Font("Segoe UI", ScaleFont(20f), FontStyle.Bold))
                        using (var brush = new SolidBrush(LRLineColor))
                        {
                            SizeF sz = g.MeasureString(lrPrediction, font);
                            float tx = pa.Left + (pa.Width - sz.Width) / 2;
                            float ty = pa.Top + S(30);
                            using (var bgBr = new SolidBrush(Color.FromArgb(210, 255, 255, 255)))
                                g.FillRectangle(bgBr, tx - 4, ty - 2, sz.Width + 8, sz.Height + 4);
                            g.DrawString(lrPrediction, font, brush, tx, ty);
                            
                            if (RunTimeSeconds > 0)
                            {
                                int totalSec = (int)RunTimeSeconds;
                                int h = totalSec / 3600;
                                int m = (totalSec % 3600) / 60;
                                int s = totalSec % 60;
                                string timeStr = string.Format("Run: {0:D2}:{1:D2}:{2:D2}", h, m, s);

                                if (lrRemainingSeconds > 0)
                                {
                                    int totSec = (int)(RunTimeSeconds + lrRemainingSeconds);
                                    int totMin = (int)Math.Round(totSec / 60.0 / 10.0) * 10;
                                    int totH = totMin / 60;
                                    int totM = totMin % 60;
                                    timeStr += string.Format(";  Tot: {0}:{1:D2} h", totH, totM);
                                }

                                using (var timeFont = new Font("Segoe UI", ScaleFont(20f), FontStyle.Bold))
                                using (var timeBrush = new SolidBrush(Color.FromArgb(100, 100, 100)))
                                {
                                    SizeF timeSz = g.MeasureString(timeStr, timeFont);
                                    float timeTx = pa.Left + (pa.Width - timeSz.Width) / 2;
                                    float timeTy = ty + sz.Height + S(4);
                                    using (var timeBgBr = new SolidBrush(Color.FromArgb(210, 255, 255, 255)))
                                        g.FillRectangle(timeBgBr, timeTx - 4, timeTy - 2, timeSz.Width + 8, timeSz.Height + 4);
                                    g.DrawString(timeStr, timeFont, timeBrush, timeTx, timeTy);
                                }
                            }
                        }
                    }
                }
            }

            // --- Average overlay ---
            if (avgClickState >= 1)
            {
                List<double> xData = avgTrace == 1 ? xData1 : 
                                    avgTrace == 2 ? xData2 : 
                                    avgTrace == 5 ? xDataTC66_1 : xDataTC66_2;
                List<double> yData = avgTrace == 1 ? yData1 : 
                                    avgTrace == 2 ? yData2 : 
                                    avgTrace == 5 ? yDataTC66_1 : yDataTC66_2;
                double yLow = avgTrace == 1 ? y1AxisMin : 
                             avgTrace == 2 ? y2AxisMin : 
                             avgTrace == 5 ? y1AxisMin : y2AxisMin;
                double ySpan = avgTrace == 1 ? y1Span : 
                              avgTrace == 2 ? y2Span : 
                              avgTrace == 5 ? y1Span : y2Span;
                Color avgColor = avgTrace == 1 ? Y1ChargeColor : 
                                avgTrace == 2 ? Y2ChargeColor : 
                                avgTrace == 5 ? TC66Y1ChargeColor : TC66Y2ChargeColor;

                DrawAvgMarker(g, pa, xData, yData, avgIdx1, xFirst, xRange, yLow, ySpan, avgColor);

                if (avgClickState == 2)
                {
                    DrawAvgMarker(g, pa, xData, yData, avgIdx2, xFirst, xRange, yLow, ySpan, avgColor);

                    int pyAvg = pa.Bottom - (int)((avgValue - yLow) / ySpan * pa.Height);
                    int px1 = pa.Left + (int)((xData[avgIdx1] - xFirst) / xRange * pa.Width);
                    int px2 = pa.Left + (int)((xData[avgIdx2] - xFirst) / xRange * pa.Width);
                    using (var pen = new Pen(avgColor, 2.5f) { DashStyle = DashStyle.Dash })
                        g.DrawLine(pen, px1, pyAvg, px2, pyAvg);

                    if (avgResult.Length > 0)
                    {
                        using (var font = new Font("Segoe UI", ScaleFont(20f), FontStyle.Bold))
                        using (var brush = new SolidBrush(avgColor))
                        {
                            SizeF sz = g.MeasureString(avgResult, font);
                            float tx = pa.Left + (pa.Width - sz.Width) / 2;
                            float ty = pa.Top + S(30) + ScaleFont(20f) + S(8);
                            using (var bgBr = new SolidBrush(Color.FromArgb(210, 255, 255, 255)))
                                g.FillRectangle(bgBr, tx - 4, ty - 2, sz.Width + 8, sz.Height + 4);
                            g.DrawString(avgResult, font, brush, tx, ty);
                        }
                    }
                }
            }

            g.ResetClip();;

            // Legend
            // Draw axis direction arrows on BMS curves at 75% of x range
            DrawAxisArrows(g, pa, xFirst, xRange, y1AxisMin, y1Span, y2AxisMin, y2Span);

            DrawLegend(g, pa, xFirst, xRange, y1AxisMin, y1Span, y2AxisMin, y2Span);

            // Current value badges
            if (yData1.Count > 0)
            {
                double lastVal = yData1[yData1.Count - 1];
                BatteryState lastSt = stateData1[stateData1.Count - 1];
                Color c = lastSt == BatteryState.Charging ? Y1ChargeColor :
                          lastSt == BatteryState.Discharging ? Y1DischargeColor : Y1IdleColor;
                string fmt = ChartTitle.Contains("Voltage") ? "F2" : "F1";
                DrawValueBadge(g, pa, lastVal, Y1Unit, c, true, fmt);

                // Qc badge on bottom-RIGHT of Chart1 (above the Current badge), at same height as TC66 temp
                if (ChartTitle.Contains("Voltage") && xDataCapCharge.Count > 0)
                {
                    double qcVal = yDataCapCharge[yDataCapCharge.Count - 1];
                    Color qcColor = Color.FromArgb(0x00, 0xAC, 0xC1);  // Teal
                    DrawValueBadgeAtY(g, pa, qcVal, "Ah", qcColor, false, "F2", 38);
                }

                // Ec badge on bottom-LEFT of Chart2 (above the Power badge), at same height as TC66 temp
                if (ChartTitle.Contains("Power") && xDataEnergySegment.Count > 0)
                {
                    double ecVal = yDataEnergySegment[yDataEnergySegment.Count - 1];
                    Color ecColor = Color.FromArgb(0xBA, 0x68, 0xC8);  // Bright medium purple
                    DrawValueBadgeAtY(g, pa, ecVal, "Wh", ecColor, true, "F2", 38);
                }
            }
            if (yData2.Count > 0)
            {
                double lastVal = yData2[yData2.Count - 1];
                BatteryState lastSt = stateData2[stateData2.Count - 1];
                Color c = lastSt == BatteryState.Charging ? Y2ChargeColor :
                          lastSt == BatteryState.Discharging ? Y2DischargeColor : Y2IdleColor;
                // Use F2 precision for Voltage/Current/Capacity chart, F1 for SOC/Power/Temperature chart
                string fmt = ChartTitle.Contains("Voltage") ? "F2" : "F1";
                DrawValueBadge(g, pa, lastVal, Y2Unit, c, false, fmt);
                
                // Draw TC66 temperature value just above SOC badge
                if (yDataTemp.Count > 0)
                {
                    double tempVal = yDataTemp[yDataTemp.Count - 1];
                    using (var font = new Font("Segoe UI", ScaleFont(23f), FontStyle.Bold))
                    using (var brush = new SolidBrush(TempColor))
                    {
                        string txt = tempVal.ToString("F1") + " °C";
                        SizeF sz = g.MeasureString(txt, font);
                        float x = pa.Right - sz.Width - 8;
                        float y = pa.Bottom - sz.Height - S(38);  // Position above SOC badge
                        
                        using (var bgBr = new SolidBrush(Color.FromArgb(210, 255, 255, 255)))
                            g.FillRectangle(bgBr, x - 2, y - 1, sz.Width + 4, sz.Height + 2);
                        g.DrawString(txt, font, brush, x, y);
                    }
                }

                // Draw BMS temperature toast in lower right corner (below TC66 temp)
                if (yDataBMSTemp.Count > 0)
                {
                    double tempVal = yDataBMSTemp[yDataBMSTemp.Count - 1];
                    using (var font = new Font("Segoe UI", ScaleFont(23f), FontStyle.Bold))
                    using (var brush = new SolidBrush(BMSTempColor))
                    {
                        string txt = "T(BMS)=" + tempVal.ToString("F1") + " °C";
                        SizeF sz = g.MeasureString(txt, font);
                        float x = pa.Right - sz.Width - 8;
                        float y = pa.Bottom - sz.Height - S(70);  // Above TC66 temp
                        
                        using (var bgBr = new SolidBrush(Color.FromArgb(210, 255, 255, 255)))
                            g.FillRectangle(bgBr, x - 2, y - 1, sz.Width + 4, sz.Height + 2);
                        g.DrawString(txt, font, brush, x, y);
                    }
                }
            }

            // LR click hint (bottom-right of chart area)
            if (EnableLR && lrClickState < 2)
            {
                string hint = lrClickState == 0 ? "Click 1st SOC point" : "Click 2nd SOC point";
                using (var font = new Font("Segoe UI", ScaleFont(17f)))
                using (var brush = new SolidBrush(LRMarkerColor))
                {
                    SizeF sz = g.MeasureString(hint, font);
                    g.DrawString(hint, font, brush, pa.Right - sz.Width - 4, pa.Top + 4);
                }
            }
            else if (EnableLR && lrClickState == 2)
            {
                string hint = "Click to clear LR";
                using (var font = new Font("Segoe UI", ScaleFont(17f)))
                using (var brush = new SolidBrush(LRMarkerColor))
                {
                    SizeF sz = g.MeasureString(hint, font);
                    g.DrawString(hint, font, brush, pa.Right - sz.Width - 4, pa.Top + 4);
                }
            }

            // Avg click hint (top-left of chart area)
            bool avgEnabled = EnableAvgY1 || EnableAvgY2 || EnableAvgTC66_1 || EnableAvgTC66_2;
            if (avgEnabled && avgClickState < 2)
            {
                string hint = avgClickState == 0 ? "Click 1st avg point" : "Click 2nd avg point";
                using (var font = new Font("Segoe UI", ScaleFont(17f)))
                using (var brush = new SolidBrush(Color.FromArgb(200, 0, 200)))
                {
                    g.DrawString(hint, font, brush, pa.Left + 4, pa.Top + 4);
                }
            }
            else if (avgEnabled && avgClickState == 2)
            {
                string hint = "Click to clear avg";
                using (var font = new Font("Segoe UI", ScaleFont(17f)))
                using (var brush = new SolidBrush(Color.FromArgb(200, 0, 200)))
                {
                    g.DrawString(hint, font, brush, pa.Left + 4, pa.Top + 4);
                }
            }

            // --- Cursor tooltip with nearest point detection ---
            if (cursorInChart && (xData1.Count > 0 || xData2.Count > 0))
            {
                DrawCursorTooltip(g, pa, xFirst, xRange, y1AxisMin, y1Span, y2AxisMin, y2Span);
            }
        }

        private void DrawLRMarker(Graphics g, Rectangle pa, int idx,
            double xFirst, double xRange, double y2Low, double y2Span)
        {
            if (idx < 0 || idx >= xData2.Count) return;
            int px = pa.Left + (int)((xData2[idx] - xFirst) / xRange * pa.Width);
            int py = pa.Bottom - (int)((yData2[idx] - y2Low) / y2Span * pa.Height);

            using (var pen = new Pen(LRMarkerColor, 2.5f))
            {
                g.DrawEllipse(pen, px - 8, py - 8, 16, 16);
                g.DrawLine(pen, px - 5, py, px + 5, py);
                g.DrawLine(pen, px, py - 5, px, py + 5);
            }
        }

        private void DrawAvgMarker(Graphics g, Rectangle pa, List<double> xData, List<double> yData,
            int idx, double xFirst, double xRange, double yLow, double ySpan, Color color)
        {
            if (idx < 0 || idx >= xData.Count) return;
            int px = pa.Left + (int)((xData[idx] - xFirst) / xRange * pa.Width);
            int py = pa.Bottom - (int)((yData[idx] - yLow) / ySpan * pa.Height);

            using (var pen = new Pen(color, 2.5f))
            {
                // Draw square marker
                g.DrawRectangle(pen, px - 7, py - 7, 14, 14);
                g.DrawLine(pen, px - 4, py, px + 4, py);
                g.DrawLine(pen, px, py - 4, px, py + 4);
            }
        }

        private void DrawTrace(Graphics g, Rectangle pa,
            List<double> xd, List<double> yd, List<BatteryState> sd,
            double xFirst, double xRange, double yLow, double ySpan,
            Color chargeCol, Color dischargeCol, Color idleCol, float lineW)
        {
            if (xd.Count < 2) return;

            int startIdx = 0;
            for (int i = 0; i < xd.Count; i++)
            {
                if (xd[i] >= xFirst) { startIdx = Math.Max(0, i - 1); break; }
            }

            for (int i = startIdx + 1; i < xd.Count; i++)
            {
                // Skip drawing line if state changed between points (prevents spurious connecting lines)
                if (sd[i] != sd[i - 1])
                    continue;
                
                // Skip if time gap is too large (indicates data collection interruption)
                // Calculate expected interval from recent data, use 3× as threshold
                double timeGap = xd[i] - xd[i - 1];
                double expectedInterval = i > 1 ? (xd[i - 1] - xd[i - 2]) : timeGap;
                double maxInterval = Math.Max(expectedInterval * 3.0, 3.0);  // At least 3 seconds
                if (timeGap > maxInterval)
                    continue;

                int px1 = pa.Left + (int)((xd[i - 1] - xFirst) / xRange * pa.Width);
                int py1 = pa.Bottom - (int)((yd[i - 1] - yLow) / ySpan * pa.Height);
                int px2 = pa.Left + (int)((xd[i] - xFirst) / xRange * pa.Width);
                int py2 = pa.Bottom - (int)((yd[i] - yLow) / ySpan * pa.Height);

                Color lc = sd[i] == BatteryState.Charging ? chargeCol :
                           sd[i] == BatteryState.Discharging ? dischargeCol : idleCol;

                using (var pen = new Pen(lc, lineW))
                    g.DrawLine(pen, px1, py1, px2, py2);
            }
        }

        private void DrawTraceSingleColor(Graphics g, Rectangle pa,
            List<double> xd, List<double> yd,
            double xFirst, double xRange, double yLow, double ySpan,
            Color lineCol, float lineW, bool dashed = false, double maxGap = 3.0)
        {
            if (xd.Count < 2) return;

            int startIdx = 0;
            for (int i = 0; i < xd.Count; i++)
            {
                if (xd[i] >= xFirst) { startIdx = Math.Max(0, i - 1); break; }
            }

            using (var pen = new Pen(lineCol, lineW))
            {
                if (dashed)
                    pen.DashStyle = DashStyle.Dash;
                    
                for (int i = startIdx + 1; i < xd.Count; i++)
                {
                    // Skip if time gap is too large (indicates data collection interruption)
                    if (xd[i] - xd[i - 1] > maxGap)
                        continue;
                    
                    int px1 = pa.Left + (int)((xd[i - 1] - xFirst) / xRange * pa.Width);
                    int py1 = pa.Bottom - (int)((yd[i - 1] - yLow) / ySpan * pa.Height);
                    int px2 = pa.Left + (int)((xd[i] - xFirst) / xRange * pa.Width);
                    int py2 = pa.Bottom - (int)((yd[i] - yLow) / ySpan * pa.Height);
                    g.DrawLine(pen, px1, py1, px2, py2);
                }
            }
        }

        private void DrawTraceDashed(Graphics g, Rectangle pa,
            List<double> xd, List<double> yd, List<BatteryState> sd,
            double xFirst, double xRange, double yLow, double ySpan,
            Color chargeCol, Color dischargeCol, Color idleCol, float lineW)
        {
            if (xd.Count < 2) return;

            int startIdx = 0;
            for (int i = 0; i < xd.Count; i++)
            {
                if (xd[i] >= xFirst) { startIdx = Math.Max(0, i - 1); break; }
            }

            for (int i = startIdx + 1; i < xd.Count; i++)
            {
                // Skip drawing line if state changed between points (prevents spurious connecting lines)
                if (sd[i] != sd[i - 1])
                    continue;
                
                // Skip if time gap is too large (indicates disconnect/reconnect)
                // TC66 polls at 1Hz, so gap > 3 seconds indicates a break
                double timeGap = xd[i] - xd[i - 1];
                if (timeGap > 3.0)
                    continue;

                int px1 = pa.Left + (int)((xd[i - 1] - xFirst) / xRange * pa.Width);
                int py1 = pa.Bottom - (int)((yd[i - 1] - yLow) / ySpan * pa.Height);
                int px2 = pa.Left + (int)((xd[i] - xFirst) / xRange * pa.Width);
                int py2 = pa.Bottom - (int)((yd[i] - yLow) / ySpan * pa.Height);

                Color lc = sd[i] == BatteryState.Charging ? chargeCol :
                           sd[i] == BatteryState.Discharging ? dischargeCol : idleCol;

                using (var pen = new Pen(lc, lineW) { DashStyle = DashStyle.Dash })
                    g.DrawLine(pen, px1, py1, px2, py2);
            }
        }

        private void DrawVerticalAxisTitle(Graphics g, string title, Color color,
            float xPos, int paTop, int paHeight, bool leftSide)
        {
            using (var font = new Font("Segoe UI", ScaleFont(22f)))  // Axis titles
            using (var brush = new SolidBrush(color))
            {
                var saved = g.Save();
                float cy = paTop + paHeight / 2f;
                g.TranslateTransform(xPos, cy);
                g.RotateTransform(leftSide ? -90 : 90);
                SizeF sz = g.MeasureString(title, font);
                g.DrawString(title, font, brush, -sz.Width / 2, 0);
                g.Restore(saved);
            }
        }

        private void DrawVerticalAxisTitleTwoColor(Graphics g, string title1, Color color1,
            string title2, Color color2, float xPos, int paTop, int paHeight, bool leftSide)
        {
            using (var font = new Font("Segoe UI", ScaleFont(22f)))  // Axis titles
            using (var brush1 = new SolidBrush(color1))
            using (var brush2 = new SolidBrush(color2))
            {
                var saved = g.Save();
                float cy = paTop + paHeight / 2f;
                g.TranslateTransform(xPos, cy);
                g.RotateTransform(leftSide ? -90 : 90);
                
                SizeF sz1 = g.MeasureString(title1, font);
                SizeF sz2 = g.MeasureString(title2, font);
                float totalWidth = sz1.Width + sz2.Width;
                float startX = -totalWidth / 2;
                
                g.DrawString(title1, font, brush1, startX, 0);
                g.DrawString(title2, font, brush2, startX + sz1.Width, 0);
                g.Restore(saved);
            }
        }

        // Draw short horizontal axis-direction arrows on BMS curves at 75% of visible X range
        private void DrawAxisArrows(Graphics g, Rectangle pa,
            double xFirst, double xRange, double y1Low, double y1Span,
            double y2Low, double y2Span)
        {
            int arrowLen = S(70);   // ~0.7 inch at 96dpi
            int headW    = S(10);   // arrowhead width (along arrow axis)
            int headH    = S(6);    // arrowhead half-height

            // Target x = 75% of visible range
            double xTarget = xFirst + xRange * 0.75;

            // Helper: find y-pixel of a trace at xTarget by nearest-point lookup
            // Returns int.MinValue if no data
            Func<List<double>, List<double>, double, double, int> getY =
                (xd, yd, yLow, ySpan) =>
                {
                    if (xd.Count == 0 || ySpan < 1e-9) return int.MinValue;
                    int best = -1;
                    double bestDist = double.MaxValue;
                    for (int i = 0; i < xd.Count; i++)
                    {
                        double d = Math.Abs(xd[i] - xTarget);
                        if (d < bestDist) { bestDist = d; best = i; }
                    }
                    if (best < 0) return int.MinValue;
                    return pa.Bottom - (int)((yd[best] - yLow) / ySpan * pa.Height);
                };

            // Helper: draw one horizontal arrow
            Action<int, int, bool, Color> drawArrow = (cx, cy, pointLeft, col) =>
            {
                if (cy < pa.Top || cy > pa.Bottom) return;
                int tipX  = pointLeft ? cx - arrowLen : cx + arrowLen;
                int tailX = cx;

                using (var pen = new Pen(col, 2.0f))
                    g.DrawLine(pen, tailX, cy, tipX - (pointLeft ? -headW : headW), cy);

                // Filled arrowhead triangle
                Point[] head = pointLeft
                    ? new[] { new Point(tipX, cy), new Point(tipX + headW, cy - headH), new Point(tipX + headW, cy + headH) }
                    : new[] { new Point(tipX, cy), new Point(tipX - headW, cy - headH), new Point(tipX - headW, cy + headH) };

                using (var brush = new SolidBrush(col))
                    g.FillPolygon(brush, head);
            };

            BatteryState st = CurrentBatteryState;

            // Y1 BMS trace → arrow points LEFT (toward left axis)
            {
                Color c = st == BatteryState.Charging ? Y1ChargeColor :
                          st == BatteryState.Discharging ? Y1DischargeColor : Y1IdleColor;
                int py = getY(xData1, yData1, y1Low, y1Span);
                if (py != int.MinValue)
                {
                    int cx = pa.Left + (int)(xRange > 0 ? (xTarget - xFirst) / xRange * pa.Width : pa.Width / 2);
                    drawArrow(cx, py, true, c);
                }
            }

            // Y2 BMS trace → arrow points RIGHT (toward right axis)
            {
                Color c = st == BatteryState.Charging ? Y2ChargeColor :
                          st == BatteryState.Discharging ? Y2DischargeColor : Y2IdleColor;
                int py = getY(xData2, yData2, y2Low, y2Span);
                if (py != int.MinValue)
                {
                    int cx = pa.Left + (int)(xRange > 0 ? (xTarget - xFirst) / xRange * pa.Width : pa.Width / 2);
                    drawArrow(cx, py, false, c);
                }
            }

            // Ec (energy segment, Y1) → arrow points LEFT, purple
            if (xDataEnergySegment.Count > 0)
            {
                Color c = Color.FromArgb(0xBA, 0x68, 0xC8);
                int py = getY(xDataEnergySegment, yDataEnergySegment, y1Low, y1Span);
                if (py != int.MinValue)
                {
                    int cx = pa.Left + (int)(xRange > 0 ? (xTarget - xFirst) / xRange * pa.Width : pa.Width / 2);
                    drawArrow(cx, py, true, c);
                }
            }

            // Qc (cycle capacity, Y2) → arrow points RIGHT, teal
            if (xDataCapCharge.Count > 0)
            {
                Color c = Color.FromArgb(0x00, 0xAC, 0xC1);
                int py = getY(xDataCapCharge, yDataCapCharge, y2Low, y2Span);
                if (py != int.MinValue)
                {
                    int cx = pa.Left + (int)(xRange > 0 ? (xTarget - xFirst) / xRange * pa.Width : pa.Width / 2);
                    drawArrow(cx, py, false, c);
                }
            }

            // BMS Temperature (Y2) → arrow points RIGHT, dark orange
            if (xDataBMSTemp.Count > 0)
            {
                int py = getY(xDataBMSTemp, yDataBMSTemp, y2Low, y2Span);
                if (py != int.MinValue)
                {
                    int cx = pa.Left + (int)(xRange > 0 ? (xTarget - xFirst) / xRange * pa.Width : pa.Width / 2);
                    drawArrow(cx, py, false, BMSTempColor);
                }
            }

            // SOC (Y2, Chart2 only via xData2 already covered above)
            // Et and Qt if present → same axis as their primary counterparts
            if (xDataEnergyTotal.Count > 0)
            {
                Color c = Color.FromArgb(0x8E, 0x24, 0xAA);
                int py = getY(xDataEnergyTotal, yDataEnergyTotal, y1Low, y1Span);
                if (py != int.MinValue)
                {
                    int cx = pa.Left + (int)(xRange > 0 ? (xTarget - xFirst) / xRange * pa.Width : pa.Width / 2);
                    drawArrow(cx, py, true, c);
                }
            }
            if (xDataCapDischarge.Count > 0)
            {
                Color c = Color.FromArgb(0x00, 0x6E, 0x7F);
                int py = getY(xDataCapDischarge, yDataCapDischarge, y2Low, y2Span);
                if (py != int.MinValue)
                {
                    int cx = pa.Left + (int)(xRange > 0 ? (xTarget - xFirst) / xRange * pa.Width : pa.Width / 2);
                    drawArrow(cx, py, false, c);
                }
            }

            // TC66 curves → same axis as their BMS counterparts, hairline arrows
            int cxTC = pa.Left + (int)(xRange > 0 ? (xTarget - xFirst) / xRange * pa.Width : pa.Width / 2);

            if (xDataTC66_1.Count > 0)
            {
                Color c = st == BatteryState.Charging ? TC66Y1ChargeColor :
                          st == BatteryState.Discharging ? TC66Y1DischargeColor : TC66Y1IdleColor;
                int py = getY(xDataTC66_1, yDataTC66_1, y1Low, y1Span);
                if (py != int.MinValue) drawArrow(cxTC, py, true, c);
            }

            if (xDataTC66_2.Count > 0)
            {
                Color c = st == BatteryState.Charging ? TC66Y2ChargeColor :
                          st == BatteryState.Discharging ? TC66Y2DischargeColor : TC66Y2IdleColor;
                int py = getY(xDataTC66_2, yDataTC66_2, y2Low, y2Span);
                if (py != int.MinValue) drawArrow(cxTC, py, false, c);
            }

            if (xDataTemp.Count > 0)
            {
                int py = getY(xDataTemp, yDataTemp, y2Low, y2Span);
                if (py != int.MinValue) drawArrow(cxTC, py, false, TempColor);
            }
        }

        private void DrawLegend(Graphics g, Rectangle pa,
            double xFirst, double xRange, double y1Min, double y1Span,
            double y2Min, double y2Span)
        {
            using (var font = new Font("Segoe UI", ScaleFont(17f), FontStyle.Regular))
            {
                int sw = 30;  // Sample line width
                int gap = 12;  // More gap between line sample and text
                int lineHeight = S(21);  // original 17=tight(2px gap); +4 → comfortable 6px gap
                int margin = 60;  // Increased margin to avoid value badges at bottom corners

                // Get current state for dynamic legend colors
                Color y1Col = CurrentBatteryState == BatteryState.Charging ? Y1ChargeColor :
                              CurrentBatteryState == BatteryState.Discharging ? Y1DischargeColor : Y1IdleColor;
                Color y2Col = CurrentBatteryState == BatteryState.Charging ? Y2ChargeColor :
                              CurrentBatteryState == BatteryState.Discharging ? Y2DischargeColor : Y2IdleColor;

                // Build legend entries (no chart title - it's already displayed above)
                List<LegendEntry> entries = new List<LegendEntry>();
                
                // Add Y1 curve (BMS - use Y1LegendTitle if set, otherwise Y1Title)
                string y1Legend = (Y1LegendTitle.Length > 0) ? Y1LegendTitle : Y1Title;
                entries.Add(new LegendEntry(y1Legend, y1Col, 3.5f, false));
                
                // Add TC66 Y1 curve if present (lighter color, dashed, thinner)
                if (xDataTC66_1.Count > 0)
                {
                    Color tc66y1Col = CurrentBatteryState == BatteryState.Charging ? TC66Y1ChargeColor :
                                      CurrentBatteryState == BatteryState.Discharging ? TC66Y1DischargeColor : TC66Y1IdleColor;
                    entries.Add(new LegendEntry(y1Legend, tc66y1Col, 0.5f, true));  // dashed - TC66 implied by thin+dashed+lighter color
                }
                
                // Add Y2 curve (BMS - use Y2LegendTitle if set, otherwise Y2Title)
                string y2Legend = (Y2LegendTitle.Length > 0) ? Y2LegendTitle : Y2Title;
                entries.Add(new LegendEntry(y2Legend, y2Col, 3.5f, false));
                
                // Add TC66 Y2 curve if present (lighter color, dashed, thinner)
                if (xDataTC66_2.Count > 0)
                {
                    Color tc66y2Col = CurrentBatteryState == BatteryState.Charging ? TC66Y2ChargeColor :
                                      CurrentBatteryState == BatteryState.Discharging ? TC66Y2DischargeColor : TC66Y2IdleColor;
                    entries.Add(new LegendEntry(y2Legend, tc66y2Col, 0.5f, true));  // dashed - TC66 implied by thin+dashed+lighter color
                }
                
                // Add Ec curve (on Y1 axis) - natural sign
                Color segmentEnergyColor = Color.FromArgb(0xBA, 0x68, 0xC8); // Bright medium purple
                if (xDataEnergySegment.Count > 0)
                    entries.Add(new LegendEntry("Ec (Wh)", segmentEnergyColor, 3.5f, true));

                // Add Et curve (on Y1 axis) - only when shown from known SOC start
                Color totalEnergyColor = Color.FromArgb(0x8E, 0x24, 0xAA);  // Vivid deep purple
                if (xDataEnergyTotal.Count > 0)
                    entries.Add(new LegendEntry("Et (Wh)", totalEnergyColor, 2.5f, false));

                // Add Qc curve (on Y2 axis, V-i chart only) - natural sign
                Color capChargeColor = Color.FromArgb(0x00, 0xAC, 0xC1);  // Teal (Qc)
                if (xDataCapCharge.Count > 0)
                    entries.Add(new LegendEntry("Qc (Ah)", capChargeColor, 3.5f, false));

                // Add Qt curve (on Y2 axis) - only when shown from known SOC start
                Color capTotalColor = Color.FromArgb(0x00, 0x6E, 0x7F);  // Darker teal (Qt)
                if (xDataCapDischarge.Count > 0)
                    entries.Add(new LegendEntry("Qt (Ah)", capTotalColor, 2.5f, false));

                // Add TC66 Temperature if present
                if (xDataTemp.Count > 0)
                    entries.Add(new LegendEntry("Temp (TC66)", TempColor, 0.5f, true));

                // Add BMS Temperature if present
                if (xDataBMSTemp.Count > 0)
                    entries.Add(new LegendEntry("Temp (BMS)", BMSTempColor, 3.5f, true));

                // Calculate box size
                float maxWidth = 0;
                foreach (LegendEntry entry in entries)
                {
                    SizeF sz = g.MeasureString(entry.Text, font);
                    float entryWidth = (entry.LineWidth > 0 ? sw + gap : 0) + sz.Width;
                    if (entryWidth > maxWidth) maxWidth = entryWidth;
                }
                
                int verticalPadding = 12;  // More top/bottom space
                int horizontalPadding = 14;  // More left/right space
                float boxWidth = maxWidth + (horizontalPadding * 2);
                float boxHeight = entries.Count * lineHeight + (verticalPadding * 2);
                
                // Auto-position legend — recompute only when layout or data window changes
                float boxX, boxY;
                int totalPts = xData1.Count + xData2.Count + xDataTC66_1.Count + xDataTC66_2.Count
                             + xDataTemp.Count + xDataBMSTemp.Count + xDataEnergySegment.Count
                             + xDataEnergyTotal.Count + xDataCapCharge.Count + xDataCapDischarge.Count;
                bool legendDirty = cachedLegendX < 0
                    || pa != cachedLegendPlotArea
                    || Math.Abs(boxWidth  - cachedLegendBoxW) > 0.5f
                    || Math.Abs(boxHeight - cachedLegendBoxH) > 0.5f
                    || Math.Abs(xFirst - cachedLegendXFirst) > 1e-6
                    || Math.Abs(xRange - cachedLegendXRange) > 1e-6
                    || (totalPts - cachedLegendDataCount) >= 60;
                if (legendDirty)
                {
                    try
                    {
                        FindBestLegendPosition(pa, xFirst, xRange,
                            y1Min, y1Span, y2Min, y2Span,
                            boxWidth, boxHeight, margin, legendPositionIndex, out boxX, out boxY);
                    }
                    catch
                    {
                        boxX = pa.Left + (pa.Width - boxWidth) / 2;
                        boxY = pa.Bottom - margin - boxHeight;
                    }
                    cachedLegendX = boxX; cachedLegendY = boxY;
                    cachedLegendBoxW = boxWidth; cachedLegendBoxH = boxHeight;
                    cachedLegendPlotArea = pa;
                    cachedLegendXFirst = xFirst; cachedLegendXRange = xRange;
                    cachedLegendDataCount = totalPts;
                }
                else
                {
                    boxX = cachedLegendX; boxY = cachedLegendY;
                }
                
                // Cache legend box bounds for click detection
                legendBoxX = boxX;
                legendBoxY = boxY;
                legendBoxWidth = boxWidth;
                legendBoxHeight = boxHeight;
                
                // Draw semi-transparent background
                using (var bgBr = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
                {
                    g.FillRectangle(bgBr, boxX, boxY, boxWidth, boxHeight);
                }
                
                // Draw border
                using (var borderPen = new Pen(Color.FromArgb(100, 0, 0, 0), 2))
                {
                    g.DrawRectangle(borderPen, boxX, boxY, boxWidth, boxHeight);
                }
                
                // Draw entries with vertically centered text
                float y = boxY + verticalPadding;
                foreach (LegendEntry entry in entries)
                {
                    float x = boxX + 8;
                    
                    if (entry.LineWidth > 0)
                    {
                        // Draw sample line (centered vertically in lineHeight)
                        int cy = (int)(y + lineHeight / 2);
                        using (var pen = new Pen(entry.Color, entry.LineWidth))
                        {
                            if (entry.Dashed)
                                pen.DashStyle = DashStyle.Dash;
                            g.DrawLine(pen, (int)x, cy, (int)(x + sw), cy);
                        }
                        x += sw + gap;
                    }
                    
                    // Draw text centered in slot
                    SizeF textSize = g.MeasureString(entry.Text, font);
                    float textY = y + (lineHeight - textSize.Height) / 2;
                    using (var brush = new SolidBrush(entry.Color))
                    {
                        g.DrawString(entry.Text, font, brush, x, textY);
                    }
                    
                    y += lineHeight;
                }
            }
        }

        private void FindBestLegendPosition(Rectangle pa, double xFirst, double xRange,
            double y1Min, double y1Span, double y2Min, double y2Span,
            float boxWidth, float boxHeight, int margin, int positionIndex, out float boxX, out float boxY)
        {
            // Default fallback
            boxX = pa.Left + (pa.Width - boxWidth) / 2;
            boxY = pa.Bottom - margin - boxHeight;

            if (pa.Width <= 0 || pa.Height <= 0 || boxWidth <= 0 || boxHeight <= 0) return;
            if (xData1 == null || yData1 == null || xData2 == null || yData2 == null) return;
            if (xRange < 1e-9 || y1Span < 1e-9 || y2Span < 1e-9) return;

            // Build 9 candidate box positions (3 cols × 3 rows)
            float[] colX = new float[]
            {
                pa.Left + margin,                          // left-aligned
                pa.Left + (pa.Width - boxWidth) / 2f,     // centered
                pa.Right - margin - boxWidth               // right-aligned
            };
            float[] rowY = new float[]
            {
                pa.Top + margin,                           // top-aligned
                pa.Top + (pa.Height - boxHeight) / 2f,    // middle
                pa.Bottom - margin - boxHeight             // bottom-aligned
            };

            // Preferred position order when scores are tied:
            // top-left, top-right, bottom-left, bottom-right, top-center, bottom-center, mid-left, mid-right, center
            int[,] preference = new int[3, 3]
            {
                { 0, 4, 1 },  // row 0: top-left=0, top-center=4, top-right=1
                { 6, 8, 7 },  // row 1: mid-left=6, center=8, mid-right=7
                { 2, 5, 3 }   // row 2: bottom-left=2, bottom-center=5, bottom-right=3
            };

            // Score each of the 9 positions by counting data points inside the box rectangle
            // Use (score * 10 + preference) as sort key so ties resolve to preferred positions
            var candidates = new List<Tuple<int, float, float>>();  // (sortKey, bx, by)

            for (int ri = 0; ri < 3; ri++)
            {
                for (int ci = 0; ci < 3; ci++)
                {
                    float bx = colX[ci];
                    float by = rowY[ri];

                    bx = Math.Max(pa.Left, Math.Min(pa.Right - boxWidth, bx));
                    by = Math.Max(pa.Top, Math.Min(pa.Bottom - boxHeight, by));

                    RectangleF box = new RectangleF(bx, by, boxWidth, boxHeight);

                    // Penalty if box overlaps LR/avg text areas:
                    // LR text appears top-left; avg appears top-left; SOC click hint top-right
                    // Only penalize left 2/3 of top area — leave top-right corner free
                    int score = 0;
                    if (lrPrediction.Length > 0 || avgClickState >= 1)
                    {
                        float penaltyRight = pa.Left + pa.Width * 0.65f;  // left 65% only
                        float penaltyBottom = pa.Top + pa.Height * 0.25f; // top 25%
                        if (bx < penaltyRight && by < penaltyBottom && by + boxHeight > pa.Top)
                            score += 10000;
                    }

                    // Count data points from every visible trace that land inside this box
                    score += CountPointsInBox(xData1, yData1, y1Min, y1Span, xFirst, xRange, pa, box, true);
                    score += CountPointsInBox(xData2, yData2, y2Min, y2Span, xFirst, xRange, pa, box, false);
                    score += CountPointsInBox(xDataTC66_1, yDataTC66_1, y1Min, y1Span, xFirst, xRange, pa, box, true);
                    score += CountPointsInBox(xDataTC66_2, yDataTC66_2, y2Min, y2Span, xFirst, xRange, pa, box, false);
                    score += CountPointsInBox(xDataTemp, yDataTemp, y2Min, y2Span, xFirst, xRange, pa, box, false);
                    score += CountPointsInBox(xDataBMSTemp, yDataBMSTemp, y2Min, y2Span, xFirst, xRange, pa, box, false);
                    score += CountPointsInBox(xDataEnergySegment, yDataEnergySegment, y1Min, y1Span, xFirst, xRange, pa, box, true);
                    score += CountPointsInBox(xDataEnergyTotal, yDataEnergyTotal, y1Min, y1Span, xFirst, xRange, pa, box, true);
                    score += CountPointsInBox(xDataCapCharge, yDataCapCharge, y2Min, y2Span, xFirst, xRange, pa, box, false);
                    score += CountPointsInBox(xDataCapDischarge, yDataCapDischarge, y2Min, y2Span, xFirst, xRange, pa, box, false);

                    // Sort key: score * 10 + preference — ties resolve to preferred corners
                    int sortKey = score * 10 + preference[ri, ci];
                    candidates.Add(Tuple.Create(sortKey, bx, by));
                }
            }

            // Sort by composite key (score first, then positional preference)
            candidates.Sort((a, b) => a.Item1.CompareTo(b.Item1));

            // Split into "clean" (no LR/avg penalty) and "fallback" groups
            // Penalty 10000 → sortKey ≥ 100000 (10000 * 10 + pref); use 50000 as safe threshold
            var clean = new List<Tuple<int, float, float>>();
            var fallback = new List<Tuple<int, float, float>>();
            foreach (var c in candidates)
            {
                if (c.Item1 < 50000) clean.Add(c);
                else fallback.Add(c);
            }
            var pool = clean.Count > 0 ? clean : fallback;

            // positionIndex 0 → best position (lowest sortKey = fewest overlapping points, prefer corners)
            // Clicks cycle through remaining positions in preference order
            int idx = positionIndex % pool.Count;
            boxX = pool[idx].Item2;
            boxY = pool[idx].Item3;
        }

        // Count how many visible data points from a trace fall inside the given pixel rectangle
        private int CountPointsInBox(List<double> xData, List<double> yData,
            double yMin, double ySpan, double xFirst, double xRange,
            Rectangle pa, RectangleF box, bool useY1)
        {
            if (xData == null || yData == null || xData.Count == 0) return 0;
            if (ySpan < 1e-9) return 0;
            int count = 0;
            // Sample up to 1000 points per trace for good spatial resolution
            int step = Math.Max(1, xData.Count / 1000);
            for (int i = 0; i < xData.Count; i += step)
            {
                if (xData[i] < xFirst) continue;
                float px = pa.Left + (float)((xData[i] - xFirst) / xRange * pa.Width);
                float py = pa.Bottom - (float)((yData[i] - yMin) / ySpan * pa.Height);
                if (box.Contains(px, py)) count++;
            }
            return count;
        }

        private void DrawValueBadge(Graphics g, Rectangle pa,
            double val, string unit, Color color, bool leftSide, string format = "F1")
        {
            using (var font = new Font("Segoe UI", ScaleFont(23f), FontStyle.Bold))
            using (var brush = new SolidBrush(color))
            {
                string txt = val.ToString(format) + " " + unit;
                SizeF sz = g.MeasureString(txt, font);
                float x = leftSide ? pa.Left + 8 : pa.Right - sz.Width - 8;
                float y = pa.Bottom - sz.Height - 6;

                using (var bgBr = new SolidBrush(Color.FromArgb(210, 255, 255, 255)))
                    g.FillRectangle(bgBr, x - 2, y - 1, sz.Width + 4, sz.Height + 2);
                g.DrawString(txt, font, brush, x, y);
            }
        }

        // Draw a value badge stacked above the primary badge using actual font height (DPI-safe)
        private void DrawValueBadgeAtY(Graphics g, Rectangle pa,
            double val, string unit, Color color, bool leftSide, string format, float bottomOffset)
        {
            using (var font = new Font("Segoe UI", ScaleFont(23f), FontStyle.Bold))
            using (var brush = new SolidBrush(color))
            {
                string txt = val.ToString(format) + " " + unit;
                SizeF sz = g.MeasureString(txt, font);
                float x = leftSide ? pa.Left + 8 : pa.Right - sz.Width - 8;
                // Exact same formula as TC66 temp above SOC: pa.Bottom - sz.Height - S(38)
                float y = pa.Bottom - sz.Height - S(38);

                using (var bgBr = new SolidBrush(Color.FromArgb(210, 255, 255, 255)))
                    g.FillRectangle(bgBr, x - 2, y - 1, sz.Width + 4, sz.Height + 2);
                g.DrawString(txt, font, brush, x, y);
            }
        }

        private string FormatTime(double seconds)
        {
            int totalSec = (int)Math.Round(seconds);
            int h = totalSec / 3600;
            int m = (totalSec % 3600) / 60;
            int s = totalSec % 60;
            if (h > 0)
                return string.Format("{0}:{1:D2}", h, m);  // "1:30", "10:00"
            return string.Format("{0}:{1:D2}", m, s);      // "0:00", "15:00", "45:00"
        }

        private void DrawCursorTooltip(Graphics g, Rectangle pa,
            double xFirst, double xRange, double y1Low, double y1Span,
            double y2Low, double y2Span)
        {
            int bestIdx1 = -1, bestIdx2 = -1;
            int bestIdxTC66_1 = -1, bestIdxTC66_2 = -1, bestIdxTemp = -1, bestIdxBMSTemp = -1;
            int bestIdxEnergySegment = -1, bestIdxEnergyTotal = -1;
            int bestIdxCapCharge = -1, bestIdxCapDischarge = -1;
            double bestX = 0;
            
            double y1Min = y1Low;
            double y1Max = y1Low + y1Span;
            double y2Min = y2Low;
            double y2Max = y2Low + y2Span;

            BatteryState st = CurrentBatteryState;
            Color y1Col = st == BatteryState.Charging ? Y1ChargeColor :
                          st == BatteryState.Discharging ? Y1DischargeColor : Y1IdleColor;
            Color y2Col = st == BatteryState.Charging ? Y2ChargeColor :
                          st == BatteryState.Discharging ? Y2DischargeColor : Y2IdleColor;
            
            // Find which curve point is closest to cursor using dx²+dy² distance
            double cursorX = cursorPos.X;
            double cursorY = cursorPos.Y;
            double minDist = double.MaxValue;
            string closestCurve = "";
            int closestPx = 0;
            int closestPy = 0;
            
            // Check all Y1 (BMS) points
            for (int i = 0; i < xData1.Count; i++)
            {
                if (xData1[i] < xFirst) continue;
                int px = pa.Left + (int)((xData1[i] - xFirst) / xRange * pa.Width);
                double yFrac = (yData1[i] - y1Min) / (y1Max - y1Min);
                int py = pa.Bottom - (int)(yFrac * pa.Height);
                double dx = px - cursorX;
                double dy = py - cursorY;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < minDist) { minDist = dist; closestCurve = "Y1_BMS"; closestPx = px; closestPy = py; bestIdx1 = i; bestX = xData1[i]; }
            }
            
            // Check all Y2 (BMS) points
            for (int i = 0; i < xData2.Count; i++)
            {
                if (xData2[i] < xFirst) continue;
                int px = pa.Left + (int)((xData2[i] - xFirst) / xRange * pa.Width);
                double yFrac = (yData2[i] - y2Min) / (y2Max - y2Min);
                int py = pa.Bottom - (int)(yFrac * pa.Height);
                double dx = px - cursorX;
                double dy = py - cursorY;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < minDist) { minDist = dist; closestCurve = "Y2_BMS"; closestPx = px; closestPy = py; bestIdx2 = i; bestX = xData2[i]; }
            }
            
            // Check all Y1 (TC66) points
            for (int i = 0; i < xDataTC66_1.Count; i++)
            {
                if (xDataTC66_1[i] < xFirst) continue;
                int px = pa.Left + (int)((xDataTC66_1[i] - xFirst) / xRange * pa.Width);
                double yFrac = (yDataTC66_1[i] - y1Min) / (y1Max - y1Min);
                int py = pa.Bottom - (int)(yFrac * pa.Height);
                double dx = px - cursorX;
                double dy = py - cursorY;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < minDist) { minDist = dist; closestCurve = "Y1_TC66"; closestPx = px; closestPy = py; bestIdxTC66_1 = i; bestX = xDataTC66_1[i]; }
            }
            
            // Check all Y2 (TC66) points
            for (int i = 0; i < xDataTC66_2.Count; i++)
            {
                if (xDataTC66_2[i] < xFirst) continue;
                int px = pa.Left + (int)((xDataTC66_2[i] - xFirst) / xRange * pa.Width);
                double yFrac = (yDataTC66_2[i] - y2Min) / (y2Max - y2Min);
                int py = pa.Bottom - (int)(yFrac * pa.Height);
                double dx = px - cursorX;
                double dy = py - cursorY;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < minDist) { minDist = dist; closestCurve = "Y2_TC66"; closestPx = px; closestPy = py; bestIdxTC66_2 = i; bestX = xDataTC66_2[i]; }
            }
            
            // Check all Temperature points
            for (int i = 0; i < xDataTemp.Count; i++)
            {
                if (xDataTemp[i] < xFirst) continue;
                int px = pa.Left + (int)((xDataTemp[i] - xFirst) / xRange * pa.Width);
                double yFrac = (yDataTemp[i] - y2Min) / (y2Max - y2Min);
                int py = pa.Bottom - (int)(yFrac * pa.Height);
                double dx = px - cursorX;
                double dy = py - cursorY;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < minDist) { minDist = dist; closestCurve = "Temp"; closestPx = px; closestPy = py; bestIdxTemp = i; bestX = xDataTemp[i]; }
            }
            
            // Check all BMS Temperature points (on Y2 axis)
            for (int i = 0; i < xDataBMSTemp.Count; i++)
            {
                if (xDataBMSTemp[i] < xFirst) continue;
                int px = pa.Left + (int)((xDataBMSTemp[i] - xFirst) / xRange * pa.Width);
                double yFrac = (yDataBMSTemp[i] - y2Min) / (y2Max - y2Min);
                int py = pa.Bottom - (int)(yFrac * pa.Height);
                double dx = px - cursorX;
                double dy = py - cursorY;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < minDist) { minDist = dist; closestCurve = "BMSTemp"; closestPx = px; closestPy = py; bestIdxBMSTemp = i; bestX = xDataBMSTemp[i]; }
            }
            
            // Check all Segment Energy (Ec) points (on Y1 axis)
            for (int i = 0; i < xDataEnergySegment.Count; i++)
            {
                if (xDataEnergySegment[i] < xFirst) continue;
                int px = pa.Left + (int)((xDataEnergySegment[i] - xFirst) / xRange * pa.Width);
                double yFrac = (yDataEnergySegment[i] - y1Min) / (y1Max - y1Min);
                int py = pa.Bottom - (int)(yFrac * pa.Height);
                double dx = px - cursorX; double dy = py - cursorY;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < minDist) { minDist = dist; closestCurve = "EnergySegment"; closestPx = px; closestPy = py; bestIdxEnergySegment = i; bestX = xDataEnergySegment[i]; }
            }

            // Check all Total Energy (Et) points (on Y1 axis)
            for (int i = 0; i < xDataEnergyTotal.Count; i++)
            {
                if (xDataEnergyTotal[i] < xFirst) continue;
                int px = pa.Left + (int)((xDataEnergyTotal[i] - xFirst) / xRange * pa.Width);
                double yFrac = (yDataEnergyTotal[i] - y1Min) / (y1Max - y1Min);
                int py = pa.Bottom - (int)(yFrac * pa.Height);
                double dx = px - cursorX; double dy = py - cursorY;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < minDist) { minDist = dist; closestCurve = "EnergyTotal"; closestPx = px; closestPy = py; bestIdxEnergyTotal = i; bestX = xDataEnergyTotal[i]; }
            }

            // Qc - cycle capacity (on Y2 axis)
            for (int i = 0; i < xDataCapCharge.Count; i++)
            {
                if (xDataCapCharge[i] < xFirst) continue;
                int px = pa.Left + (int)((xDataCapCharge[i] - xFirst) / xRange * pa.Width);
                int py = pa.Bottom - (int)((yDataCapCharge[i] - py2Low) / py2Span * pa.Height);
                double dist = Math.Sqrt((double)(cursorPos.X - px) * (cursorPos.X - px) + (double)(cursorPos.Y - py) * (cursorPos.Y - py));
                if (dist < minDist) { minDist = dist; closestCurve = "CapCharge"; closestPx = px; closestPy = py; bestIdxCapCharge = i; bestX = xDataCapCharge[i]; }
            }

            // Qt - total capacity (on Y2 axis)
            for (int i = 0; i < xDataCapDischarge.Count; i++)
            {
                if (xDataCapDischarge[i] < xFirst) continue;
                int px = pa.Left + (int)((xDataCapDischarge[i] - xFirst) / xRange * pa.Width);
                int py = pa.Bottom - (int)((yDataCapDischarge[i] - py2Low) / py2Span * pa.Height);
                double dist = Math.Sqrt((double)(cursorPos.X - px) * (cursorPos.X - px) + (double)(cursorPos.Y - py) * (cursorPos.Y - py));
                if (dist < minDist) { minDist = dist; closestCurve = "CapDischarge"; closestPx = px; closestPy = py; bestIdxCapDischarge = i; bestX = xDataCapDischarge[i]; }
            }
            
            if (closestCurve == "") return;
            if (minDist > 50) return;
            
            // Build tooltip text
            List<string> lines = new List<string>();
            string timeStr = FormatTime(bestX);
            lines.Add("Time: " + timeStr);
            
            // Show only the closest curve with its actual color
            Color curveColor = Color.Black;
            if (closestCurve == "Y1_BMS" && bestIdx1 >= 0)
            {
                lines.Add(Y1Title + ": " + yData1[bestIdx1].ToString("F2") + " " + Y1Unit);
                curveColor = y1Col;
            }
            else if (closestCurve == "Y2_BMS" && bestIdx2 >= 0)
            {
                lines.Add(Y2Title + ": " + yData2[bestIdx2].ToString("F2") + " " + Y2Unit);
                curveColor = y2Col;
            }
            else if (closestCurve == "Y1_TC66" && bestIdxTC66_1 >= 0)
            {
                lines.Add("TC66 " + Y1Title + ": " + yDataTC66_1[bestIdxTC66_1].ToString("F2") + " " + Y1Unit);
                curveColor = st == BatteryState.Charging ? TC66Y1ChargeColor :
                             st == BatteryState.Discharging ? TC66Y1DischargeColor : TC66Y1IdleColor;
            }
            else if (closestCurve == "Y2_TC66" && bestIdxTC66_2 >= 0)
            {
                lines.Add("TC66 " + Y2Title + ": " + yDataTC66_2[bestIdxTC66_2].ToString("F2") + " " + Y2Unit);
                curveColor = st == BatteryState.Charging ? TC66Y2ChargeColor :
                             st == BatteryState.Discharging ? TC66Y2DischargeColor : TC66Y2IdleColor;
            }
            else if (closestCurve == "Temp" && bestIdxTemp >= 0)
            {
                lines.Add("Temperature (TC66): " + yDataTemp[bestIdxTemp].ToString("F0") + " °C");
                curveColor = TempColor;
            }
            else if (closestCurve == "BMSTemp" && bestIdxBMSTemp >= 0)
            {
                lines.Add("Temperature (BMS): " + yDataBMSTemp[bestIdxBMSTemp].ToString("F1") + " °C");
                curveColor = BMSTempColor;
            }
            else if (closestCurve == "EnergySegment" && bestIdxEnergySegment >= 0)
            {
                lines.Add("Ec (Wh): " + yDataEnergySegment[bestIdxEnergySegment].ToString("F2"));
                curveColor = Color.FromArgb(0xBA, 0x68, 0xC8);
            }
            else if (closestCurve == "EnergyTotal" && bestIdxEnergyTotal >= 0)
            {
                lines.Add("Et (Wh): " + yDataEnergyTotal[bestIdxEnergyTotal].ToString("F2"));
                curveColor = Color.FromArgb(0x8E, 0x24, 0xAA);
            }
            else if (closestCurve == "CapCharge" && bestIdxCapCharge >= 0)
            {
                lines.Add("Qc (Ah): " + yDataCapCharge[bestIdxCapCharge].ToString("F3"));
                curveColor = Color.FromArgb(0x00, 0xAC, 0xC1);
            }
            else if (closestCurve == "CapDischarge" && bestIdxCapDischarge >= 0)
            {
                lines.Add("Qt (Ah): " + yDataCapDischarge[bestIdxCapDischarge].ToString("F3"));
                curveColor = Color.FromArgb(0x00, 0x6E, 0x7F);
            }

            // Draw tooltip box
            using (var font = new Font("Segoe UI", ScaleFont(16f), FontStyle.Bold))
            {
                float maxWidth = 0;
                float totalHeight = 0;
                List<SizeF> sizes = new List<SizeF>();
                foreach (string line in lines)
                {
                    SizeF sz = g.MeasureString(line, font);
                    sizes.Add(sz);
                    if (sz.Width > maxWidth) maxWidth = sz.Width;
                    totalHeight += sz.Height + 2;
                }

                // Position tooltip - avoid going off screen
                float tx = closestPx + 15;
                float ty = cursorPos.Y - totalHeight / 2;
                if (tx + maxWidth + 12 > pa.Right) tx = closestPx - maxWidth - 20;
                if (ty < pa.Top + 5) ty = pa.Top + 5;
                if (ty + totalHeight + 5 > pa.Bottom) ty = pa.Bottom - totalHeight - 5;

                // Draw background and border
                using (var bgBr = new SolidBrush(Color.FromArgb(240, 255, 255, 255)))
                using (var borderPen = new Pen(Color.FromArgb(150, 0, 0, 0), 2f))
                {
                    RectangleF rect = new RectangleF(tx - 6, ty - 4, maxWidth + 12, totalHeight + 6);
                    g.FillRectangle(bgBr, rect);
                    g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width, rect.Height);
                }

                // Draw each line
                float yPos = ty;
                for (int i = 0; i < lines.Count; i++)
                {
                    Color textColor = (i == 1) ? curveColor : Color.Black;  // Line 1 is the value, use curve color

                    using (var brush = new SolidBrush(textColor))
                    {
                        g.DrawString(lines[i], font, brush, tx, yPos);
                    }
                    yPos += sizes[i].Height + 2;
                }
            }
        }
    }

    // ========================================================================
    // Main Form
    // ========================================================================
    public class MainForm : Form
    {
        private const string AppVersion = "v34.0";  // ← UPDATE ONLY HERE
        private const float FontScale = 1.0f;       // ← ADJUST TO SCALE ALL FONTS
        private BatteryReader reader = new BatteryReader();
        private List<BatteryReading> allReadings = new List<BatteryReading>();
        private System.Threading.Timer sampleTimer;  // BMS sampling timer
        private System.Threading.Timer tc66Timer;    // TC66 1Hz polling timer
        private Timer clockTimer;
        private DateTime startTime;
        private bool isRunning = false;
        private bool isCsvLoaded = false;  // True when viewing a loaded CSV — η is meaningless then
        
        // DPI scaling factor (1.0 = 96 DPI, 1.5 = 144 DPI, 2.0 = 192 DPI)
        private float dpiScale = 1.0f;

        // CSV auto-recording
        private bool isCsvRecording = false;
        private Timer csvTimer;
        private string csvFilePath = "";
        private int lastCsvSavedIndex = -1;  // Track last saved reading index

        // Cumulative and segment integrations
        private double cumulCapacity_mAh = 0;
        private double cumulEnergy_Wh = 0;
        private double segCapacity_mAh = 0;
        private double segEnergy_Wh = 0;
        private bool showQtEt = false;       // True only when session starts from known SOC (≥99% or ≤6%)
        private bool showQtEtDecided = false; // True after first reading has been evaluated
        private BatteryState lastSegState = BatteryState.Idle;

        // Header labels: live readings + segment and total capacity/energy
        private Label lblStatus, lblVoltage, lblCurrent, lblResistance, lblPower, lblSOC;
        private Label lblSegCap, lblSegEnergy, lblTotCap, lblTotEnergy;

        // Charts
        private DualAxisChartPanel chartVI;
        private DualAxisChartPanel chartPS;

        private TableLayoutPanel chartGrid;
        private bool isStacked = false;

        // Controls
        private Button btnToggle, btnClear, btnLayout;
        private NumericUpDown nudInterval;
        private Button btnExportCSV;
        private ComboBox cmbTimeWindow;
        private Label lblElapsed, lblSamples;

        // TC66 USB Meter
        private TC66Reader tc66Reader = new TC66Reader();
        private ComboBox cmbTC66Port;
        private Button btnTC66Connect;
        private Button btnTC66Disconnect;
        private Button btnTC66ScreenFlip;
        private Button btnTC66Refresh;
        private Panel tc66HeaderPanel;
        private Label lblTC66Status, lblTC66Volt, lblTC66Amp, lblTC66Resistance, lblTC66Power;
        private Label lblTC66Temp, lblTC66mAh, lblTC66mWh, lblTC66Eff;
        private TC66Reading lastTC66Reading;
        private double tc66BaseEnergy_mWh = 0;  // Starting mWh for efficiency calc
        private DateTime efficiencyStartTime;   // When efficiency tracking started
        private NumericUpDown nudPsys;          // System baseline power (W)

        // Keep system awake while recording (screen can turn off, CPU stays active)
        [DllImport("kernel32.dll")]
        private static extern uint SetThreadExecutionState(uint esFlags);
        private const uint ES_CONTINUOUS       = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED  = 0x00000001;
        private const uint ES_AWAYMODE_REQUIRED = 0x00000040;

        // Modern power request API (works with Modern Standby / Connected Standby)
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr PowerCreateRequest(ref POWER_REQUEST_CONTEXT ctx);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool PowerSetRequest(IntPtr handle, int type);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool PowerClearRequest(IntPtr handle, int type);
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);
        private const int PowerRequestSystemRequired = 1;
        private const int PowerRequestAwayModeRequired = 2;
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct POWER_REQUEST_CONTEXT
        {
            public uint Version;
            public uint Flags;
            [MarshalAs(UnmanagedType.LPWStr)] public string SimpleReasonString;
        }
        private IntPtr powerRequest = IntPtr.Zero;

        private void EnableStayAwake()
        {
            SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_AWAYMODE_REQUIRED);
            try
            {
                var ctx = new POWER_REQUEST_CONTEXT
                {
                    Version = 0, Flags = 0x00000001, // POWER_REQUEST_CONTEXT_SIMPLE_STRING
                    SimpleReasonString = "Battery Monitor recording"
                };
                powerRequest = PowerCreateRequest(ref ctx);
                if (powerRequest != IntPtr.Zero)
                {
                    PowerSetRequest(powerRequest, PowerRequestSystemRequired);
                    PowerSetRequest(powerRequest, PowerRequestAwayModeRequired);
                }
            }
            catch { }
        }

        private void DisableStayAwake()
        {
            SetThreadExecutionState(ES_CONTINUOUS);
            try
            {
                if (powerRequest != IntPtr.Zero)
                {
                    PowerClearRequest(powerRequest, PowerRequestSystemRequired);
                    PowerClearRequest(powerRequest, PowerRequestAwayModeRequired);
                    CloseHandle(powerRequest);
                    powerRequest = IntPtr.Zero;
                }
            }
            catch { }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        // Theme (light)
        static readonly Color FormBg    = Color.FromArgb(240, 240, 245);
        static readonly Color PanelBg   = Color.FromArgb(230, 232, 238);
        static readonly Color TextCol   = Color.FromArgb(50, 50, 60);
        static readonly Color AccentCol = Color.FromArgb(30, 80, 180);
        static readonly Color GreenCol  = Color.FromArgb(46, 140, 50);
        static readonly Color RedCol    = Color.FromArgb(211, 47, 47);
        static readonly Color GrayCol   = Color.FromArgb(120, 120, 130);
        static readonly Color LRCol     = Color.FromArgb(255, 0, 128);

        public MainForm()
        {
            // Calculate DPI scaling factor before UI setup
            using (Graphics g = this.CreateGraphics())
            {
                dpiScale = g.DpiX / 96.0f;  // 96 DPI = 100%, 144 DPI = 150%, 192 DPI = 200%
            }
            
            SetupForm();
            BuildUI();
            SetupTimers();

            // Force full repaint of charts on resize
            this.Resize += (s, e) => { chartVI.Invalidate(); chartPS.Invalidate(); };

            try
            {
                int val = 0;
                DwmSetWindowAttribute(this.Handle, 20, ref val, 4);
            }
            catch { }
        }

        // Global scale factor based on Windows DPI setting
        private double screenScale = 1.0;
        
        private void SetupForm()
        {
            this.Text = "Battery Monitor " + AppVersion;
            
            // Use DPI scaling instead of resolution-based scaling
            // This properly handles 100%, 125%, 150%, 200% Windows display settings
            screenScale = dpiScale;  // Already calculated in constructor
            
            // Completely disable DPI auto-scaling (we handle it manually)
            this.AutoScaleMode = AutoScaleMode.None;
            this.WindowState = FormWindowState.Normal;
            this.MaximizeBox = true;
            
            // Scale window size: base 1600×900 for FHD
            this.Size = new Size(S(1600), S(900));
            this.MinimumSize = new Size(S(1400), S(750));
            
            this.BackColor = FormBg;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10);
            this.FormBorderStyle = FormBorderStyle.Sizable;
        }
        
        // Helper: Scale integer values
        private int S(int baseValue)
        {
            return (int)(baseValue * screenScale);
        }
        
        // Helper: Scale float values
        private float SF(float baseValue)
        {
            return (float)(baseValue * FontScale * screenScale);
        }

        // Helper: Button Width - measures text at controlFont and adds padding, ensures button fits its label
        private Font _controlFontRef;  // set during BuildUI for BW() access
        private int BW(string text, int extraPad = 18)
        {
            Font f = _controlFontRef ?? SystemFonts.DefaultFont;
            int measured = TextRenderer.MeasureText(text, f).Width;
            return Math.Max(measured + extraPad, S(40));  // never narrower than S(40)
        }

        private void BuildUI()
        {
            // ---- HEADER: BMS row — FlowLayoutPanel so labels take exact space needed ----
            Panel header = new Panel { Dock = DockStyle.Top, Height = S(40), BackColor = PanelBg };

            FlowLayoutPanel hdr1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, BackColor = PanelBg,
                Padding = new Padding(S(4), 0, 0, 0),
                Margin = Padding.Empty,
                WrapContents = false,
                AutoSize = false
            };

            lblStatus     = MakeHeaderLabel("IDLE",      GrayCol);
            lblVoltage    = MakeHeaderLabel("V=-- V",    TextCol);
            lblCurrent    = MakeHeaderLabel("i=-- A",    TextCol);
            lblResistance = MakeHeaderLabel("R=-- Ω",    TextCol);
            lblPower      = MakeHeaderLabel("P=-- W",    TextCol);
            lblSOC        = MakeHeaderLabel("SOC=-- %",  TextCol);
            lblSOC.BackColor = Color.FromArgb(255, 255, 180);  // Yellow highlight
            lblSOC.Padding   = new Padding(S(6), 0, S(6), 0);   // Horizontal inset
            lblSegCap     = MakeHeaderLabel("Qc=0.00 Ah", GreenCol);
            lblSegEnergy  = MakeHeaderLabel("Ec=0.00 Wh", GreenCol);
            lblTotCap     = MakeHeaderLabel("Qt=0.00 Ah", AccentCol);
            lblTotEnergy  = MakeHeaderLabel("Et=0.00 Wh", AccentCol);

            foreach (var lbl in new[] { lblStatus, lblVoltage, lblCurrent, lblResistance,
                                         lblPower, lblSegCap, lblSegEnergy,
                                         lblTotCap, lblTotEnergy, lblSOC })
                hdr1.Controls.Add(lbl);

            header.Controls.Add(hdr1);

            // ---- TC66 HEADER ROW (initially hidden) ----
            tc66HeaderPanel = new Panel { Dock = DockStyle.Top, Height = S(40), BackColor = Color.FromArgb(220, 235, 220), Visible = false };

            FlowLayoutPanel hdrTC66 = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(220, 235, 220),
                Padding = new Padding(S(4), 0, 0, 0),
                Margin = Padding.Empty,
                WrapContents = false,
                AutoSize = false
            };

            Color TC66Col = Color.FromArgb(0, 120, 60);
            lblTC66Status     = MakeHeaderLabel("TC66 ",   TC66Col);  // Trailing space aligns with CHRG width
            lblTC66Volt       = MakeHeaderLabel("V=-- V",  TC66Col);
            lblTC66Amp        = MakeHeaderLabel("i=-- A",  TC66Col);
            lblTC66Resistance = MakeHeaderLabel("R=-- Ω",  TC66Col);
            lblTC66Power      = MakeHeaderLabel("P=-- W",  TC66Col);
            lblTC66Temp       = MakeHeaderLabel("T=--°C",  TC66Col);
            lblTC66mAh        = MakeHeaderLabel("Qin=0.00 Ah",  TC66Col);
            lblTC66mWh        = MakeHeaderLabel("Ein=0.00 Wh",  TC66Col);
            lblTC66Eff        = MakeHeaderLabel("η=--%",   Color.FromArgb(180, 0, 180));

            foreach (var lbl in new[] { lblTC66Status, lblTC66Volt, lblTC66Amp, lblTC66Resistance,
                                         lblTC66Power, lblTC66mAh, lblTC66mWh, lblTC66Eff, lblTC66Temp })
                hdrTC66.Controls.Add(lbl);

            tc66HeaderPanel.Controls.Add(hdrTC66);

            // ---- CONTROL BAR - UNIFIED DESIGN ----
            // All elements use consistent 13pt base font, proper DPI scaling, uniform height
            int btnHeight = S(48);  // Uniform button height (scales with DPI)
            int barH = S(58);       // Bar height with padding
            Font controlFont = new Font("Segoe UI", SF(14), FontStyle.Bold);
            _controlFontRef = controlFont;  // Make available to BW() helper
            
            Panel ctrlBar = new Panel { Dock = DockStyle.Bottom, Height = barH, BackColor = PanelBg };

            // Start/Stop button
            btnToggle = new Button
            {
                Text = "Start", Width = BW("Start"), Height = btnHeight,
                Font = controlFont, ForeColor = GreenCol, BackColor = FormBg,
                FlatStyle = FlatStyle.Flat, Margin = new Padding(S(3))
            };
            btnToggle.FlatAppearance.BorderColor = GreenCol;
            btnToggle.Click += OnToggleRun;

            // Clear button
            btnClear = new Button
            {
                Text = "Clear", Width = BW("Clear"), Height = btnHeight,
                Font = controlFont, ForeColor = GrayCol, BackColor = FormBg,
                FlatStyle = FlatStyle.Flat, Margin = new Padding(S(3))
            };
            btnClear.FlatAppearance.BorderColor = GrayCol;
            btnClear.Click += OnClear;

            // Load CSV button (replaces Stack; Stack still available via Ctrl+L)
            btnLayout = new Button
            {
                Text = "Load CSV", Width = BW("Load CSV"), Height = btnHeight,
                Font = controlFont, ForeColor = Color.FromArgb(0, 120, 215), BackColor = FormBg,
                FlatStyle = FlatStyle.Flat, Margin = new Padding(S(3))
            };
            btnLayout.FlatAppearance.BorderColor = Color.FromArgb(0, 120, 215);
            btnLayout.Click += OnLoadCSV;

            // Interval label and control
            Label li = new Label
            {
                Text = "Int:", ForeColor = TextCol, AutoSize = true,
                Font = controlFont, TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(S(8), 0, S(3), 0)
            };
            
            nudInterval = new NumericUpDown
            {
                Minimum = 1, Maximum = 300, Value = 1, Width = S(60), Height = btnHeight,
                Font = controlFont, BackColor = FormBg, ForeColor = TextCol,
                Margin = new Padding(0)
            };
            
            Label ls = new Label
            {
                Text = "s", ForeColor = TextCol, AutoSize = true,
                Font = controlFont, TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(S(3), 0, S(8), 0)
            };

            // CSV Record button
            btnExportCSV = new Button
            {
                Text = "Rec CSV", Width = BW("Rec CSV"), Height = btnHeight,
                Font = controlFont, ForeColor = Color.FromArgb(0, 120, 215), BackColor = FormBg,
                FlatStyle = FlatStyle.Flat, Margin = new Padding(S(3))
            };
            btnExportCSV.FlatAppearance.BorderColor = Color.FromArgb(0, 120, 215);
            btnExportCSV.Click += OnExportCSV;

            // Diagnostics button
            Button btnDiag = new Button
            {
                Text = "Diag", Width = BW("Diag"), Height = btnHeight,
                Font = controlFont, ForeColor = Color.FromArgb(100, 100, 200), BackColor = FormBg,
                FlatStyle = FlatStyle.Flat, Margin = new Padding(S(3))
            };
            btnDiag.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 200);
            btnDiag.Click += OnDiagnostics;

            // Time span label and combo
            Label lw = new Label
            {
                Text = "Span:", ForeColor = TextCol, AutoSize = true,
                Font = controlFont, TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(S(8), 0, S(3), 0)
            };
            
            cmbTimeWindow = new ComboBox
            {
                Width = BW("2 hours", 28), Height = btnHeight, Font = controlFont,
                BackColor = FormBg, ForeColor = TextCol, DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0)
            };
            cmbTimeWindow.Items.AddRange(new object[] { "All", "10 min", "30 min", "1 hour", "2 hours" });
            cmbTimeWindow.SelectedIndex = 0;
            cmbTimeWindow.SelectedIndexChanged += OnWindowChanged;

            // TC66 section
            Label ltc = new Label
            {
                Text = "TC66:", ForeColor = Color.FromArgb(0, 120, 60), AutoSize = true,
                Font = controlFont, TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(S(8), 0, S(3), 0)
            };

            cmbTC66Port = new ComboBox
            {
                Width = BW("COM10", 28), Height = btnHeight, Font = controlFont,
                BackColor = FormBg, ForeColor = TextCol, DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0)
            };
            cmbTC66Port.DropDown += OnTC66PortDropDown;
            RefreshTC66Ports();

            btnTC66Connect = new Button
            {
                Text = "Con", Width = BW("Con"), Height = btnHeight,
                Font = controlFont, ForeColor = Color.FromArgb(0, 120, 60), BackColor = FormBg,
                FlatStyle = FlatStyle.Flat, Margin = new Padding(S(3))
            };
            btnTC66Connect.FlatAppearance.BorderColor = Color.FromArgb(0, 120, 60);
            btnTC66Connect.Click += OnTC66Connect;

            btnTC66ScreenFlip = new Button
            {
                Text = "⇅", Width = BW("⇅", 22), Height = btnHeight,
                Font = new Font("Segoe UI", SF(20), FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 60), BackColor = FormBg,
                FlatStyle = FlatStyle.Flat, Enabled = false, Margin = new Padding(S(3))
            };
            btnTC66ScreenFlip.FlatAppearance.BorderColor = Color.FromArgb(0, 120, 60);
            btnTC66ScreenFlip.Click += OnTC66ScreenFlip;

            btnTC66Refresh = new Button
            {
                Text = "Refr", Width = BW("Refr", 32), Height = btnHeight,
                Font = controlFont, ForeColor = Color.FromArgb(0, 200, 0), BackColor = FormBg,
                FlatStyle = FlatStyle.Flat, Margin = new Padding(S(3))
            };
            btnTC66Refresh.FlatAppearance.BorderColor = Color.FromArgb(0, 200, 0);
            btnTC66Refresh.Click += OnRefreshCharts;

            btnTC66Disconnect = new Button
            {
                Text = "Disc", Width = BW("Disc"), Height = btnHeight,
                Font = controlFont, ForeColor = RedCol, BackColor = FormBg,
                FlatStyle = FlatStyle.Flat, Enabled = false, Visible = false,
                Margin = new Padding(S(3))
            };
            btnTC66Disconnect.FlatAppearance.BorderColor = RedCol;
            btnTC66Disconnect.Click += OnTC66Disconnect;

            // System baseline power for efficiency correction
            Label lps = new Label
            {
                Text = "Psys:", ForeColor = Color.FromArgb(180, 0, 180), AutoSize = true,
                Font = controlFont, TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(S(8), 0, S(3), 0)
            };
            
            nudPsys = new NumericUpDown
            {
                Minimum = 0, Maximum = 100, Value = 0, DecimalPlaces = 1, Increment = 0.5M,
                Width = S(60), Height = btnHeight, Font = controlFont,
                BackColor = FormBg, ForeColor = Color.FromArgb(180, 0, 180),
                Margin = new Padding(0)
            };
            
            Label lpsw = new Label
            {
                Text = "W", ForeColor = Color.FromArgb(180, 0, 180), AutoSize = true,
                Font = controlFont, TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(S(3), 0, S(8), 0)
            };

            // Run time and sample count
            Label lrt = new Label
            {
                Text = "Run time", ForeColor = TextCol, AutoSize = true,
                Font = controlFont, TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(S(8), 0, S(5), 0)
            };
            
            lblElapsed = new Label
            {
                Text = "00:00:00", ForeColor = AccentCol, AutoSize = true,
                Font = new Font("Consolas", SF(19), FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0)
            };
            
            lblSamples = new Label
            {
                Text = "0 samples", ForeColor = TextCol, AutoSize = true,
                Font = controlFont, TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(S(8), 0, 0, 0)
            };

            // Build arrays of controls and gaps
            Control[] ctrls = new Control[] {
                btnToggle, btnClear, btnLayout,
                li, nudInterval, ls,
                btnExportCSV, btnDiag, lw, cmbTimeWindow, btnTC66Refresh,
                ltc, cmbTC66Port, btnTC66Connect, btnTC66ScreenFlip,
                lps, nudPsys, lpsw,
                lrt, lblElapsed, lblSamples
            };
            int[] gaps = new int[] {
                4, 4, 8,
                2, 2, 8,
                4, 4, 2, 2, 8,
                2, 2, 3, 8,
                2, 2, 8,
                3, 6, 0
            };

            // Add all controls to panel
            foreach (Control c in ctrls)
                ctrlBar.Controls.Add(c);
            
            // Add Disconnect button separately (positioned manually over Connect button)
            ctrlBar.Controls.Add(btnTC66Disconnect);

            // Layout event to position controls after they're measured
            ctrlBar.Layout += (s, ev) => {
                int x = 8;
                int h = ctrlBar.Height;
                for (int i = 0; i < ctrls.Length; i++)
                {
                    Control c = ctrls[i];
                    int ch = c.Height > 0 ? c.Height : c.PreferredSize.Height;
                    int cw = c.Width > 0 ? c.Width : c.PreferredSize.Width;
                    c.Left = x;
                    c.Top = (h - ch) / 2;
                    x += cw + gaps[i];
                }
                
                // Position Disconnect button at same location as Connect button
                btnTC66Disconnect.Left = btnTC66Connect.Left;
                btnTC66Disconnect.Top = btnTC66Connect.Top;
            };

            // ---- CHARTS ----
            chartVI = new DualAxisChartPanel
            {
                ChartTitle = "Voltage, Current & Capacity",
                Y1Title = "Voltage (V)", Y1Unit = "V",
                Y2Title = "Current (A)", Y2Unit = "A",
                Dock = DockStyle.Fill, Margin = new Padding(3),
                DpiScale = dpiScale,  // Apply DPI scaling to chart fonts
                FontScale = FontScale,
                EnableLR = false,
                EnableAvgY1 = true,  // Enable avg on BMS Voltage
                EnableAvgY2 = true,  // Enable avg on BMS Current
                EnableAvgTC66_1 = true,  // Enable avg on TC66 Voltage
                EnableAvgTC66_2 = true,  // Enable avg on TC66 Current
                // Voltage: Darker Blue (charge) / Darker Magenta (discharge) / Dark Gray (idle)
                Y1ChargeColor = Color.FromArgb(0x21, 0x96, 0xF3),      // Brighter blue (more visible)
                Y1DischargeColor = Color.FromArgb(0xCC, 0x00, 0xCC),   // Darker magenta
                Y1IdleColor = Color.FromArgb(0x50, 0x50, 0x50),        // Dark gray (differentiate from current)
                // Current: Darker Green (charge) / Darker Red (discharge) / Light Gray (idle)
                Y2ChargeColor = Color.FromArgb(0x4C, 0x9A, 0x50),      // Darker green
                Y2DischargeColor = Color.FromArgb(0xC8, 0x32, 0x28),   // Darker red
                Y2IdleColor = Color.FromArgb(0x88, 0x88, 0x88),        // Light gray (differentiate from voltage)
                // TC66 colors (lighter for differentiation)
                TC66Y1ChargeColor = Color.FromArgb(0x42, 0xA5, 0xF5),     // Light blue
                TC66Y1DischargeColor = Color.FromArgb(0xFF, 0x4D, 0xFF),  // Light magenta
                TC66Y1IdleColor = Color.FromArgb(0x70, 0x70, 0x70),       // Medium-dark gray (between BMS V and I)
                TC66Y2ChargeColor = Color.FromArgb(0xA5, 0xD6, 0xA7),     // Lighter green (vs BMS current)
                TC66Y2DischargeColor = Color.FromArgb(0xEF, 0x53, 0x50),  // Light red
                TC66Y2IdleColor = Color.FromArgb(0xA0, 0xA0, 0xA0)        // Lightest gray (lighter than BMS I)
            };

            chartPS = new DualAxisChartPanel
            {
                ChartTitle = "SOC, Power & Temperature",
                Y1Title = "Power (W)", Y1Unit = "W",
                Y1LegendTitle = "Power (W)",
                Y2Title = "SOC (%)", Y2Unit = "%",
                Y2LegendTitle = "SOC (%)",
                Dock = DockStyle.Fill, Margin = new Padding(3),
                DpiScale = dpiScale,
                FontScale = FontScale,
                EnableLR = true,
                EnableAvgY1 = true,
                EnableAvgTC66_1 = true,
                EnableAvgTC66_2 = true,  // Temperature avg
                Y1ChargeColor = Color.FromArgb(0x00, 0x96, 0xA8),
                Y1DischargeColor = Color.FromArgb(0xCC, 0x72, 0x00),
                Y1IdleColor = Color.FromArgb(0x60, 0x60, 0x60),
                Y2ChargeColor = Color.FromArgb(0x15, 0x4A, 0x18),
                Y2DischargeColor = Color.FromArgb(0x99, 0x28, 0x09),
                Y2IdleColor = Color.FromArgb(0x60, 0x60, 0x60),
                TC66Y1ChargeColor = Color.FromArgb(0x26, 0xC6, 0xDA),
                TC66Y1DischargeColor = Color.FromArgb(0xFF, 0xB7, 0x4D),
                TC66Y1IdleColor = Color.FromArgb(0x70, 0x70, 0x70),
                TC66Y2ChargeColor = Color.FromArgb(0x43, 0xA0, 0x47),
                TC66Y2DischargeColor = Color.FromArgb(0xE6, 0x4A, 0x19),
                TC66Y2IdleColor = Color.FromArgb(0xA0, 0xA0, 0xA0)
            };

            // Wire up LR prediction callback
            // LR prediction is displayed directly on the chart
            chartPS.OnLRPrediction = (pred) => { };

            chartGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, Padding = new Padding(2), BackColor = FormBg
            };
            ApplyLayout();

            // Dock order (bottom to top in add order)
            this.Controls.Add(chartGrid);
            this.Controls.Add(ctrlBar);
            this.Controls.Add(tc66HeaderPanel);  // TC66 row (hidden initially)
            this.Controls.Add(header);
        }

        private void ApplyLayout()
        {
            chartGrid.SuspendLayout();
            chartGrid.Controls.Clear();
            chartGrid.ColumnStyles.Clear();
            chartGrid.RowStyles.Clear();

            if (isStacked)
            {
                chartGrid.ColumnCount = 1;
                chartGrid.RowCount = 2;
                chartGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                chartGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
                chartGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
                chartGrid.Controls.Add(chartVI, 0, 0);
                chartGrid.Controls.Add(chartPS, 0, 1);
            }
            else
            {
                chartGrid.ColumnCount = 2;
                chartGrid.RowCount = 1;
                chartGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                chartGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                chartGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                chartGrid.Controls.Add(chartVI, 0, 0);
                chartGrid.Controls.Add(chartPS, 1, 0);
            }

            chartGrid.ResumeLayout(true);
        }

        private Label MakeHeaderLabel(string text, Color color)
        {
            // At 100% DPI, use smaller font (12pt) to fit all units
            // At 150%/200%, use 14pt for better readability
            int baseFontSize = (dpiScale <= 1.0f) ? 22 : 25;
            return MakeHeaderLabel(text, color, baseFontSize);
        }

        private Label MakeHeaderLabel(string text, Color color, int fontSize = 16)
        {
            return new Label
            {
                Text = text, ForeColor = color,
                Font = new Font("Segoe UI", SF(fontSize), FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, S(4), S(14), 0),
                Padding = Padding.Empty,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        // ---- TC66 Methods ----
        private void RefreshTC66Ports()
        {
            cmbTC66Port.Items.Clear();
            string[] ports = TC66Reader.GetAvailablePorts();
            foreach (string p in ports)
                cmbTC66Port.Items.Add(p);
            if (cmbTC66Port.Items.Count > 0)
                cmbTC66Port.SelectedIndex = 0;
        }

        private void OnTC66PortDropDown(object sender, EventArgs e)
        {
            RefreshTC66Ports();
        }

        private void OnTC66Connect(object sender, EventArgs e)
        {
            // Connect
            if (cmbTC66Port.SelectedItem == null)
            {
                MessageBox.Show("Select a COM port first.", "TC66", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string portName = cmbTC66Port.SelectedItem.ToString();
            if (tc66Reader.Connect(portName))
            {
                tc66HeaderPanel.Visible = true;
                btnTC66Connect.Visible = false;
                btnTC66ScreenFlip.Enabled = true;
                btnTC66Disconnect.Visible = true;
                btnTC66Disconnect.Enabled = true;
                
                // Do initial read
                var r = tc66Reader.Poll();
                if (r.IsValid)
                {
                    lastTC66Reading = r;
                    UpdateTC66Header(r);
                    lblTC66Status.Text = "TC66 ";  // Version number removed — use trailing space for column alignment
                }
                
                // Set efficiency baseline if recording is already running
                if (isRunning)
                {
                    ResetEfficiencyBaseline();
                    
                    // CRITICAL: Start TC66 polling timer if not already running
                    if (tc66Timer == null)
                    {
                        tc66Timer = new System.Threading.Timer(OnTC66Tick, null, 1000, 1000);
                    }
                }
            }
            else
            {
                MessageBox.Show("Failed to connect to TC66 on " + portName + ".\n\nMake sure the TC66 is connected via micro-USB and no other program is using the port.",
                    "TC66", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnTC66Disconnect(object sender, EventArgs e)
        {
            // Disconnect
            tc66Reader.Disconnect();
            tc66HeaderPanel.Visible = false;
            btnTC66Connect.Visible = true;
            btnTC66ScreenFlip.Enabled = false;
            btnTC66Disconnect.Visible = false;
            btnTC66Disconnect.Enabled = false;
            
            // Stop TC66 polling timer if running
            if (tc66Timer != null)
            {
                tc66Timer.Dispose();
                tc66Timer = null;
            }
        }

        private void OnTC66ScreenFlip(object sender, EventArgs e)
        {
            if (tc66Reader.IsConnected)
            {
                tc66Reader.RotateScreen();
            }
        }

        private void OnRefreshCharts(object sender, EventArgs e)
        {
            // Reset time window to "All" (full range)
            if (cmbTimeWindow != null)
            {
                cmbTimeWindow.SelectedIndex = 0;  // Set to "All"
                
                // Force chart refresh even if already at "All" (index 0)
                double[] wins = { double.MaxValue, 600, 1800, 3600, 7200 };
                double tw = wins[0];  // "All" = double.MaxValue
                chartVI.TimeWindow = tw;
                chartPS.TimeWindow = tw;
                chartVI.Invalidate();
                chartPS.Invalidate();
            }
        }

        private void OnExportCSV(object sender, EventArgs e)
        {
            if (!isCsvRecording)
            {
                // Start CSV recording
                StartCsvRecording();
            }
            else
            {
                // Stop CSV recording
                StopCsvRecording();
            }
        }

        private void StartCsvRecording()
        {
            if (allReadings.Count == 0)
            {
                MessageBox.Show("No data to record. Start monitoring first.",
                    "Battery Monitor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Generate filename with timestamp
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            csvFilePath = Path.Combine(desktop,
                "BatteryLog_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");

            try
            {
                // Check if any TC66 data is present
                bool hasAnyTC66Data = allReadings.Any(r => r.HasTC66Data);

                // Create CSV file with header
                using (StreamWriter sw = new StreamWriter(csvFilePath, false, Encoding.UTF8))
                {
                    // Always include TC66 columns in header (even if not yet connected)
                    string header = "Timestamp,Elapsed_s,Voltage_V,Current_A,Power_W,SOC_Pct,State,Qt_Ah,Et_Wh,Qc_Ah,Ec_Wh,TC66_V,TC66_A,TC66_W,TC66_Temp,TC66_Ah,TC66_Wh";
                    sw.WriteLine(header);

                    // Write all existing data (TC66 columns will be empty if no TC66 data)
                    foreach (BatteryReading r in allReadings)
                    {
                        WriteReadingToCsv(sw, r, true);  // Always pass true to write TC66 columns
                    }
                }

                lastCsvSavedIndex = allReadings.Count - 1;
                isCsvRecording = true;

                // Start 10-second auto-save timer
                csvTimer = new Timer();
                csvTimer.Interval = 10000;  // 10 seconds
                csvTimer.Tick += OnCsvTimerTick;
                csvTimer.Start();

                // Update button appearance
                btnExportCSV.Text = "Stop CSV";
                btnExportCSV.ForeColor = Color.FromArgb(211, 47, 47);  // Red
                btnExportCSV.FlatAppearance.BorderColor = Color.FromArgb(211, 47, 47);

                string fn = Path.GetFileNameWithoutExtension(csvFilePath);
                this.Text = "Battery Monitor " + AppVersion + "  —  " + fn;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting CSV recording:\n" + ex.Message,
                    "Battery Monitor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopCsvRecording()
        {
            if (csvTimer != null)
            {
                csvTimer.Stop();
                csvTimer.Dispose();
                csvTimer = null;
            }

            isCsvRecording = false;

            // Update button appearance
            btnExportCSV.Text = "Rec CSV";
            btnExportCSV.ForeColor = Color.FromArgb(0, 120, 215);
            btnExportCSV.FlatAppearance.BorderColor = Color.FromArgb(0, 120, 215);
            this.Text = "Battery Monitor " + AppVersion;
        }

        private void OnCsvTimerTick(object sender, EventArgs e)
        {
            // Append new data since last save
            if (allReadings.Count > lastCsvSavedIndex + 1)
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(csvFilePath, true, Encoding.UTF8))  // append mode
                    {
                        // Write only new readings (always include TC66 columns to match header)
                        for (int i = lastCsvSavedIndex + 1; i < allReadings.Count; i++)
                        {
                            WriteReadingToCsv(sw, allReadings[i], true);  // Always true to match header
                        }
                    }

                    lastCsvSavedIndex = allReadings.Count - 1;
                }
                catch (Exception ex)
                {
                    // Don't show messagebox during auto-save (would be annoying)
                    // Just log to debug if needed
                    System.Diagnostics.Debug.WriteLine("CSV auto-save error: " + ex.Message);
                }
            }
        }

        private void WriteReadingToCsv(StreamWriter sw, BatteryReading r, bool hasAnyTC66Data)
        {
            string line = string.Format("\"{0}\",{1:F1},{2:F3},{3:F3},{4:F2},{5:F1},{6},{7:F6},{8:F3},{9:F6},{10:F3}",
                r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                r.ElapsedSeconds, r.Voltage_V, r.Current_mA / 1000.0, r.Power_W, r.SOC_Percent, r.State,
                r.TotalCapacity_mAh / 1000.0, r.TotalEnergy_Wh, r.SegmentCapacity_mAh / 1000.0, r.SegmentEnergy_Wh);

            // Add TC66 data if available for this reading
            if (hasAnyTC66Data)
            {
                if (r.HasTC66Data)
                {
                    line += string.Format(",{0:F4},{1:F5},{2:F4},{3},{4:F6},{5:F3}",
                        r.TC66_V, r.TC66_A, r.TC66_W, r.TC66_Temp_C, r.TC66_mAh / 1000.0, r.TC66_mWh / 1000.0);
                }
                else
                {
                    line += ",,,,,,";  // Empty TC66 columns
                }
            }
            sw.WriteLine(line);
        }

        private void OnDiagnostics(object sender, EventArgs e)
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filepath = Path.Combine(desktop,
                "BatteryMonitor_ThermalDiag_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
            
            reader.WriteThermalDiagnostics(filepath);
            
            // Removed confirmation popup - diagnostics written silently
        }

        private void UpdateTC66Header(TC66Reading r)
        {
            if (r == null || !r.IsValid) return;
            
            // Get current BMS state to match colors
            BatteryState currentState = BatteryState.Idle;
            if (allReadings.Count > 0)
                currentState = allReadings[allReadings.Count - 1].State;
            
            // Set colors based on state
            Color voltageColor, currentColor, powerColor;
            if (currentState == BatteryState.Charging)
            {
                voltageColor = chartVI.Y1ChargeColor;    // Blue
                currentColor = chartVI.Y2ChargeColor;    // Light Green
                powerColor = chartPS.Y1ChargeColor;      // Cyan
            }
            else if (currentState == BatteryState.Discharging)
            {
                voltageColor = chartVI.Y1DischargeColor;  // Magenta
                currentColor = chartVI.Y2DischargeColor;  // Bright Red
                powerColor = chartPS.Y1DischargeColor;    // Amber
            }
            else
            {
                voltageColor = chartVI.Y1IdleColor;
                currentColor = chartVI.Y2IdleColor;
                powerColor = chartPS.Y1IdleColor;
            }
            
            lblTC66Volt.Text = "V=" + r.Voltage_V.ToString("F2") + " V";
            lblTC66Volt.ForeColor = voltageColor;
            
            // TC66 current - use as-is (TC66 internal direction sensor determines sign)
            double displayTC66Current = r.Current_A;
            lblTC66Amp.Text = "i=" + displayTC66Current.ToString("F2") + " A";
            lblTC66Amp.ForeColor = currentColor;
            
            // TC66 Resistance: R = V / I (with I in Amps)
            if (r.Current_A > 0.01)
            {
                double tc66Resistance = r.Voltage_V / r.Current_A;
                lblTC66Resistance.Text = "R=" + tc66Resistance.ToString("F1") + " Ω";
                lblTC66Resistance.ForeColor = Color.FromArgb(0x8A, 0x2B, 0xE2);  // Purple
            }
            else
            {
                lblTC66Resistance.Text = "R=-- Ω";
                lblTC66Resistance.ForeColor = Color.FromArgb(0, 120, 60);  // TC66 green
            }
            
            // TC66 power - use as-is (TC66 internal direction sensor determines sign)
            double displayTC66Power = r.Power_W;
            lblTC66Power.Text = "P=" + (Math.Abs(displayTC66Power) < 10.0 ? displayTC66Power.ToString("F2") : displayTC66Power.ToString("F1")) + " W";
            lblTC66Power.ForeColor = powerColor;
            
            lblTC66Temp.Text = "T=" + r.Temperature_C + "°C";
            lblTC66Temp.ForeColor = Color.Red;  // Bright red to match curve
            
            // Display whichever group has non-zero values (TC66 can use Group0 or Group1)
            int mAh = r.Group0_mAh != 0 ? r.Group0_mAh : r.Group1_mAh;
            int mWh = r.Group0_mWh != 0 ? r.Group0_mWh : r.Group1_mWh;
            lblTC66mAh.Text = "Qin=" + (mAh / 1000.0).ToString("F2") + " Ah";
            lblTC66mWh.Text = "Ein=" + (mWh / 1000.0).ToString("F2") + " Wh";
        }

        private void ResetEfficiencyBaseline()
        {
            if (tc66Reader.IsConnected && lastTC66Reading != null && lastTC66Reading.IsValid)
            {
                int mWh = lastTC66Reading.Group0_mWh != 0 ? lastTC66Reading.Group0_mWh : lastTC66Reading.Group1_mWh;
                tc66BaseEnergy_mWh = mWh;
                efficiencyStartTime = DateTime.Now;
            }

            // UpdateEfficiency will auto-activate on first valid TC66 reading
        }

        private void UpdateEfficiency()
        {
            if (isCsvLoaded)
            {
                lblTC66Eff.Text = "η=n/a";
                return;
            }
            if (lastTC66Reading == null || !lastTC66Reading.IsValid)
            {
                lblTC66Eff.Text = "η=--%";
                return;
            }

            // Ec (segEnergy_Wh) = BMS charge energy for current segment — always positive during charge.
            // TC66 delta = current − baseline captured at recording start, so un-zeroed TC66 is handled correctly.
            double ecWh = segEnergy_Wh;

            // Use whichever TC66 group has non-zero values; subtract baseline so un-zeroed TC66 works too
            int currentMWh = lastTC66Reading.Group0_mWh != 0 ? lastTC66Reading.Group0_mWh : lastTC66Reading.Group1_mWh;
            double einWh = (currentMWh - tc66BaseEnergy_mWh) / 1000.0;

            // Subtract system baseline power × elapsed time
            double psys = (double)nudPsys.Value;
            if (psys > 0)
            {
                double elapsedHours = (DateTime.Now - efficiencyStartTime).TotalHours;
                einWh -= psys * elapsedHours;
            }

            if (einWh > 0.1 && ecWh > 0.1)  // Need at least 100mWh on both sides
            {
                double eff = (ecWh / einWh) * 100.0;
                if (eff > 0 && eff <= 150)
                    lblTC66Eff.Text = "η=" + eff.ToString("F1") + "%";
                else
                    lblTC66Eff.Text = "η=--";
            }
            else
            {
                lblTC66Eff.Text = "η=--%";
            }
        }


        private Button MakeButton(string text, Color color)
        {
            var b = new Button
            {
                Text = text, ForeColor = color, BackColor = FormBg,
                Font = new Font("Segoe UI", SF(19), FontStyle.Bold),
                FlatStyle = FlatStyle.Flat, Width = S(85), Height = S(40), Margin = new Padding(S(3))
            };
            b.FlatAppearance.BorderColor = color;
            return b;
        }

        private void SetupTimers()
        {
            // sampleTimer created on Start (System.Threading.Timer)

            clockTimer = new Timer { Interval = 500 };
            clockTimer.Tick += OnClockTick;
        }

        private void OnSampleTick(object state)
        {
            // Threading.Timer fires on a thread pool thread; marshal to UI
            try
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                    this.BeginInvoke(new Action(TakeSample));
            }
            catch { }
        }

        private void OnTC66Tick(object state)
        {
            // TC66 timer fires at 1 Hz
            try
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                    this.BeginInvoke(new Action(TakeSampleTC66));
            }
            catch { }
        }

        private void OnClockTick(object sender, EventArgs e)
        {
            if (isRunning)
            {
                TimeSpan ts = DateTime.Now - startTime;
                lblElapsed.Text = ts.ToString(@"hh\:mm\:ss");
                
                // Update run time in chart for display below LR prediction
                chartPS.RunTimeSeconds = ts.TotalSeconds;
                chartPS.Invalidate();
            }
        }

        private void OnToggleRun(object sender, EventArgs e)
        {
            if (isRunning) { StopRecording(); return; }
            if (!reader.IsBatteryPresent())
            {
                MessageBox.Show(
                    "No battery detected.\n\nThis app requires a laptop with a battery.",
                    "Battery Monitor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isRunning = true;
            startTime = DateTime.Now;
            
            // Start BMS sampling timer at user-configured interval
            int intervalMs = (int)nudInterval.Value * 1000;
            sampleTimer = new System.Threading.Timer(OnSampleTick, null, intervalMs, intervalMs);
            
            // Start TC66 polling timer at 1 Hz (1000ms) if connected
            if (tc66Reader.IsConnected)
            {
                tc66Timer = new System.Threading.Timer(OnTC66Tick, null, 1000, 1000);
            }
            
            clockTimer.Start();

            // Prevent system sleep while recording
            EnableStayAwake();
            btnToggle.Text = "Stop";
            btnToggle.ForeColor = RedCol; btnToggle.FlatAppearance.BorderColor = RedCol;
            nudInterval.Enabled = false;

            // Set TC66 efficiency baseline if connected
            if (tc66Reader.IsConnected)
                ResetEfficiencyBaseline();

            isCsvLoaded = false;  // Live recording — η calculation re-enabled
            TakeSample();
        }


        private void OnClear(object sender, EventArgs e)
        {
            isCsvLoaded = false;
            if (isRunning) StopRecording();
            allReadings.Clear();
            chartVI.ChartTitle = "Voltage, Current & Capacity";
            chartPS.ChartTitle = "SOC, Power & Temperature";
            this.Text = "Battery Monitor " + AppVersion;
            chartVI.ClearData();
            chartPS.ClearData();
            cumulCapacity_mAh = 0; segCapacity_mAh = 0;
            cumulEnergy_Wh = 0; segEnergy_Wh = 0; lastSegState = BatteryState.Idle;
            showQtEt = false; showQtEtDecided = false;
            UpdateHeader(null);
            lblSegCap.Text = "Qc=0.00 Ah"; lblTotCap.Text = "Qt=0.00 Ah";
            lblSegEnergy.Text = "Ec=0.00 Wh"; lblTotEnergy.Text = "Et=0.00 Wh";
            lblSamples.Text = "0 samples";
            lblElapsed.Text = " 00:00:00";
            
            // Reset run time in charts
            chartPS.RunTimeSeconds = 0;
            chartVI.RunTimeSeconds = 0;
            
            if (tc66Reader.IsConnected)
            {
                lblTC66mAh.Text = "Qin=0.00 Ah";
                lblTC66mWh.Text = "Ein=0.00 Wh";
                lblTC66Eff.Text = "η=--%";
            }
        }

        private void OnToggleLayout(object sender, EventArgs e)
        {
            isStacked = !isStacked;
            ApplyLayout();
        }

        private void OnLoadCSV(object sender, EventArgs e)
        {
            if (isRunning)
            {
                MessageBox.Show("Stop recording before loading a CSV file.",
                    "Battery Monitor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new OpenFileDialog
            {
                Title = "Load Battery Monitor CSV",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                LoadCSVFile(dlg.FileName);
            }
        }

        // Split a CSV line handling quoted fields and semicolon delimiters
        private string[] SplitCSVLine(string line, char delimiter)
        {
            var fields = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"') { inQuotes = !inQuotes; }
                else if (c == delimiter && !inQuotes) { fields.Add(current.ToString().Trim()); current.Clear(); }
                else { current.Append(c); }
            }
            fields.Add(current.ToString().Trim());
            return fields.ToArray();
        }

        private void LoadCSVFile(string path)
        {
            try
            {
                // Guard against unexpectedly large files (>50 MB)
                if (new System.IO.FileInfo(path).Length > 50L * 1024 * 1024)
                {
                    MessageBox.Show("File is too large (>50 MB). Please select a Battery Monitor CSV.",
                        "Battery Monitor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string[] lines = File.ReadAllLines(path, new System.Text.UTF8Encoding(true));
                if (lines.Length < 2)
                {
                    MessageBox.Show("File contains no data.", "Battery Monitor",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Clear existing data
                OnClear(null, null);
                isCsvLoaded = true;   // Suppress η — TC66 delta meaningless vs replayed BMS data

                // Detect whether first row is a header or data
                // If first non-BOM character is a digit or date, assume no header — use fixed column order
                string firstRow = lines[0].TrimStart('\uFEFF', '\u200B');
                // Auto-detect delimiter: semicolon, tab, or comma
                char delim = firstRow.Contains('\t') ? '\t' :
                             firstRow.Contains(';')  ? ';'  : ',';
                // Header detection: strip leading quote then check first real char
                char c0 = firstRow.TrimStart('"')[0];
                bool hasHeader = !char.IsDigit(c0) && c0 != '-';

                string[] hdr;
                int dataStartLine;
                if (hasHeader)
                {
                    hdr = SplitCSVLine(firstRow, delim);
                    dataStartLine = 1;
                }
                else
                {
                    hdr = new string[] { "Timestamp","Elapsed_s","Voltage_V","Current_A","Power_W","SOC_Pct","State","Qt_Ah","Et_Wh","Qc_Ah","Ec_Wh","TC66_V","TC66_A","TC66_W","TC66_Temp","TC66_Ah","TC66_Wh" };
                    dataStartLine = 0;
                }
                int iElapsed  = Array.IndexOf(hdr, "Elapsed_s");
                int iVoltage  = Array.IndexOf(hdr, "Voltage_V");
                int iCurrent  = Array.IndexOf(hdr, "Current_A");
                int iPower    = Array.IndexOf(hdr, "Power_W");
                int iSOC      = Array.IndexOf(hdr, "SOC_Pct");
                int iState    = Array.IndexOf(hdr, "State");
                int iQc       = Array.IndexOf(hdr, "Qc_Ah");
                int iEc       = Array.IndexOf(hdr, "Ec_Wh");
                int iQt       = Array.IndexOf(hdr, "Qt_Ah");
                int iEt       = Array.IndexOf(hdr, "Et_Wh");
                int iTC66V    = Array.IndexOf(hdr, "TC66_V");
                int iTC66A    = Array.IndexOf(hdr, "TC66_A");
                int iTC66W    = Array.IndexOf(hdr, "TC66_W");
                int iTC66T    = Array.IndexOf(hdr, "TC66_Temp");
                int iTC66Ah   = Array.IndexOf(hdr, "TC66_Ah");
                int iTC66Wh   = Array.IndexOf(hdr, "TC66_Wh");

                if (iElapsed < 0 || iVoltage < 0 || iSOC < 0)
                {
                    MessageBox.Show(
                        "File does not appear to be a Battery Monitor CSV.\n\n" +
                        "First 5 columns found: " + string.Join(", ", hdr.Take(5)) + "\n\n" +
                        "Note: Excel 2003 may have corrupted the file by splitting\n" +
                        "the timestamp field (which contains a space) into two columns.\n" +
                        "Try opening the original .csv file instead.",
                        "Battery Monitor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int loaded = 0;
                double maxElapsed = 0;

                foreach (string line in lines.Skip(dataStartLine))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] f = SplitCSVLine(line, delim);
                    if (f.Length <= Math.Max(iSOC, iState)) continue;

                    double elapsed = 0, v = 0, i_A = 0, p = 0, soc = 0;
                    if (!double.TryParse(f[iElapsed], out elapsed)) continue;
                    double.TryParse(f[iVoltage], out v);
                    double.TryParse(f[iCurrent], out i_A);
                    double.TryParse(f[iPower],   out p);
                    double.TryParse(f[iSOC],     out soc);

                    string stateStr = iState >= 0 && iState < f.Length ? f[iState].Trim() : "";
                    BatteryState state = stateStr == "Charging"    ? BatteryState.Charging :
                                        stateStr == "Discharging"  ? BatteryState.Discharging :
                                                                     BatteryState.Idle;

                    // Feed BMS data to charts
                    chartVI.AddY1Point(elapsed, v, state);
                    chartVI.AddY2Point(elapsed, i_A, state);
                    chartPS.AddY1Point(elapsed, p,   state);
                    chartPS.AddY2Point(elapsed, soc, state);

                    // Qc / Ec
                    if (iQc >= 0 && iQc < f.Length) { double qc; if (double.TryParse(f[iQc], out qc)) chartVI.AddCapacityChargePoint(elapsed, qc); }
                    if (iEc >= 0 && iEc < f.Length) { double ec; if (double.TryParse(f[iEc], out ec)) chartPS.AddEnergySegmentPoint(elapsed, ec); }

                    // Qt / Et (conditional)
                    if (iQt >= 0 && iQt < f.Length) { double qt; if (double.TryParse(f[iQt], out qt)) chartVI.AddCapacityDischargePoint(elapsed, qt); }
                    if (iEt >= 0 && iEt < f.Length) { double et; if (double.TryParse(f[iEt], out et)) chartPS.AddEnergyTotalPoint(elapsed, et); }

                    // TC66 data
                    bool hasTC66 = iTC66V >= 0 && iTC66V < f.Length;
                    if (hasTC66)
                    {
                        double tc66v = 0, tc66a = 0, tc66w = 0, tc66t = 0;
                        double.TryParse(f[iTC66V], out tc66v);
                        double.TryParse(f[iTC66A], out tc66a);
                        double.TryParse(f[iTC66W], out tc66w);
                        if (iTC66T < f.Length) double.TryParse(f[iTC66T], out tc66t);

                        if (tc66v > 0 || tc66a > 0)
                        {
                            chartVI.AddTC66Y1Point(elapsed, tc66v, state);
                            chartVI.AddTC66Y2Point(elapsed, tc66a, state);
                            chartPS.AddTC66Y1Point(elapsed, tc66w, state);
                            chartPS.AddTempPoint(elapsed, tc66t);
                        }
                    }

                    // Reconstruct BatteryReading for tooltip/LR/avg
                    var r = new BatteryReading
                    {
                        ElapsedSeconds    = elapsed,
                        Voltage_V         = v,
                        Current_mA        = Math.Abs(i_A) * 1000.0,
                        Power_W           = Math.Abs(p),
                        SOC_Percent       = soc,
                        State             = state,
                        SegmentCapacity_mAh = iQc >= 0 && iQc < f.Length ? double.Parse(f[iQc], System.Globalization.CultureInfo.InvariantCulture) * 1000 : 0,
                        SegmentEnergy_Wh  = iEc >= 0 && iEc < f.Length ? double.Parse(f[iEc], System.Globalization.CultureInfo.InvariantCulture) : 0,
                        TotalCapacity_mAh = iQt >= 0 && iQt < f.Length ? double.Parse(f[iQt], System.Globalization.CultureInfo.InvariantCulture) * 1000 : 0,
                        TotalEnergy_Wh    = iEt >= 0 && iEt < f.Length ? double.Parse(f[iEt], System.Globalization.CultureInfo.InvariantCulture) : 0,
                    };
                    allReadings.Add(r);

                    if (elapsed > maxElapsed) maxElapsed = elapsed;
                    loaded++;
                }

                // Update run time display and header with last reading
                if (allReadings.Count > 0)
                {
                    var last = allReadings[allReadings.Count - 1];
                    UpdateHeader(last);
                    int h = (int)(maxElapsed / 3600);
                    int m = (int)((maxElapsed % 3600) / 60);
                    int s2 = (int)(maxElapsed % 60);
                    lblElapsed.Text = string.Format("{0:D2}:{1:D2}:{2:D2}", h, m, s2);
                    lblSamples.Text = loaded + " samples";

                    chartVI.RunTimeSeconds = maxElapsed;
                    chartPS.RunTimeSeconds = maxElapsed;
                }

                string shortName = System.IO.Path.GetFileNameWithoutExtension(path);
                this.Text = "Battery Monitor " + AppVersion + "  —  " + shortName;

                chartVI.Invalidate();
                chartPS.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading file:\n" + ex.Message,
                    "Battery Monitor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnWindowChanged(object sender, EventArgs e)
        {
            double[] wins = { double.MaxValue, 600, 1800, 3600, 7200 };
            double tw = wins[cmbTimeWindow.SelectedIndex];
            chartVI.TimeWindow = tw;
            chartPS.TimeWindow = tw;
            chartVI.Invalidate();
            chartPS.Invalidate();
        }

        private void StopRecording()
        {
            isRunning = false;
            if (sampleTimer != null) { sampleTimer.Dispose(); sampleTimer = null; }
            if (tc66Timer != null) { tc66Timer.Dispose(); tc66Timer = null; }
            clockTimer.Stop();
            
            // Reset run time in charts
            chartPS.RunTimeSeconds = 0;
            chartVI.RunTimeSeconds = 0;
            chartPS.Invalidate();
            chartVI.Invalidate();
            
            // Stop CSV recording if active
            if (isCsvRecording)
            {
                if (csvTimer != null)
                {
                    csvTimer.Stop();
                    csvTimer.Dispose();
                    csvTimer = null;
                }
                isCsvRecording = false;
                btnExportCSV.Text = "Rec CSV";
                btnExportCSV.ForeColor = Color.FromArgb(0, 120, 215);
                btnExportCSV.FlatAppearance.BorderColor = Color.FromArgb(0, 120, 215);
            }
            
            // Allow system to sleep again
            DisableStayAwake();
            btnToggle.Text = "Start";
            btnToggle.ForeColor = GreenCol; btnToggle.FlatAppearance.BorderColor = GreenCol;
            nudInterval.Enabled = true;
        }

        private void TakeSample()
        {
            double elapsed = (DateTime.Now - startTime).TotalSeconds;
            BatteryReading r = reader.Read(elapsed);

            // Integrate capacity and energy (trapezoidal rule)
            if (allReadings.Count > 0)
            {
                BatteryReading prev = allReadings[allReadings.Count - 1];
                double dt_raw = r.ElapsedSeconds - prev.ElapsedSeconds;
                // Cap dt to 10 seconds — large gaps (sleep/hibernate) must not be integrated
                double dt_hours = Math.Min(dt_raw, 10.0) / 3600.0;

                // Capacity: average current * dt
                // Current_mA is stored as Math.Abs — apply sign from state
                double sign = (r.State == BatteryState.Discharging) ? -1.0 :
                              (r.State == BatteryState.Charging)    ?  1.0 : 0.0;
                double prevSign = (prev.State == BatteryState.Discharging) ? -1.0 :
                                  (prev.State == BatteryState.Charging)    ?  1.0 : 0.0;
                double avgCurrent = (prevSign * prev.Current_mA + sign * r.Current_mA) / 2.0;

                // Reset segment on charge/discharge state transition
                if (r.State != lastSegState && lastSegState != BatteryState.Idle)
                {
                    segCapacity_mAh = 0;
                    segEnergy_Wh = 0;
                }
                lastSegState = r.State != BatteryState.Idle ? r.State : lastSegState;

                // Energy: average power * dt - Power_W is also stored positive, apply same sign
                double avgPower = (prevSign * prev.Power_W + sign * r.Power_W) / 2.0;

                // Accumulate with natural sign (discharge negative, charge positive)
                segCapacity_mAh += avgCurrent * dt_hours;
                segEnergy_Wh += avgPower * dt_hours;

                // Cumulative total (same natural sign)
                cumulCapacity_mAh += avgCurrent * dt_hours;
                cumulEnergy_Wh += avgPower * dt_hours;
            }

            // Populate TC66 data if available
            if (tc66Reader.IsConnected && lastTC66Reading != null && lastTC66Reading.IsValid)
            {
                r.TC66_V = lastTC66Reading.Voltage_V;
                r.TC66_A = lastTC66Reading.Current_A;
                r.TC66_W = lastTC66Reading.Power_W;
                r.TC66_Temp_C = lastTC66Reading.Temperature_C;
                r.TC66_mAh = lastTC66Reading.Group0_mAh;
                r.TC66_mWh = lastTC66Reading.Group0_mWh;
                r.HasTC66Data = true;
            }
            else
            {
                r.HasTC66Data = false;
            }
            
            // Populate cumulative capacity and energy
            r.TotalCapacity_mAh = cumulCapacity_mAh;
            r.TotalEnergy_Wh = cumulEnergy_Wh;
            r.SegmentCapacity_mAh = segCapacity_mAh;
            r.SegmentEnergy_Wh = segEnergy_Wh;

            allReadings.Add(r);

            // Charts - BMS data
            chartVI.AddY1Point(elapsed, r.Voltage_V, r.State);
            // Current fed with natural sign: positive=charging, negative=discharging
            double signedCurrent = r.Current_mA / 1000.0 *
                (r.State == BatteryState.Discharging ? -1.0 : 1.0);
            chartVI.AddY2Point(elapsed, signedCurrent, r.State);
            chartPS.AddY1Point(elapsed, r.Power_W, r.State);
            chartPS.AddY2Point(elapsed, r.SOC_Percent, r.State);
            
            // Add BMS temperature if available
            if (r.HasTemperature)
            {
                chartPS.AddBMSTempPoint(elapsed, r.Temperature_C);
            }
            
            // Add Ec (segment energy) to Chart2 Y1 axis
            chartPS.AddEnergySegmentPoint(elapsed, r.SegmentEnergy_Wh);

            // Add Qc (cycle capacity) to Chart1 Y2 axis - natural sign
            chartVI.AddCapacityChargePoint(elapsed, r.SegmentCapacity_mAh / 1000.0);

            // Decide showQtEt on first reading: SOC ≥99% (starting discharge) or ≤6% (starting charge)
            if (!showQtEtDecided)
            {
                double soc = r.SOC_Percent;
                showQtEt = (soc >= 99.0 || soc <= 6.0);
                showQtEtDecided = true;
            }

            // Feed Qt and Et only when session started from a known SOC reference point
            if (showQtEt)
            {
                chartPS.AddEnergyTotalPoint(elapsed, r.TotalEnergy_Wh);
                chartVI.AddCapacityDischargePoint(elapsed, r.TotalCapacity_mAh / 1000.0);
            }
            
            // Resistance removed from display (crowded chart, artificial when voltage clamped)
            // double current_A = r.Current_mA / 1000.0;
            // if (current_A > 0.01)
            // {
            //     double resistance = r.Voltage_V / current_A;
            // Resistance is displayed in header only, not charted
            // }

            // Header
            UpdateHeader(r);
            
            // Update chart current state for proper tick label colors
            chartVI.CurrentBatteryState = r.State;
            chartPS.CurrentBatteryState = r.State;
            
            lblSegCap.Text = "Qc=" + (segCapacity_mAh / 1000.0).ToString("F2") + " Ah";
            lblTotCap.Text = "Qt=" + (cumulCapacity_mAh / 1000.0).ToString("F2") + " Ah";
            lblSegEnergy.Text = "Ec=" + segEnergy_Wh.ToString("F2") + " Wh";
            lblTotEnergy.Text = "Et=" + cumulEnergy_Wh.ToString("F2") + " Wh";
            lblSamples.Text = allReadings.Count + " samples";

            // Update efficiency calculation
            if (tc66Reader.IsConnected)
                UpdateEfficiency();
        }

        private void TakeSampleTC66()
        {
            if (!tc66Reader.IsConnected || !isRunning) return;

            double elapsed = (DateTime.Now - startTime).TotalSeconds;
            TC66Reading tc66 = tc66Reader.Poll();
            
            if (!tc66.IsValid) return;

            lastTC66Reading = tc66;
            UpdateTC66Header(tc66);

            // Add TC66 data to charts at 1 Hz
            // Use last BMS state for color-coding
            BatteryState currentState = allReadings.Count > 0 ? 
                allReadings[allReadings.Count - 1].State : BatteryState.Idle;

            chartVI.AddTC66Y1Point(elapsed, tc66.Voltage_V, currentState);  // TC66 voltage
            chartVI.AddTC66Y2Point(elapsed, tc66.Current_A, currentState);  // TC66 current
            chartPS.AddTC66Y1Point(elapsed, tc66.Power_W, currentState);    // TC66 power
            // TC66 resistance displayed in header only, not charted
            chartPS.AddTempPoint(elapsed, tc66.Temperature_C);              // TC66 temperature
            
            // Force chart refresh to ensure TC66 curves appear immediately
            chartVI.Invalidate();
            chartPS.Invalidate();
        }

        private void UpdateHeader(BatteryReading r)
        {
            if (r == null)
            {
                lblStatus.Text = "--"; lblStatus.ForeColor = GrayCol;
                lblVoltage.Text = "V=-- V"; lblVoltage.ForeColor = TextCol;
                lblCurrent.Text = "i=-- A"; lblCurrent.ForeColor = TextCol;
                lblResistance.Text = "R=-- Ω"; lblResistance.ForeColor = TextCol;
                lblPower.Text = "P=-- W"; lblPower.ForeColor = TextCol;
                lblSOC.Text = "SOC=-- %"; lblSOC.ForeColor = TextCol;
                return;
            }

            // Status and state-based colors
            Color voltageColor, currentColor, powerColor, socColor;
            
            if (r.State == BatteryState.Charging)
            { 
                lblStatus.Text = "CHRG"; lblStatus.ForeColor = GreenCol;
                voltageColor = chartVI.Y1ChargeColor;    // Blue
                currentColor = chartVI.Y2ChargeColor;    // Light Green
                powerColor = chartPS.Y1ChargeColor;      // Cyan
                socColor = chartPS.Y2ChargeColor;        // Forest Green
            }
            else if (r.State == BatteryState.Discharging)
            { 
                lblStatus.Text = "DCHRG"; lblStatus.ForeColor = RedCol;
                voltageColor = chartVI.Y1DischargeColor;  // Magenta
                currentColor = chartVI.Y2DischargeColor;  // Bright Red
                powerColor = chartPS.Y1DischargeColor;    // Amber
                socColor = chartPS.Y2DischargeColor;      // Rust
            }
            else
            { 
                lblStatus.Text = "IDLE"; lblStatus.ForeColor = GrayCol;
                voltageColor = chartVI.Y1IdleColor;
                currentColor = chartVI.Y2IdleColor;
                powerColor = chartPS.Y1IdleColor;
                socColor = chartPS.Y2IdleColor;
            }

            lblVoltage.Text = "V=" + (r.Voltage_V > 0 ? r.Voltage_V.ToString("F2") : "--") + " V";
            lblVoltage.ForeColor = voltageColor;
            
            // Display current as negative during discharge
            double displayCurrent = r.Current_mA / 1000.0;
            if (r.State == BatteryState.Discharging) displayCurrent = -displayCurrent;
            lblCurrent.Text = "i=" + (r.Current_mA > 0 ? displayCurrent.ToString("F2") : "--") + " A";
            lblCurrent.ForeColor = currentColor;
            
            // R = V / I (with I in Amps)
            double current_A = r.Current_mA / 1000.0;
            if (current_A > 0.01)
            {
                double resistance = r.Voltage_V / current_A;
                lblResistance.Text = "R=" + resistance.ToString("F1") + " Ω";
                lblResistance.ForeColor = Color.FromArgb(0x8A, 0x2B, 0xE2);  // Purple
            }
            else
            {
                lblResistance.Text = "R=-- Ω";
                lblResistance.ForeColor = TextCol;
            }
            
            // Display power as negative during discharge
            double displayPower = r.Power_W;
            if (r.State == BatteryState.Discharging) displayPower = -displayPower;
            lblPower.Text = "P=" + (Math.Abs(displayPower) < 10.0 ? displayPower.ToString("F2") : displayPower.ToString("F1")) + " W";
            lblPower.ForeColor = powerColor;
            
            lblSOC.Text = "SOC=" + r.SOC_Percent.ToString("F1") + " %";
            lblSOC.ForeColor = socColor;
            
            // BMS temperature removed from header in v18.1 (never available on Yoga)
            // Temperature is displayed via TC66 only when TC66 is connected
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (sampleTimer != null) { sampleTimer.Dispose(); sampleTimer = null; }
            if (tc66Timer != null)   { tc66Timer.Dispose();   tc66Timer = null; }
            if (clockTimer != null)  { clockTimer.Stop(); clockTimer.Dispose(); clockTimer = null; }
            if (csvTimer != null)    { csvTimer.Stop();   csvTimer.Dispose();   csvTimer = null; }
            if (tc66Reader != null)  { tc66Reader.Dispose(); tc66Reader = null; }
            if (_controlFontRef != null) { _controlFontRef.Dispose(); _controlFontRef = null; }
            DisableStayAwake();
            base.OnFormClosing(e);
        }
    }

    // ========================================================================
    // Entry Point
    // ========================================================================
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
