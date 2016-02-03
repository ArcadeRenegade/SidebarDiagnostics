using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SidebarDiagnostics.Windows;
using SidebarDiagnostics.Models;
using WindowsDesktop;

namespace SidebarDiagnostics
{
    /// <summary>
    /// Interaction logic for AppBar.xaml
    /// </summary>
    public partial class AppBar : AppBarWindow
    {
        public AppBar()
        {
            InitializeComponent();
        }

        public void Reload()
        {
            App._reloading = true;

            Close();
        }

        private void InitWindow()
        {
            if (Properties.Settings.Default.AlwaysTop)
            {
                SetTop();
            }

            if (Properties.Settings.Default.ClickThrough)
            {
                ClickThrough.SetClickThrough(this);
            }

            if (OS.SupportVirtualDesktop)
            {
                VirtualDesktop.CurrentChanged += VirtualDesktop_CurrentChanged;
            }

            Hotkey.Initialize(this, Properties.Settings.Default.Hotkeys);
            Devices.Initialize(this);
        }

        private void InitAppBar()
        {
            WorkArea _windowWA;
            WorkArea _appbarWA;

            Windows.Monitor.GetWorkArea(this, out _windowWA, out _appbarWA);
            
            Left = _windowWA.Left;
            Top = _windowWA.Top;
            Width = _windowWA.Width;
            Height = _windowWA.Height;

            if (Properties.Settings.Default.UseAppBar)
            {
                SetAppBar(Properties.Settings.Default.DockEdge, _windowWA, _appbarWA);
            }
        }

        private void InitContent()
        {
            DataContext = Model = new AppBarModel();
        }

        private void VirtualDesktop_CurrentChanged(object sender, VirtualDesktopChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
            {
                try
                {
                    this.MoveToDesktop(VirtualDesktop.Current);
                }
                catch (InvalidOperationException) { }
            }));
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            new Settings(this);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ScrollViewer_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            (sender as ScrollViewer).VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }

        private void ScrollViewer_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            (sender as ScrollViewer).VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitWindow();
            InitAppBar();
            InitContent();

            Ready = true;
        }

        private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            WindowControls.Visibility = Visibility.Visible;
        }

        private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            WindowControls.Visibility = Properties.Settings.Default.CollapseMenuBar ? Visibility.Collapsed : Visibility.Hidden;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Ready = false;

            if (Model != null)
            {
                Model.Dispose();
            }

            if (IsAppBar)
            {
                ClearAppBar();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (App._reloading)
            {
                App._reloading = false;

                new AppBar().Show();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        public bool Ready { get; private set; } = false;

        public AppBarModel Model { get; private set; }
    }
}