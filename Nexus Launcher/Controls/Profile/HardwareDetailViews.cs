using DevExpress.XtraEditors;
using Nexus_Launcher.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls.Profile
{
    /// <summary>
    /// A detail page opened from one of the My Rig info buttons: fixed
    /// facts bound once, live readings refreshed every second.
    /// </summary>
    internal abstract class LiveDetailView : VerticalStack
    {
        protected const string Icon = "svgimages/icon%20builder/";

        public abstract string Title { get; }

        /// <summary>
        /// Builds the parts that do not change. Called once, after the
        /// hardware profile has loaded.
        /// </summary>
        public abstract void Bind(HardwareProfile profile);

        /// <summary>
        /// Updates the live readings. Called once a second.
        /// </summary>
        public abstract void Tick();

        protected static CardPanel Card(
            IList<Control> rows)
        {
            CardPanel card =
                new CardPanel();

            CardStack.Fill(card, rows);

            return card;
        }

        protected static string Or(
            string value,
            string fallback = "Unknown")
        {
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : value;
        }

        protected static string FormatUptime()
        {
            TimeSpan uptime =
                TimeSpan.FromMilliseconds(
                    Environment.TickCount & int.MaxValue);

            try
            {
                using (PerformanceCounter counter =
                    new PerformanceCounter("System", "System Up Time"))
                {
                    counter.NextValue();
                    uptime = TimeSpan.FromSeconds(counter.NextValue());
                }
            }
            catch
            {
                // TickCount wraps after about 25 days, so the counter is
                // preferred, but it is good enough as a fallback.
            }

            return (int)uptime.TotalDays + "d " +
                uptime.Hours + "h " +
                uptime.Minutes + "m";
        }
    }

    /// <summary>
    /// One row in a top processes list.
    /// </summary>
    internal class ProcessUsage
    {
        public string Name { get; set; }

        public double Value { get; set; }

        public int Instances { get; set; }
    }

    /// <summary>
    /// Works out which programs are using the most CPU or memory.
    ///
    /// Processes are grouped by name, so Chrome's thirty processes count
    /// as one Chrome rather than filling the whole list. Protected system
    /// processes refuse access and are skipped.
    /// </summary>
    internal class ProcessSampler
    {
        private readonly Dictionary<int, TimeSpan> lastCpu =
            new Dictionary<int, TimeSpan>();

        private readonly Stopwatch clock =
            Stopwatch.StartNew();

        private double lastSeconds;

        /// <summary>
        /// CPU share since the last call, as a percentage of the whole
        /// machine. The first call only records a baseline.
        /// </summary>
        public List<ProcessUsage> TopByCpu(
            int logicalProcessors,
            int count)
        {
            double now =
                clock.Elapsed.TotalSeconds;

            double elapsed =
                now - lastSeconds;

            lastSeconds = now;

            Dictionary<string, ProcessUsage> byName =
                new Dictionary<string, ProcessUsage>(
                    StringComparer.OrdinalIgnoreCase);

            HashSet<int> seen =
                new HashSet<int>();

            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    // Idle is the CPU doing nothing, not a program.
                    if (process.Id == 0)
                        continue;

                    TimeSpan cpu =
                        process.TotalProcessorTime;

                    seen.Add(process.Id);

                    TimeSpan previous;

                    if (lastCpu.TryGetValue(process.Id, out previous) &&
                        elapsed > 0)
                    {
                        double share =
                            (cpu - previous).TotalSeconds /
                            (elapsed * Math.Max(1, logicalProcessors)) * 100;

                        Add(byName, process.ProcessName, Math.Max(0, share));
                    }

                    lastCpu[process.Id] = cpu;
                }
                catch
                {
                    // Access denied, or the process exited mid read.
                }
                finally
                {
                    process.Dispose();
                }
            }

            // Forget processes that have exited so the table cannot grow
            // without limit, and a reused id does not inherit old time.
            foreach (int gone in lastCpu.Keys.Where(x => !seen.Contains(x)).ToList())
            {
                lastCpu.Remove(gone);
            }

            return Top(byName, count);
        }

        /// <summary>
        /// Working set per program, in bytes.
        /// </summary>
        public List<ProcessUsage> TopByMemory(
            int count)
        {
            Dictionary<string, ProcessUsage> byName =
                new Dictionary<string, ProcessUsage>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    if (process.Id == 0)
                        continue;

                    Add(byName, process.ProcessName, process.WorkingSet64);
                }
                catch
                {
                }
                finally
                {
                    process.Dispose();
                }
            }

            return Top(byName, count);
        }

        private static void Add(
            Dictionary<string, ProcessUsage> byName,
            string name,
            double value)
        {
            ProcessUsage usage;

            if (!byName.TryGetValue(name, out usage))
            {
                usage = new ProcessUsage { Name = name };
                byName[name] = usage;
            }

            usage.Value += value;
            usage.Instances++;
        }

        private static List<ProcessUsage> Top(
            Dictionary<string, ProcessUsage> byName,
            int count)
        {
            return byName.Values
                .OrderByDescending(x => x.Value)
                .Take(count)
                .ToList();
        }

        public static int CountProcesses(
            out int threads)
        {
            threads = 0;

            Process[] all =
                Process.GetProcesses();

            foreach (Process process in all)
            {
                try
                {
                    threads += process.Threads.Count;
                }
                catch
                {
                }
                finally
                {
                    process.Dispose();
                }
            }

            return all.Length;
        }
    }

    //------------------------------------------------------------------
    // Processor
    //------------------------------------------------------------------

    internal class CpuDetailView : LiveDetailView
    {
        private const int TopCount = 6;

        private readonly ProcessSampler sampler = new ProcessSampler();
        private readonly List<PerformanceCounter> coreCounters = new List<PerformanceCounter>();
        private readonly List<MeterRow> coreRows = new List<MeterRow>();
        private readonly List<MeterRow> topRows = new List<MeterRow>();

        private PerformanceCounter clockCounter;
        private int baseClockMHz;
        private int threads;
        private int ticks;

        private SpecRow loadRow;
        private SpecRow clockRow;
        private SpecRow processRow;
        private SpecRow uptimeRow;

        public override string Title
        {
            get
            {
                return "Processor details";
            }
        }

        public override void Bind(
            HardwareProfile profile)
        {
            baseClockMHz = profile.CpuMaxClockMHz;
            threads = Math.Max(1, profile.CpuThreads);

            Controls.Add(Card(new List<Control>
            {
                new SectionTitle("Specification", Icon + "actions_info.svg"),
                new SpecRow("Name", Or(profile.CpuName)),
                new SpecRow("Manufacturer", Or(profile.CpuManufacturer)),
                new SpecRow("Socket", Or(profile.CpuSocket)),
                new SpecRow("Architecture", Or(profile.CpuArchitecture)),
                new SpecRow("Cores / threads", profile.CpuCores + " / " + profile.CpuThreads),
                new SpecRow("Base clock", baseClockMHz > 0 ? (baseClockMHz / 1000d).ToString("0.00") + " GHz" : "Unknown"),
                new SpecRow("L2 cache", profile.CpuL2CacheKB > 0 ? ProfileStyle.FormatBytes(profile.CpuL2CacheKB * 1024L) : "Unknown"),
                new SpecRow("L3 cache", profile.CpuL3CacheKB > 0 ? ProfileStyle.FormatBytes(profile.CpuL3CacheKB * 1024L) : "Unknown"),
                new SpecRow(
                    "Virtualization",
                    profile.CpuVirtualization.HasValue
                        ? (profile.CpuVirtualization.Value ? "Enabled" : "Disabled")
                        : "Unknown")
            }));

            loadRow = new SpecRow("Load", "-");
            clockRow = new SpecRow("Current clock", "-");
            processRow = new SpecRow("Processes / threads", "-");
            uptimeRow = new SpecRow("Up time", FormatUptime());

            Controls.Add(Card(new List<Control>
            {
                new SectionTitle("Right now", Icon + "actions_clock.svg"),
                loadRow,
                clockRow,
                processRow,
                uptimeRow
            }));

            List<Control> cores = new List<Control>
            {
                new SectionTitle("Load per thread", Icon + "business_barchart.svg")
            };

            for (int i = 0; i < threads; i++)
            {
                MeterRow row =
                    new MeterRow("Thread " + i);

                row.Height = 30;

                coreRows.Add(row);
                cores.Add(row);

                try
                {
                    coreCounters.Add(
                        new PerformanceCounter(
                            "Processor",
                            "% Processor Time",
                            i.ToString(CultureInfo.InvariantCulture)));
                }
                catch
                {
                    coreCounters.Add(null);
                }
            }

            Controls.Add(Card(cores));

            List<Control> top = new List<Control>
            {
                new SectionTitle("Busiest programs", Icon + "actions_rating.svg")
            };

            for (int i = 0; i < TopCount; i++)
            {
                MeterRow row = new MeterRow("-");
                topRows.Add(row);
                top.Add(row);
            }

            Controls.Add(Card(top));

            try
            {
                // Effective speed as a percentage of base; above 100
                // means boosting.
                clockCounter =
                    new PerformanceCounter(
                        "Processor Information",
                        "% Processor Performance",
                        "_Total");
            }
            catch
            {
                clockCounter = null;
            }

            ProfileTheme.Apply(this);
        }

        public override void Tick()
        {
            ticks++;

            double total = 0;
            int counted = 0;

            for (int i = 0; i < coreRows.Count; i++)
            {
                PerformanceCounter counter = coreCounters[i];

                if (counter == null)
                {
                    coreRows[i].SetValue(0, "n/a");
                    continue;
                }

                try
                {
                    float value = counter.NextValue();

                    coreRows[i].SetValue(value / 100d, value.ToString("F0") + "%");

                    total += value;
                    counted++;
                }
                catch
                {
                    coreRows[i].SetValue(0, "n/a");
                }
            }

            if (counted > 0)
                loadRow.SetValue((total / counted).ToString("F0") + "%");

            if (clockCounter != null && baseClockMHz > 0)
            {
                try
                {
                    double mhz =
                        baseClockMHz * clockCounter.NextValue() / 100d;

                    clockRow.SetValue((mhz / 1000d).ToString("0.00") + " GHz");
                }
                catch
                {
                    clockRow.SetValue("Unavailable");
                }
            }
            else
            {
                clockRow.SetValue("Unavailable");
            }

            // Walking every process is the expensive part, so it only
            // runs every other second.
            if (ticks % 2 == 1)
            {
                int threadCount;
                int processes = ProcessSampler.CountProcesses(out threadCount);

                processRow.SetValue(
                    processes.ToString("N0", CultureInfo.CurrentCulture) + " / " +
                    threadCount.ToString("N0", CultureInfo.CurrentCulture));

                List<ProcessUsage> busiest =
                    sampler.TopByCpu(threads, TopCount);

                for (int i = 0; i < topRows.Count; i++)
                {
                    if (i < busiest.Count)
                    {
                        ProcessUsage usage = busiest[i];

                        topRows[i].Caption =
                            usage.Name +
                            (usage.Instances > 1 ? "  (" + usage.Instances + ")" : string.Empty);

                        topRows[i].SetValue(usage.Value / 100d, usage.Value.ToString("F1") + "%");
                    }
                    else
                    {
                        topRows[i].Caption = "-";
                        topRows[i].SetValue(0, string.Empty);
                    }
                }

                uptimeRow.SetValue(FormatUptime());
            }
        }

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                foreach (PerformanceCounter counter in coreCounters)
                {
                    if (counter != null)
                        counter.Dispose();
                }

                if (clockCounter != null)
                    clockCounter.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    //------------------------------------------------------------------
    // Memory
    //------------------------------------------------------------------

    internal class MemoryDetailView : LiveDetailView
    {
        private const int TopCount = 8;

        private readonly ProcessSampler sampler = new ProcessSampler();
        private readonly List<MeterRow> topRows = new List<MeterRow>();

        private MeterRow inUseRow;
        private MeterRow committedRow;
        private SpecRow availableRow;
        private int ticks;

        public override string Title
        {
            get
            {
                return "Memory details";
            }
        }

        public override void Bind(
            HardwareProfile profile)
        {
            int sticks =
                profile.RamSticks.Count;

            Controls.Add(Card(new List<Control>
            {
                new SectionTitle("Specification", Icon + "actions_info.svg"),
                new SpecRow("Installed", ProfileStyle.FormatBytes(profile.TotalRamBytes)),
                new SpecRow("Type", Or(profile.MemoryType)),
                new SpecRow("Speed", profile.MemorySpeedMHz > 0 ? profile.MemorySpeedMHz + " MT/s" : "Unknown"),
                new SpecRow("Form factor", Or(profile.MemoryFormFactor)),
                new SpecRow(
                    "Slots used",
                    profile.MemorySlotsTotal > 0
                        ? sticks + " of " + profile.MemorySlotsTotal
                        : sticks.ToString(CultureInfo.CurrentCulture)),
                new SpecRow(
                    "Maximum supported",
                    profile.MemoryMaxCapacityBytes > 0
                        ? ProfileStyle.FormatBytes(profile.MemoryMaxCapacityBytes)
                        : "Unknown")
            }));

            inUseRow = new MeterRow("In use");
            committedRow = new MeterRow("Committed");
            availableRow = new SpecRow("Available", "-");

            Controls.Add(Card(new List<Control>
            {
                new SectionTitle("Right now", Icon + "actions_clock.svg"),
                inUseRow,
                committedRow,
                availableRow
            }));

            List<Control> top = new List<Control>
            {
                new SectionTitle("Biggest programs", Icon + "actions_rating.svg")
            };

            for (int i = 0; i < TopCount; i++)
            {
                MeterRow row = new MeterRow("-");
                topRows.Add(row);
                top.Add(row);
            }

            Controls.Add(Card(top));

            Controls.Add(new SectionTitle("Modules", Icon + "electronics_keyboard.svg"));

            CardGrid modules = new CardGrid();

            int slot = 1;

            foreach (Dictionary<string, string> stick in profile.RamSticks)
            {
                ramRowControl row = new ramRowControl(stick, slot++);
                row.Margin = new Padding(0, 0, 10, 10);
                modules.Controls.Add(row);
            }

            Controls.Add(modules);

            ProfileTheme.Apply(this);
        }

        public override void Tick()
        {
            ticks++;

            SystemStatsService.MemoryDetail memory =
                SystemStatsService.ReadMemoryDetail();

            if (memory.TotalBytes > 0)
            {
                long used =
                    memory.TotalBytes - memory.AvailableBytes;

                inUseRow.SetValue(
                    (double)used / memory.TotalBytes,
                    ProfileStyle.FormatBytes(used) + " / " +
                    ProfileStyle.FormatBytes(memory.TotalBytes));

                availableRow.SetValue(
                    ProfileStyle.FormatBytes(memory.AvailableBytes));
            }

            if (memory.CommitLimitBytes > 0)
            {
                committedRow.SetValue(
                    (double)memory.CommittedBytes / memory.CommitLimitBytes,
                    ProfileStyle.FormatBytes(memory.CommittedBytes) + " / " +
                    ProfileStyle.FormatBytes(memory.CommitLimitBytes));
            }

            if (ticks % 2 != 1)
                return;

            List<ProcessUsage> biggest =
                sampler.TopByMemory(TopCount);

            double largest =
                biggest.Count == 0
                    ? 1
                    : Math.Max(1, biggest[0].Value);

            for (int i = 0; i < topRows.Count; i++)
            {
                if (i < biggest.Count)
                {
                    ProcessUsage usage = biggest[i];

                    topRows[i].Caption =
                        usage.Name +
                        (usage.Instances > 1 ? "  (" + usage.Instances + ")" : string.Empty);

                    // Scaled to the biggest program rather than all of
                    // memory, which would leave most bars nearly empty.
                    topRows[i].SetValue(
                        usage.Value / largest,
                        ProfileStyle.FormatBytes((long)usage.Value));
                }
                else
                {
                    topRows[i].Caption = "-";
                    topRows[i].SetValue(0, string.Empty);
                }
            }
        }
    }

    //------------------------------------------------------------------
    // Network
    //------------------------------------------------------------------

    internal class NetworkDetailView : LiveDetailView
    {
        /// <summary>
        /// The live rows for one adapter, plus the counters its rates
        /// are worked out from.
        /// </summary>
        private class AdapterRows
        {
            public string Id;
            public SpecRow Down;
            public SpecRow Up;
            public SpecRow Totals;
            public long LastReceived;
            public long LastSent;
            public bool Primed;
        }

        private readonly List<AdapterRows> adapters = new List<AdapterRows>();
        private readonly NetworkMonitor monitor = new NetworkMonitor();
        private readonly Stopwatch clock = Stopwatch.StartNew();

        private Sparkline history;
        private SpecRow totalDown;
        private SpecRow totalUp;
        private double lastSeconds;

        public override string Title
        {
            get
            {
                return "Network details";
            }
        }

        public override void Bind(
            HardwareProfile profile)
        {
            history = new Sparkline();
            history.Height = 80;
            history.ShowSecondary = true;

            totalDown = new SpecRow("Download", "-");
            totalUp = new SpecRow("Upload", "-");

            Controls.Add(Card(new List<Control>
            {
                new SectionTitle("All adapters", Icon + "business_world.svg"),
                totalDown,
                totalUp,
                history
            }));

            // Connected adapters first, then the rest.
            IEnumerable<NetworkInterface> all =
                NetworkInterface.GetAllNetworkInterfaces()
                    .Where(x =>
                        x.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        x.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    .OrderBy(x => x.OperationalStatus == OperationalStatus.Up ? 0 : 1)
                    .ThenBy(x => x.Name);

            foreach (NetworkInterface adapter in all)
            {
                Controls.Add(AdapterCard(adapter));
            }

            ProfileTheme.Apply(this);
        }

        private CardPanel AdapterCard(
            NetworkInterface adapter)
        {
            IPInterfaceProperties ip = null;

            try
            {
                ip = adapter.GetIPProperties();
            }
            catch
            {
            }

            AdapterRows rows =
                new AdapterRows
                {
                    Id = adapter.Id,
                    Down = new SpecRow("Download", "-"),
                    Up = new SpecRow("Upload", "-"),
                    Totals = new SpecRow("Since start", "-")
                };

            adapters.Add(rows);

            bool up =
                adapter.OperationalStatus == OperationalStatus.Up;

            List<Control> list = new List<Control>
            {
                new SectionTitle(adapter.Name, Icon + (up ? "actions_checkcircled.svg" : "actions_removecircled.svg")),
                new SpecRow("Description", Or(adapter.Description)),
                new SpecRow("Type", adapter.NetworkInterfaceType.ToString()),
                new SpecRow("Status", adapter.OperationalStatus.ToString()),
                new SpecRow("Link speed", up ? NetworkMonitor.FormatLinkSpeed(adapter.Speed) : "-"),
                new SpecRow("MAC address", FormatMac(adapter.GetPhysicalAddress()))
            };

            if (ip != null)
            {
                list.Add(new SpecRow("IPv4", Or(Addresses(ip.UnicastAddresses.Select(x => x.Address), AddressFamily.InterNetwork), "None")));
                list.Add(new SpecRow("IPv6", Or(Addresses(ip.UnicastAddresses.Select(x => x.Address), AddressFamily.InterNetworkV6), "None")));
                list.Add(new SpecRow("Gateway", Or(Addresses(ip.GatewayAddresses.Select(x => x.Address), AddressFamily.InterNetwork), "None")));
                list.Add(new SpecRow("DNS", Or(Addresses(ip.DnsAddresses, null), "None")));
            }

            if (up)
            {
                list.Add(rows.Down);
                list.Add(rows.Up);
                list.Add(rows.Totals);
            }

            return Card(list);
        }

        private static string Addresses(
            IEnumerable<System.Net.IPAddress> addresses,
            AddressFamily? family)
        {
            return string.Join(
                ", ",
                addresses
                    .Where(x => x != null &&
                        (family == null || x.AddressFamily == family.Value))
                    .Select(x => x.ToString()));
        }

        private static string FormatMac(
            PhysicalAddress address)
        {
            byte[] bytes =
                address == null
                    ? new byte[0]
                    : address.GetAddressBytes();

            return bytes.Length == 0
                ? "None"
                : string.Join(":", bytes.Select(x => x.ToString("X2")));
        }

        public override void Tick()
        {
            NetworkMonitor.Sample sample =
                monitor.Read();

            totalDown.SetValue(NetworkMonitor.FormatRate(sample.DownloadBytesPerSecond));
            totalUp.SetValue(NetworkMonitor.FormatRate(sample.UploadBytesPerSecond));

            history.Add(sample.DownloadBytesPerSecond, sample.UploadBytesPerSecond);

            double now = clock.Elapsed.TotalSeconds;
            double elapsed = now - lastSeconds;
            lastSeconds = now;

            Dictionary<string, NetworkInterface> live;

            try
            {
                live =
                    NetworkInterface.GetAllNetworkInterfaces()
                        .ToDictionary(x => x.Id, x => x);
            }
            catch
            {
                return;
            }

            foreach (AdapterRows rows in adapters)
            {
                NetworkInterface adapter;

                if (!live.TryGetValue(rows.Id, out adapter))
                    continue;

                try
                {
                    IPv4InterfaceStatistics stats =
                        adapter.GetIPv4Statistics();

                    if (rows.Primed && elapsed > 0)
                    {
                        rows.Down.SetValue(NetworkMonitor.FormatRate(
                            Math.Max(0, (stats.BytesReceived - rows.LastReceived) / elapsed)));

                        rows.Up.SetValue(NetworkMonitor.FormatRate(
                            Math.Max(0, (stats.BytesSent - rows.LastSent) / elapsed)));
                    }

                    rows.Totals.SetValue(
                        "Received " + ProfileStyle.FormatBytes(stats.BytesReceived) +
                        "   Sent " + ProfileStyle.FormatBytes(stats.BytesSent));

                    rows.LastReceived = stats.BytesReceived;
                    rows.LastSent = stats.BytesSent;
                    rows.Primed = true;
                }
                catch
                {
                }
            }
        }
    }

    //------------------------------------------------------------------
    // Window
    //------------------------------------------------------------------

    /// <summary>
    /// Hosts a detail view and drives its once a second refresh.
    /// </summary>
    internal class HardwareDetailForm : XtraForm
    {
        private readonly LiveDetailView view;
        private readonly Timer timer = new Timer();

        public HardwareDetailForm(
            LiveDetailView view)
        {
            this.view = view;

            Text = view.Title;
            Size = new Size(680, 760);
            MinimumSize = new Size(520, 400);
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            view.Dock = DockStyle.Fill;
            Controls.Add(view);

            timer.Interval = 1000;
            timer.Tick += (s, e) => view.Tick();
        }

        protected override async void OnLoad(
            EventArgs e)
        {
            base.OnLoad(e);

            try
            {
                HardwareProfile profile =
                    await HardwareInfoService.GetAsync();

                if (IsDisposed)
                    return;

                view.Bind(profile);
                view.Tick();

                timer.Start();
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        protected override void OnFormClosed(
            FormClosedEventArgs e)
        {
            timer.Stop();

            base.OnFormClosed(e);
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
