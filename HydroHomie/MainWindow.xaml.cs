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

namespace HydroHomie
{
    public partial class MainWindow : AdonisWindow
    {
        readonly string customTextsFile = "custom_alert_texts.txt";
        readonly string customSoundsFolder = "custom_sounds";
        readonly string[] alertTexts = { "Hydration alert!", "It's time to hydrate!", "Bottoms up!", "Stay hydrated!", "H20 time!", "Drink your favorite water!", "Take a sip!", "Aren't you thristy already?" };

        int duration = 10;
        int frequency = 30;
        bool displayNotifications = true;
        bool startup = false;
        bool customText = false;
        bool customSound = false;
        TimeSpan lastAlert;
        readonly SoundPlayer soundPlayer;
        readonly MediaPlayer mediaPlayer;

        public MainWindow()
        {
            InitializeComponent();

            CreateCustomTextsFile();
            CreateCustomSoundsFolder();

            soundPlayer = new SoundPlayer();
            mediaPlayer = new MediaPlayer();

            ContextMenuEnableNotifications.Header = displayNotifications ? "Disable notifications" : "Enable notifications";

            DispatcherTimer aTimer = new DispatcherTimer();
            aTimer.Tick += new EventHandler(OnTimedEvent);
            aTimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            aTimer.IsEnabled = true;
            aTimer.Start();

            NotificationsToggled(displayNotifications);
            UpdateUI(startup, customSound, customText, frequency, duration);
        }

        private void OnTimedEvent(object source, EventArgs e)
        {
            if (frequency > 0)
            {
                if (this.Visibility == Visibility.Visible)
                {
                    int minutesUntil = (frequency - DateTime.Now.Minute % frequency);
                    DateTime now = DateTime.Now;
                    TimeSpan timeSpan = new TimeSpan(now.Hour, now.Minute, now.Second);
                    timeSpan = timeSpan.Add(new TimeSpan(0, minutesUntil, 0)) - timeSpan.Add(new TimeSpan(0, 0, now.Second));
                    TimeUntilNotificationTextBlock.Text = timeSpan.ToString();
                }
                if (displayNotifications)
                {
                    DateTime now = DateTime.Now;
                    TimeSpan timeSpan = new TimeSpan(now.Hour, now.Minute, 0);
                    if (DateTime.Now.Minute % frequency == 0 && lastAlert != timeSpan)
                    {
                        if (duration > 0)
                        {
                            TrayIcon.ShowCustomBalloon(new AlertBalloon(GetNotificationText()), System.Windows.Controls.Primitives.PopupAnimation.Slide, duration * 1000);
                        }
                        PlayNotificationSound();
                        lastAlert = timeSpan;
                    }
                }
            }
            else
            {
                TimeUntilNotificationTextBlock.Text = "ERROR";
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
            if (customText)
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
            if (customSound)
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
                    NotificationsToggled(!displayNotifications);
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

        private void NotificationsToggled(bool enabled)
        {
            EnableNotificationsCheckBox.IsChecked = enabled;
            displayNotifications = enabled;
            ContextMenuEnableNotifications.Header = displayNotifications ? "Disable notifications" : "Enable notifications";
        }

        private void UpdateUI(bool newStartUp, bool newCustomSound, bool newCustomText, int newFrequency, int newDuration)
        {
            EnableStartupCheckBox.IsChecked = newStartUp;
            startup = newStartUp;

            CustomSoundCheckBox.IsChecked = newCustomSound;
            customSound = newCustomSound;

            CustomTextCheckBox.IsChecked = newCustomText;
            customText = newCustomText;

            frequency = newFrequency;
            FrequencySlider.Value = (int)Math.Sqrt((double)newFrequency);
            FrequencyTextBox.Text = newFrequency.ToString();

            duration = newDuration;
            FrequencySlider.Value = (int)Math.Sqrt((double)newDuration);
            DurationTextBox.Text = newDuration.ToString();
        }

        private void EnableNotificationsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                NotificationsToggled((bool)checkBox.IsChecked);
            }
        }

        private void EnableStartupCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                startup = (bool)checkBox.IsChecked;
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (startup)
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
            }
        }

        private void FrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (FrequencyTextBox != null)
            {
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
                        duration = (int)double.Parse(textBox.Text);
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
                        frequency = (int)double.Parse(textBox.Text);
                    }
                }
            }
        }

        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void OpenTextsFileButton_Click(object sender, RoutedEventArgs e)
        {
            CreateCustomTextsFile();
            if (File.Exists(customTextsFile))
            {
                Process.Start(customTextsFile);
            }
        }

        private void OpenSoundsFolderButton_Click(object sender, RoutedEventArgs e)
        {
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
        }

        private void CustomTextCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                customText = (bool)checkBox.IsChecked;
            }
        }

        private void CustomTextCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                customText = (bool)checkBox.IsChecked;
            }
        }

        private void CustomSoundCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                customSound = (bool)checkBox.IsChecked;
            }
        }

        private void CustomSoundCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                customSound = (bool)checkBox.IsChecked;
            }
        }
    }
}
