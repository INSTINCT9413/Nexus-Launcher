using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace Nexus_Launcher.Services
{
    public class GpuInfo
    {
        public string Name { get; set; }

        public long VramBytes { get; set; }

        public string DriverVersion { get; set; }

        public string Resolution { get; set; }
    }

    /// <summary>
    /// The fixed facts about the machine. Gathered once per session:
    /// none of it changes while Nexus is running.
    /// </summary>
    public class HardwareProfile
    {
        public string ComputerName { get; set; }

        public string OperatingSystem { get; set; }

        public string CpuName { get; set; }

        public int CpuCores { get; set; }

        public int CpuThreads { get; set; }

        public int CpuMaxClockMHz { get; set; }

        public string CpuManufacturer { get; set; }

        public string CpuSocket { get; set; }

        public string CpuArchitecture { get; set; }

        public int CpuL2CacheKB { get; set; }

        public int CpuL3CacheKB { get; set; }

        /// <summary>
        /// Null when Windows will not say, which is common without
        /// elevation.
        /// </summary>
        public bool? CpuVirtualization { get; set; }

        public List<GpuInfo> Gpus { get; set; }

        public long TotalRamBytes { get; set; }

        /// <summary>
        /// One entry per stick, in the shape ramRowControl expects.
        /// </summary>
        public List<Dictionary<string, string>> RamSticks { get; set; }

        public string Motherboard { get; set; }

        public string MemoryType { get; set; }

        public string MemoryFormFactor { get; set; }

        public int MemorySpeedMHz { get; set; }

        public int MemorySlotsTotal { get; set; }

        public long MemoryMaxCapacityBytes { get; set; }

        public HardwareProfile()
        {
            Gpus = new List<GpuInfo>();
            RamSticks = new List<Dictionary<string, string>>();
        }
    }

    /// <summary>
    /// Reads the hardware profile off the UI thread and caches it.
    ///
    /// The old My Rig ran a full WMI scan synchronously on load, twice,
    /// which froze the UI every time the page opened. This runs once in
    /// the background and every caller shares the same result.
    /// </summary>
    internal static class HardwareInfoService
    {
        private static readonly object sync =
            new object();

        private static Task<HardwareProfile> cached;

        public static Task<HardwareProfile> GetAsync()
        {
            lock (sync)
            {
                if (cached == null)
                    cached = Task.Run(() => Read());

                return cached;
            }
        }

        private static HardwareProfile Read()
        {
            HardwareProfile profile =
                new HardwareProfile();

            profile.ComputerName =
                Environment.MachineName;

            ReadOperatingSystem(profile);
            ReadCpu(profile);
            ReadGpus(profile);
            ReadRam(profile);
            ReadMemoryArray(profile);
            ReadMotherboard(profile);

            return profile;
        }

        //--------------------------------------------------------------
        // Components
        //--------------------------------------------------------------

        private static void ReadOperatingSystem(
            HardwareProfile profile)
        {
            foreach (ManagementObject item in Query(
                "SELECT Caption, BuildNumber FROM Win32_OperatingSystem"))
            {
                profile.OperatingSystem =
                    Text(item, "Caption") +
                    " (Build " + Text(item, "BuildNumber") + ")";
            }
        }

        private static void ReadCpu(
            HardwareProfile profile)
        {
            foreach (ManagementObject item in Query(
                "SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, Manufacturer, SocketDesignation, Architecture, L2CacheSize, L3CacheSize, VirtualizationFirmwareEnabled FROM Win32_Processor"))
            {
                profile.CpuName = Text(item, "Name");
                profile.CpuCores += Int(item, "NumberOfCores");
                profile.CpuThreads += Int(item, "NumberOfLogicalProcessors");
                profile.CpuMaxClockMHz = Int(item, "MaxClockSpeed");
                profile.CpuManufacturer = Text(item, "Manufacturer");
                profile.CpuSocket = Text(item, "SocketDesignation");
                profile.CpuArchitecture = DescribeArchitecture(Int(item, "Architecture"));
                profile.CpuL2CacheKB = Int(item, "L2CacheSize");
                profile.CpuL3CacheKB = Int(item, "L3CacheSize");
                profile.CpuVirtualization = Bool(item, "VirtualizationFirmwareEnabled");
            }
        }

        private static void ReadGpus(
            HardwareProfile profile)
        {
            Dictionary<string, long> registryVram =
                ReadRegistryVram();

            foreach (ManagementObject item in Query(
                "SELECT Name, AdapterRAM, DriverVersion, CurrentHorizontalResolution, CurrentVerticalResolution FROM Win32_VideoController"))
            {
                GpuInfo gpu =
                    new GpuInfo();

                gpu.Name = Text(item, "Name");
                gpu.DriverVersion = Text(item, "DriverVersion");

                int width = Int(item, "CurrentHorizontalResolution");
                int height = Int(item, "CurrentVerticalResolution");

                gpu.Resolution =
                    width > 0 && height > 0
                        ? width + " x " + height
                        : null;

                // AdapterRAM is a 32 bit field, so anything over 4 GB is
                // reported capped or wrapped: a 12 GB RTX 4070 Ti comes
                // back as exactly 4 GB. The display driver's registry
                // entry carries the real 64 bit size, so prefer that.
                long vram;

                if (!string.IsNullOrEmpty(gpu.Name) &&
                    registryVram.TryGetValue(gpu.Name, out vram) &&
                    vram > 0)
                {
                    gpu.VramBytes = vram;
                }
                else
                {
                    gpu.VramBytes = Long(item, "AdapterRAM");
                }

                profile.Gpus.Add(gpu);
            }
        }

        /// <summary>
        /// Display adapter name to its true VRAM size, from the driver's
        /// class key in the registry.
        /// </summary>
        private static Dictionary<string, long> ReadRegistryVram()
        {
            Dictionary<string, long> result =
                new Dictionary<string, long>(
                    StringComparer.OrdinalIgnoreCase);

            const string ClassKey =
                @"SYSTEM\ControlSet001\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

            try
            {
                using (RegistryKey root = Registry.LocalMachine.OpenSubKey(ClassKey))
                {
                    if (root == null)
                        return result;

                    foreach (string subKeyName in root.GetSubKeyNames())
                    {
                        try
                        {
                            using (RegistryKey adapter = root.OpenSubKey(subKeyName))
                            {
                                if (adapter == null)
                                    continue;

                                string name =
                                    adapter.GetValue("DriverDesc") as string;

                                long size =
                                    ToLong(adapter.GetValue(
                                        "HardwareInformation.qwMemorySize"));

                                if (!string.IsNullOrEmpty(name) && size > 0)
                                    result[name] = size;
                            }
                        }
                        catch
                        {
                            // "Properties" and some other subkeys are
                            // not readable without elevation. Skip them.
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            return result;
        }

        private static void ReadRam(
            HardwareProfile profile)
        {
            foreach (ManagementObject item in Query(
                "SELECT BankLabel, Capacity, Speed, ConfiguredClockSpeed, Manufacturer, PartNumber, SMBIOSMemoryType, FormFactor FROM Win32_PhysicalMemory"))
            {
                long capacity =
                    Long(item, "Capacity");

                profile.TotalRamBytes += capacity;

                // Every stick in a working system matches, so the first
                // one that reports a value speaks for all of them.
                if (profile.MemoryType == null)
                    profile.MemoryType = DescribeMemoryType(Int(item, "SMBIOSMemoryType"));

                if (profile.MemoryFormFactor == null)
                    profile.MemoryFormFactor = DescribeFormFactor(Int(item, "FormFactor"));

                if (profile.MemorySpeedMHz == 0)
                {
                    // Configured is what it actually runs at, which is
                    // often below the rated Speed until XMP/EXPO is on.
                    profile.MemorySpeedMHz = Int(item, "ConfiguredClockSpeed");

                    if (profile.MemorySpeedMHz == 0)
                        profile.MemorySpeedMHz = Int(item, "Speed");
                }

                profile.RamSticks.Add(new Dictionary<string, string>
                {
                    { "BankLabel", Text(item, "BankLabel") },
                    { "Capacity", (capacity / 1073741824d).ToString("F2") + " GB" },
                    { "Speed", Text(item, "Speed") },
                    { "Manufacturer", Text(item, "Manufacturer") },
                    { "PartNumber", Text(item, "PartNumber") }
                });
            }
        }

        private static void ReadMemoryArray(
            HardwareProfile profile)
        {
            foreach (ManagementObject item in Query(
                "SELECT MemoryDevices, MaxCapacityEx FROM Win32_PhysicalMemoryArray"))
            {
                profile.MemorySlotsTotal += Int(item, "MemoryDevices");

                // MaxCapacityEx is in kilobytes.
                profile.MemoryMaxCapacityBytes += Long(item, "MaxCapacityEx") * 1024;
            }
        }

        private static void ReadMotherboard(
            HardwareProfile profile)
        {
            foreach (ManagementObject item in Query(
                "SELECT Manufacturer, Product FROM Win32_BaseBoard"))
            {
                profile.Motherboard =
                    (Text(item, "Manufacturer") + " " +
                        Text(item, "Product")).Trim();
            }
        }

        //--------------------------------------------------------------
        // WMI helpers
        //--------------------------------------------------------------

        private static IEnumerable<ManagementObject> Query(
            string wql)
        {
            List<ManagementObject> items =
                new List<ManagementObject>();

            try
            {
                using (ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher(wql))
                {
                    foreach (ManagementObject item in searcher.Get())
                    {
                        items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                // One missing WMI class must not blank the whole page.
                Program.LogCrash(ex);
            }

            return items;
        }

        private static string Text(
            ManagementObject item,
            string property)
        {
            try
            {
                object value = item[property];

                return value == null
                    ? null
                    : value.ToString().Trim();
            }
            catch
            {
                return null;
            }
        }

        private static bool? Bool(
            ManagementObject item,
            string property)
        {
            try
            {
                object value = item[property];

                return value is bool
                    ? (bool?)(bool)value
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static string DescribeArchitecture(
            int code)
        {
            switch (code)
            {
                case 0: return "x86";
                case 5: return "ARM";
                case 9: return "x64";
                case 12: return "ARM64";
                default: return null;
            }
        }

        /// <summary>
        /// SMBIOS memory type codes, from the SMBIOS specification.
        /// </summary>
        private static string DescribeMemoryType(
            int code)
        {
            switch (code)
            {
                case 20: return "DDR";
                case 21: return "DDR2";
                case 24: return "DDR3";
                case 26: return "DDR4";
                case 27: return "LPDDR";
                case 28: return "LPDDR2";
                case 29: return "LPDDR3";
                case 30: return "LPDDR4";
                case 34: return "DDR5";
                case 35: return "LPDDR5";
                default: return null;
            }
        }

        private static string DescribeFormFactor(
            int code)
        {
            switch (code)
            {
                case 8: return "DIMM";
                case 12: return "SODIMM";
                default: return null;
            }
        }

        private static int Int(
            ManagementObject item,
            string property)
        {
            return (int)Long(item, property);
        }

        private static long Long(
            ManagementObject item,
            string property)
        {
            try
            {
                return ToLong(item[property]);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// The registry can hand the VRAM size back as a QWORD, a DWORD
        /// or a raw byte array depending on the driver.
        /// </summary>
        private static long ToLong(
            object value)
        {
            if (value == null)
                return 0;

            byte[] bytes = value as byte[];

            if (bytes != null)
            {
                return bytes.Length >= 8
                    ? BitConverter.ToInt64(bytes, 0)
                    : bytes.Length >= 4
                        ? BitConverter.ToUInt32(bytes, 0)
                        : 0;
            }

            long result;

            return long.TryParse(
                value.ToString(),
                out result)
                ? result
                : 0;
        }
    }
}
