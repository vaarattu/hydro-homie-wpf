using AdonisUI.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace HydroHomie
{
    public partial class MainWindow : AdonisWindow
    {
        readonly string settingsFile = "Settings.xml";
        readonly string customTextsFile = "custom_alert_texts.txt";
        readonly string customSoundsFolder = "custom_sounds";
        readonly string[] alertTexts = { "Hydration alert!", "It's time to hydrate!", "Bottoms up!", "Stay hydrated!", "H20 time!", "Drink your favorite water!", "Take a sip!", "Aren't you thristy already?" };

        readonly SoundPlayer soundPlayer;
        readonly MediaPlayer mediaPlayer;
        DispatcherTimer aTimer;

        TimeSpan lastAlert;

        private Settings _settings;

        public MainWindow()
        {
            InitializeComponent();

            soundPlayer = new SoundPlayer();
            mediaPlayer = new MediaPlayer();

            _settings = ReadSettingsFile();

            CreateCustomTextsFile();
            CreateCustomSoundsFolder();

            UpdateUI();

            if (_settings.StartMinimized)
            {
                this.WindowState = WindowState.Minimized;
                this.Visibility = Visibility.Hidden;
                this.WindowState = WindowState.Normal;
                this.Hide();
            }

            SetupTimer();
        }

        private Settings ReadSettingsFile()
        {
            if (!File.Exists(settingsFile))
            {
                WriteSettingsFile(new Settings {
                    EnableAlerts = true,
                    MuteAlerts = false,
                    StartWithWindows = false,
                    StartMinimized = false,
                    UseCustomTexts = false,
                    UseCustomSounds = false,
                    AlertFrequency = 60,
                    AlertDuration = 10
                });
            }

            XmlSerializer reader = new XmlSerializer(typeof(Settings));
            StreamReader file = new StreamReader(settingsFile);
            Settings settings = (Settings)reader.Deserialize(file);
            file.Close();

            return settings;
        }

        private void WriteSettingsFile(Settings settings)
        {
            XmlSerializer writer = new XmlSerializer(typeof(Settings));
            StreamWriter wfile = new StreamWriter(settingsFile);
            writer.Serialize(wfile, settings);
            wfile.Close();
        }

        private void SetupTimer()
        {
            aTimer = new DispatcherTimer();
            aTimer.Tick += new EventHandler(OnTimedEvent);
            aTimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            aTimer.IsEnabled = true;
            aTimer.Start();
        }

        private void OnTimedEvent(object source, EventArgs e)
        {
            if (_settings.AlertFrequency > 0)
            {
                if (this.Visibility == Visibility.Visible)
                {
                    int minutesUntil = (_settings.AlertFrequency - DateTime.Now.Minute % _settings.AlertFrequency);
                    DateTime now = DateTime.Now;
                    TimeSpan timeSpan = new TimeSpan(now.Hour, now.Minute, now.Second);
                    timeSpan = timeSpan.Add(new TimeSpan(0, minutesUntil, 0)) - timeSpan.Add(new TimeSpan(0, 0, now.Second));
                    TimeUntilNextAlertTextBlock.Text = timeSpan.ToString();
                }
                if (_settings.EnableAlerts)
                {
                    DateTime now = DateTime.Now;
                    TimeSpan timeSpan = new TimeSpan(now.Hour, now.Minute, 0);
                    if (DateTime.Now.Minute % _settings.AlertFrequency == 0 && lastAlert != timeSpan)
                    {
                        if (_settings.AlertDuration > 0)
                        {
                            PlayAlert();
                        }
                        lastAlert = timeSpan;
                    }
                }
            }
            else
            {
                TimeUntilNextAlertTextBlock.Text = "INVALID FREQUENCY";
            }
        }

        private void PlayAlert()
        {
            TrayIcon.ShowCustomBalloon(new AlertBalloon(GetNotificationText()), System.Windows.Controls.Primitives.PopupAnimation.Slide, _settings.AlertDuration * 1000);
            if (!_settings.MuteAlerts)
            {
                PlayNotificationSound();
            }
        }

        private void CreateCustomTextsFile()
        {
            if (!File.Exists(customTextsFile))
            {
                File.WriteAllLines(customTextsFile, alertTexts, Encoding.UTF8);
            }
        }

        private void CreateCustomSoundsFolder()
        {
            if (!Directory.Exists(customSoundsFolder))
            {
                Directory.CreateDirectory(customSoundsFolder);
                using (var stream = Properties.Resources.Alert)
                {
                    using (FileStream file = new FileStream(customSoundsFolder + "/Alert.wav", FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(file);
                        stream.Close();
                        file.Close();
                    }
                }
            }
        }

        private string GetNotificationText()
        {
            if (_settings.UseCustomTexts)
            {
                if (File.Exists(customTextsFile))
                {
                    string[] lines = File.ReadAllLines(customTextsFile);
                    if (lines.Length > 0)
                    {
                        return lines[new Random().Next(0, lines.Length - 1)];
                    }
                }
            }

            return "Bottoms up!";
        }

        private void PlayNotificationSound()
        {
            if (_settings.UseCustomSounds)
            {
                if (Directory.Exists(customSoundsFolder))
                {
                    List<string> files = Directory.EnumerateFiles(customSoundsFolder, "*.*", SearchOption.TopDirectoryOnly).Where(file => file.ToLower().EndsWith("mp3") || file.ToLower().EndsWith("wav")).ToList();
                    if (files.Count > 0)
                    {
                        var uri = new Uri(files[new Random().Next(0, files.Count - 1)], UriKind.RelativeOrAbsolute);
                        mediaPlayer.Open(uri);
                        mediaPlayer.Play();
                    }
                }
            }
            else
            {
                using (var stream = Properties.Resources.Alert)
                {
                    soundPlayer.Stream = stream;
                    soundPlayer.Play();
                }
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            switch (RightClickMenu.Items.IndexOf((MenuItem)e.OriginalSource))
            {
                case 0:
                    AlertsToggled(!_settings.EnableAlerts);
                    break;
                case 1:
                    // separator
                    break;
                case 2:
                    ShowWindow();
                    break;
                case 3:
                    // separator
                    break;
                case 4:
                    this.Close();
                    break;
            }
        }

        private void AdonisWindow_Closed(object sender, EventArgs e)
        {
            TrayIcon.CloseBalloon();
            TrayIcon.Visibility = Visibility.Collapsed;
            TrayIcon.Dispose();
        }

        private void AdonisWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Visibility = Visibility.Hidden;
                this.WindowState = WindowState.Normal;
                this.Hide();
            }
        }

        private void TrayIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }

        private void ShowWindow()
        {
            this.Visibility = Visibility.Visible;
            this.Show();
            this.Activate();
            this.Topmost = true;
            this.Topmost = false;
            this.Focus();
        }

        private void AlertsToggled(bool enabled)
        {
            EnableAlertsCheckBox.IsChecked = enabled;
            _settings.EnableAlerts = enabled;
            EnableAlertsMenuItem.Header = _settings.EnableAlerts ? "Disable notifications" : "Enable notifications";
        }

        private void UpdateUI()
        {
            AlertsToggled(_settings.EnableAlerts);
            MuteAlertsCheckBox.IsChecked = _settings.MuteAlerts;

            EnableStartupCheckBox.IsChecked = _settings.StartWithWindows;
            StartMinimizedCheckBox.IsChecked = _settings.StartMinimized;

            CustomSoundCheckBox.IsChecked = _settings.UseCustomSounds;
            CustomTextCheckBox.IsChecked = _settings.UseCustomTexts;

            FrequencySlider.Value = (int)Math.Sqrt(_settings.AlertFrequency);
            FrequencyTextBox.Text = _settings.AlertFrequency.ToString();

            FrequencySlider.Value = (int)Math.Sqrt(_settings.AlertDuration);
            DurationTextBox.Text = _settings.AlertDuration.ToString();
        }

        private void EnableStartupCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                _settings.StartWithWindows = (bool)checkBox.IsChecked;
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (_settings.StartWithWindows)
                    {
                        key.SetValue("HydroHomie", "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"");
                    }
                    else
                    {
                        key.DeleteValue("HydroHomie", false);
                    }
                }
            }
        }

        private void DurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DurationTextBox != null)
            {
                switch ((int)e.NewValue)
                {
                    case 1:
                        _settings.AlertDuration = 0;
                        break;
                    case 2:
                        _settings.AlertDuration = 5;
                        break;
                    case 3:
                        _settings.AlertDuration = 10;
                        break;
                    case 4:
                        _settings.AlertDuration = 15;
                        break;
                    case 5:
                        _settings.AlertDuration = 30;
                        break;
                    case 6:
                        _settings.AlertDuration = 60;
                        break;
                    case 7:
                        _settings.AlertDuration = 120;
                        break;
                }
                DurationTextBox.Text = _settings.AlertDuration.ToString();
            }
        }

        private void FrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (FrequencyTextBox != null)
            {
                switch ((int)e.NewValue)
                {
                    case 1:
                        _settings.AlertFrequency = 5;
                        break;
                    case 2:
                        _settings.AlertFrequency = 15;
                        break;
                    case 3:
                        _settings.AlertFrequency = 30;
                        break;
                    case 4:
                        _settings.AlertFrequency = 45;
                        break;
                    case 5:
                        _settings.AlertFrequency = 60;
                        break;
                    case 6:
                        _settings.AlertFrequency = 120;
                        break;
                    case 7:
                        _settings.AlertFrequency = 240;
                        break;
                }
                FrequencyTextBox.Text = _settings.AlertFrequency.ToString();
            }
        }

        private void DurationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DurationSlider != null)
            {
                if (sender is TextBox textBox)
                {
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        _settings.AlertDuration = (int)double.Parse(textBox.Text);
                    }
                }
            }
        }

        private void FrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (FrequencySlider != null)
            {
                if (sender is TextBox textBox)
                {
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        _settings.AlertFrequency = (int)double.Parse(textBox.Text);
                    }
                }
            }
        }

        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Button_Clicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                switch (button.Name)
                {
                    case "OpenSoundsFolderButton":
                        CreateCustomSoundsFolder();
                        if (Directory.Exists(customSoundsFolder))
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                Arguments = customSoundsFolder,
                                FileName = "explorer.exe"
                            };
                            Process.Start(startInfo);
                        }
                        break;
                    case "OpenTextsFileButton":
                        CreateCustomTextsFile();
                        if (File.Exists(customTextsFile))
                        {
                            Process.Start(customTextsFile);
                        }
                        break;
                    case "TestAlertButton":
                        PlayAlert();
                        break;
                    default:
                        break;
                }
            }
        }

        private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                switch (checkBox.Name)
                {
                    case "EnableAlertsCheckBox":
                        AlertsToggled((bool)checkBox.IsChecked);
                        break;
                    case "MuteAlertsCheckBox":
                        _settings.MuteAlerts = (bool)checkBox.IsChecked;
                        break;
                    case "EnableStartupCheckBox":
                        _settings.StartWithWindows = (bool)checkBox.IsChecked;
                        break;
                    case "StartMinimizedCheckBox":
                        _settings.StartMinimized = (bool)checkBox.IsChecked;
                        break;
                    case "CustomTextCheckBox":
                        _settings.UseCustomTexts = (bool)checkBox.IsChecked;
                        break;
                    case "CustomSoundCheckBox":
                        _settings.UseCustomSounds = (bool)checkBox.IsChecked;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
