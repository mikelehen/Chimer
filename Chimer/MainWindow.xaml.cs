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

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int STATUS_THRESHOLD = 10000000; // characters to keep in the status box.
        private Config currentConfig = null;
        private AudioPlaybackEngine engine = null;
        private Dictionary<string, CachedSound> cachedSounds = new Dictionary<string, CachedSound>();
        private ChimeScheduler scheduler = null;

        public MainWindow()
        {
            InitializeComponent();

            LogStatus("Chimer started.");

            this.Closed += (s, e) =>
            {
                if (engine != null)
                {
                    engine.Dispose();
                }
            };
            this.Loaded += (s, e) =>
            {
                txtConfigFile.Text = ConfigHelper.ConfigFile;
                ConfigHelper.InitializeIfNecessary();
                LoadConfig();
            };
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            Edit();
        }

        private void Edit()
        {
            Process p = Process.Start("notepad.exe", ConfigHelper.ConfigFile);
        }

        private void LoadConfig()
        {
            try
            {
                Config newConfig = ConfigHelper.Load();
                InitializeWithConfig(newConfig);

                currentConfig = newConfig;
                LogStatus("Successfully loaded " + ConfigHelper.ConfigFile);
            }
            catch (Exception e)
            {
                // Try to get back to a good state.
                if (currentConfig != null)
                {
                    InitializeWithConfig(currentConfig);
                }

                string text = e.Message + "\n\n\nDebug Info:\n" + e.ToString();
                ConfigFileErrorDialog d = new ConfigFileErrorDialog(text);
                if (currentConfig == null)
                {
                    d.HideRevert = true;
                }

                d.EditClicked += Edit;
                d.RevertClicked += () => File.WriteAllText(ConfigHelper.ConfigFile, currentConfig.RawText);
                d.Owner = this;
                d.ShowDialog();
            }
        }

        private void InitializeWithConfig(Config config) {
            if (engine != null) {
                engine.Dispose();
                engine = null;
            }
            engine = new AudioPlaybackEngine(44100, config.channels);

            cachedSounds.Clear();
            foreach (var kvp in config.sounds)
            {
                SoundConfig sndConf = kvp.Value;
                cachedSounds[kvp.Key] = new CachedSound(sndConf.file, sndConf.volume);
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
                ZoneCombo.ItemsSource = config.zones.Keys;
            }
            else
            {
                ZoneCombo.Items.Add("No zones configured.");
            }
            ZoneCombo.SelectedIndex = 0;
        }

        private void playChime(string zone, string sound)
        {
            engine.PlaySound(cachedSounds[sound], currentConfig.zones[zone]);
            LogStatus("Played " + sound + " for " + zone + " at " + DateTime.Now.ToString());
        }

        private void btnReload_Click(object sender, RoutedEventArgs e)
        {
            LoadConfig();
        }

        private void LogStatus(string text)
        {
            string message = DateTime.Now.ToString() + ": " + text + "\n";
            string newStatusText = txtStatus.Text + message;
            if (newStatusText.Length > STATUS_THRESHOLD)
            {
                newStatusText = newStatusText.Substring(newStatusText.Length - STATUS_THRESHOLD);
            }
            txtStatus.Text = newStatusText;
            txtStatus.ScrollToEnd();
        }

        private void PlayChime_Click(object sender, RoutedEventArgs e)
        {
            string zone = ZoneCombo.SelectedValue.ToString();
            string sound = SoundCombo.SelectedValue.ToString();
            if (currentConfig.zones.ContainsKey(zone) && currentConfig.sounds.ContainsKey(sound))
            {
                playChime(zone, sound);
            }
        }
    }
}
