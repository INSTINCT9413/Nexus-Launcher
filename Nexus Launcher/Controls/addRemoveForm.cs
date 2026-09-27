using DevExpress.CodeParser;
using DevExpress.Mvvm.Native;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Models;
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

        //--------------------------------------------------------------
        // First time here
        //--------------------------------------------------------------

        private bool tipsQueued;

        /// <summary>
        /// Walks through adding and removing your own games the first
        /// time this tab is opened.
        ///
        /// Public because the tab that hosts it is the thing that
        /// knows when it has been opened.
        /// </summary>
        public void ShowAddRemoveTips()
        {
            if (tipsQueued ||
                TutorialService.HasSeen(TutorialService.AddRemove))
            {
                return;
            }

            tipsQueued = true;

            TutorialService.ShowOnce(
                FindForm(),
                TutorialService.AddRemove,
                () => this,
                new[]
                {
                    new TutorialStep
                    {
                        Title = "What you have added",
                        Body =
                            "Everything you have put into Nexus " +
                            "yourself is listed here. Click one to " +
                            "pick it out; its icon and name appear " +
                            "below so you can be sure it is the right " +
                            "one before removing it.",
                        Target = () => galleryControl1,
                        Location = DevExpress.Utils.VisualEffects.GuideFlyoutLocation.Right
                    },
                    new TutorialStep
                    {
                        Title = "Adding one",
                        Body =
                            "Give it a name and point Path at the " +
                            "program or shortcut. The button beside " +
                            "the box browses for it, and the icon is " +
                            "taken from the file for you.",
                        Target = () => textEdit2,
                        Location = DevExpress.Utils.VisualEffects.GuideFlyoutLocation.Bottom
                    },
                    new TutorialStep
                    {
                        Title = "A whole folder at once",
                        Body =
                            "Turn this on and Add asks for a folder " +
                            "instead, pulling in every shortcut and " +
                            "program it finds there and in the folders " +
                            "underneath. Quick, but it takes " +
                            "everything, so aim it at a tidy folder.",
                        Target = () => toggleSwitch1,
                        Location = DevExpress.Utils.VisualEffects.GuideFlyoutLocation.Bottom
                    },
                    new TutorialStep
                    {
                        Title = "Add",
                        Body =
                            "The new entry appears in the Nexus group " +
                            "in the sidebar straight away, alongside " +
                            "the games your launchers found, and works " +
                            "the same way.",
                        Target = () => simpleButton1,
                        Location = DevExpress.Utils.VisualEffects.GuideFlyoutLocation.Left
                    },
                    new TutorialStep
                    {
                        Title = "Removing several",
                        Body =
                            "With this on you can tick more than one " +
                            "entry in the list and clear them all in " +
                            "one go, rather than one at a time.",
                        Target = () => toggleSwitch2,
                        Location = DevExpress.Utils.VisualEffects.GuideFlyoutLocation.Top
                    },
                    new TutorialStep
                    {
                        Title = "Remove",
                        Body =
                            "This only takes the entry out of Nexus. " +
                            "Nothing is uninstalled and no files are " +
                            "deleted, so adding it again later is just " +
                            "a matter of pointing at it once more.",
                        Target = () => simpleButton2,
                        Location = DevExpress.Utils.VisualEffects.GuideFlyoutLocation.Left
                    }
                });
        }

        /// <summary>
        /// Guides were reset in Settings, so this tab offers its own
        /// again the next time it is opened.
        /// </summary>
        private void Tutorials_Reset()
        {
            tipsQueued = false;
        }
        private async void addRemoveForm_Load(object sender, EventArgs e)
        {
            TutorialService.Reset += Tutorials_Reset;

            Disposed += (s, a) => TutorialService.Reset -= Tutorials_Reset;

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
            // 1. Folder Import Path (Toggle is ON)
            if (toggleSwitch1.IsOn)
            {
                using (var folderBrowserDialog = new XtraFolderBrowserDialog
                {
                    Description = "Select Folder Containing Shortcuts",
                    ShowNewFolderButton = false
                })
                {
                    if (folderBrowserDialog.ShowDialog() != DialogResult.OK) return;

                    string selectedFolder = folderBrowserDialog.SelectedPath;
                    var targetExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".lnk", ".url", ".exe" };

                    bool hasValidFiles = Directory.EnumerateFiles(selectedFolder, "*.*", SearchOption.TopDirectoryOnly)
                        .Any(file => targetExtensions.Contains(Path.GetExtension(file)));

                    if (!hasValidFiles)
                    {
                        MessageBox.Show(
                            "No shortcuts or executables found in the selected folder.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    ShortcutImporter.ImportFolder(selectedFolder);
                    await RefreshGameViewsAsync();
                }
            }
            // 2. Toggle is OFF Path
            else
            {
                // Special Action: TextEdit1 is empty but TextEdit2 has text -> forward the click
                if (string.IsNullOrEmpty(textEdit1.Text) && string.IsNullOrEmpty(textEdit2.Text))
                {
                    simpleButton3.PerformClick();
                    return;
                }

                // Regular Action: Manual validation & entry code
                if (string.IsNullOrWhiteSpace(textEdit1.Text) || string.IsNullOrWhiteSpace(textEdit2.Text))
                {
                    MessageBox.Show(
                        "Please fill in all fields.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                add();
                await RefreshGameViewsAsync();

                // Clear UI Inputs
                textEdit1.Text = string.Empty;
                textEdit2.Text = string.Empty;
                pictureEdit1.Image = null;
            }
        }

        private async Task RefreshGameViewsAsync()
        {
            if (this.FindForm() is MainView mainView)
            {
                await mainView.LoadNexusGamesAsync();
            }
            await LoadNexusGamesAsync();
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
                    textEdit1.Text = Path.GetFileNameWithoutExtension(textEdit2.Text);
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
