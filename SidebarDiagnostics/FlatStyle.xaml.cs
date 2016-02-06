using System.Windows;
using System.Windows.Controls;

namespace SidebarDiagnostics.Style
{
    public partial class FlatStyle : ResourceDictionary
    {
        public FlatStyle()
        {
            InitializeComponent();
        }

        private void PART_TITLEBAR_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Border _titlebar = (Border)sender;

            if (_titlebar != null)
            {
                Window _window = Window.GetWindow(_titlebar);

                if (_window != null && _window.IsInitialized)
                {
                    _window.DragMove();
                }
            }
        }

        public void PART_MINIMIZE_Click(object sender, RoutedEventArgs e)
        {
            Button _button = (Button)sender;

            if (_button != null)
            {
                Window _window = Window.GetWindow(_button);

                if (_window != null && _window.IsInitialized)
                {
                    _window.WindowState = WindowState.Minimized;
                }
            }
        }

        public void PART_MAXIMIZE_RESTORE_Click(object sender, RoutedEventArgs e)
        {
            Button _button = (Button)sender;

            if (_button != null)
            {
                Window _window = Window.GetWindow(_button);

                if (_window != null && _window.IsInitialized)
                {
                    switch (_window.WindowState)
                    {
                        case WindowState.Normal:
                            _window.WindowState = WindowState.Maximized;
                            break;

                        case WindowState.Maximized:
                            _window.WindowState = WindowState.Normal;
                            break;
                    }
                }
            }
        }

        public void PART_CLOSE_Click(object sender, RoutedEventArgs e)
        {
            Button _button = (Button)sender;

            if (_button != null)
            {
                Window _window = Window.GetWindow(_button);

                if (_window != null && _window.IsInitialized)
                {
                    _window.Close();
                }
            }
        }
    }

    public partial class FlatWindow : Window
    {
        public static readonly DependencyProperty ShowIconProperty = DependencyProperty.Register("ShowIcon", typeof(bool), typeof(FlatWindow), new UIPropertyMetadata(true));

        public bool ShowIcon
        {
            get
            {
                return (bool)GetValue(ShowIconProperty);
            }
            set
            {
                SetValue(ShowIconProperty, value);
            }
        }

        public static readonly DependencyProperty ShowTitleProperty = DependencyProperty.Register("ShowTitle", typeof(bool), typeof(FlatWindow), new UIPropertyMetadata(true));

        public bool ShowTitle
        {
            get
            {
                return (bool)GetValue(ShowTitleProperty);
            }
            set
            {
                SetValue(ShowTitleProperty, value);
            }
        }

        public static readonly DependencyProperty ShowCloseProperty = DependencyProperty.Register("ShowClose", typeof(bool), typeof(FlatWindow), new UIPropertyMetadata(true));

        public bool ShowClose
        {
            get
            {
                return (bool)GetValue(ShowCloseProperty);
            }
            set
            {
                SetValue(ShowCloseProperty, value);
            }
        }

        public static readonly DependencyProperty ShowMaximizeProperty = DependencyProperty.Register("ShowMaximize", typeof(bool), typeof(FlatWindow), new UIPropertyMetadata(true));

        public bool ShowMaximize
        {
            get
            {
                return (bool)GetValue(ShowMaximizeProperty);
            }
            set
            {
                SetValue(ShowMaximizeProperty, value);
            }
        }

        public static readonly DependencyProperty ShowMinimizeProperty = DependencyProperty.Register("ShowMinimize", typeof(bool), typeof(FlatWindow), new UIPropertyMetadata(true));

        public bool ShowMinimize
        {
            get
            {
                return (bool)GetValue(ShowMinimizeProperty);
            }
            set
            {
                SetValue(ShowMinimizeProperty, value);
            }
        }
    }
}
