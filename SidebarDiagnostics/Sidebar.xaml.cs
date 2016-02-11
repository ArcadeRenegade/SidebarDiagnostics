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
    /// Interaction logic for Sidebar.xaml
    /// </summary>
    public partial class Sidebar : AppBarWindow
    {
        public Sidebar(bool openSettings)
        {
            InitializeComponent();

            _openSettings = openSettings;
        }

        public void Reload()
        {
            if (!Ready)
            {
                return;
            }

            Ready = false;

            App._reloading = true;

            Close();
        }

        public void Reset(bool enableHotkeys)
        {
            if (!Ready)
            {
                return;
            }

            Ready = false;

            BindSettings(enableHotkeys);

            new BindModelHandler(BindModel).BeginInvoke(null, null);
        }

        private void Initialize()
        {
            Ready = false;

            Devices.Initialize(this);

            if (OS.SupportVirtualDesktop)
            {
                VirtualDesktop.CurrentChanged += VirtualDesktop_CurrentChanged;
            }

            BindSettings(true);

            new BindModelHandler(BindModel).BeginInvoke(null, null);
        }

        private void BindSettings(bool enableHotkeys)
        {
            int _screen;
            DockEdge _edge;
            WorkArea _windowWA;
            WorkArea _appbarWA;

            Monitor.GetWorkArea(this, out _screen, out _edge, out _windowWA, out _appbarWA);

            Left = _windowWA.Left;
            Top = _windowWA.Top;
            Width = _windowWA.Width;
            Height = _windowWA.Height;

            if (Framework.Settings.Instance.UseAppBar)
            {
                SetAppBar(_screen, _edge, _windowWA, _appbarWA);
            }
            else
            {
                ClearAppBar();
            }

            if (Framework.Settings.Instance.AlwaysTop)
            {
                SetTop();
            }
            else
            {
                ClearTop();
            }

            if (Framework.Settings.Instance.ClickThrough)
            {
                SetClickThrough();
            }
            else
            {
                ClearClickThrough();
            }

            Hotkey.Initialize(this, Framework.Settings.Instance.Hotkeys);

            if (enableHotkeys)
            {
                Hotkey.Enable();
            }
        }

        private delegate void BindModelHandler();

        private void BindModel()
        {
            if (Model != null)
            {
                Model.Dispose();
                Model = null;
            }

            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ModelReadyHandler(ModelReady), new SidebarModel());
        }

        private delegate void ModelReadyHandler(SidebarModel model);

        private void ModelReady(SidebarModel model)
        {
            DataContext = Model = model;
            model.Start();

            Ready = true;

            if (_openSettings)
            {
                _openSettings = false;

                (Application.Current as App).OpenSettings();
            }
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
            (Application.Current as App).OpenSettings();
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

        private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            WindowControls.Visibility = Visibility.Visible;
        }

        private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            WindowControls.Visibility = Framework.Settings.Instance.CollapseMenuBar ? Visibility.Collapsed : Visibility.Hidden;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Initialize();
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

                new Sidebar(false).Show();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private bool _ready { get; set; } = false;

        public bool Ready
        {
            get
            {
                return _ready;
            }
            set
            {
                _ready = value;

                if (Model != null)
                {
                    Model.Ready = value;
                }
            }
        }

        public SidebarModel Model { get; private set; }

        private bool _openSettings { get; set; } = false;
    }
}