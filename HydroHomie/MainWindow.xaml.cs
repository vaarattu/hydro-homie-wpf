using AdonisUI.Controls;
using System;
using System.Media;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace HydroHomie
{
    public partial class MainWindow : AdonisWindow
    {
        int duration = 10;
        int frequency = 30;
        bool displayNotifications = true;
        bool startup = false;
        TimeSpan lastAlert;

        public MainWindow()
        {
            InitializeComponent();

            ContextMenuEnableNotifications.Header = displayNotifications ? "Disable notifications" : "Enable notifications";

            DispatcherTimer aTimer = new DispatcherTimer();
            aTimer.Tick += new EventHandler(OnTimedEvent);
            aTimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            aTimer.IsEnabled = true;
            aTimer.Start();

            NotificationsToggled(displayNotifications);
            FrequencyChanged(frequency);
            DurationChanged(duration);
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
                        TrayIcon.ShowCustomBalloon(new AlertBalloon(), System.Windows.Controls.Primitives.PopupAnimation.Slide, duration*1000);
                        MediaPlayer player = new MediaPlayer();
                        player.Open(new Uri(@"luksus.mp3", UriKind.RelativeOrAbsolute));
                        player.Play();
                        lastAlert = timeSpan;
                    }
                }
            }
            else
            {
                TimeUntilNotificationTextBlock.Text = "ERROR";
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

        private void FrequencyChanged(int newFrequency)
        {
            frequency = newFrequency;
            FrequencyTextBox.Text = newFrequency.ToString();
            FrequencySlider.Value = newFrequency;
        }

        private void DurationChanged(int newDuration)
        {
            duration = newDuration;
            DurationTextBox.Text = newDuration.ToString();
            DurationSlider.Value = newDuration;
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
            }
        }

        private void DurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DurationTextBox != null)
            {
                duration = (int)e.NewValue;
                DurationTextBox.Text = e.NewValue.ToString();
            }
        }

        private void FrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (FrequencyTextBox != null)
            {
                frequency = (int)e.NewValue;
                FrequencyTextBox.Text = e.NewValue.ToString();
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
                        DurationSlider.Value = double.Parse(textBox.Text);
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
                        FrequencySlider.Value = double.Parse(textBox.Text);
                    }
                }
            }
        }
    }
}
