using DevExpress.XtraEditors;
using Nexus_Launcher.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls.Profile
{
    /// <summary>
    /// The full My Rig tab: live CPU, memory and network with a minute
    /// of history each, the machine's spec sheet, storage and memory
    /// modules.
    ///
    /// Live readings only tick while the tab is actually on screen.
    /// </summary>
    internal class RigPanel : VerticalStack
    {
        private const string Icon = "svgimages/icon%20builder/";

        private readonly Timer timer = new Timer();
        private readonly NetworkMonitor network = new NetworkMonitor();

        private readonly RadialGauge cpuGauge;
        private readonly Sparkline cpuHistory;
        private readonly RadialGauge ramGauge;
        private readonly Sparkline ramHistory;

        private readonly LabelControl downLabel;
        private readonly LabelControl upLabel;
        private readonly LabelControl adapterLabel;
        private readonly Sparkline netHistory;

        private readonly CardPanel specsCard;
        private readonly CardGrid driveGrid;
        private readonly CardGrid ramGrid;

        private readonly List<DriveRowControl> drives =
            new List<DriveRowControl>();

        private bool hardwareLoaded;

        public RigPanel()
        {
            CardGrid liveRow =
                new CardGrid();

            cpuGauge = new RadialGauge();
            cpuHistory = CreateHistory(100);

            ramGauge = new RadialGauge();
            ramHistory = CreateHistory(100);

            liveRow.Controls.Add(
                GaugeCard(
                    "Processor",
                    Icon + "electronics_desktopwindows.svg",
                    cpuGauge,
                    cpuHistory,
                    () => ShowDetail(new CpuDetailView())));

            liveRow.Controls.Add(
                GaugeCard(
                    "Memory",
                    Icon + "actions_database.svg",
                    ramGauge,
                    ramHistory,
                    () => ShowDetail(new MemoryDetailView())));

            netHistory = CreateHistory(0);
            netHistory.ShowSecondary = true;

            downLabel = BigLabel();
            upLabel = BigLabel();

            adapterLabel = new LabelControl();
            adapterLabel.AutoSizeMode = LabelAutoSizeMode.None;
            adapterLabel.Appearance.Font = ProfileStyle.Font(8.5F);
            adapterLabel.Appearance.Options.UseFont = true;
            adapterLabel.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            adapterLabel.Appearance.Options.UseTextOptions = true;

            liveRow.Controls.Add(NetworkCard());

            specsCard = new CardPanel();

            CardStack.Fill(specsCard, new List<Control>
            {
                new SectionTitle("System", Icon + "actions_info.svg"),
                CardStack.Message("Reading hardware...")
            });

            driveGrid = new CardGrid();
            ramGrid = new CardGrid();

            Controls.Add(liveRow);
            Controls.Add(specsCard);
            Controls.Add(new SectionTitle("Storage", Icon + "actions_database.svg"));
            Controls.Add(driveGrid);
            Controls.Add(new SectionTitle("Memory modules", Icon + "electronics_keyboard.svg"));
            Controls.Add(ramGrid);

            BuildDrives();

            timer.Interval = 1000;
            timer.Tick += Timer_Tick;
        }

        //--------------------------------------------------------------
        // Building
        //--------------------------------------------------------------

        private static Sparkline CreateHistory(
            double maximum)
        {
            Sparkline line =
                new Sparkline();

            line.Height = 64;
            line.FixedMaximum = maximum;

            return line;
        }

        private static LabelControl BigLabel()
        {
            LabelControl label =
                new LabelControl();

            label.AutoSizeMode = LabelAutoSizeMode.None;
            label.Height = 34;
            label.Appearance.Font = ProfileStyle.Font(15F, FontStyle.Bold);
            label.Appearance.Options.UseFont = true;
            label.Text = "-";

            return label;
        }

        /// <summary>
        /// Title on top, history along the bottom and the gauge filling
        /// what is left. Fill is added first because WinForms lays out
        /// the first added docked control last.
        /// </summary>
        private static CardPanel GaugeCard(
            string title,
            string iconKey,
            RadialGauge gauge,
            Sparkline history,
            Action showDetail)
        {
            CardPanel card =
                new CardPanel();

            card.Size = new Size(300, 262);
            card.Margin = new Padding(0, 0, 12, 12);

            gauge.Dock = DockStyle.Fill;
            gauge.Caption = title.ToUpperInvariant();

            history.Dock = DockStyle.Bottom;

            card.Controls.Add(gauge);
            card.Controls.Add(history);
            card.Controls.Add(TitleBar(title, iconKey, showDetail));

            return card;
        }

        private CardPanel NetworkCard()
        {
            CardPanel card =
                new CardPanel();

            card.Size = new Size(300, 262);
            card.Margin = new Padding(0, 0, 12, 12);

            Panel body =
                new Panel();

            body.Dock = DockStyle.Fill;
            body.BackColor = Color.Transparent;

            LabelControl downCaption = SmallCaption("DOWNLOAD");
            LabelControl upCaption = SmallCaption("UPLOAD");

            downCaption.Location = new Point(0, 6);
            downLabel.Location = new Point(0, 22);
            downLabel.Width = 130;

            upCaption.Location = new Point(140, 6);
            upLabel.Location = new Point(140, 22);
            upLabel.Width = 130;

            adapterLabel.Location = new Point(0, 64);
            adapterLabel.Size = new Size(270, 50);

            body.Controls.Add(downCaption);
            body.Controls.Add(downLabel);
            body.Controls.Add(upCaption);
            body.Controls.Add(upLabel);
            body.Controls.Add(adapterLabel);

            netHistory.Dock = DockStyle.Bottom;

            card.Controls.Add(body);
            card.Controls.Add(netHistory);
            card.Controls.Add(
                TitleBar(
                    "Network",
                    Icon + "business_world.svg",
                    () => ShowDetail(new NetworkDetailView())));

            return card;
        }

        /// <summary>
        /// A card heading with an info button on the right that opens
        /// the detail view for that card.
        /// </summary>
        private static Panel TitleBar(
            string title,
            string iconKey,
            Action showDetail)
        {
            Panel bar =
                new Panel();

            bar.Dock = DockStyle.Top;
            bar.Height = 28;
            bar.BackColor = Color.Transparent;

            SectionTitle label =
                new SectionTitle(title, iconKey);

            label.Dock = DockStyle.Fill;

            SimpleButton info =
                new SimpleButton();

            info.Dock = DockStyle.Right;
            info.Width = 28;
            info.Text = string.Empty;
            info.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Light;
            info.ShowFocusRectangle = DevExpress.Utils.DefaultBoolean.False;
            info.ImageOptions.SvgImage = ProfileStyle.Svg(Icon + "actions_info.svg");
            info.ImageOptions.SvgImageSize = new Size(16, 16);
            info.ImageOptions.Location = ImageLocation.MiddleCenter;
            info.ToolTip = "More " + title.ToLowerInvariant() + " details";
            info.Click += (s, e) => showDetail();

            // Fill first, so it is laid out last and takes what is left.
            bar.Controls.Add(label);
            bar.Controls.Add(info);

            return bar;
        }

        private void ShowDetail(
            LiveDetailView view)
        {
            // Disposing the form disposes the view with it, which
            // releases its performance counters.
            using (HardwareDetailForm form = new HardwareDetailForm(view))
            {
                form.ShowDialog(FindForm());
            }
        }

        private static LabelControl SmallCaption(
            string text)
        {
            MutedLabel label =
                new MutedLabel();

            label.AutoSizeMode = LabelAutoSizeMode.Horizontal;
            label.Text = text;
            label.Appearance.Font = ProfileStyle.Font(7.5F, FontStyle.Bold);
            label.Appearance.Options.UseFont = true;

            return label;
        }

        /// <summary>
        /// Drives come from DriveInfo rather than WMI, so they appear
        /// straight away.
        /// </summary>
        private void BuildDrives()
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                try
                {
                    // Skip empty card readers and optical drives.
                    if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
                        continue;

                    DriveRowControl row =
                        new DriveRowControl(drive);

                    row.Margin = new Padding(0, 0, 0, 8);

                    drives.Add(row);
                    driveGrid.Controls.Add(row);
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                }
            }
        }

        //--------------------------------------------------------------
        // Hardware profile
        //--------------------------------------------------------------

        private async void LoadHardware()
        {
            if (hardwareLoaded)
                return;

            hardwareLoaded = true;

            HardwareProfile profile;

            try
            {
                profile = await HardwareInfoService.GetAsync();
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return;
            }

            if (IsDisposed)
                return;

            BindHardware(profile);
        }

        private void BindHardware(
            HardwareProfile profile)
        {
            SuspendLayout();

            try
            {
                cpuGauge.SubText =
                    profile.CpuCores > 0
                        ? profile.CpuCores + " cores / " + profile.CpuThreads + " threads"
                        : string.Empty;

                List<Control> rows = new List<Control>
                {
                    new SectionTitle("System", Icon + "actions_info.svg"),
                    new SpecRow("Computer", profile.ComputerName),
                    new SpecRow("Windows", profile.OperatingSystem),
                    new SpecRow("Motherboard", profile.Motherboard),
                    new SpecRow("Processor", profile.CpuName),
                    new SpecRow(
                        "Cores / threads",
                        profile.CpuCores + " / " + profile.CpuThreads +
                        (profile.CpuMaxClockMHz > 0
                            ? "  @ " + (profile.CpuMaxClockMHz / 1000d).ToString("0.00") + " GHz"
                            : string.Empty)),
                    new SpecRow("Memory", ProfileStyle.FormatBytes(profile.TotalRamBytes))
                };

                int index = 1;

                foreach (GpuInfo gpu in profile.Gpus)
                {
                    string label =
                        profile.Gpus.Count > 1
                            ? "Graphics " + index++
                            : "Graphics";

                    rows.Add(new SpecRow(
                        label,
                        gpu.Name +
                        (gpu.VramBytes > 0
                            ? "  (" + ProfileStyle.FormatBytes(gpu.VramBytes) + ")"
                            : string.Empty)));

                    if (!string.IsNullOrWhiteSpace(gpu.Resolution))
                        rows.Add(new SpecRow("  Resolution", gpu.Resolution));

                    if (!string.IsNullOrWhiteSpace(gpu.DriverVersion))
                        rows.Add(new SpecRow("  Driver", gpu.DriverVersion));
                }

                CardStack.Fill(specsCard, rows);

                ramGrid.Controls.Clear();

                int slot = 1;

                foreach (Dictionary<string, string> stick in profile.RamSticks)
                {
                    ramRowControl row =
                        new ramRowControl(stick, slot++);

                    row.Margin = new Padding(0, 0, 10, 10);

                    ramGrid.Controls.Add(row);
                }
            }
            finally
            {
                ResumeLayout(true);
            }

            ProfileTheme.Apply(this);
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

        /// <summary>
        /// Visible is false whenever any parent is hidden, so this also
        /// stops the timer when another tab is selected.
        /// </summary>
        private void UpdateTimer()
        {
            bool showing =
                Visible && IsHandleCreated && !IsDisposed;

            if (showing)
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

        private void Timer_Tick(
            object sender,
            EventArgs e)
        {
            Sample();
        }

        private void Sample()
        {
            try
            {
                SystemStatsService.Snapshot stats =
                    SystemStatsService.Read();

                cpuGauge.Value = stats.CpuPercent / 100d;
                cpuGauge.ValueText = stats.CpuPercent.ToString("F0") + "%";
                cpuHistory.Add(stats.CpuPercent);

                ramGauge.Value = stats.RamPercent / 100d;
                ramGauge.ValueText = stats.RamPercent.ToString("F0") + "%";
                ramGauge.SubText =
                    stats.RamUsedGb.ToString("F1") + " / " +
                    stats.RamTotalGb.ToString("F1") + " GB";
                ramHistory.Add(stats.RamPercent);

                NetworkMonitor.Sample net =
                    network.Read();

                downLabel.Text = NetworkMonitor.FormatRate(net.DownloadBytesPerSecond);
                upLabel.Text = NetworkMonitor.FormatRate(net.UploadBytesPerSecond);

                adapterLabel.Text =
                    net.IsConnected
                        ? net.AdapterName + Environment.NewLine +
                            (net.IpAddress ?? "No IPv4 address") + "   " +
                            NetworkMonitor.FormatLinkSpeed(net.LinkSpeed)
                        : "Not connected";

                netHistory.Add(
                    net.DownloadBytesPerSecond,
                    net.UploadBytesPerSecond);

                foreach (DriveRowControl drive in drives)
                {
                    drive.UpdateDiskSpeeds();
                }
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
