using DevExpress.Mvvm.Native;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Models;
using Nexus_Launcher.Properties;
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
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Nexus_Launcher.Forms
{
    public partial class addRemoveForm : XtraUserControl
    {
        private GalleryItem selectedGalleryItem;
        private string deleteItemname;
        private string deleteItempath;
        public addRemoveForm()
        {
            InitializeComponent();
        }
        private async void addRemoveForm_Load(object sender, EventArgs e)
        {
            await LoadNexusGamesAsync();
            FontManager.ApplyFont(
    this,
    Settings.Default.UIFont);
        }
        private void add()
        {
            // Implement the logic to add a new item to the tree list
            // For example, you can show a dialog to get the item details and then add it to the tree list
            List<GameInfo> apps =
    NexusAddRemoveManager.Load();

            apps.Add(new GameInfo
            {
                Name = textEdit1.Text,
                ExecutablePath = textEdit2.Text,
                IsInstalled = true
            });

            NexusAddRemoveManager.Save(apps);
        }
        private void Remove()
        {
            if (selectedGalleryItem == null)
            {
                MessageBox.Show(
                    "Please select an item first.");

                return;
            }

            GameInfo game =
                selectedGalleryItem.Tag as GameInfo;

            if (game == null)
                return;

            List<GameInfo> apps =
                NexusAddRemoveManager.Load();

            apps.RemoveAll(x =>
                string.Equals(
                    x.ExecutablePath,
                    game.ExecutablePath,
                    StringComparison.OrdinalIgnoreCase));

            NexusAddRemoveManager.Save(apps);

            // Remove from gallery immediately
            selectedGalleryItem.GalleryGroup.Items.Remove(
                selectedGalleryItem);

            selectedGalleryItem = null;
        }
        private void treeList1_FocusedNodeChanged(object sender, DevExpress.XtraTreeList.FocusedNodeChangedEventArgs e)
        {

        }

        private void imageListBoxControl1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        public async Task LoadNexusGamesAsync()
        {
            try
            {
                List<GameInfo> apps =
                    await Task.Run(() =>
                    {
                        return NexusAddRemoveManager.Load();
                    });

                galleryControl1.Gallery.Groups.Clear();

                GalleryItemGroup group =
                    new GalleryItemGroup();

                group.Caption =
                    "Custom Applications";

                galleryControl1.Gallery.Groups.Add(
                    group);

                foreach (GameInfo game in apps)
                {
                    GalleryItem item =
                        new GalleryItem();

                    item.Caption =
                        game.Name;

                    item.Tag =
                        game;

                    if (File.Exists(
                        game.ExecutablePath))
                    {
                        item.Image =
                            IconHelper.ExtractExeIcon(
                                game.ExecutablePath);
                    }

                    group.Items.Add(item);
                }
                //DevExpress.Utils.WaitDialogForm waitDialog = new DevExpress.Utils.WaitDialogForm("Loading Nexus Apps...", "Please wait");
                //MessageBox.Show(
                //    "Found and loaded " +
                //    apps.Count +
                //    " app(s) for Nexus Launcher. Add or Remove them as needed.", "Found Nexus Apps", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //waitDialog.Hide();
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                MessageBox.Show(ex.ToString());
            }
        }




        private async void simpleButton1_Click(object sender, EventArgs e)
        {
            if (toggleSwitch1.IsOn)
            {
                XtraFolderBrowserDialog folderBrowserDialog = new XtraFolderBrowserDialog
                {
                    Description = "Select Folder Containing Shortcuts",
                    ShowNewFolderButton = false
                };
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFolder =
                        folderBrowserDialog.SelectedPath;

                    List<string> files =
                        new List<string>();

                    files.AddRange(
                        Directory.GetFiles(
                            selectedFolder,
                            "*.lnk",
                            SearchOption.TopDirectoryOnly));

                    files.AddRange(
                        Directory.GetFiles(
                            selectedFolder,
                            "*.url",
                            SearchOption.TopDirectoryOnly));

                    files.AddRange(
                        Directory.GetFiles(
                            selectedFolder,
                            "*.exe",
                            SearchOption.TopDirectoryOnly));

                    if (files.Count == 0)
                    {
                        MessageBox.Show(
                            "No shortcuts or executables found in the selected folder.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);

                        return;
                    }

                    ShortcutImporter.ImportFolder(
                        selectedFolder);

                    MainView mainView =
                        this.FindForm() as MainView;

                    if (mainView != null)
                    {
                        await mainView.LoadNexusGamesAsync();
                    }

                    await LoadNexusGamesAsync();
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(textEdit1.Text) ||
                string.IsNullOrWhiteSpace(textEdit2.Text))
                {
                    MessageBox.Show(
                        "Please fill in all fields.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                add();
                MainView mainView = this.FindForm() as MainView;
                if (mainView != null)
                {
                    await mainView.LoadNexusGamesAsync();
                }
                await LoadNexusGamesAsync();
                textEdit1.Text = "";
                textEdit2.Text = "";
                pictureEdit1.Image = null;
            }
        }

        private async void simpleButton2_Click(object sender, EventArgs e)
        {
            if (!toggleSwitch2.IsOn)
            {
                if (selectedGalleryItem == null)
                {
                    MessageBox.Show(
                        "Please select an item to remove.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                if (MessageBox.Show(
                    "Are you sure you want to remove this item?\n" + selectedGalleryItem.Caption,
                    "Confirm Removal",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Remove();
                }
                MainView mainView = this.FindForm() as MainView;
                if (mainView != null)
                {
                    await mainView.LoadNexusGamesAsync();
                }
                await LoadNexusGamesAsync();
                labelControl3.Text = "Name: ";
                pictureEdit2.Image = null;
            }
            else
            {
                // Remove all checked gallery items
                foreach (GalleryItemGroup group in galleryControl1.Gallery.Groups)
                {
                    List<GalleryItem> itemsToRemove =
                        group.Items
                            .Cast<GalleryItem>()
                            .Where(x => x.Checked)
                            .ToList();

                    foreach (GalleryItem item in itemsToRemove)
                    {
                        if (item.Tag is GameInfo game)
                        {
                            List<GameInfo> apps =
                                NexusAddRemoveManager.Load();

                            apps.RemoveAll(x =>
                                string.Equals(
                                    x.ExecutablePath,
                                    game.ExecutablePath,
                                    StringComparison.OrdinalIgnoreCase));

                            NexusAddRemoveManager.Save(apps);
                        }

                        group.Items.Remove(item);
                    }
                }
                MainView mainView = this.FindForm() as MainView;
                if (mainView != null)
                {
                    await mainView.LoadNexusGamesAsync();
                }
                await LoadNexusGamesAsync();
            }
        }

        private void textEdit2_EditValueChanged(object sender, EventArgs e)
        {

        }

        private void textEdit2_Click(object sender, EventArgs e)
        {
            XtraOpenFileDialog openFileDialog = new XtraOpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                Title = "Select Executable"
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                textEdit2.Text = openFileDialog.FileName;
                if (File.Exists(
                        textEdit2.Text))
                {
                    pictureEdit1.Image =
                        IconHelper.ExtractExeIcon(
                            textEdit2.Text);
                }
            }
        }

        private void galleryControl1_Gallery_ItemClick(object sender, GalleryItemClickEventArgs e)
        {

            if (!toggleSwitch2.IsOn)
            {
                foreach (GalleryItemGroup group in galleryControl1.Gallery.Groups)
                {
                    foreach (GalleryItem item in group.Items)
                    {
                        item.Checked = false;
                    }
                }
            }

            if (e.Item.Tag is GameInfo game)
            {
                selectedGalleryItem = e.Item;

                deleteItemname = game.Name;
                deleteItempath = game.ExecutablePath;

                e.Item.Checked = true;
                if (File.Exists(
                        deleteItempath))
                {
                    pictureEdit2.Image =
                        IconHelper.ExtractExeIcon(
                            deleteItempath);
                    labelControl3.Text = "Name: " + deleteItemname;
                }
            }
        }

        private void simpleButton3_Click(object sender, EventArgs e)
        {
            XtraOpenFileDialog openFileDialog = new XtraOpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                Title = "Select Executable"
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                textEdit2.Text = openFileDialog.FileName;
                if (File.Exists(
                        textEdit2.Text))
                {
                    pictureEdit1.Image =
                        IconHelper.ExtractExeIcon(
                            textEdit2.Text);
                }
            }
        }

        private void galleryControl1_Click(object sender, EventArgs e)
        {

        }

        private void toggleSwitch2_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch2.IsOn)
            {
                galleryControl1.Gallery.ItemCheckMode = DevExpress.XtraBars.Ribbon.Gallery.ItemCheckMode.Multiple;
                MessageBox.Show(
                    "Multi-Select mode is enabled. You can now check multiple items to remove them all at once by clicking the 'Remove' button.(Use the shift or ctrl key to select multiple items)", "Multi-Select Mode", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                galleryControl1.Gallery.ItemCheckMode = DevExpress.XtraBars.Ribbon.Gallery.ItemCheckMode.SingleCheck;
            }
        }

        private void toggleSwitch1_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch1.IsOn)
            {
                textEdit1.Enabled = false;
                textEdit2.Enabled = false;
                simpleButton3.Enabled = false;
                MessageBox.Show(
                    "Add Folder mode is enabled. Click the 'Add' button to select a folder containing shortcuts or executables to import. This will also scan subfolders for additional shortcuts or executables. Use with caution as this will add any and all found items.", "Add Folder Mode", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                textEdit1.Enabled = true;
                textEdit2.Enabled = true;
                simpleButton3.Enabled = true;
            }
        }
    }
}
