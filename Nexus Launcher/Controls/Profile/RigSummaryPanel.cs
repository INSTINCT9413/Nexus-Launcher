using DevExpress.XtraEditors;
using Nexus_Launcher.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls.Profile
{
    /// <summary>
    /// A compact live view of the machine for the account tab, with a
    /// link through to the full My Rig tab.
    /// </summary>
    internal class RigSummaryPanel : Panel, IProfileThemed
    {
        private readonly Timer timer = new Timer();
        private readonly NetworkMonitor network = new NetworkMonitor();

        private readonly SpecRow cpuRow;
        private readonly MeterRow cpuLoad;
        private readonly MeterRow memoryLoad;
        private readonly SpecRow gpuRow;
        private readonly SpecRow networkRow;
        private readonly HyperlinkLabelControl openLink;

        private bool hardwareLoaded;

        /// <summary>
        /// Raised when the user asks to see the full rig details.
        /// </summary>
        public event EventHandler OpenRigRequested;

        public RigSummaryPanel()
        {
            BackColor = Color.Transparent;
            Padding = new Padding(14, 10, 14, 10);

            cpuRow = new SpecRow("Processor", "Reading...");
            cpuLoad = new MeterRow("CPU load");
            memoryLoad = new MeterRow("Memory");
            gpuRow = new SpecRow("Graphics", "Reading...");
            networkRow = new SpecRow("Network", "-");

            openLink = new HyperlinkLabelControl();
            openLink.Text = "Open My Rig  >";
            openLink.Height = 28;
            openLink.AutoSizeMode = LabelAutoSizeMode.None;
            openLink.Appearance.Font = ProfileStyle.Font(9F, FontStyle.Bold);
            openLink.Appearance.Options.UseFont = true;
            openLink.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            openLink.Appearance.Options.UseTextOptions = true;
            openLink.HyperlinkClick += (s, e) => OpenRigRequested?.Invoke(this, EventArgs.Empty);

            List<Control> rows = new List<Control>
            {
                cpuRow,
                cpuLoad,
                memoryLoad,
                gpuRow,
                networkRow,
                openLink
            };

            // Docked top in display order; WinForms docks the last
            // added control first, hence back to front.
            for (int i = rows.Count - 1; i >= 0; i--)
            {
                rows[i].Dock = DockStyle.Top;
                Controls.Add(rows[i]);
            }

            timer.Interval = 2000;
            timer.Tick += (s, e) => Sample();
        }

        public void ApplyTheme()
        {
            Invalidate(true);
        }

        //--------------------------------------------------------------
        // Hardware
        //--------------------------------------------------------------

        private async void LoadHardware()
        {
            if (hardwareLoaded)
                return;

            hardwareLoaded = true;

            try
            {
                HardwareProfile profile =
                    await HardwareInfoService.GetAsync();

                if (IsDisposed)
                    return;

                cpuRow.SetValue(profile.CpuName);

                GpuInfo gpu =
                    profile.Gpus
                        .OrderByDescending(x => x.VramBytes)
                        .FirstOrDefault();

                gpuRow.SetValue(
                    gpu == null
                        ? "Unknown"
                        : gpu.Name +
                            (gpu.VramBytes > 0
                                ? "  (" + ProfileStyle.FormatBytes(gpu.VramBytes) + ")"
                                : string.Empty));
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        //--------------------------------------------------------------
        // Live readings
        //--------------------------------------------------------------

        protected override void OnVisibleChanged(
            EventArgs e)
        {
            base.OnVisibleChanged(e);

            UpdateTimer();
        }

        protected override void OnHandleCreated(
            EventArgs e)
        {
            base.OnHandleCreated(e);

            UpdateTimer();
        }

        private void UpdateTimer()
        {
            if (Visible && IsHandleCreated && !IsDisposed)
            {
                LoadHardware();

                if (!timer.Enabled)
                {
                    Sample();
                    timer.Start();
                }
            }
            else
            {
                timer.Stop();
            }
        }

        private void Sample()
        {
            try
            {
                SystemStatsService.Snapshot stats =
                    SystemStatsService.Read();

                cpuLoad.SetValue(
                    stats.CpuPercent / 100d,
                    stats.CpuPercent.ToString("F0") + "%");

                memoryLoad.SetValue(
                    stats.RamPercent / 100d,
                    stats.RamUsedGb.ToString("F1") + " / " +
                    stats.RamTotalGb.ToString("F1") + " GB");

                NetworkMonitor.Sample net =
                    network.Read();

                networkRow.SetValue(
                    net.IsConnected
                        ? "Down " + NetworkMonitor.FormatRate(net.DownloadBytesPerSecond) +
                            "   Up " + NetworkMonitor.FormatRate(net.UploadBytesPerSecond)
                        : "Not connected");
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                timer.Stop();
                timer.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
