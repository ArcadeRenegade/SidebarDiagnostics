using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SidebarDiagnostics.Models;
using SidebarDiagnostics.Monitor;
using SidebarDiagnostics.Windows;

namespace SidebarDiagnostics
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : DPIAwareWindow
    {
        public Settings()
        {
            InitializeComponent();

            DataContext = Model = new SettingsModel();
        }

        private void Save()
        {
            Model.Save();

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
            {
                (Owner as AppBar).Reload();
            }));
        }

        private void ClickThroughCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            ShowTrayIconCheckbox.IsChecked = true;
        }

        private void ShowTrayIconCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            ClickThroughCheckbox.IsChecked = false;
        }

        private void ColorTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox _textbox = (TextBox)sender;

            if (!new Regex("^#[a-fA-F0-9]{6}$").IsMatch(_textbox.Text))
            {
                _textbox.Text = "#000000";
            }
        }
        
        private void MonitorUp_Click(object sender, RoutedEventArgs e)
        {
            MonitorConfig _row = (MonitorConfig)(sender as Button).DataContext;

            if (_row.Order <= 1)
                return;

            MonitorConfig[] _config = Model.MonitorConfig;

            _config.Where(c => c.Order == _row.Order - 1).Single().Order += 1;
            _row.Order -= 1;

            Model.NotifyPropertyChanged("MonitorConfig");
        }

        private void MonitorDown_Click(object sender, RoutedEventArgs e)
        {
            MonitorConfig _row = (MonitorConfig)(sender as Button).DataContext;

            MonitorConfig[] _config = Model.MonitorConfig;

            if (_row.Order >= _config.Length)
                return;

            _config.Where(c => c.Order == _row.Order + 1).Single().Order -= 1;
            _row.Order += 1;

            Model.NotifyPropertyChanged("MonitorConfig");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public SettingsModel Model { get; private set; }
    }
}
