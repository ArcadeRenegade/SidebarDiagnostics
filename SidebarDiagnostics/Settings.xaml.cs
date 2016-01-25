using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SidebarDiagnostics.Windows;
using SidebarDiagnostics.Helpers;
using SidebarDiagnostics.Monitor;

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

            IsSave = false;

            DataContext = Properties.Settings.Default;

            DockEdgeComboBox.Items.Add(DockEdge.Left);
            DockEdgeComboBox.Items.Add(DockEdge.Right);
            DockEdgeComboBox.SelectedValue = Properties.Settings.Default.DockEdge;

            int _screenCount = Monitors.GetNoOfMonitors();

            for (int i = 0; i < _screenCount; i++)
            {
                ScreenComboBox.Items.Add(new { Text = string.Format("#{0}", i + 1), Value = i });
            }

            ScreenComboBox.DisplayMemberPath = "Text";
            ScreenComboBox.SelectedValuePath = "Value";

            if (Properties.Settings.Default.ScreenIndex < _screenCount)
            {
                ScreenComboBox.SelectedValue = Properties.Settings.Default.ScreenIndex;
            }
            else
            {
                ScreenComboBox.SelectedValue = 0;
            }

            FontSizeComboBox.Items.Add(FontSetting.x10);
            FontSizeComboBox.Items.Add(FontSetting.x12);
            FontSizeComboBox.Items.Add(FontSetting.x14);
            FontSizeComboBox.Items.Add(FontSetting.x16);
            FontSizeComboBox.Items.Add(FontSetting.x18);

            FontSizeComboBox.DisplayMemberPath = FontSizeComboBox.SelectedValuePath = "FontSize";
            FontSizeComboBox.SelectedValue = Properties.Settings.Default.FontSize;
            
            StartupCheckBox.IsChecked = Utilities.StartupTaskExists();
        }

        private void Save()
        {
            IsSave = true;

            Properties.Settings.Default.DockEdge = (DockEdge)DockEdgeComboBox.SelectedValue;
            Properties.Settings.Default.ScreenIndex = (int)ScreenComboBox.SelectedValue;
            
            FontSetting _fontSetting = (FontSetting)FontSizeComboBox.SelectedItem;
            Properties.Settings.Default.FontSize = _fontSetting.FontSize;
            Properties.Settings.Default.TitleFontSize = _fontSetting.TitleFontSize;
            Properties.Settings.Default.IconSize = _fontSetting.IconSize;
            
            Properties.Settings.Default.Save();

            if (StartupCheckBox.IsChecked == true)
            {
                Utilities.EnableStartupTask();
            }
            else
            {
                Utilities.DisableStartupTask();
            }

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
            {
                (Owner as AppBar).Reload();
            }));
        }

        private void ColorTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox _textbox = (TextBox)sender;

            if (!new Regex("^#[a-fA-F0-9]{6}$").IsMatch(_textbox.Text))
            {
                _textbox.Text = "#000000";
            }
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

        private void MonitorUp_Click(object sender, RoutedEventArgs e)
        {
            MonitorConfig _row = (MonitorConfig)(sender as Button).DataContext;

            if (_row.Order == 1)
                return;

            MonitorConfig[] _config = Properties.Settings.Default.MonitorConfig;

            _config.Where(c => c.Order == _row.Order - 1).Single().Order += 1;
            _row.Order -= 1;

            Properties.Settings.Default.MonitorConfig = _config.OrderBy(c => c.Order).ToArray();
        }

        private void MonitorDown_Click(object sender, RoutedEventArgs e)
        {
            MonitorConfig _row = (MonitorConfig)(sender as Button).DataContext;

            MonitorConfig[] _config = Properties.Settings.Default.MonitorConfig;

            if (_row.Order == _config.Length)
                return;

            _config.Where(c => c.Order == _row.Order + 1).Single().Order -= 1;
            _row.Order += 1;

            Properties.Settings.Default.MonitorConfig = _config.OrderBy(c => c.Order).ToArray();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!IsSave)
            {
                Properties.Settings.Default.Reload();
            }
        }

        private bool IsSave { get; set; }
    }
}
