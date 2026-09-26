using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Nexus_Launcher.Services
{
    /// <summary>
    /// The four steps a reset runs through, one per step indicator on
    /// the Client Reset tab.
    /// </summary>
    public enum ResetStage
    {
        StoppingClient = 0,

        LauncherData = 1,

        SystemTemp = 2,

        WindowsUpdate = 3
    }

    /// <summary>
    /// Everything a client's reset intends to clear. Built by
    /// ClientResetDefinitions and handed to the runner, so the "what"
    /// stays declarative and the "how" lives in one place.
    /// </summary>
    public class ClientResetPlan
    {
        public string ClientName { get; set; }

        public List<string> TempFolders { get; set; }

        public List<string> CacheFolders { get; set; }

        public List<string> LogFolders { get; set; }

        public List<string> CrashFolders { get; set; }

        public List<string> FoldersToClear { get; set; }

        public List<string> RepairTools { get; set; }

        public List<string> ProcessNames { get; set; }

        /// <summary>
        /// The categories the user ticked on the reset screen. A plan
        /// still describes every folder it knows about; these say which
        /// of them this particular run should touch, so the definitions
        /// stay a description of the client rather than of one run.
        /// </summary>
        public bool ClearClientTemp { get; set; }

        public bool ClearClientCaches { get; set; }

        public bool ClearClientLogs { get; set; }

        public bool ClearClientCrashDumps { get; set; }

        public bool ClearClientData { get; set; }

        public bool ClearWindowsTemp { get; set; }

        /// <summary>
        /// The graphics drivers' compiled shader caches. Pure derived
        /// data, and usually the largest thing a reset frees, but the
        /// first run of each game afterwards stutters while they are
        /// rebuilt, so it is worth being its own choice.
        /// </summary>
        public bool ClearShaderCaches { get; set; }

        /// <summary>
        /// Whether the Windows Update and Delivery Optimization caches
        /// are included. Needs administrator rights to do anything.
        /// </summary>
        public bool ClearWindowsUpdateCache { get; set; }

        public bool ClearDns { get; set; }

        public ClientResetPlan()
        {
            ClearClientTemp = true;
            ClearClientCaches = true;
            ClearClientLogs = true;
            ClearClientCrashDumps = true;
            ClearClientData = true;

            ClearWindowsTemp = true;
            ClearShaderCaches = true;
            ClearWindowsUpdateCache = true;

            TempFolders = new List<string>();
            CacheFolders = new List<string>();
            LogFolders = new List<string>();
            CrashFolders = new List<string>();
            FoldersToClear = new List<string>();
            RepairTools = new List<string>();
            ProcessNames = new List<string>();
        }
    }

    /// <summary>
    /// One line of the results log: a whole target, summarised.
    ///
    /// Deliberately one entry per folder rather than per file. Clearing
    /// %TEMP% alone can touch tens of thousands of files, and a list
    /// that long is not something anyone reads.
    /// </summary>
    public class ResetLogEntry
    {
        public string Category { get; set; }

        public string Path { get; set; }

        public int FilesDeleted { get; set; }

        public int FilesFailed { get; set; }

        public long BytesFreed { get; set; }

        /// <summary>
        /// Set when the whole target could not be touched, for example
        /// a Windows Update folder without administrator rights.
        /// </summary>
        public string Note { get; set; }

        public bool Skipped
        {
            get
            {
                return !string.IsNullOrEmpty(Note);
            }
        }
    }

    public class ResetProgress
    {
        public ResetStage Stage { get; set; }

        public string StageName { get; set; }

        /// <summary>
        /// How far through the current stage, 0 to 1.
        /// </summary>
        public double Fraction { get; set; }

        public string Detail { get; set; }
    }

    /// <summary>
    /// What a finished reset did, for the results screen.
    /// </summary>
    public class ResetReport
    {
        public string ClientName { get; set; }

        public long BytesFreed { get; set; }

        public int FilesDeleted { get; set; }

        public int FilesFailed { get; set; }

        public int TargetsCleared { get; set; }

        public bool NeededAdmin { get; set; }

        public TimeSpan Duration { get; set; }

        public List<ResetLogEntry> Log { get; set; }

        public List<string> Errors { get; set; }

        public ResetReport()
        {
            Log = new List<ResetLogEntry>();
            Errors = new List<string>();
        }
    }

    /// <summary>
    /// Runs a client reset in four stages, measuring what it frees.
    ///
    /// Nothing here touches sign-in state. The folders come from
    /// ClientResetDefinitions, which lists caches, logs and crash dumps
    /// only, and never the files a client keeps credentials in, so a
    /// reset leaves the user logged in.
    /// </summary>
    internal static class ClientResetRunner
    {
        /// <summary>
        /// Folders that must never be cleared, whatever a definition
        /// says. A wrong path in a definition would otherwise delete
        /// something irreplaceable, so the runner refuses outright
        /// rather than trusting its input.
        /// </summary>
        private static readonly string[] ForbiddenRoots =
            BuildForbiddenRoots();

        private static string[] BuildForbiddenRoots()
        {
            List<string> roots = new List<string>();

            Action<Environment.SpecialFolder> add = folder =>
            {
                try
                {
                    string path =
                        Environment.GetFolderPath(folder);

                    if (!string.IsNullOrWhiteSpace(path))
                        roots.Add(Normalize(path));
                }
                catch (Exception)
                {
                }
            };

            add(Environment.SpecialFolder.Windows);
            add(Environment.SpecialFolder.System);
            add(Environment.SpecialFolder.ProgramFiles);
            add(Environment.SpecialFolder.ProgramFilesX86);
            add(Environment.SpecialFolder.UserProfile);
            add(Environment.SpecialFolder.MyDocuments);
            add(Environment.SpecialFolder.MyPictures);
            add(Environment.SpecialFolder.MyVideos);
            add(Environment.SpecialFolder.MyMusic);
            add(Environment.SpecialFolder.Desktop);
            add(Environment.SpecialFolder.DesktopDirectory);
            add(Environment.SpecialFolder.ApplicationData);
            add(Environment.SpecialFolder.LocalApplicationData);
            add(Environment.SpecialFolder.CommonApplicationData);

            return roots.ToArray();
        }

        private static string Normalize(
            string path)
        {
            try
            {
                return Path.GetFullPath(path)
                    .TrimEnd(Path.DirectorySeparatorChar)
                    .ToLowerInvariant();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// True when a path is safe to empty: a real folder, not a
        /// drive root, and not one of the places that must survive.
        /// </summary>
        private static bool IsClearable(
            string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // "C:" is not the root of C:, it is the current directory
            // on that drive, so GetFullPath would quietly turn it into
            // whatever folder the process happens to be sitting in. A
            // real target always spells out a path.
            if (path.IndexOf(Path.DirectorySeparatorChar) < 0 &&
                path.IndexOf(Path.AltDirectorySeparatorChar) < 0)
            {
                return false;
            }

            string full = Normalize(path);

            if (string.IsNullOrEmpty(full))
                return false;

            // A drive root such as "c:" has no separator left after
            // trimming, and must never be emptied.
            if (full.Length <= 3)
                return false;

            foreach (string forbidden in ForbiddenRoots)
            {
                if (string.Equals(
                    full,
                    forbidden,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return Directory.Exists(path);
        }

        public static bool IsElevated
        {
            get
            {
                try
                {
                    using (WindowsIdentity identity =
                        WindowsIdentity.GetCurrent())
                    {
                        return new WindowsPrincipal(identity)
                            .IsInRole(WindowsBuiltInRole.Administrator);
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public static string StageName(
            ResetStage stage,
            string clientName)
        {
            switch (stage)
            {
                case ResetStage.StoppingClient:
                    return "Closing " +
                        (string.IsNullOrWhiteSpace(clientName)
                            ? "the client"
                            : clientName);

                case ResetStage.LauncherData:
                    return "Clearing client caches and logs";

                case ResetStage.SystemTemp:
                    return "Clearing system temp and shader caches";

                default:
                    return "Clearing Windows Update cache";
            }
        }

        //--------------------------------------------------------------
        // Running
        //--------------------------------------------------------------

        public static Task<ResetReport> RunAsync(
            ClientResetPlan plan,
            IProgress<ResetProgress> progress,
            CancellationToken token)
        {
            return Task.Run(() => Run(plan, progress, token), token);
        }

        private static ResetReport Run(
            ClientResetPlan plan,
            IProgress<ResetProgress> progress,
            CancellationToken token)
        {
            ResetReport report =
                new ResetReport();

            Stopwatch clock =
                Stopwatch.StartNew();

            if (plan == null)
                return report;

            report.ClientName = plan.ClientName;

            try
            {
                StopClient(plan, report, progress, token);

                ClearLauncherData(plan, report, progress, token);

                if (plan.ClearWindowsTemp || plan.ClearShaderCaches)
                    ClearSystemTemp(plan, report, progress, token);
                else
                    Report(progress, ResetStage.SystemTemp, null, 1, "Skipped");

                if (plan.ClearWindowsUpdateCache)
                    ClearWindowsUpdate(report, progress, token);
                else
                    Report(progress, ResetStage.WindowsUpdate, null, 1, "Skipped");
            }
            catch (OperationCanceledException)
            {
                report.Errors.Add("Reset was cancelled.");
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                report.Errors.Add(ex.Message);
            }

            report.Duration = clock.Elapsed;

            return report;
        }

        private static void Report(
            IProgress<ResetProgress> progress,
            ResetStage stage,
            string clientName,
            double fraction,
            string detail)
        {
            if (progress == null)
                return;

            progress.Report(new ResetProgress
            {
                Stage = stage,
                StageName = StageName(stage, clientName),
                Fraction = fraction < 0 ? 0 : (fraction > 1 ? 1 : fraction),
                Detail = detail
            });
        }

        //--------------------------------------------------------------
        // Stage 1: stop the client
        //--------------------------------------------------------------

        private static void StopClient(
            ClientResetPlan plan,
            ResetReport report,
            IProgress<ResetProgress> progress,
            CancellationToken token)
        {
            List<string> names =
                plan.ProcessNames ?? new List<string>();

            Report(progress, ResetStage.StoppingClient, plan.ClientName, 0, null);

            for (int i = 0; i < names.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                StopProcess(names[i], report);

                Report(
                    progress,
                    ResetStage.StoppingClient,
                    plan.ClientName,
                    (i + 1) / (double)Math.Max(1, names.Count),
                    names[i]);
            }

            // Files stay locked for a moment after a process goes.
            if (names.Count > 0)
                Thread.Sleep(600);

            Report(progress, ResetStage.StoppingClient, plan.ClientName, 1, null);
        }

        private static void StopProcess(
            string processName,
            ResetReport report)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return;

            try
            {
                string name =
                    Path.GetFileNameWithoutExtension(processName);

                foreach (Process process in
                    Process.GetProcessesByName(name))
                {
                    try
                    {
                        if (process.HasExited)
                            continue;

                        process.CloseMainWindow();

                        if (!process.WaitForExit(3000))
                        {
                            process.Kill();

                            process.WaitForExit(3000);
                        }
                    }
                    catch (Exception ex)
                    {
                        report.Errors.Add(
                            "Could not close " + processName +
                            ": " + ex.Message);
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                report.Errors.Add(
                    "Could not inspect " + processName +
                    ": " + ex.Message);
            }
        }

        //--------------------------------------------------------------
        // Stage 2: the client's own caches, logs and crash dumps
        //--------------------------------------------------------------

        private static readonly string[] LogPatterns =
            { "*.log", "*.txt", "*.dmp", "*.mdmp", "*.old", "*.bak" };

        private static readonly string[] CrashPatterns =
            { "*.dmp", "*.mdmp", "*.hdmp", "*.wer" };

        private static void ClearLauncherData(
            ClientResetPlan plan,
            ResetReport report,
            IProgress<ResetProgress> progress,
            CancellationToken token)
        {
            List<Tuple<string, string, string[]>> targets =
                new List<Tuple<string, string, string[]>>();

            if (plan.ClearClientTemp)
            {
                foreach (string p in plan.TempFolders)
                    targets.Add(Target("Client temp", p, null));
            }

            if (plan.ClearClientCaches)
            {
                foreach (string p in plan.CacheFolders)
                    targets.Add(Target("Client cache", p, null));
            }

            if (plan.ClearClientData)
            {
                foreach (string p in plan.FoldersToClear)
                    targets.Add(Target("Client data", p, null));
            }

            if (plan.ClearClientLogs)
            {
                foreach (string p in plan.LogFolders)
                    targets.Add(Target("Client logs", p, LogPatterns));
            }

            if (plan.ClearClientCrashDumps)
            {
                foreach (string p in plan.CrashFolders)
                    targets.Add(Target("Crash dumps", p, CrashPatterns));
            }

            RunTargets(
                targets,
                report,
                progress,
                ResetStage.LauncherData,
                plan.ClientName,
                token);
        }

        private static Tuple<string, string, string[]> Target(
            string category,
            string path,
            string[] patterns)
        {
            return Tuple.Create(category, path, patterns);
        }

        //--------------------------------------------------------------
        // Stage 3: system temp, error reports and shader caches
        //--------------------------------------------------------------

        private static void ClearSystemTemp(
            ClientResetPlan plan,
            ResetReport report,
            IProgress<ResetProgress> progress,
            CancellationToken token)
        {
            string local =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            string windows =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.Windows);

            string programData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData);

            List<Tuple<string, string, string[]>> targets =
                new List<Tuple<string, string, string[]>>();

            if (plan.ClearWindowsTemp)
            {
                targets.Add(Target("System temp", Path.GetTempPath(), null));

                targets.Add(Target(
                    "System temp",
                    Path.Combine(windows, "Temp"),
                    null));

                // Windows error reporting queues. Rebuilt on demand.
                foreach (string root in new[] { local, programData })
                {
                    targets.Add(Target(
                        "Error reports",
                        Path.Combine(root, @"Microsoft\Windows\WER\ReportQueue"),
                        null));

                    targets.Add(Target(
                        "Error reports",
                        Path.Combine(root, @"Microsoft\Windows\WER\ReportArchive"),
                        null));
                }

                targets.Add(Target(
                    "Crash dumps",
                    Path.Combine(local, "CrashDumps"),
                    CrashPatterns));
            }

            // Shader caches. These are pure derived data: the driver
            // rebuilds them, at the cost of some stutter the first time
            // a game runs afterwards. They are also usually the largest
            // thing a reset frees.
            if (plan.ClearShaderCaches)
            {
                foreach (string relative in new[]
                {
                    @"NVIDIA\DXCache",
                    @"NVIDIA\GLCache",
                    @"NVIDIA Corporation\NV_Cache",
                    @"D3DSCache",
                    @"AMD\DxCache",
                    @"AMD\GLCache",
                    @"Intel\ShaderCache"
                })
                {
                    targets.Add(Target(
                        "Shader cache",
                        Path.Combine(local, relative),
                        null));
                }
            }

            RunTargets(
                targets,
                report,
                progress,
                ResetStage.SystemTemp,
                null,
                token);
        }

        //--------------------------------------------------------------
        // Stage 4: Windows Update and Delivery Optimization
        //--------------------------------------------------------------

        private static void ClearWindowsUpdate(
            ResetReport report,
            IProgress<ResetProgress> progress,
            CancellationToken token)
        {
            string windows =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.Windows);

            List<Tuple<string, string, string[]>> targets =
                new List<Tuple<string, string, string[]>>();

            targets.Add(Target(
                "Windows Update",
                Path.Combine(windows, @"SoftwareDistribution\Download"),
                null));

            targets.Add(Target(
                "Delivery Optimization",
                Path.Combine(
                    windows,
                    @"ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\Cache"),
                null));

            if (!IsElevated)
            {
                // Without administrator rights these folders are not
                // writable, and trying would produce a wall of access
                // errors. Saying so once is more use than failing
                // hundreds of times.
                report.NeededAdmin = true;

                foreach (Tuple<string, string, string[]> target in targets)
                {
                    report.Log.Add(new ResetLogEntry
                    {
                        Category = target.Item1,
                        Path = target.Item2,
                        Note = "Skipped: needs administrator rights"
                    });
                }

                Report(progress, ResetStage.WindowsUpdate, null, 1,
                    "Skipped, needs administrator");

                return;
            }

            bool stopped =
                SetService("wuauserv", false);

            try
            {
                RunTargets(
                    targets,
                    report,
                    progress,
                    ResetStage.WindowsUpdate,
                    null,
                    token);
            }
            finally
            {
                // Always put the service back, even if clearing threw.
                if (stopped)
                    SetService("wuauserv", true);
            }
        }

        /// <summary>
        /// Starts or stops a Windows service with net.exe.
        ///
        /// Done this way rather than with ServiceController so the
        /// project does not need a System.ServiceProcess reference for
        /// one call.
        /// </summary>
        private static bool SetService(
            string name,
            bool start)
        {
            try
            {
                ProcessStartInfo info =
                    new ProcessStartInfo("net",
                        (start ? "start " : "stop ") + name);

                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;

                using (Process process = Process.Start(info))
                {
                    if (process == null)
                        return false;

                    process.WaitForExit(20000);

                    return process.HasExited && process.ExitCode == 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        //--------------------------------------------------------------
        // Clearing
        //--------------------------------------------------------------

        private static void RunTargets(
            List<Tuple<string, string, string[]>> targets,
            ResetReport report,
            IProgress<ResetProgress> progress,
            ResetStage stage,
            string clientName,
            CancellationToken token)
        {
            Report(progress, stage, clientName, 0, null);

            for (int i = 0; i < targets.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                Tuple<string, string, string[]> target = targets[i];

                Report(
                    progress,
                    stage,
                    clientName,
                    i / (double)Math.Max(1, targets.Count),
                    Shorten(target.Item2));

                ResetLogEntry entry =
                    Clear(target.Item1, target.Item2, target.Item3, token);

                if (entry == null)
                    continue;

                report.Log.Add(entry);

                report.BytesFreed += entry.BytesFreed;
                report.FilesDeleted += entry.FilesDeleted;
                report.FilesFailed += entry.FilesFailed;

                if (entry.FilesDeleted > 0)
                    report.TargetsCleared++;
            }

            Report(progress, stage, clientName, 1, null);
        }

        /// <summary>
        /// Empties one folder, measuring as it goes. The folder itself
        /// is left in place: clients expect their cache directories to
        /// exist, and some will not recreate them.
        /// </summary>
        private static ResetLogEntry Clear(
            string category,
            string path,
            string[] patterns,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            ResetLogEntry entry =
                new ResetLogEntry
                {
                    Category = category,
                    Path = path
                };

            if (!Directory.Exists(path))
                return null;

            if (!IsClearable(path))
            {
                entry.Note = "Skipped: protected location";

                return entry;
            }

            List<string> files =
                new List<string>();

            try
            {
                if (patterns == null || patterns.Length == 0)
                {
                    files.AddRange(
                        Directory.GetFiles(
                            path,
                            "*",
                            SearchOption.AllDirectories));
                }
                else
                {
                    foreach (string pattern in patterns)
                    {
                        files.AddRange(
                            Directory.GetFiles(
                                path,
                                pattern,
                                SearchOption.AllDirectories));
                    }
                }
            }
            catch (Exception ex)
            {
                entry.Note = "Could not read: " + ex.Message;

                return entry;
            }

            foreach (string file in files)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    FileInfo info = new FileInfo(file);

                    long size = info.Exists ? info.Length : 0;

                    if (info.Exists &&
                        (info.Attributes & FileAttributes.ReadOnly) != 0)
                    {
                        info.Attributes = FileAttributes.Normal;
                    }

                    File.Delete(file);

                    entry.FilesDeleted++;
                    entry.BytesFreed += size;
                }
                catch (Exception)
                {
                    // In use, or not ours to delete. Normal during a
                    // reset and not worth a line of its own.
                    entry.FilesFailed++;
                }
            }

            // Only whole-folder clears prune directories; a pattern
            // clear is meant to leave the structure alone.
            if (patterns == null || patterns.Length == 0)
                RemoveEmptyDirectories(path, token);

            return entry;
        }

        private static void RemoveEmptyDirectories(
            string root,
            CancellationToken token)
        {
            string[] directories;

            try
            {
                directories =
                    Directory.GetDirectories(
                        root,
                        "*",
                        SearchOption.AllDirectories);
            }
            catch (Exception)
            {
                return;
            }

            // Deepest first, so a parent is empty by the time it is
            // considered.
            foreach (string directory in
                directories.OrderByDescending(x => x.Length))
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    if (Directory.GetFileSystemEntries(directory).Length == 0)
                        Directory.Delete(directory);
                }
                catch (Exception)
                {
                }
            }
        }

        private static string Shorten(
            string path)
        {
            if (string.IsNullOrEmpty(path) || path.Length <= 58)
                return path;

            return "..." + path.Substring(path.Length - 55);
        }

        //--------------------------------------------------------------
        // Formatting
        //--------------------------------------------------------------

        public static string FormatBytes(
            long bytes)
        {
            if (bytes <= 0)
                return "0 B";

            string[] units = { "B", "KB", "MB", "GB", "TB" };

            double value = bytes;

            int unit = 0;

            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }

            return value.ToString(unit == 0 ? "0" : "0.##") +
                " " + units[unit];
        }
    }
}
