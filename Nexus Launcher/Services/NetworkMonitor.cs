using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Nexus_Launcher.Services
{
    /// <summary>
    /// Live network throughput, worked out from the change in total
    /// bytes between two samples.
    ///
    /// An instance rather than static: each screen polls on its own
    /// timer, and sharing one set of "previous" counters between two
    /// timers would give both of them wrong rates.
    /// </summary>
    internal class NetworkMonitor
    {
        public class Sample
        {
            public double DownloadBytesPerSecond { get; set; }

            public double UploadBytesPerSecond { get; set; }

            public string AdapterName { get; set; }

            public string IpAddress { get; set; }

            /// <summary>
            /// Negotiated link speed in bits per second, 0 if unknown.
            /// </summary>
            public long LinkSpeed { get; set; }

            public bool IsConnected { get; set; }
        }

        private readonly Stopwatch clock =
            Stopwatch.StartNew();

        private long lastReceived;
        private long lastSent;
        private double lastSeconds;
        private bool primed;

        public Sample Read()
        {
            Sample sample =
                new Sample();

            try
            {
                NetworkInterface[] adapters =
                    NetworkInterface.GetAllNetworkInterfaces()
                        .Where(IsCounted)
                        .ToArray();

                long received = 0;
                long sent = 0;

                foreach (NetworkInterface adapter in adapters)
                {
                    IPv4InterfaceStatistics stats =
                        adapter.GetIPv4Statistics();

                    received += stats.BytesReceived;
                    sent += stats.BytesSent;
                }

                double now =
                    clock.Elapsed.TotalSeconds;

                // The first read has nothing to compare against, so it
                // only records a baseline.
                if (primed)
                {
                    double elapsed =
                        now - lastSeconds;

                    if (elapsed > 0)
                    {
                        sample.DownloadBytesPerSecond =
                            Math.Max(0, (received - lastReceived) / elapsed);

                        sample.UploadBytesPerSecond =
                            Math.Max(0, (sent - lastSent) / elapsed);
                    }
                }

                lastReceived = received;
                lastSent = sent;
                lastSeconds = now;
                primed = true;

                // Describe the adapter most likely carrying traffic: the
                // one with a gateway, falling back to the fastest.
                NetworkInterface primary =
                    adapters
                        .OrderByDescending(HasGateway)
                        .ThenByDescending(x => x.Speed)
                        .FirstOrDefault();

                if (primary != null)
                {
                    sample.IsConnected = true;
                    sample.AdapterName = primary.Name;
                    sample.LinkSpeed = primary.Speed;
                    sample.IpAddress = GetIPv4(primary);
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            return sample;
        }

        /// <summary>
        /// Real adapters that are up. Loopback and tunnels would double
        /// count traffic or add noise.
        /// </summary>
        private static bool IsCounted(
            NetworkInterface adapter)
        {
            return adapter.OperationalStatus == OperationalStatus.Up &&
                adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                adapter.NetworkInterfaceType != NetworkInterfaceType.Tunnel;
        }

        private static bool HasGateway(
            NetworkInterface adapter)
        {
            try
            {
                return adapter.GetIPProperties()
                    .GatewayAddresses
                    .Any(x => x.Address != null &&
                        x.Address.AddressFamily == AddressFamily.InterNetwork);
            }
            catch
            {
                return false;
            }
        }

        private static string GetIPv4(
            NetworkInterface adapter)
        {
            try
            {
                return adapter.GetIPProperties()
                    .UnicastAddresses
                    .Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(x => x.Address.ToString())
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Bytes per second as a readable rate.
        /// </summary>
        public static string FormatRate(
            double bytesPerSecond)
        {
            string[] units = { "B/s", "KB/s", "MB/s", "GB/s" };

            int unit = 0;
            double value = bytesPerSecond;

            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }

            return value.ToString(unit == 0 ? "F0" : "F1") + " " + units[unit];
        }

        public static string FormatLinkSpeed(
            long bitsPerSecond)
        {
            if (bitsPerSecond <= 0)
                return "Unknown";

            if (bitsPerSecond >= 1000000000)
                return (bitsPerSecond / 1000000000d).ToString("0.#") + " Gbps";

            return (bitsPerSecond / 1000000d).ToString("0") + " Mbps";
        }
    }
}
