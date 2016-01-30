using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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

        private void BindButton_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_hotkey != null)
            {
                EndBind();
            }

            (sender as ToggleButton).IsChecked = false;
        }

        private void BindToggle_Click(object sender, RoutedEventArgs e)
        {
            _keybinder = (ToggleButton)sender;

            if (_keybinder.IsChecked == true)
            {
                BeginBind(Hotkey.KeyAction.Toggle);
            }
            else
            {
                EndBind();
            }
        }

        private void BindShow_Click(object sender, RoutedEventArgs e)
        {
            _keybinder = (ToggleButton)sender;

            if (_keybinder.IsChecked == true)
            {
                BeginBind(Hotkey.KeyAction.Show);
            }
            else
            {
                EndBind();
            }
        }

        private void BindHide_Click(object sender, RoutedEventArgs e)
        {
            _keybinder = (ToggleButton)sender;

            if (_keybinder.IsChecked == true)
            {
                BeginBind(Hotkey.KeyAction.Hide);
            }
            else
            {
                EndBind();
            }
        }

        private void BindReload_Click(object sender, RoutedEventArgs e)
        {
            _keybinder = (ToggleButton)sender;

            if (_keybinder.IsChecked == true)
            {
                BeginBind(Hotkey.KeyAction.Reload);
            }
            else
            {
                EndBind();
            }
        }

        private void BindClose_Click(object sender, RoutedEventArgs e)
        {
            _keybinder = (ToggleButton)sender;

            if (_keybinder.IsChecked == true)
            {
                BeginBind(Hotkey.KeyAction.Close);
            }
            else
            {
                EndBind();
            }
        }

        private void BeginBind(Hotkey.KeyAction action)
        {
            _hotkey = new Hotkey();
            _hotkey.Action = action;
            _hotkey.WinKey = Key.Escape;

            KeyDown += Window_KeyDown;
            KeyUp += Window_KeyUp;
        }

        private void EndBind()
        {
            KeyDown -= Window_KeyDown;
            KeyUp -= Window_KeyUp;

            Hotkey.KeyAction _action = _hotkey.Action;

            if (_hotkey.WinKey == Key.Escape)
            {
                _hotkey = null;
            }
            
            switch (_action)
            {
                case Hotkey.KeyAction.Toggle:
                    Model.ToggleKey = _hotkey;
                    break;

                case Hotkey.KeyAction.Show:
                    Model.ShowKey = _hotkey;
                    break;

                case Hotkey.KeyAction.Hide:
                    Model.HideKey = _hotkey;
                    break;

                case Hotkey.KeyAction.Reload:
                    Model.ReloadKey = _hotkey;
                    break;

                case Hotkey.KeyAction.Close:
                    Model.CloseKey = _hotkey;
                    break;
            }

            _keybinder.IsChecked = false;
        }
        
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.System:
                    return;

                case Key.LeftAlt:
                    _hotkey.AltMod = true;
                    return;

                case Key.LeftCtrl:
                    _hotkey.CtrlMod = true;
                    return;

                case Key.LeftShift:
                    _hotkey.ShiftMod = true;
                    return;

                case Key.LWin:
                    _hotkey.WinMod = true;
                    return;

                default:
                    _hotkey.WinKey = e.Key;
                    EndBind();
                    break;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftAlt:
                    _hotkey.AltMod = false;
                    return;

                case Key.LeftCtrl:
                    _hotkey.CtrlMod = false;
                    return;

                case Key.LeftShift:
                    _hotkey.ShiftMod = false;
                    return;

                case Key.LWin:
                    _hotkey.WinMod = false;
                    return;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Hotkey.Disable();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Hotkey.Enable();
        }

        public SettingsModel Model { get; private set; }

        private Hotkey _hotkey { get; set; }

        private ToggleButton _keybinder { get; set; }
    }
}
