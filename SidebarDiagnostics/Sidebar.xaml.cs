using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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

        public async Task Reset(bool enableHotkeys)
        {
            if (!Ready)
            {
                return;
            }

            Ready = false;

            BindSettings(enableHotkeys);

            await BindModel();
        }

        public void Reposition()
        {
            if (!Ready)
            {
                return;
            }

            Ready = false;

            BindPosition(() => Ready = true);
        }

        public void ContentReload()
        {
            if (!Ready)
            {
                return;
            }

            Ready = false;

            Model.Reload();

            Ready = true;

            BindGraphs();
        }

        public override void AppBarShow()
        {
            base.AppBarShow();

            Model.Resume();
        }

        public override void AppBarHide()
        {
            base.AppBarHide();

            Model.Pause();
        }

        private async Task Initialize()
        {
            Ready = false;

            Devices.AddHook(this);

            if (OS.SupportVirtualDesktop)
            {
                try
                {
                    VirtualDesktop.CurrentChanged += VirtualDesktop_CurrentChanged;
                }
                catch (TypeInitializationException) { }
            }
            
            BindSettings(true);

            await BindModel();
        }

        private void BindSettings(bool enableHotkeys)
        {
            BindPosition(null);

            if (Framework.Settings.Instance.AlwaysTop)
            {
                SetTopMost(false);

                ShowDesktop.RemoveHook();
            }
            else
            {
                ClearTopMost(false);

                ShowDesktop.AddHook(this);
            }

            if (Framework.Settings.Instance.ClickThrough)
            {
                SetClickThrough();
            }
            else
            {
                ClearClickThrough();
            }

            if (Framework.Settings.Instance.ShowAltTab)
            {
                ShowInAltTab();
            }
            else
            {
                HideInAltTab();
            }

            Hotkey.Initialize(this, Framework.Settings.Instance.Hotkeys);

            if (enableHotkeys)
            {
                Hotkey.Enable();
            }
        }

        private void BindPosition(Action callback)
        {
            int _screen;
            DockEdge _edge;
            WorkArea _windowWA;
            WorkArea _appbarWA;

            Monitor.GetWorkArea(this, out _screen, out _edge, out _windowWA, out _appbarWA);

            Move(_windowWA);

            SetAppBar(_screen, _edge, _windowWA, _appbarWA, callback);
        }
        
        private async Task BindModel()
        {
            await Task.Run(async () =>
            {
                if (Model != null)
                {
                    Model.Dispose();
                    Model = null;
                }

                await Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ModelReadyHandler(ModelReady), new SidebarModel());
            });
        }

        private delegate void ModelReadyHandler(SidebarModel model);

        private void ModelReady(SidebarModel model)
        {
            DataContext = Model = model;
            model.Start();

            Ready = true;

            BindGraphs();

            if (_openSettings)
            {
                _openSettings = false;

                App.Current.OpenSettings();
            }
        }

        private void BindGraphs()
        {
            foreach (Graph _graph in App.Current.Graphs)
            {
                _graph.Model.BindData(Model.MonitorManager);
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

        private void GraphButton_Click(object sender, RoutedEventArgs e)
        {
            App.Current.OpenGraph();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            App.Current.OpenSettings();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }
        
        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            WindowControls.Visibility = Visibility.Visible;
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            WindowControls.Visibility = Framework.Settings.Instance.CollapseMenuBar ? Visibility.Collapsed : Visibility.Hidden;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Initialize();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Normal)
            {
                WindowState = WindowState.Normal;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Ready = false;

            DataContext = null;

            if (Model != null)
            {
                Model.Dispose();
                Model = null;
            }

            if (OS.SupportVirtualDesktop)
            {
                VirtualDesktop.CurrentChanged -= VirtualDesktop_CurrentChanged;
            }

            ClearAppBar();

            Devices.RemoveHook(this);
            ShowDesktop.RemoveHook();
            Hotkey.Dispose();
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
                App.Current.Shutdown();
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