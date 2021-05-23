using System.Windows;
using System.Windows.Controls;

namespace HydroHomie
{
    /// <summary>
    /// Interaction logic for AlertBalloon.xaml
    /// </summary>
    public partial class AlertBalloon : UserControl
    {
        public AlertBalloon(string text, bool tracking)
        {
            InitializeComponent();
            TextTextBlock.Text = text;

            TrackingUniformGrid.Visibility = tracking ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
