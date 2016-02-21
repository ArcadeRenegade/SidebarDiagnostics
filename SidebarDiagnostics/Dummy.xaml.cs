using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
            int _screen;
            DockEdge _edge;
            WorkArea _windowWA;
            WorkArea _appbarWA;

            Monitor.GetWorkArea(this, out _screen, out _edge, out _windowWA, out _appbarWA);

            Move(_windowWA);
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
