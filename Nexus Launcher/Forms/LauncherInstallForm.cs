using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexus_Launcher.Forms
{
    /// <summary>
    /// Downloads a launcher's installer and hands it to Windows to run.
    ///
    /// Deliberately the same shape as UpdateDownloadForm: size, speed
    /// and time left, a cancel that leaves nothing behind, and Retry
    /// when it fails. Setup used to open the publisher's web page in a
    /// browser and leave the user to it.
    ///
    /// Nexus only downloads and starts the installer. It never installs
    /// silently: the publisher's own installer runs, with its own
    /// prompts and its own elevation, which is also why the file is
    /// started through the shell rather than with a forged manifest.
    /// </summary>
    internal class LauncherInstallForm : XtraForm
    {
        private readonly string launcherName;
        private readonly string url;
        private readonly string fileName;

        private readonly PictureEdit loader;
        private readonly LabelControl titleLabel;
        private readonly LabelControl subtitleLabel;
        private readonly BarMeter meter;
        private readonly LabelControl detailLabel;
        private readonly SimpleButton primaryButton;
        private readonly SimpleButton secondaryButton;

        private readonly System.Windows.Forms.Timer refresh =
            new System.Windows.Forms.Timer();

        /// <summary>
        /// Recent (time, bytes) samples, for a speed that does not
        /// jump about.
        /// </summary>
        private readonly Queue<KeyValuePair<DateTime, long>> samples =
            new Queue<KeyValuePair<DateTime, long>>();

        private enum Stage
        {
            Downloading,
            Ready,
            Failed
        }

        private Stage stage;

        private CancellationTokenSource cancellation;
        private bool downloading;
        private string installerPath;
        private string failure;

        // Written by the download, read by the timer.
        private long downloadedBytes;
        private long totalBytes;

        /// <summary>True once the installer has been started.</summary>
        public bool Started { get; private set; }

        private static string Folder
        {
            get
            {
                return Path.Combine(
                    Path.GetTempPath(),
                    "NexusLauncher",
                    "installers");
            }
        }

        public LauncherInstallForm(
            string launcherName,
            string url,
            string fileName)
        {
            this.launcherName = launcherName;
            this.url = url;
            this.fileName = fileName;

            Text = "Install " + launcherName;
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
            loader.Properties.AnimatedImageLoopMode =
                DevExpress.Utils.AnimatedImageLoopMode.Infinite;
            loader.Properties.Appearance.BackColor = Color.Transparent;
            loader.Properties.Appearance.Options.UseBackColor = true;
            loader.TabStop = false;

            titleLabel = Label(104, 24, 12.5F, FontStyle.Bold);
            subtitleLabel = Label(104, 52, 9F, FontStyle.Regular);

            meter = new BarMeter();
            meter.Location = new Point(104, 82);
            meter.Size = new Size(354, 8);

            detailLabel = Label(104, 98, 8.5F, FontStyle.Regular);

            primaryButton = new SimpleButton();
            primaryButton.Size = new Size(120, 30);
            primaryButton.Location = new Point(338, 140);
            primaryButton.Click += PrimaryButton_Click;

            secondaryButton = new SimpleButton();
            secondaryButton.Size = new Size(100, 30);
            secondaryButton.Location = new Point(228, 140);
            secondaryButton.Click += SecondaryButton_Click;

            Controls.AddRange(new Control[]
            {
                loader, titleLabel, subtitleLabel, meter,
                detailLabel, primaryButton, secondaryButton
            });

            ApplyThemeColors();

            refresh.Interval = 250;
            refresh.Tick += (s, e) => RefreshProgress();
        }

        private LabelControl Label(
            int x,
            int y,
            float size,
            FontStyle style)
        {
            LabelControl label = new LabelControl();
            label.Location = new Point(x, y);
            label.AutoSizeMode = LabelAutoSizeMode.None;
            label.Size = new Size(354, 20);
            label.Appearance.Font = new Font(Font.FontFamily, size, style);
            label.Appearance.Options.UseFont = true;
            return label;
        }

        private void ApplyThemeColors()
        {
            subtitleLabel.Appearance.ForeColor = ProfileStyle.MutedTextColor;
            subtitleLabel.Appearance.Options.UseForeColor = true;

            detailLabel.Appearance.ForeColor = ProfileStyle.MutedTextColor;
            detailLabel.Appearance.Options.UseForeColor = true;
        }

        protected override void OnShown(
            EventArgs e)
        {
            base.OnShown(e);

            Start();
        }

        //--------------------------------------------------------------
        // Downloading
        //--------------------------------------------------------------

        private async void Start()
        {
            SetStage(Stage.Downloading);

            downloading = true;
            downloadedBytes = 0;
            totalBytes = 0;
            samples.Clear();
            refresh.Start();

            cancellation = new CancellationTokenSource();

            try
            {
                installerPath =
                    await DownloadAsync(cancellation.Token);

                if (IsDisposed)
                    return;

                SetStage(Stage.Ready);
            }
            catch (OperationCanceledException)
            {
                if (!IsDisposed)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                failure = ex.Message;

                if (!IsDisposed)
                    SetStage(Stage.Failed);
            }
            finally
            {
                downloading = false;
                refresh.Stop();
            }
        }

        /// <summary>
        /// Straight to a .part file, renamed only once the whole thing
        /// has arrived, so a dropped connection can never leave a
        /// truncated installer looking like a finished one.
        /// </summary>
        private async Task<string> DownloadAsync(
            CancellationToken token)
        {
            Directory.CreateDirectory(Folder);

            string destination = Path.Combine(Folder, fileName);
            string partial = destination + ".part";

            // Some of these are on hosts that still need it spelled out.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("NexusLauncher");

                using (HttpResponseMessage response =
                    await client.GetAsync(
                        url,
                        HttpCompletionOption.ResponseHeadersRead,
                        token))
                {
                    response.EnsureSuccessStatusCode();

                    // GOG sends the installer chunked, with no length.
                    // That is legitimate; it only means the bar cannot
                    // show a percentage.
                    totalBytes = response.Content.Headers.ContentLength ?? 0;

                    using (Stream input =
                        await response.Content.ReadAsStreamAsync())
                    using (FileStream output =
                        new FileStream(
                            partial,
                            FileMode.Create,
                            FileAccess.Write,
                            FileShare.None))
                    {
                        byte[] buffer = new byte[81920];
                        int read;

                        while ((read = await input.ReadAsync(
                                   buffer, 0, buffer.Length, token)) > 0)
                        {
                            await output.WriteAsync(buffer, 0, read, token);

                            downloadedBytes += read;
                        }
                    }

                    if (totalBytes > 0 && downloadedBytes != totalBytes)
                    {
                        TryDelete(partial);

                        throw new IOException(
                            "The download ended early (" +
                            UpdateDownloader.FormatSize(downloadedBytes) +
                            " of " +
                            UpdateDownloader.FormatSize(totalBytes) + ").");
                    }
                }
            }

            if (File.Exists(destination))
                File.Delete(destination);

            File.Move(partial, destination);

            return destination;
        }

        private static void TryDelete(
            string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        //--------------------------------------------------------------
        // Progress
        //--------------------------------------------------------------

        private void RefreshProgress()
        {
            long done = Interlocked.Read(ref downloadedBytes);
            long total = totalBytes;

            DateTime now = DateTime.UtcNow;
            samples.Enqueue(new KeyValuePair<DateTime, long>(now, done));

            while (samples.Count > 0 &&
                   (now - samples.Peek().Key).TotalSeconds > 5)
            {
                samples.Dequeue();
            }

            double speed = 0;

            if (samples.Count > 1)
            {
                KeyValuePair<DateTime, long> first = samples.Peek();

                double seconds = (now - first.Key).TotalSeconds;

                if (seconds > 0.5)
                    speed = (done - first.Value) / seconds;
            }

            if (total > 0)
            {
                meter.Value = (double)done / total;

                string text =
                    UpdateDownloader.FormatSize(done) + " of " +
                    UpdateDownloader.FormatSize(total);

                if (speed > 0)
                {
                    text += "   ·   " +
                        UpdateDownloader.FormatSize((long)speed) + "/s";

                    double left = (total - done) / speed;

                    if (left > 1 && left < 86400)
                        text += "   ·   " + FormatTime(left) + " left";
                }

                detailLabel.Text = text;
            }
            else
            {
                // No total to divide by, so the bar creeps rather than
                // claiming a percentage it cannot know.
                meter.Value = Math.Min(0.95, done / (double)(24 * 1024 * 1024));

                detailLabel.Text =
                    UpdateDownloader.FormatSize(done) + " downloaded" +
                    (speed > 0
                        ? "   ·   " + UpdateDownloader.FormatSize((long)speed) + "/s"
                        : string.Empty);
            }
        }

        private static string FormatTime(
            double seconds)
        {
            TimeSpan span = TimeSpan.FromSeconds(seconds);

            if (span.TotalMinutes < 1)
                return (int)span.TotalSeconds + "s";

            if (span.TotalHours < 1)
                return span.Minutes + "m " + span.Seconds + "s";

            return (int)span.TotalHours + "h " + span.Minutes + "m";
        }

        //--------------------------------------------------------------
        // Stages
        //--------------------------------------------------------------

        private void SetStage(
            Stage next)
        {
            stage = next;

            switch (stage)
            {
                case Stage.Downloading:
                    loader.Visible = true;
                    meter.Visible = true;
                    titleLabel.Text = "Downloading " + launcherName;
                    subtitleLabel.Text =
                        "Getting the installer from the publisher.";
                    detailLabel.Text = "Starting...";
                    primaryButton.Visible = false;
                    secondaryButton.Text = "Cancel";
                    secondaryButton.Visible = true;
                    break;

                case Stage.Ready:
                    loader.Visible = false;
                    meter.Value = 1;
                    titleLabel.Text = launcherName + " is ready to install";
                    subtitleLabel.Text =
                        "The installer will open with its own prompts.";
                    detailLabel.Text = Path.GetFileName(installerPath);
                    primaryButton.Text = "Run installer";
                    primaryButton.Visible = true;
                    secondaryButton.Text = "Later";
                    secondaryButton.Visible = true;
                    AcceptButton = primaryButton;
                    break;

                case Stage.Failed:
                    loader.Visible = false;
                    meter.Visible = false;
                    titleLabel.Text = "Could not download " + launcherName;
                    subtitleLabel.Text = failure ?? "The download failed.";
                    detailLabel.Text = string.Empty;
                    primaryButton.Text = "Retry";
                    primaryButton.Visible = true;
                    secondaryButton.Text = "Close";
                    secondaryButton.Visible = true;
                    break;
            }
        }

        //--------------------------------------------------------------
        // Buttons
        //--------------------------------------------------------------

        private void PrimaryButton_Click(
            object sender,
            EventArgs e)
        {
            if (stage == Stage.Failed)
            {
                Start();

                return;
            }

            RunInstaller();
        }

        /// <summary>
        /// Stands our windows down so the installer can come to the
        /// front.
        ///
        /// The setup wizard is TopMost, which outranks any window the
        /// installer can raise for itself: it opened behind Nexus and
        /// looked like nothing had happened. A window we do not own
        /// cannot be promoted above a topmost window, so the only
        /// honest fix is to stop being topmost while it runs.
        ///
        /// Topmost is restored when the installer exits. If the process
        /// cannot be watched, which is what happens when the shell
        /// hands an .msi off to Windows Installer and gives us nothing
        /// back, it is restored the next time the user comes back to
        /// the window instead. Either way Nexus does not sit in front
        /// of an installer the user is trying to use.
        /// </summary>
        private void StandDownForInstaller(
            Process process)
        {
            List<Form> lowered =
                new List<Form>();

            foreach (Form form in Application.OpenForms)
            {
                if (form.TopMost)
                {
                    form.TopMost = false;

                    lowered.Add(form);
                }
            }

            if (lowered.Count == 0)
                return;

            Action restore = () =>
            {
                foreach (Form form in lowered)
                {
                    try
                    {
                        if (form.IsDisposed || !form.IsHandleCreated)
                            continue;

                        form.BeginInvoke(new Action(() =>
                        {
                            if (!form.IsDisposed)
                                form.TopMost = true;
                        }));
                    }
                    catch (Exception ex)
                    {
                        Program.LogCrash(ex);
                    }
                }
            };

            bool watching = false;

            if (process != null)
            {
                try
                {
                    process.EnableRaisingEvents = true;

                    process.Exited += (s, e) => restore();

                    watching = true;
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                }
            }

            if (watching)
                return;

            // Nothing to watch, so wait for the user to come back.
            foreach (Form form in lowered)
            {
                Form owner = form;

                EventHandler once = null;

                once = (s, e) =>
                {
                    owner.Activated -= once;

                    if (!owner.IsDisposed)
                        owner.TopMost = true;
                };

                owner.Activated += once;
            }
        }

        /// <summary>
        /// Hands the file to the shell, which is what lets an .msi open
        /// with Windows Installer and lets each installer raise its own
        /// elevation prompt.
        /// </summary>
        private void RunInstaller()
        {
            try
            {
                ProcessStartInfo start =
                    new ProcessStartInfo(installerPath);

                start.UseShellExecute = true;

                Process process =
                    Process.Start(start);

                Started = true;

                StandDownForInstaller(process);

                DialogResult = DialogResult.OK;

                Close();
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                failure =
                    "The installer was downloaded but would not start. " +
                    "It is at " + installerPath;

                SetStage(Stage.Failed);
            }
        }

        private void SecondaryButton_Click(
            object sender,
            EventArgs e)
        {
            if (downloading)
            {
                if (cancellation != null)
                    cancellation.Cancel();

                return;
            }

            DialogResult = DialogResult.Cancel;

            Close();
        }

        protected override void OnFormClosing(
            FormClosingEventArgs e)
        {
            if (downloading && cancellation != null)
                cancellation.Cancel();

            base.OnFormClosing(e);
        }
    }
}
