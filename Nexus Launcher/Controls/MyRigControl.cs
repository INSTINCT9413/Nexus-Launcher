using CCWin.Win32.Const;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Properties;
using Nexus_Launcher.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace Nexus_Launcher.Controls
{
    public partial class MyRigControl : DevExpress.XtraEditors.XtraUserControl
    {
        SystemHardwareScanner SHS = new SystemHardwareScanner();
        private List<DriveRowControl> _activeDriveControls = new List<DriveRowControl>();
        private Timer _speedTimer;
        public MyRigControl()
        {
            InitializeComponent();
        }

        private void MyRigControl_Load(object sender, EventArgs e)
        {
            FontManager.ApplyFont(
    this,
    Settings.Default.UIFont);
            var HardwareInfo = SHS.GetAllHardwareInfo();
            var sb = new StringBuilder();
            foreach (var component in HardwareInfo)
            {
                sb.AppendLine($"--- {component.Key} ---");

                int index = 1;
                foreach (var itemProperties in component.Value)
                {
                    if (component.Value.Count > 1)
                    {
                        sb.AppendLine($"[Device #{index++}]");
                    }

                    foreach (var kvp in itemProperties)
                    {
                        sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
                    }
                    sb.AppendLine();
                }
                sb.AppendLine(new string('-', 40));
            }
            InitializeDrivesAtRuntime();
            InitializeRamAtRuntime();
            SetupSpeedTimer();
            // 3. Assign the text to your UI element (Do not use Console.ReadKey!)
            //MessageBox.Show(sb.ToString());
            if (HardwareInfo.TryGetValue("Processor (CPU)", out var cpuList) && cpuList.Count > 0)
            {
                string cpuName = (cpuList[0].ContainsKey("Name") ? cpuList[0]["Name"] : "Unknown");
                // You can now assign this directly to a DevExpress Label, TextBox, etc.
                labelControl1.Text = cpuName;
            }
            if (HardwareInfo.TryGetValue("Memory (RAM)", out var ramList) && ramList.Count > 0)
            {
                string ramName = (ramList[0].ContainsKey("Name") ? ramList[0]["Name"] : "Unknown");
                labelControl3.Text = "RAM Details";
            }

            // 2. Get the Primary GPU Name and VRAM
            if (HardwareInfo.TryGetValue("Graphics Card (GPU)", out var gpuList) && gpuList.Count > 0)
            {
                string gpuName = (gpuList[0].ContainsKey("Name") ? gpuList[0]["Name"] : "Unknown");
                string gpuVram = (gpuList[0].ContainsKey("AdapterRAM") ? gpuList[0]["AdapterRAM"] : "Unknown");

                labelControl2.Text = $"{gpuName} ({gpuVram})";
            }
        }
        private void InitializeDrivesAtRuntime()
        {
            flowLayoutPanel1.Controls.Clear();
            _activeDriveControls.Clear();

            // Fetch all fixed (internal/external) hard drives plugged into the system
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (DriveInfo drive in allDrives)
            {
                // Ensure the drive is active/ready (skips empty DVD slots or card readers)
                if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                {
                    // Create a new instance of our row control
                    DriveRowControl row = new DriveRowControl(drive);

                    // Add it to our FlowLayoutPanel (it will auto-position itself at the bottom of the list)
                    flowLayoutPanel1.Controls.Add(row);
                    row.Anchor = AnchorStyles.None;
                    // Save reference to update speeds later
                    _activeDriveControls.Add(row);
                }
            }
        }
        private void InitializeRamAtRuntime()
        {
            flowLayoutPanel2.Controls.Clear();

            // 1. Grab the raw hardware specs using our original scanner class
            var scanner = new SystemHardwareScanner();
            var hardwareData = scanner.GetAllHardwareInfo();

            if (hardwareData.TryGetValue("Memory (RAM)", out var ramSticks))
            {
                int slotIndex = 1;
                foreach (var stickData in ramSticks)
                {
                    // 2. Create the row control for this specific stick of RAM
                    ramRowControl ramRow = new ramRowControl(stickData, slotIndex++);

                    // 3. Throw it into your FlowLayoutPanel layout
                    flowLayoutPanel2.Controls.Add(ramRow);
                }
            }
        }
        private void SetupSpeedTimer()
        {
            _speedTimer = new Timer();
            _speedTimer.Interval = 1000; // Update every 1 second (1000ms)
            _speedTimer.Tick += SpeedTimer_Tick;
            _speedTimer.Start();
        }

        private void SpeedTimer_Tick(object sender, EventArgs e)
        {
            // Loop through our generated controls and update their read/write speeds
            foreach (var driveControl in _activeDriveControls)
            {
                driveControl.UpdateDiskSpeeds();
            }
        }

        private void flowLayoutPanel2_SizeChanged(object sender, EventArgs e)
        {
            int totalWidth = 0;

            // Sum up the width and margins of all visible controls
            foreach (Control ctrl in flowLayoutPanel2.Controls)
            {
                if (ctrl.Visible)
                {
                    totalWidth += ctrl.Width + ctrl.Margin.Left + ctrl.Margin.Right;
                }
            }

            // Calculate left padding to push items to the center
            if (flowLayoutPanel2.Width > totalWidth)
            {
                int leftPadding = (flowLayoutPanel1.Width - totalWidth) / 2;
                flowLayoutPanel2.Padding = new Padding(leftPadding, flowLayoutPanel2.Padding.Top, 0, 0);
            }
            else
            {
                flowLayoutPanel2.Padding = new Padding(0, flowLayoutPanel2.Padding.Top, 0, 0);
            }
            flowLayoutPanel2.Refresh();
        }

        private void flowLayoutPanel1_SizeChanged(object sender, EventArgs e)
        {
            
        }
    }
}
