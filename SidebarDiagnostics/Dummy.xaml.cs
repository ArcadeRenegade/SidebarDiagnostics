using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using SidebarDiagnostics.Windows;

namespace SidebarDiagnostics
{
    /// <summary>
    /// Interaction logic for Dummy.xaml
    /// </summary>
    public partial class Dummy : AppBarWindow
    {
        public Dummy(Setup setup)
        {
            InitializeComponent();

            Setup = setup;
        }

        public void Reposition()
        {
            Monitor.GetWorkArea(this, out int _screen, out DockEdge _edge, out WorkArea _initPos, out WorkArea _windowWA, out WorkArea _appbarWA);

            Move(_initPos);

            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
            {
                Move(_windowWA);
            }));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Reposition();

            Setup.Owner = this;
            Setup.ShowDialog();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Normal)
            {
                WindowState = WindowState.Normal;
            }
        }

        public Setup Setup { get; private set; }
    }
}
