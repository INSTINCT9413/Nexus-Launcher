using Nexus.Plugin;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;

namespace Nexus.Plugin.DiscordPresence
{
    //==================================================================
    // Shows what you are doing on your Discord profile: the game you
    // are playing, or that you are browsing your library.
    //
    // Talks to the Discord client over its local named pipe. No
    // library and no NuGet package: the protocol is a four byte
    // opcode, a four byte length and a JSON payload, which is less
    // code than taking a dependency would be.
    //
    // Discord shows nothing for an application it does not know, so
    // this needs Nexus's own application id. That is not something to
    // ask a user for: it identifies Nexus Launcher, not them, and a
    // wrong one simply makes the plugin look broken.
    //
    // It is fetched from the Nexus site so it can be corrected
    // without shipping a build, cached so it keeps working offline,
    // and falls back to the id compiled in below.
    //==================================================================

    [NexusPlugin(
        "nexus.discordpresence",
        "Discord Rich Presence",
        Author = "Nexus Launcher",
        Version = "1.1.0",
        Description = "Shows the game you are playing, or that you " +
                      "are browsing your library, on your Discord " +
                      "profile.")]
    public class DiscordPresencePlugin : INexusPlugin, IPluginSettings
    {
        private const string KeyIdle = "showWhileBrowsing";
        private const string KeyTemplate = "playingTemplate";
        private const string KeyState = "stateLine";

        private const string DefaultTemplate = "Playing {game}";
        private const string DefaultState = "from Nexus Launcher";
        private const string DefaultIdle = "Browsing the library";

        private INexusHost host;
        private DiscordPipe pipe;
        private ApplicationId appId;

        public void Initialize(
            INexusHost host)
        {
            this.host = host;

            pipe = new DiscordPipe(host);

            appId = new ApplicationId(host);

            // Off the startup thread: a web request must never be
            // something Nexus waits on to finish loading.
            System.Threading.Tasks.Task.Run(() =>
            {
                appId.Refresh();

                ShowIdle();
            });

            host.GameLaunched += (s, e) => ShowGame(e.Game);

            host.GameExited += (s, e) => ShowIdle();

            // Something to show straight away, so the plugin is
            // visibly doing its job before a game is ever started.
            ShowIdle();
        }

        public void Shutdown()
        {
            try
            {
                if (pipe != null)
                {
                    pipe.ClearActivity();

                    pipe.Dispose();
                }
            }
            catch (Exception ex)
            {
                host.Log.Info("could not clear presence: " + ex.Message);
            }
        }

        //--------------------------------------------------------------
        // Settings
        //--------------------------------------------------------------

        public IEnumerable<PluginSetting> GetSettings()
        {
            return new[]
            {
                new PluginSetting
                {
                    Key = KeyTemplate,
                    Label = "What to show while playing",
                    Kind = PluginSettingKind.Text,
                    Default = DefaultTemplate,
                    Description = "{game} is replaced with the game's name."
                },
                new PluginSetting
                {
                    Key = KeyState,
                    Label = "Second line",
                    Kind = PluginSettingKind.Text,
                    Default = DefaultState,
                    Description =
                        "The smaller line underneath. {source} is replaced " +
                        "with the launcher the game came from."
                },
                new PluginSetting
                {
                    Key = KeyIdle,
                    Label = "Show something while I am just browsing",
                    Kind = PluginSettingKind.Toggle,
                    Default = "true",
                    Description =
                        "Shows \"" + DefaultIdle + "\" when Nexus is open " +
                        "but no game is running."
                }
            };
        }

        public void SettingsChanged(
            IDictionary<string, string> values)
        {
            ShowIdle();
        }

        //--------------------------------------------------------------
        // What to show
        //--------------------------------------------------------------

