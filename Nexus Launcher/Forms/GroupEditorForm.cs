using DevExpress.XtraEditors;
using Nexus_Launcher.Models;
using Nexus_Launcher.Services.Library;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Nexus_Launcher.Forms
{
    /// <summary>
    /// Names a library group and picks the icon shown beside it.
    /// Used for both creating and editing a group.
    ///
    /// Built in code rather than with the designer because it is a
    /// plain grid of buttons and a text box.
    /// </summary>
    public class GroupEditorForm : XtraForm
    {
        private const int IconsPerRow = 10;
        private const int IconButtonSize = 34;

        private readonly TextEdit nameEdit;
        private readonly FlowLayoutPanel iconPanel;
        private CheckButton customButton;

        public string GroupName
        {
            get
            {
                return nameEdit.Text.Trim();
            }
        }

        public string IconKey
        {
            get;
            private set;
        }

        public string IconPath
        {
            get;
            private set;
        }

        public GroupEditorForm(
            string title,
            LibraryGroup existing)
        {
            Text = title;

            FormBorderStyle =
                FormBorderStyle.FixedDialog;

            StartPosition =
                FormStartPosition.CenterParent;

            MinimizeBox = false;
            MaximizeBox = false;
            ShowIcon = false;

            ClientSize =
                new Size(
                    IconsPerRow * (IconButtonSize + 4) + 32,
                    300);

            IconKey =
                existing != null && !string.IsNullOrWhiteSpace(existing.IconKey)
                    ? existing.IconKey
                    : LibraryGroupIcons.DefaultKey;

            IconPath =
                existing != null
                    ? existing.IconPath
                    : null;

            //----------------------------------------------------------
            // Name
            //----------------------------------------------------------

            LabelControl nameLabel =
                new LabelControl();

            nameLabel.Text = "Group name";
            nameLabel.Location = new Point(16, 16);
            nameLabel.AutoSizeMode = LabelAutoSizeMode.None;
            nameLabel.Size = new Size(100, 20);

            nameEdit =
                new TextEdit();

            nameEdit.Location = new Point(16, 38);
            nameEdit.Size = new Size(ClientSize.Width - 32, 24);

            nameEdit.Text =
                existing != null
                    ? existing.Name
                    : string.Empty;

            //----------------------------------------------------------
            // Icons
            //----------------------------------------------------------

            LabelControl iconLabel =
                new LabelControl();

            iconLabel.Text = "Icon";
            iconLabel.Location = new Point(16, 74);
            iconLabel.AutoSizeMode = LabelAutoSizeMode.None;
            iconLabel.Size = new Size(100, 20);

            iconPanel =
                new FlowLayoutPanel();

            iconPanel.Location = new Point(16, 96);

            iconPanel.Size =
                new Size(
                    ClientSize.Width - 32,
                    150);

            iconPanel.AutoScroll = true;

            foreach (string key in LibraryGroupIcons.Keys)
            {
                CheckButton button =
                    CreateIconButton();

                button.ImageOptions.SvgImage =
                    LibraryGroupIcons.GetSvgImage(key);

                button.Tag = key;

                button.Checked =
                    string.IsNullOrWhiteSpace(IconPath) &&
                    key == IconKey;

                button.CheckedChanged += BuiltInIcon_CheckedChanged;

                iconPanel.Controls.Add(button);
            }

            if (!string.IsNullOrWhiteSpace(IconPath))
                ShowCustomIcon(IconPath);

            //----------------------------------------------------------
            // Buttons
            //----------------------------------------------------------

            SimpleButton browseButton =
                new SimpleButton();

            browseButton.Text = "Browse...";
            browseButton.Location = new Point(16, 258);
            browseButton.Size = new Size(90, 26);
            browseButton.Click += BrowseButton_Click;

            SimpleButton okButton =
                new SimpleButton();

            okButton.Text = "OK";

            okButton.Location =
                new Point(ClientSize.Width - 196, 258);

            okButton.Size = new Size(90, 26);
            okButton.Click += OkButton_Click;

            SimpleButton cancelButton =
                new SimpleButton();

            cancelButton.Text = "Cancel";

            cancelButton.Location =
                new Point(ClientSize.Width - 100, 258);

            cancelButton.Size = new Size(90, 26);
            cancelButton.DialogResult = DialogResult.Cancel;

            Controls.Add(nameLabel);
            Controls.Add(nameEdit);
            Controls.Add(iconLabel);
            Controls.Add(iconPanel);
            Controls.Add(browseButton);
            Controls.Add(okButton);
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        private CheckButton CreateIconButton()
        {
            CheckButton button =
                new CheckButton();

            button.Size =
                new Size(
                    IconButtonSize,
                    IconButtonSize);

            button.Margin = new Padding(2);

            // One icon at a time, and never none.
            button.GroupIndex = 1;

            button.AllowAllUnchecked = false;

            button.AllowFocus = false;

            button.ImageOptions.SvgImageSize =
                new Size(20, 20);

            return button;
        }

        private void BuiltInIcon_CheckedChanged(
            object sender,
            EventArgs e)
        {
            CheckButton button =
                (CheckButton)sender;

            if (!button.Checked)
                return;

            IconKey = button.Tag as string;

            // A built in icon replaces any custom one.
            IconPath = null;
        }

        private void CustomIcon_CheckedChanged(
            object sender,
            EventArgs e)
        {
            CheckButton button =
                (CheckButton)sender;

            if (!button.Checked)
                return;

            IconPath = button.Tag as string;
        }

        private void BrowseButton_Click(
            object sender,
            EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Choose a group icon";

                dialog.Filter =
                    "Images (*.png;*.ico;*.jpg;*.jpeg;*.bmp)|" +
                    "*.png;*.ico;*.jpg;*.jpeg;*.bmp";

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                if (LibraryGroupIcons.GetCustomImage(
                    dialog.FileName) == null)
                {
                    XtraMessageBox.Show(
                        this,
                        "That image could not be loaded.",
                        "Group Icon",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                ShowCustomIcon(dialog.FileName);

                IconPath = dialog.FileName;
            }
        }

        /// <summary>
        /// Adds the browsed icon to the front of the grid as the
        /// selected option, replacing any previously browsed one.
        /// </summary>
        private void ShowCustomIcon(
            string path)
        {
            if (customButton != null)
            {
                iconPanel.Controls.Remove(customButton);

                customButton.Dispose();
            }

            customButton =
                CreateIconButton();

            // Sized for the picker button, not for the accordion.
            customButton.ImageOptions.Image =
                LibraryGroupIcons.GetCustomImage(
                    path,
                    new Size(20, 20));

            customButton.Tag = path;

            customButton.CheckedChanged += CustomIcon_CheckedChanged;

            iconPanel.Controls.Add(customButton);

            iconPanel.Controls.SetChildIndex(
                customButton,
                0);

            customButton.Checked = true;
        }

        private void OkButton_Click(
            object sender,
            EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(nameEdit.Text))
            {
                XtraMessageBox.Show(
                    this,
                    "Enter a name for the group.",
                    "Group Name",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                nameEdit.Focus();

                return;
            }

            DialogResult =
                DialogResult.OK;
        }
    }
}
