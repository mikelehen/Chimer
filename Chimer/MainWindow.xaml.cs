namespace Chimer
{
    using Audio;
    using Chimer.Scheduler;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Windows;
    using System.Linq;
    using System.Windows.Threading;
    using System.Windows.Data;
    using System.Threading;
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int STATUS_THRESHOLD = 10000000; // characters to keep in the status box.
        private Config currentConfig = null;
        private AudioEngine engine = null;
        private Dictionary<string, CachedSound> cachedSounds = new Dictionary<string, CachedSound>();
        private ChimeScheduler scheduler = null;

        public MainWindow()
        {
            InitializeComponent();

            Logger.MessageLogged += (s, message) =>
            {
                this.Dispatcher.BeginInvoke(new Action(() => this.AddToStatusText(message)), DispatcherPriority.Background);
            };

            txtConfigFile.Text = Paths.ConfigFile;

            Logger.Log("Chimer started.");
            AudioEngine.LogAvailableDevices();

            this.Closed += (s, e) =>
            {
                if (engine != null)
                {
                    engine.Dispose();
                }
            };
            this.Loaded += (s, e) =>
            {
                LoadConfig();
            };
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            Edit();
        }

        private void Edit()
        {
            Process p = Process.Start("notepad.exe", Paths.ConfigFile);
        }

        private void LoadConfig()
        {
            Logger.Log("Loading configuration from " + Paths.ConfigFile);

            // Hack.  Dispatch this so the loading message shows up before we start
            // loading the config, which can take a couple seconds (reading in the audio files).
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate(object parameter)
            {
                LoadConfigInternal();
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
        }

        private void LoadConfigInternal() {
            try
            {
                Config newConfig = ConfigHelper.Load(Paths.ConfigFile);
                InitializeWithConfig(newConfig);

                currentConfig = newConfig;
                Logger.Log("Successfully loaded " + Paths.ConfigFile);
            }
            catch (Exception e)
            {
                Logger.Log("Failed to use configuration: " + e.Message + "\r\n\r\n" + e.ToString());
                // Try to get back to a good state.
                if (currentConfig != null)
                {
                    InitializeWithConfig(currentConfig);
                }

                string text = e.Message + "\r\n\r\n\r\nDebug Info:\r\n" + e.ToString();
                ConfigFileErrorDialog d = new ConfigFileErrorDialog(text);
                if (currentConfig == null)
                {
                    d.HideRevert = true;
                }

                d.EditClicked += Edit;
                d.RevertClicked += () => File.WriteAllText(Paths.ConfigFile, currentConfig.RawText);
                d.Owner = this;
                d.ShowDialog();
            }
        }

        private void InitializeWithConfig(Config config) {
            if (engine != null) {
                engine.Dispose();
                engine = null;
            }
            engine = new AudioEngine(44100, config.inputDevice, config.outputDevice, config.inputLatency, config.outputLatency, config.inputVolume, config.inputThreshold);

            cachedSounds.Clear();
            foreach (var kvp in config.sounds)
            {
                var cachedSound = new CachedSound(kvp.Value);
                cachedSounds[kvp.Key] = cachedSound;
            }

            if (scheduler != null)
            {
                scheduler.Dispose();
                scheduler = null;
            }
            scheduler = new ChimeScheduler(config);
            scheduler.Chime += (s, scheduledChime) => playChime(scheduledChime.Zone, scheduledChime.Sound);

            scheduleDataGrid.ItemsSource = scheduler.UpcomingChimes;

            SoundCombo.ItemsSource = null;
            SoundCombo.Items.Clear();
            if (config.sounds.Count > 0)
            {
                SoundCombo.ItemsSource = config.sounds.Keys;
            }
            else
            {
                SoundCombo.Items.Add("No sounds configured.");
            }
            SoundCombo.SelectedIndex = 0;

            ZoneCombo.ItemsSource = null;
            ZoneCombo.Items.Clear();
            if (config.zones.Count > 0)
            {
                CompositeCollection zoneCompositeCollection = new CompositeCollection();
                zoneCompositeCollection.Add("All");

                CollectionContainer zones = new CollectionContainer();
                zones.Collection = config.zones.Keys;
                zoneCompositeCollection.Add(zones);

                ZoneCombo.ItemsSource = zoneCompositeCollection;
            }
            else
            {
                ZoneCombo.Items.Add("No zones configured.");
            }
            ZoneCombo.SelectedIndex = 0;

            PassThroughCombo.ItemsSource = new string[] { "Off", "Auto", "On" };
            PassThroughCombo.SelectedIndex = 1;

            engine.InputStatusChange += (sender, status) =>
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    lblPassThrough.Content = "Volume: " + status.Volume + (status.Muted ? " (Muted)" : " (Pass Through)");
                }));
            };
        }

        private void playChime(string zone, string sound)
        {
            engine.PlaySound(cachedSounds[sound], currentConfig.zones[zone]);
            Logger.Log("Played '" + sound + "' for '" + zone + "'.");
        }

        private void btnReload_Click(object sender, RoutedEventArgs e)
        {
            LoadConfig();
        }

        private void AddToStatusText(string text)
        {
            string newStatusText = txtStatus.Text + text;
            if (newStatusText.Length > STATUS_THRESHOLD)
            {
                newStatusText = newStatusText.Substring(newStatusText.Length - STATUS_THRESHOLD);
            }
            txtStatus.Text = newStatusText;
            txtStatus.ScrollToEnd();
        }

        private void PlayChime_Click(object sender, RoutedEventArgs e)
        {
            string sound = SoundCombo.SelectedValue.ToString();
            Debug.Assert(currentConfig.sounds.ContainsKey(sound));

            string zone = ZoneCombo.SelectedValue.ToString();
            if (zone == "All")
            {
                foreach (var zoneNum in currentConfig.zones.Keys)
                {
                    playChime(zoneNum, sound);
                }
            }
            else
            {
                Debug.Assert(currentConfig.zones.ContainsKey(zone));
                playChime(zone, sound);
            }
        }

        private void PassThroughCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string value = PassThroughCombo.SelectedValue.ToString();
            if (value == "Off")
            {
                engine.PassThroughMode = PassThroughMode.Off;
            }
            else if (value == "Auto")
            {
                engine.PassThroughMode = PassThroughMode.Auto;
            }
            else
            {
                Debug.Assert(value == "On");
                engine.PassThroughMode = PassThroughMode.On;
            }
        }
    }
}