        private void ShowGame(
            GameRef game)
        {
            if (game == null)
                return;

            string details = host
                .GetSetting(KeyTemplate, DefaultTemplate)
                .Replace("{game}", game.Name ?? "a game");

            string state = host
                .GetSetting(KeyState, DefaultState)
                .Replace("{source}", game.SourceId ?? "Nexus");

            Push(details, state);
        }

        private void ShowIdle()
        {
            if (!host.GetSetting(KeyIdle, true))
            {
                Push(null, null);

                return;
            }

            Push(DefaultIdle, "from Nexus Launcher");
        }

        private void Push(
            string details,
            string state)
        {
            string id = appId.Value;

            if (string.IsNullOrWhiteSpace(id))
            {
                host.Log.Info(
                    "no Discord application id available yet, so " +
                    "nothing is shown");

                return;
            }

            try
            {
                if (details == null)
                    pipe.ClearActivity();
                else
                    pipe.SetActivity(id, details, state);
            }
            catch (Exception ex)
            {
                // Discord being closed is the normal case, not a
                // fault, so this never reaches the user.
                host.Log.Info("could not set presence: " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Works out which Discord application to appear as.
    ///
    /// The site wins, so the id can be corrected without a release;
    /// a cached copy covers being offline; and the compiled in value
    /// is the last resort. Nothing here involves the user.
    /// </summary>
    internal class ApplicationId
    {
        /// <summary>
        /// Nexus Launcher's own Discord application id. Fill this in
        /// to make the plugin work without the site being reachable.
        /// </summary>
        private const string Compiled = "";

        private const string Url =
            "https://nexuspowered.com/Nexus/discord.json";

        private readonly INexusHost host;

        private string resolved;

        public ApplicationId(
            INexusHost host)
        {
            this.host = host;

            resolved = Cached() ?? Compiled;
        }

        public string Value
        {
            get { return resolved; }
        }

        private string CacheFile
        {
            get
            {
                return System.IO.Path.Combine(
                    host.DataFolder, "application-id.txt");
            }
        }

        private string Cached()
        {
            try
            {
                return System.IO.File.Exists(CacheFile)
                    ? System.IO.File.ReadAllText(CacheFile).Trim()
                    : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Asks the site. Quiet about failure: being offline is
        /// normal and the cached or compiled id still works.
        /// </summary>
        public void Refresh()
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol |=
                    System.Net.SecurityProtocolType.Tls12;

                using (System.Net.WebClient client =
                    new System.Net.WebClient())
                {
                    client.Headers.Add("User-Agent", "NexusLauncher");

                    string body = client.DownloadString(Url);

                    string id = Read(body, "applicationId");

                    if (string.IsNullOrWhiteSpace(id))
                        return;

                    resolved = id;

                    try
                    {
                        System.IO.File.WriteAllText(CacheFile, id);
                    }
                    catch
                    {
                    }

                    host.Log.Info("using Discord application id from the site");
                }
            }
            catch (Exception ex)
            {
                host.Log.Info(
                    "could not fetch the Discord application id: " +
                    ex.Message);
            }
        }

        /// <summary>
        /// Pulls one string value out of a small flat JSON object.
        /// A whole parser would be a dependency for one field.
        /// </summary>
        private static string Read(
            string json,
            string key)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            int at = json.IndexOf("\"" + key + "\"",
                StringComparison.OrdinalIgnoreCase);

            if (at < 0)
                return null;

            int colon = json.IndexOf(':', at);

            if (colon < 0)
                return null;

            int open = json.IndexOf('"', colon);

            if (open < 0)
                return null;

            int close = json.IndexOf('"', open + 1);

            return close < 0
                ? null
                : json.Substring(open + 1, close - open - 1).Trim();
        }
    }

    /// <summary>
    /// The Discord local IPC protocol, as much of it as presence
    /// needs.
    /// </summary>
    internal class DiscordPipe : IDisposable
    {
        private const int OpHandshake = 0;
        private const int OpFrame = 1;
        private const int OpClose = 2;

        private readonly INexusHost host;

        private NamedPipeClientStream stream;
        private string handshakeId;
        private int nonce;

        public DiscordPipe(
            INexusHost host)
        {
            this.host = host;
        }

        public void Reconnect()
        {
            Close();
        }

        /// <summary>
        /// Discord numbers its pipes and which one answers depends on
        /// how many clients are running, so each is tried in turn.
        ///
        /// The handshake is tied to the application id, so changing
        /// the id means starting the connection again.
        /// </summary>
        private bool Connect(
            string applicationId)
        {
            if (stream != null && stream.IsConnected &&
                handshakeId == applicationId)
            {
                return true;
            }

            Close();

            for (int i = 0; i < 10; i++)
            {
                NamedPipeClientStream candidate = null;

                try
                {
                    candidate = new NamedPipeClientStream(
                        ".",
                        "discord-ipc-" + i,
                        PipeDirection.InOut,
                        PipeOptions.None);

                    // A short wait: a pipe that is not there is the
                    // normal case and must not hold anything up.
                    candidate.Connect(200);

                    stream = candidate;

                    Send(OpHandshake,
                        "{\"v\":1,\"client_id\":\"" + applicationId + "\"}");

                    handshakeId = applicationId;

                    host.Log.Info("connected to discord-ipc-" + i);

                    return true;
                }
                catch
                {
                    if (candidate != null)
                    {
                        try { candidate.Dispose(); } catch { }
                    }

                    stream = null;
                }
            }

            return false;
        }

        private void Send(
            int opcode,
            string json)
        {
            byte[] payload = Encoding.UTF8.GetBytes(json);

            byte[] frame = new byte[8 + payload.Length];

            Buffer.BlockCopy(BitConverter.GetBytes(opcode), 0, frame, 0, 4);

            Buffer.BlockCopy(
                BitConverter.GetBytes(payload.Length), 0, frame, 4, 4);

            Buffer.BlockCopy(payload, 0, frame, 8, payload.Length);

            stream.Write(frame, 0, frame.Length);

            stream.Flush();
        }

        private static string Escape(
            string value)
        {
            if (value == null)
                return string.Empty;

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", " ")
                .Replace("\r", " ");
        }

        /// <summary>
        /// Discord rejects an activity whose lines are shorter than
        /// two characters, which is a quiet way to get nothing at all.
        /// </summary>
        private static string AtLeastTwo(
            string value,
            string fallback)
        {
            value = (value ?? string.Empty).Trim();

            return value.Length >= 2 ? value : fallback;
        }

        public void SetActivity(
            string applicationId,
            string details,
            string state)
        {
            if (!Connect(applicationId))
                return;

            long start = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string json =
                "{\"cmd\":\"SET_ACTIVITY\",\"args\":{\"pid\":" +
                System.Diagnostics.Process.GetCurrentProcess().Id +
                ",\"activity\":{" +
                "\"details\":\"" +
                Escape(AtLeastTwo(details, "In Nexus Launcher")) + "\"," +
                "\"state\":\"" +
                Escape(AtLeastTwo(state, "from Nexus Launcher")) + "\"," +
                "\"timestamps\":{\"start\":" + start + "}" +
                "}},\"nonce\":\"" + (++nonce) + "\"}";

            Send(OpFrame, json);
        }

        public void ClearActivity()
        {
            if (stream == null || !stream.IsConnected)
                return;

            string json =
                "{\"cmd\":\"SET_ACTIVITY\",\"args\":{\"pid\":" +
                System.Diagnostics.Process.GetCurrentProcess().Id +
                "},\"nonce\":\"" + (++nonce) + "\"}";

            Send(OpFrame, json);
        }

        private void Close()
        {
            try
            {
                if (stream != null)
                {
                    if (stream.IsConnected)
                        Send(OpClose, "{}");

                    stream.Dispose();
                }
            }
            catch
            {
            }
            finally
            {
                stream = null;

                handshakeId = null;
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
