using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace SidebarDiagnostics
{
    public enum ABEdge : int
    {
        Left = 0,
        Top,
        Right,
        Bottom,
        None
    }

    internal static class AppBarFunctions
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uCallbackMessage;
            public int uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        private enum ABMsg : int
        {
            ABM_NEW = 0,
            ABM_REMOVE,
            ABM_QUERYPOS,
            ABM_SETPOS,
            ABM_GETSTATE,
            ABM_GETTASKBARPOS,
            ABM_ACTIVATE,
            ABM_GETAUTOHIDEBAR,
            ABM_SETAUTOHIDEBAR,
            ABM_WINDOWPOSCHANGED,
            ABM_SETSTATE
        }
        private enum ABNotify : int
        {
            ABN_STATECHANGE = 0,
            ABN_POSCHANGED,
            ABN_FULLSCREENAPP,
            ABN_WINDOWARRANGE
        }

        [DllImport("SHELL32", CallingConvention = CallingConvention.StdCall)]
        private static extern uint SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern int RegisterWindowMessage(string msg);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO mi);


        private const int MONITOR_DEFAULTTONEAREST = 0x2;
        private const int MONITORINFOF_PRIMARY = 0x1;

        private class RegisterInfo
        {
            public int CallbackId { get; set; }
            public bool IsRegistered { get; set; }
            public Window Window { get; set; }
            public ABEdge Edge { get; set; }
            public WindowStyle OriginalStyle { get; set; }
            public Point OriginalPosition { get; set; }
            public Size OriginalSize { get; set; }
            public ResizeMode OriginalResizeMode { get; set; }


            public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam,
                                    IntPtr lParam, ref bool handled)
            {
                if (msg == CallbackId)
                {
                    if (wParam.ToInt32() == (int)ABNotify.ABN_POSCHANGED)
                    {
                        ABSetPos(Edge, Window);
                        handled = true;
                    }
                }
                return IntPtr.Zero;
            }

        }
        
        private static Dictionary<Window, RegisterInfo> s_RegisteredWindowInfo = new Dictionary<Window, RegisterInfo>();

        private static RegisterInfo GetRegisterInfo(Window appbarWindow)
        {
            RegisterInfo reg;
            if (s_RegisteredWindowInfo.ContainsKey(appbarWindow))
            {
                reg = s_RegisteredWindowInfo[appbarWindow];
            }
            else
            {
                reg = new RegisterInfo()
                {
                    CallbackId = 0,
                    Window = appbarWindow,
                    IsRegistered = false,
                    Edge = ABEdge.Top,
                    OriginalStyle = appbarWindow.WindowStyle,
                    OriginalPosition = new Point(appbarWindow.Left, appbarWindow.Top),
                    OriginalSize =
                        new Size(appbarWindow.ActualWidth, appbarWindow.ActualHeight),
                    OriginalResizeMode = appbarWindow.ResizeMode,
                };
                s_RegisteredWindowInfo.Add(appbarWindow, reg);
            }
            return reg;
        }

        private static void RestoreWindow(Window appbarWindow)
        {
            RegisterInfo info = GetRegisterInfo(appbarWindow);

            appbarWindow.WindowStyle = info.OriginalStyle;
            appbarWindow.ResizeMode = info.OriginalResizeMode;
            appbarWindow.Topmost = false;

            Rect rect = new Rect(info.OriginalPosition.X, info.OriginalPosition.Y,
                info.OriginalSize.Width, info.OriginalSize.Height);
            appbarWindow.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                    new ResizeDelegate(DoResize), appbarWindow, rect);

        }

        public static void SetAppBar(Window appbarWindow, ABEdge edge, bool topMost)
        {
            RegisterInfo info = GetRegisterInfo(appbarWindow);

            info.Edge = edge;

            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = new WindowInteropHelper(appbarWindow).Handle;

            if (edge == ABEdge.None)
            {
                if (info.IsRegistered)
                {
                    SHAppBarMessage((int)ABMsg.ABM_REMOVE, ref abd);
                    info.IsRegistered = false;
                }
                RestoreWindow(appbarWindow);
                return;
            }

            if (!info.IsRegistered)
            {
                info.IsRegistered = true;
                info.CallbackId = RegisterWindowMessage("AppBarMessage");
                abd.uCallbackMessage = info.CallbackId;

                uint ret = SHAppBarMessage((int)ABMsg.ABM_NEW, ref abd);

                //HwndSource source = HwndSource.FromHwnd(abd.hWnd);
                //source.AddHook(new HwndSourceHook(info.WndProc));
            }

            appbarWindow.WindowStyle = WindowStyle.None;
            appbarWindow.ResizeMode = ResizeMode.NoResize;
            appbarWindow.Topmost = topMost;

            ABSetPos(info.Edge, appbarWindow);
        }

        private delegate void ResizeDelegate(Window appbarWindow, Rect rect);
        private static void DoResize(Window appbarWindow, Rect rect)
        {
            appbarWindow.Width = rect.Width;
            appbarWindow.Height = rect.Height;
            appbarWindow.Top = rect.Top;
            appbarWindow.Left = rect.Left;
        }

        private static void GetActualScreenData(ABEdge edge, Window appbarWindow, ref int leftOffset, ref int topOffset, ref int actualScreenWidth, ref int actualScreenHeight)
        {
            IntPtr handle = new WindowInteropHelper(appbarWindow).Handle;
            IntPtr monitorHandle = MonitorFromWindow(handle, MONITOR_DEFAULTTONEAREST);

            MONITORINFO mi = new MONITORINFO();
            mi.cbSize = Marshal.SizeOf(mi);

            if (GetMonitorInfo(monitorHandle, ref mi))
            {
                if (mi.dwFlags == MONITORINFOF_PRIMARY)
                {
                    return;
                }
                leftOffset = mi.rcWork.left;
                topOffset = mi.rcWork.top;
                actualScreenWidth = mi.rcWork.right - leftOffset;
                actualScreenHeight = mi.rcWork.bottom - mi.rcWork.top;
            }
        }

        private static void ABSetPos(ABEdge edge, Window appbarWindow)
        {
            APPBARDATA barData = new APPBARDATA();
            barData.cbSize = Marshal.SizeOf(barData);
            barData.hWnd = new WindowInteropHelper(appbarWindow).Handle;
            barData.uEdge = (int)edge;

            int leftOffset = 0;
            int topOffset = 0;
            int actualScreenWidth = (int)SystemParameters.PrimaryScreenWidth;
            int actualScreenHeight = (int)SystemParameters.PrimaryScreenHeight;

            GetActualScreenData(edge, appbarWindow, ref leftOffset, ref topOffset, ref actualScreenWidth, ref actualScreenHeight);

            if (barData.uEdge == (int)ABEdge.Left || barData.uEdge == (int)ABEdge.Right)
            {
                barData.rc.top = topOffset;
                barData.rc.bottom = actualScreenHeight;
                if (barData.uEdge == (int)ABEdge.Left)
                {
                    barData.rc.left = leftOffset;
                    barData.rc.right = (int)Math.Round(appbarWindow.ActualWidth) + leftOffset;
                }
                else
                {
                    barData.rc.right = actualScreenWidth + leftOffset;
                    barData.rc.left = barData.rc.right - (int)Math.Round(appbarWindow.ActualWidth);
                }
            }
            else
            {
                barData.rc.left = leftOffset;
                barData.rc.right = actualScreenWidth + leftOffset;
                if (barData.uEdge == (int)ABEdge.Top)
                {
                    barData.rc.top = topOffset;
                    barData.rc.bottom = (int)Math.Round(appbarWindow.ActualHeight) + topOffset;
                }
                else
                {
                    barData.rc.bottom = actualScreenHeight + topOffset;
                    barData.rc.top = barData.rc.bottom - (int)Math.Round(appbarWindow.ActualHeight);
                }
            }

            SHAppBarMessage((int)ABMsg.ABM_QUERYPOS, ref barData);
            SHAppBarMessage((int)ABMsg.ABM_SETPOS, ref barData);

            Rect rect = new Rect((double)barData.rc.left, (double)barData.rc.top,
                (double)(barData.rc.right - barData.rc.left), (double)(barData.rc.bottom - barData.rc.top));
            //This is done async, because WPF will send a resize after a new appbar is added.  
            //if we size right away, WPFs resize comes last and overrides us.
            appbarWindow.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                new ResizeDelegate(DoResize), appbarWindow, rect);
        }
    }
}