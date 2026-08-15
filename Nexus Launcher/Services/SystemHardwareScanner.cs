using System;
using System.Collections.Generic;
using System.Management;

namespace Nexus_Launcher.Services
{
    public class SystemHardwareScanner
    {
        /// <summary>
        /// Gathers all hardware component specifications from the system.
        /// </summary>
        public Dictionary<string, List<Dictionary<string, string>>> GetAllHardwareInfo()
        {
            return new Dictionary<string, List<Dictionary<string, string>>>
            {
                { "Processor (CPU)", GetWmiDetails("Win32_Processor", new[] { "Name", "NumberOfCores", "NumberOfLogicalProcessors", "MaxClockSpeed", "Manufacturer", "Architecture" }) },
                { "Memory (RAM)", GetWmiDetails("Win32_PhysicalMemory", new[] { "BankLabel", "Capacity", "Speed", "Manufacturer", "PartNumber" }) },
                { "Graphics Card (GPU)", GetWmiDetails("Win32_VideoController", new[] { "Name", "DriverVersion", "AdapterRAM", "VideoProcessor", "CurrentHorizontalResolution", "CurrentVerticalResolution" }) },
                { "Storage Drives", GetWmiDetails("Win32_DiskDrive", new[] { "Model", "Size", "InterfaceType", "SerialNumber", "Partitions" }) },
                { "Network Adapters", GetWmiDetails("Win32_NetworkAdapterConfiguration", new[] { "Description", "MACAddress", "IPAddress", "DHCPEnabled" }, "IPEnabled = True") }
            };
        }

        /// <summary>
        /// Helper method to query WMI and extract specific properties safely.
        /// </summary>
        private List<Dictionary<string, string>> GetWmiDetails(string wmiClass, string[] properties, string filter = "")
        {
            var componentList = new List<Dictionary<string, string>>();
            string query = $"SELECT * FROM {wmiClass}";

            if (!string.IsNullOrEmpty(filter))
            {
                query += $" WHERE {filter}";
            }

            try
            {
                using (var searcher = new ManagementObjectSearcher(query))
                using (var collection = searcher.Get())
                {
                    foreach (ManagementObject obj in collection)
                    {
                        var details = new Dictionary<string, string>();
                        foreach (var prop in properties)
                        {
                            try
                            {
                                var value = obj[prop];
                                if (value != null)
                                {
                                    // Handle arrays (like IP Addresses) gracefully
                                    if (value is string[] arrayValue)
                                    {
                                        details[prop] = string.Join(", ", arrayValue);
                                    }
                                    // Convert RAM/Storage bytes to Gigabytes for readability
                                    else if ((prop == "Capacity" || prop == "Size" || prop == "AdapterRAM") && double.TryParse(value.ToString(), out double bytes))
                                    {
                                        double gb = bytes / (1024 * 1024 * 1024);
                                        details[prop] = $"{gb:F2} GB";
                                    }
                                    else
                                    {
                                        details[prop] = value.ToString().Trim();
                                    }
                                }
                                else
                                {
                                    details[prop] = "N/A";
                                }
                            }
                            catch
                            {
                                details[prop] = "Unknown Error";
                            }
                        }
                        componentList.Add(details);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                var errorDict = new Dictionary<string, string> { { "Error", $"Failed to query {wmiClass}: {ex.Message}" } };
                componentList.Add(errorDict);
            }

            return componentList;
        }
    }
}