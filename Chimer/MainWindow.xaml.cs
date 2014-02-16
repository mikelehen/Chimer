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

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Config currentConfig = null;
        private AudioPlaybackEngine engine = null;
        private Dictionary<string, CachedSound> cachedSounds = new Dictionary<string, CachedSound>();
        private ChimeScheduler scheduler = null;

        public MainWindow()
        {
            InitializeComponent();

            this.Closed += (s, e) => engine.Dispose();
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
                txtConfig.Text = currentConfig.RawText;
                UpdateStatus("Successfully loaded " + ConfigHelper.ConfigFile);
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
                cachedSounds[kvp.Key] = new CachedSound(kvp.Value);
            }

            if (scheduler != null)
            {
                scheduler.Dispose();
                scheduler = null;
            }
            scheduler = new ChimeScheduler(config);
            scheduler.Chime += (s, scheduledChime) => playChime(scheduledChime);

            scheduleDataGrid.ItemsSource = scheduler.UpcomingChimes;
        }

        private void playChime(ScheduledChime chime)
        {
            engine.PlaySound(cachedSounds[chime.Sound], currentConfig.zones[chime.Zone]);
            UpdateStatus("Played " + chime.Sound + " for " + chime.Zone + " at " + DateTime.Now.ToString());
        }

        private void btnReload_Click(object sender, RoutedEventArgs e)
        {
            LoadConfig();
        }

        private void UpdateStatus(string text)
        {
            lblStatus.Content = text;
            lblStatus.ToolTip = text;
            txtStatus.Text += DateTime.Now.ToString() + ": " + text + "\n";
        }
    }
}
