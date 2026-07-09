using DevExpress.XtraEditors;
using DevExpress.XtraPrinting.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls
{
  
        public partial class DriveRowControl : XtraUserControl
        {
            private DriveInfo _driveInfo;
            private PerformanceCounter _readCounter;
            private PerformanceCounter _writeCounter;
            public string DriveLetter { get; private set; }

            public DriveRowControl(DriveInfo drive)
            {
                InitializeComponent();
                _driveInfo = drive;
                DriveLetter = drive.Name.Replace("\\", ""); // e.g., "C:"

                InitPerformanceCounters();
                UpdateStorageSpace();
            }

            private void InitPerformanceCounters()
            {
                try
                {
                    // Performance counters use instance names like "C:"
                    _readCounter = new PerformanceCounter("LogicalDisk", "Disk Read Bytes/sec", DriveLetter);
                    _writeCounter = new PerformanceCounter("LogicalDisk", "Disk Write Bytes/sec", DriveLetter);
                }
                catch
                {
                    // Performance counters can sometimes fail depending on Windows permissions
                    _readCounter = null;
                    _writeCounter = null;
                }
            }

        // Call this once at creation
        // Call this once at creation
        public void UpdateStorageSpace()
        {
            if (!_driveInfo.IsReady) return;

            long totalSize = _driveInfo.TotalSize;
            long freeSpace = _driveInfo.TotalFreeSpace;
            long usedSpace = totalSize - freeSpace;

            // 1. These are your beautiful, scaled strings (e.g., "4.20 TB")
            string totalFormatted = FormatBytes(totalSize);
            string usedFormatted = FormatBytes(usedSpace);

            // Set the Main Drive Header
            labelControl1.Text = $"{_driveInfo.VolumeLabel} ({_driveInfo.Name})";

            // 2. FIX: Use the formatted string variables here! 
            // We also set ShowText to true so DevExpress actually renders it.
            //progressBarControl1.Properties.ShowText = true;
            progressBarControl1.Properties.CustomDisplayText += (s, e) => {
                e.DisplayText = $"{usedFormatted} used out of {totalFormatted}";
            };

            // Progress bar percentage calculation
            double percentUsed = ((double)usedSpace / totalSize) * 100;
            progressBarControl1.Position = (int)Math.Min(100, Math.Max(0, percentUsed));
            if(percentUsed <= 95)
            {
                progressBarControl1.Properties.Appearance.BorderColor = Color.Red;
            }
            if(percentUsed <= 85)
            {
                progressBarControl1.Properties.Appearance.BorderColor = Color.OrangeRed;
            }
            if(percentUsed <= 50)
            {
                progressBarControl1.Properties.Appearance.BorderColor = Color.Blue;
            }
            if (percentUsed <= 35)
            {
                progressBarControl1.Properties.Appearance.BorderColor = Color.Green;
            }
        }
        /// <summary>
        /// Converts raw bytes into the most accurate, readable string unit up to Petabytes.
        /// </summary>
        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
            int counter = 0;
            double number = bytes;

            // Keeps dividing by 1024 until the number drops below 1024, 
            // dynamically discovering the optimal unit.
            while (number >= 1024 && counter < suffixes.Length - 1)
            {
                counter++;
                number /= 1024;
            }

            // Returns the number formatted to 2 decimal places with its matching suffix
            return $"{number:F2} {suffixes[counter]}";
        }
        // Call this inside a Timer_Tick event to update speeds every second
        public void UpdateDiskSpeeds()
            {
                if (_readCounter == null || _writeCounter == null)
                {
                    labelControl2.Text = "Read: N/A | Write: N/A";
                    return;
                }

                try
                {
                    // Convert bytes/sec to Megabytes/sec (MB/s)
                    double readSpeedMB = _readCounter.NextValue() / (1024 * 1024);
                    double writeSpeedMB = _writeCounter.NextValue() / (1024 * 1024);

                    labelControl2.Text = $"Read: {readSpeedMB:F1} MB/s | Write: {writeSpeedMB:F1} MB/s";
                }
                catch
                {
                    labelControl2.Text = "Speed tracking error";
                }
            }

        private void DriveRowControl_Load(object sender, EventArgs e)
        {

        }
    }
    }

