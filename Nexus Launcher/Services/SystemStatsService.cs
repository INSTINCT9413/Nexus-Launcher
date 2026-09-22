using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Nexus_Launcher.Services
{
    /// <summary>
    /// Cheap system readings for the side panel, polled every couple
    /// of seconds.
    ///
    /// Everything here comes from a performance counter, a P/Invoke or
    /// DriveInfo rather than WMI: WMI is fine for the one off hardware
    /// scan but far too slow to sit on a timer.
    /// </summary>
    internal static class SystemStatsService
    {
        public class Snapshot
        {
            public float CpuPercent { get; set; }

            public double RamUsedGb { get; set; }

            public double RamTotalGb { get; set; }

            public double RamPercent { get; set; }

            public string DriveLabel { get; set; }

            public double DriveUsedGb { get; set; }

            public double DriveTotalGb { get; set; }

            public double DrivePercent { get; set; }

            /// <summary>
            /// Null on a desktop with no battery.
            /// </summary>
            public int? BatteryPercent { get; set; }

            public bool OnMains { get; set; }

            public TimeSpan Uptime { get; set; }
        }

        private static PerformanceCounter cpuCounter;

        private static bool cpuCounterFailed;

        //--------------------------------------------------------------
        // Native
        //--------------------------------------------------------------

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength =
                    (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(
            [In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();

        //--------------------------------------------------------------
        // Reading
        //--------------------------------------------------------------

        /// <summary>
        /// The first CPU reading is always 0, so prime the counter at
        /// startup and let a couple of seconds pass before display.
        /// </summary>
        public static void Prime()
        {
            ReadCpu();
        }

        public static Snapshot Read()
        {
            Snapshot snapshot =
                new Snapshot();

            snapshot.CpuPercent =
                ReadCpu();

            ReadMemory(snapshot);

            ReadDrive(snapshot);

            ReadBattery(snapshot);

            snapshot.Uptime =
                ReadUptime();

            return snapshot;
        }

        private static float ReadCpu()
        {
            if (cpuCounterFailed)
                return 0;

            try
            {
                if (cpuCounter == null)
                {
                    cpuCounter =
                        new PerformanceCounter(
                            "Processor",
                            "% Processor Time",
                            "_Total");
                }

                return cpuCounter.NextValue();
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                // Counters can be missing or corrupt on some machines.
                // Stop trying rather than throwing every tick.
                cpuCounterFailed = true;

                return 0;
            }
        }

        private static void ReadMemory(
            Snapshot snapshot)
        {
            try
            {
                MEMORYSTATUSEX status =
                    new MEMORYSTATUSEX();

                if (!GlobalMemoryStatusEx(status))
                    return;

                const double Gb =
                    1024d * 1024d * 1024d;

                snapshot.RamTotalGb =
                    status.ullTotalPhys / Gb;

                snapshot.RamUsedGb =
                    (status.ullTotalPhys - status.ullAvailPhys) / Gb;

                snapshot.RamPercent =
                    status.dwMemoryLoad;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        public class MemoryDetail
        {
            public long TotalBytes { get; set; }

            public long AvailableBytes { get; set; }

            /// <summary>
            /// Memory promised to programs, including what has been
            /// paged out. Can exceed physical memory.
            /// </summary>
            public long CommittedBytes { get; set; }

            /// <summary>
            /// Physical memory plus the page file.
            /// </summary>
            public long CommitLimitBytes { get; set; }
        }

        /// <summary>
        /// The fuller memory picture for the memory detail view.
        /// </summary>
        public static MemoryDetail ReadMemoryDetail()
        {
            MemoryDetail detail =
                new MemoryDetail();

            try
            {
                MEMORYSTATUSEX status =
                    new MEMORYSTATUSEX();

                if (!GlobalMemoryStatusEx(status))
                    return detail;

                detail.TotalBytes = (long)status.ullTotalPhys;
                detail.AvailableBytes = (long)status.ullAvailPhys;

                // Despite the names, these two are the commit limit and
                // the commit still available, not the page file alone.
                detail.CommitLimitBytes = (long)status.ullTotalPageFile;
                detail.CommittedBytes =
                    (long)(status.ullTotalPageFile - status.ullAvailPageFile);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            return detail;
        }

        private static void ReadDrive(
            Snapshot snapshot)
        {
            try
            {
                string root =
                    Path.GetPathRoot(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.System));

                DriveInfo drive =
                    new DriveInfo(root);

                if (!drive.IsReady)
                    return;

                const double Gb =
                    1024d * 1024d * 1024d;

                snapshot.DriveLabel =
                    drive.Name.TrimEnd(
                        Path.DirectorySeparatorChar);

                snapshot.DriveTotalGb =
                    drive.TotalSize / Gb;

                snapshot.DriveUsedGb =
                    (drive.TotalSize - drive.TotalFreeSpace) / Gb;

                snapshot.DrivePercent =
                    drive.TotalSize == 0
                        ? 0
                        : (double)(drive.TotalSize - drive.TotalFreeSpace) *
                            100d / drive.TotalSize;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        private static void ReadBattery(
            Snapshot snapshot)
        {
            try
            {
                PowerStatus power =
                    SystemInformation.PowerStatus;

                snapshot.OnMains =
                    power.PowerLineStatus == PowerLineStatus.Online;

                // 255 is the documented "unknown" value, and a desktop
                // reports NoSystemBattery.
                if (power.BatteryChargeStatus ==
                        BatteryChargeStatus.NoSystemBattery ||
                    power.BatteryLifePercent > 1f)
                {
                    snapshot.BatteryPercent = null;

                    return;
                }

                snapshot.BatteryPercent =
                    (int)Math.Round(power.BatteryLifePercent * 100f);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        private static TimeSpan ReadUptime()
        {
            try
            {
                return TimeSpan.FromMilliseconds(
                    GetTickCount64());
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return TimeSpan.Zero;
            }
        }
    }
}
