using AdonisUI.Controls;
using LiveCharts;
using LiveCharts.Wpf;
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
        readonly string[] alertTexts = { "Hydration alert!", "It's time to hydrate!", "Bottoms up!", "Stay hydrated!", "H20 time!", "Drink your favorite water!", "Take a sip!", "Aren't you thristy already?" };

        readonly SoundPlayer soundPlayer;
        readonly MediaPlayer mediaPlayer;
        DispatcherTimer aTimer;

        TimeSpan lastAlert;

        private readonly Settings _settings;
        private bool allowEvents = false;

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

            SetupChart();
        }

        private void SetupChart()
        {
            double[] values = { 4.52, 3.52, 1.42, 5.72 };

            LvcXAxis.Title = "Date";
            LvcYAxis.Title = "Water consumed (in l)";

            SeriesCollection SeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Values = new ChartValues<double>(values)
                }
            };

            string[] Labels = new[] { "20.5.2021", "21.5.2021", "22.5.2021", "23.5.2021" };
            Func<double, string> Formatter = value => value.ToString();

            LvcChart.Series = SeriesCollection;
            LvcXAxis.Labels = Labels;
            LvcYAxis.LabelFormatter = Formatter;

            AvgAxis.Value = values.Average();
            GoalAxis.Value = values.Average() + 1;
        }

        private string GetSettingsFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xml");
        }

        private string GetCustomTextsFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "custom_alert_texts.txt");
        }

        private string GetCustomSoundsFolderPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "custom_alert_sounds");
        }

        private Settings ReadSettingsFile()
        {
            if (!File.Exists(GetSettingsFilePath()))
            {
                WriteSettingsFile(new Settings {
                    EnableAlerts = true,
                    MuteAlerts = false,
                    StartWithWindows = false,
                    StartMinimized = false,
                    UseCustomTexts = false,
                    UseCustomSounds = false,
                    AlertFrequency = 60,
                    AlertDuration = 10,
                    TrackConsumption = false
                });
            }

            XmlSerializer reader = new XmlSerializer(typeof(Settings));
            StreamReader file = new StreamReader(GetSettingsFilePath());
            Settings settings = (Settings)reader.Deserialize(file);
            file.Close();

            return settings;
        }

        private void WriteSettingsFile(Settings settings)
        {
            XmlSerializer writer = new XmlSerializer(typeof(Settings));
            StreamWriter wfile = new StreamWriter(GetSettingsFilePath());
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
                        PlayAlert();
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
            if (_settings.AlertDuration > 0)
            {
                TrayIcon.ShowCustomBalloon(new AlertBalloon(GetNotificationText(), _settings.TrackConsumption), System.Windows.Controls.Primitives.PopupAnimation.Slide, _settings.AlertDuration * 1000);
            }
            if (!_settings.MuteAlerts)
            {
                PlayNotificationSound();
            }
        }

        private void CreateCustomTextsFile()
        {
            if (!File.Exists(GetCustomTextsFilePath()))
            {
                File.WriteAllLines(GetCustomTextsFilePath(), alertTexts, Encoding.UTF8);
            }
        }

        private void CreateCustomSoundsFolder()
        {
            if (!Directory.Exists(GetCustomSoundsFolderPath()))
            {
                Directory.CreateDirectory(GetCustomSoundsFolderPath());
                using (var stream = Properties.Resources.Alert)
                {
                    using (FileStream file = new FileStream(GetCustomSoundsFolderPath() + "/Alert.wav", FileMode.Create, FileAccess.Write))
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
                if (File.Exists(GetCustomTextsFilePath()))
                {
                    string[] lines = File.ReadAllLines(GetCustomTextsFilePath());
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
                if (Directory.Exists(GetCustomSoundsFolderPath()))
                {
                    List<string> files = Directory.EnumerateFiles(GetCustomSoundsFolderPath(), "*.*", SearchOption.TopDirectoryOnly).Where(file => file.ToLower().EndsWith("mp3") || file.ToLower().EndsWith("wav")).ToList();
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

        private void SetFrequency(int frequency)
        {
            _settings.AlertFrequency = frequency;
            WriteSettingsFile(_settings);
        }

        private void SetDuration(int duration)
        {
            _settings.AlertDuration = duration;
            WriteSettingsFile(_settings);
        }

        private void AlertsToggled(bool enabled)
        {
            EnableAlertsCheckBox.IsChecked = enabled;
            _settings.EnableAlerts = enabled;
            EnableAlertsMenuItem.Header = _settings.EnableAlerts ? "Disable notifications" : "Enable notifications";
        }

        private void UpdateUI()
        {
            allowEvents = false;

            AlertsToggled(_settings.EnableAlerts);
            MuteAlertsCheckBox.IsChecked = _settings.MuteAlerts;

            EnableStartupCheckBox.IsChecked = _settings.StartWithWindows;
            StartMinimizedCheckBox.IsChecked = _settings.StartMinimized;

            CustomSoundCheckBox.IsChecked = _settings.UseCustomSounds;
            CustomTextCheckBox.IsChecked = _settings.UseCustomTexts;

            FrequencyTextBox.Text = _settings.AlertFrequency.ToString();
            FrequencySlider.Value = (int)Math.Sqrt(_settings.AlertFrequency);

            DurationTextBox.Text = _settings.AlertDuration.ToString();
            DurationSlider.Value = (int)Math.Sqrt(_settings.AlertDuration);

            allowEvents = true;
        }

        private void StartupToggled(bool toggle)
        {
            _settings.StartWithWindows = toggle;
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

        private void DurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DurationTextBox != null && allowEvents)
            {
                int duration = 0;
                switch ((int)e.NewValue)
                {
                    case 1:
                        duration = 0;
                        break;
                    case 2:
                        duration = 5;
                        break;
                    case 3:
                        duration = 10;
                        break;
                    case 4:
                        duration = 15;
                        break;
                    case 5:
                        duration = 30;
                        break;
                    case 6:
                        duration = 60;
                        break;
                    case 7:
                        duration = 120;
                        break;
                }
                DurationTextBox.Text = duration.ToString();
                SetDuration(duration);
            }
        }

        private void FrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (FrequencyTextBox != null && allowEvents)
            {
                int frequency = 0;
                switch ((int)e.NewValue)
                {
                    case 1:
                        frequency = 5;
                        break;
                    case 2:
                        frequency = 15;
                        break;
                    case 3:
                        frequency = 30;
                        break;
                    case 4:
                        frequency = 45;
                        break;
                    case 5:
                        frequency = 60;
                        break;
                    case 6:
                        frequency = 120;
                        break;
                    case 7:
                        frequency = 240;
                        break;
                }
                FrequencyTextBox.Text = frequency.ToString();
                SetFrequency(frequency);
            }
        }

        private void DurationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DurationSlider != null && allowEvents)
            {
                if (sender is TextBox textBox)
                {
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        SetDuration((int)double.Parse(textBox.Text));
                    }
                }
            }
        }

        private void FrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (FrequencySlider != null && allowEvents)
            {
                if (sender is TextBox textBox)
                {
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        SetFrequency((int)double.Parse(textBox.Text));
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
                        if (Directory.Exists(GetCustomSoundsFolderPath()))
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                Arguments = GetCustomSoundsFolderPath(),
                                FileName = "explorer.exe"
                            };
                            Process.Start(startInfo);
                        }
                        break;
                    case "OpenTextsFileButton":
                        CreateCustomTextsFile();
                        if (File.Exists(GetCustomTextsFilePath()))
                        {
                            Process.Start(GetCustomTextsFilePath());
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
            if (sender is CheckBox checkBox && allowEvents)
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
                        StartupToggled((bool)checkBox.IsChecked);
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
                    case "TrackWaterCheckBox":
                        WaterTrackingGroupBox.IsEnabled = (bool)checkBox.IsChecked;
                        _settings.TrackConsumption = (bool)checkBox.IsChecked;
                        break;
                    default:
                        break;
                }
                WriteSettingsFile(_settings);
            }
        }

        private void SexRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            CalculateRecommendedWaterIntake();
        }

        private void WaterTrackingTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateRecommendedWaterIntake();
        }

        private void CalculateRecommendedWaterIntake()
        {
            double weight;
            int exercise;
            double.TryParse(BodyWeightTextBox.Text, out weight);
            int.TryParse(DailyExerciseTextBox.Text, out exercise);

            double water = 0;
            if (weight > 0)
            {
                weight *= 2.20462262;
                water += (weight * (2.0 / 3.0));
            }
            if (exercise > 0)
            {
                water += ((exercise / 30.0) * 12.0);
            }
            if (MetricRadioButton.IsChecked == true)
            {
                water /= 33.814;
            }
            if (FemaleRadioButton.IsChecked == true)
            {
                water *= 0.9;
            }
            water *= 0.8;
            water -= 1;
            GoalAxis.Visibility = water > 0 ? Visibility.Visible : Visibility.Collapsed;
            GoalAxis.Value = water;
        }

        private void UnitRadioButton_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
