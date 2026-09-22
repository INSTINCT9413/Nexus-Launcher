using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Models;
using Nexus_Launcher.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexus_Launcher.Forms
{
    /// <summary>
    /// Downloads an update with visible progress, then asks whether to
    /// install now or later.
    ///
    /// The old flow downloaded with no feedback and launched the installer
    /// the moment it finished. This shows size, speed and time left, can
    /// be cancelled, offers Retry when a download fails, and leaves the
    /// install decision to the user.
    ///
    /// The form only downloads. Launching the updater and exiting stay
    /// with the caller, exactly as before, once InstallNow is set.
    /// </summary>
    internal class UpdateDownloadForm : XtraForm
    {
        private const string Icon = "svgimages/icon%20builder/";

        private readonly UpdateInfo update;

        private readonly PictureEdit loader;
        private readonly LabelControl statusIcon;
        private readonly LabelControl titleLabel;
        private readonly LabelControl subtitleLabel;
        private readonly BarMeter meter;
        private readonly LabelControl detailLabel;
        private readonly SimpleButton primaryButton;
        private readonly SimpleButton secondaryButton;

        private readonly System.Windows.Forms.Timer refresh =
            new System.Windows.Forms.Timer();

        /// <summary>
        /// Recent (time, bytes) samples for a steady speed reading.
        /// </summary>
        private readonly Queue<KeyValuePair<DateTime, long>> samples =
            new Queue<KeyValuePair<DateTime, long>>();

        /// <summary>
        /// Which screen the form is on. Drives what the buttons do, rather
        /// than reading it back off the title text.
        /// </summary>
        private enum Stage
        {
            Downloading,
            Ready,
            Failed
        }

        private Stage stage;

        private CancellationTokenSource cancellation;
        private bool downloading;
        private bool closeWhenCancelled;

        // Written by the download, read by the refresh timer.
        private long downloadedBytes;
        private long totalBytes;

        /// <summary>
        /// The finished package, once there is one.
        /// </summary>
        public string PackagePath { get; private set; }

        /// <summary>
        /// True when the user chose to install straight away.
        /// </summary>
        public bool InstallNow { get; private set; }

        /// <param name="existingPackage">
        /// A package already downloaded for this build. When given, the
        /// form opens ready to install instead of downloading again.
        /// </param>
        public UpdateDownloadForm(
            UpdateInfo update,
            string existingPackage)
        {
            this.update = update;

            Text = "Nexus Launcher Update";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            ClientSize = new Size(480, 188);

            loader = new PictureEdit();
            loader.Location = new Point(22, 26);
            loader.Size = new Size(64, 64);
            loader.EditValue = Properties.Resources.nexus_loader_128;
            loader.Properties.SizeMode = PictureSizeMode.Zoom;
            loader.Properties.ShowMenu = false;
            loader.Properties.ReadOnly = true;
            loader.Properties.AllowFocused = false;
            loader.Properties.BorderStyle = BorderStyles.NoBorder;
            loader.Properties.AnimatedImageLoopMode = DevExpress.Utils.AnimatedImageLoopMode.Infinite;
            loader.Properties.Appearance.BackColor = Color.Transparent;
            loader.Properties.Appearance.Options.UseBackColor = true;
            loader.TabStop = false;

            statusIcon = new LabelControl();
            statusIcon.AutoSizeMode = LabelAutoSizeMode.None;
            statusIcon.Location = new Point(22, 26);
            statusIcon.Size = new Size(64, 64);
            statusIcon.ImageOptions.SvgImageSize = new Size(52, 52);
            statusIcon.ImageOptions.Alignment = ContentAlignment.MiddleCenter;
            statusIcon.Visible = false;

            titleLabel = Label(104, 24, 12.5F, FontStyle.Bold);
            subtitleLabel = Label(104, 52, 9F, FontStyle.Regular);

            meter = new BarMeter();
            meter.Location = new Point(104, 82);
            meter.Size = new Size(354, 8);

            detailLabel = Label(104, 98, 8.5F, FontStyle.Regular);
            detailLabel.Size = new Size(354, 34);
            detailLabel.AutoSizeMode = LabelAutoSizeMode.None;
            detailLabel.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            detailLabel.Appearance.Options.UseTextOptions = true;

            primaryButton = new SimpleButton();
            primaryButton.Size = new Size(150, 30);
            primaryButton.Location = new Point(ClientSize.Width - 22 - 150, ClientSize.Height - 22 - 30);
            primaryButton.Click += PrimaryButton_Click;

            secondaryButton = new SimpleButton();
            secondaryButton.Size = new Size(110, 30);
            secondaryButton.Location = new Point(primaryButton.Left - 10 - 110, primaryButton.Top);
            secondaryButton.Click += SecondaryButton_Click;

            Controls.Add(loader);
            Controls.Add(statusIcon);
            Controls.Add(titleLabel);
            Controls.Add(subtitleLabel);
            Controls.Add(meter);
            Controls.Add(detailLabel);
            Controls.Add(secondaryButton);
            Controls.Add(primaryButton);

            subtitleLabel.Text = DescribeUpdate();

            refresh.Interval = 100;
            refresh.Tick += (s, e) => RefreshProgress();

            PackagePath = existingPackage;
        }

        private static LabelControl Label(
            int x,
            int y,
            float size,
            FontStyle style)
        {
            LabelControl label =
                new LabelControl();

            label.Location = new Point(x, y);
            label.AutoSizeMode = LabelAutoSizeMode.Horizontal;
            label.Appearance.Font = ProfileStyle.Font(size, style);
            label.Appearance.Options.UseFont = true;

            return label;
        }

        private string DescribeUpdate()
        {
            string version =
                string.IsNullOrWhiteSpace(update.version)
                    ? string.Empty
                    : update.version + " ";

            return "Nexus Launcher " + version + "(Build " + update.build + ")";
        }

        protected override void OnShown(
            EventArgs e)
        {
            base.OnShown(e);

            ApplyThemeColors();

            if (!string.IsNullOrEmpty(PackagePath))
                ShowReady(alreadyDownloaded: true);
            else
                StartDownload();
        }

        //--------------------------------------------------------------
        // Downloading
        //--------------------------------------------------------------

        private async void StartDownload()
        {
            cancellation = new CancellationTokenSource();
            downloading = true;
            stage = Stage.Downloading;
            downloadedBytes = 0;
            totalBytes = 0;
            samples.Clear();

            SetState(
                "Downloading update",
                null,
                "Connecting...",
                "Cancel",
                null);

            meter.Value = 0;
            meter.Visible = true;

            UpdateDownloader.DownloadProgressChanged += Downloader_Progress;
            refresh.Start();

            try
            {
                PackagePath =
                    await UpdateDownloader.DownloadAsync(
                        update,
                        cancellation.Token);

                downloading = false;
                refresh.Stop();

                if (!IsDisposed)
                    ShowReady(alreadyDownloaded: false);
            }
            catch (OperationCanceledException)
            {
                downloading = false;
                refresh.Stop();

                if (closeWhenCancelled && !IsDisposed)
                    Close();
            }
            catch (Exception ex)
            {
                downloading = false;
                refresh.Stop();
                Program.LogCrash(ex);

                if (!IsDisposed)
                    ShowFailed(ex);
            }
            finally
            {
                UpdateDownloader.DownloadProgressChanged -= Downloader_Progress;
            }
        }

        /// <summary>
        /// Called for every chunk. Only records the numbers; the refresh
        /// timer draws them ten times a second, so the UI is never
        /// touched from here and is not redrawn hundreds of times.
        /// </summary>
        private void Downloader_Progress(
            long downloaded,
            long total)
        {
            Interlocked.Exchange(ref downloadedBytes, downloaded);
            Interlocked.Exchange(ref totalBytes, total);
        }

        private void RefreshProgress()
        {
            long done = Interlocked.Read(ref downloadedBytes);
            long total = Interlocked.Read(ref totalBytes);

            if (done <= 0)
                return;

            DateTime now = DateTime.UtcNow;

            samples.Enqueue(new KeyValuePair<DateTime, long>(now, done));

            // Two seconds of history smooths out bursty chunks without
            // lagging behind a real change in speed.
            while (samples.Count > 2 && (now - samples.Peek().Key).TotalSeconds > 2)
                samples.Dequeue();

            KeyValuePair<DateTime, long> oldest = samples.Peek();

            double seconds = (now - oldest.Key).TotalSeconds;

            double speed =
                seconds > 0.2
                    ? (done - oldest.Value) / seconds
                    : 0;

            string text;

            if (total > 0)
            {
                meter.Value = (double)done / total;

                text =
                    UpdateDownloader.FormatSize(done) + " of " +
                    UpdateDownloader.FormatSize(total);

                if (speed > 0)
                {
                    text +=
                        "   ·   " + UpdateDownloader.FormatSize((long)speed) + "/s" +
                        "   ·   " + FormatRemaining((total - done) / speed);
                }
            }
            else
            {
                // The server did not say how big the file is, so there is
                // no percentage to show, only how much has arrived.
                text = UpdateDownloader.FormatSize(done) + " downloaded";

                if (speed > 0)
                    text += "   ·   " + UpdateDownloader.FormatSize((long)speed) + "/s";
            }

            detailLabel.Text = text;
        }

        private static string FormatRemaining(
            double seconds)
        {
            if (double.IsInfinity(seconds) || double.IsNaN(seconds))
                return string.Empty;

            if (seconds < 5)
                return "almost done";

            if (seconds < 60)
                return "about " + (int)Math.Ceiling(seconds) + " s left";

            return "about " + (int)Math.Ceiling(seconds / 60) + " min left";
        }

        //--------------------------------------------------------------
        // States
        //--------------------------------------------------------------

        private void ShowReady(
            bool alreadyDownloaded)
        {
            stage = Stage.Ready;
            meter.Value = 1;

            SetState(
                "Update ready to install",
                Icon + "actions_checkcircled.svg",
                alreadyDownloaded
                    ? "This update was downloaded earlier. Nexus Launcher will close while it installs, then you can open it again."
                    : "Download complete. Nexus Launcher will close while it installs, then you can open it again.",
                "Install and restart",
                "Install later");
        }

        private void ShowFailed(
            Exception error)
        {
            stage = Stage.Failed;
            primaryButton.Enabled = true;
            meter.Visible = false;

            SetState(
                "Download failed",
                Icon + "security_warningcircled1.svg",
                DescribeError(error),
                "Retry",
                "Close");
        }

        /// <summary>
        /// A readable reason, rather than an exception type name.
        /// </summary>
        private static string DescribeError(
            Exception error)
        {
            if (error is UnauthorizedAccessException)
            {
                return "Nexus Launcher is not allowed to save the update in its folder. " +
                    "Try running it as administrator.";
            }

            if (error is System.Net.Http.HttpRequestException)
                return "Could not reach the update server. Check your connection and try again.";

            return error.Message;
        }

        private void SetState(
            string title,
            string iconKey,
            string detail,
            string primary,
            string secondary)
        {
            titleLabel.Text = title;
            detailLabel.Text = detail;

            // The spinning logo means work is happening; a static icon
            // replaces it once there is an outcome.
            bool busy = iconKey == null;

            loader.Visible = busy;
            statusIcon.Visible = !busy;

            if (busy)
            {
                loader.StartAnimation();
            }
            else
            {
                loader.StopAnimation();
                statusIcon.ImageOptions.SvgImage = ProfileStyle.Svg(iconKey);
            }

            primaryButton.Text = primary;

            secondaryButton.Text = secondary ?? string.Empty;
            secondaryButton.Visible = secondary != null;

            if (busy)
            {
                // While downloading the only button is Cancel. It answers
                // Esc and the mouse but not Enter, so a stray Enter cannot
                // abort the download. Clearing AcceptButton alone was not
                // enough: as the only button it took focus, and WinForms
                // presses the focused button on Enter.
                AcceptButton = null;
                CancelButton = primaryButton;

                primaryButton.TabStop = false;
                ActiveControl = null;
            }
            else
            {
                // Enter runs the main action (Install / Retry) and Esc the
                // other (Install later / Close). Focus moves to the main
                // button so Enter reaches it rather than whichever button
                // happened to get focus first.
                AcceptButton = primaryButton;
                CancelButton = secondary != null ? secondaryButton : primaryButton;

                primaryButton.TabStop = true;

                if (IsHandleCreated)
                    primaryButton.Focus();
            }
        }

        private void ApplyThemeColors()
        {
            subtitleLabel.Appearance.ForeColor = ProfileStyle.MutedTextColor;
            subtitleLabel.Appearance.Options.UseForeColor = true;

            detailLabel.Appearance.ForeColor = ProfileStyle.MutedTextColor;
            detailLabel.Appearance.Options.UseForeColor = true;

            statusIcon.ImageOptions.SvgImageColorizationMode =
                DevExpress.Utils.SvgImageColorizationMode.CommonPalette;
        }

        //--------------------------------------------------------------
        // Buttons
        //--------------------------------------------------------------

        private void PrimaryButton_Click(
            object sender,
            EventArgs e)
        {
            if (downloading)
            {
                CancelDownload();
                return;
            }

            if (stage == Stage.Ready)
            {
                InstallNow = true;
                DialogResult = DialogResult.OK;
                return;
            }

            // Failed: try again.
            StartDownload();
        }

        private void SecondaryButton_Click(
            object sender,
            EventArgs e)
        {
            // Install later keeps the package so the next check can offer
            // it straight away instead of downloading it again.
            if (stage == Stage.Ready)
                UpdateDownloader.SavePending(update, PackagePath);

            DialogResult = DialogResult.Cancel;
        }

        private void CancelDownload()
        {
            closeWhenCancelled = true;
            primaryButton.Enabled = false;
            detailLabel.Text = "Cancelling...";

            cancellation?.Cancel();
        }

        protected override void OnFormClosing(
            FormClosingEventArgs e)
        {
            // Closing mid download cancels it; the form then closes once
            // the download has actually stopped and cleaned up.
            if (downloading)
            {
                e.Cancel = true;
                CancelDownload();
                return;
            }

            base.OnFormClosing(e);
        }

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                refresh.Stop();
                refresh.Dispose();
                cancellation?.Dispose();
                UpdateDownloader.DownloadProgressChanged -= Downloader_Progress;
            }

            base.Dispose(disposing);
        }
    }
}
