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
        public Dummy()
        {
            InitializeComponent();
        }

        public void Position()
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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Position();
        }
    }
}
