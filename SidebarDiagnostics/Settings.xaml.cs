using System.Linq;
using System.Windows;
using System.Windows.Input;
using SidebarDiagnostics.AB;
using SidebarDiagnostics.Helpers;

namespace SidebarDiagnostics
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();

            DockEdgeComboBox.Items.Add(ABEdge.Left);
            DockEdgeComboBox.Items.Add(ABEdge.Right);
            DockEdgeComboBox.SelectedValue = Properties.Settings.Default.DockEdge;

            Monitor[] _monitors = Monitor.AllMonitors.ToArray();

            for (int i = 0; i < _monitors.Length; i++)
            {
                ScreenComboBox.Items.Add(new { Text = string.Format("#{0}", i + 1), Value = i });
            }

            ScreenComboBox.DisplayMemberPath = "Text";
            ScreenComboBox.SelectedValuePath = "Value";

            if (Properties.Settings.Default.ScreenIndex < _monitors.Length)
            {
                ScreenComboBox.SelectedValue = Properties.Settings.Default.ScreenIndex;
            }
            else
            {
                ScreenComboBox.SelectedValue = 0;
            }

            BGColorTextBox.Text = Properties.Settings.Default.BGColor;

            BGOpacitySlider.Value = Properties.Settings.Default.BGOpacity;
            
            TextColorTextBox.Text = Properties.Settings.Default.TextColor;

            PollingIntervalTextBox.Text = Properties.Settings.Default.PollingInterval.ToString();

            AlwaysTopCheckBox.IsChecked = Properties.Settings.Default.AlwaysTop;

            StartupCheckBox.IsChecked = Utilities.IsStartupEnabled();
        }

        private void PollingIntervalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
            {
                e.Handled = true;
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DockEdge = (ABEdge)DockEdgeComboBox.SelectedValue;
            Properties.Settings.Default.ScreenIndex = (int)ScreenComboBox.SelectedValue;
            Properties.Settings.Default.BGColor = BGColorTextBox.Text;
            Properties.Settings.Default.BGOpacity = BGOpacitySlider.Value;
            Properties.Settings.Default.TextColor = TextColorTextBox.Text;
            Properties.Settings.Default.PollingInterval = int.Parse(PollingIntervalTextBox.Text);
            Properties.Settings.Default.AlwaysTop = AlwaysTopCheckBox.IsChecked.HasValue && AlwaysTopCheckBox.IsChecked.Value;
            Properties.Settings.Default.Save();

            Utilities.SetStartupEnabled(StartupCheckBox.IsChecked.HasValue && StartupCheckBox.IsChecked.Value);

            (Owner as AppBar).SettingsUpdate();
            
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
