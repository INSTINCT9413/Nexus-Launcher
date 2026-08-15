using Nexus_Launcher.Helpers;
using Nexus_Launcher.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls
{
    public partial class ramRowControl : DevExpress.XtraEditors.XtraUserControl
    {
        public ramRowControl(Dictionary<string, string> ramData, int slotIndex)
        {
            InitializeComponent();
            PopulateRamDetails(ramData, slotIndex);
        }
        private void PopulateRamDetails(Dictionary<string, string> ramData, int slotIndex)
        {
            // Extract properties safely using GetValueOrDefault or basic checking
            string bank = ramData.ContainsKey("BankLabel") ? ramData["BankLabel"] : $"Slot {slotIndex}";
            string manufacturer = ramData.ContainsKey("Manufacturer") ? ramData["Manufacturer"] : "Unknown";
            string speed = ramData.ContainsKey("Speed") ? ramData["Speed"] + " MHz" : "Unknown Speed";
            string partNumber = ramData.ContainsKey("PartNumber") ? ramData["PartNumber"] : "N/A";
            
            // Raw capacity comes from our original hardware scanner class as a pre-formatted GB string
            string capacity = ramData.ContainsKey("Capacity") ? ramData["Capacity"] : "0 GB";

            // Clean up generic manufacturer names that Windows sometimes reports
            if (manufacturer.StartsWith("04CB")) manufacturer = "A-DATA";
            if (manufacturer.StartsWith("017A")) manufacturer = "Apacer";
            if (manufacturer.StartsWith("029E")) manufacturer = "Corsair";
            if (manufacturer.StartsWith("04CD")) manufacturer = "G.Skill";
            if (manufacturer.StartsWith("0198")) manufacturer = "Kingston";
            if (manufacturer.StartsWith("0583")) manufacturer = "Crucial";

            // Populate the UI
            labelControl2.Text = $"{bank}";
            labelControl5.Text = $"Manufacturer: {manufacturer}";
            labelControl3.Text = $"Capacity: {capacity}";
            labelControl4.Text = $"Speed: {speed}";
            labelControl6.Text = $"Part: {partNumber.Trim()}";
        }

        private void ramRowControl_Load(object sender, EventArgs e)
        {
            FontManager.ApplyFont(
    this,
    Settings.Default.UIFont);
        }
    }
}
