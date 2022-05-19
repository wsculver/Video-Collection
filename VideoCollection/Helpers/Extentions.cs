using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace VideoCollection.Helpers
{
    public static partial class Extensions
    {
        static class OSInterop
        {
            [DllImport("user32.dll")]
            public static extern int GetSystemMetrics(int smIndex);
            public const int SM_CMONITORS = 80;

            [DllImport("user32.dll")]
            public static extern bool SystemParametersInfo(int nAction, int nParam, ref RECT rc, int nUpdate);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern bool GetMonitorInfo(HandleRef hmonitor, [In, Out] MONITORINFO info);

            [DllImport("user32.dll")]
            public static extern IntPtr MonitorFromWindow(HandleRef handle, int flags);

            [DllImport("user32")]
            internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

            [DllImport("user32")]
            internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

            /// <summary>
            /// POINT aka POINTAPI
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                /// <summary>
                /// x coordinate of point.
                /// </summary>
                public int x;
                /// <summary>
                /// y coordinate of point.
                /// </summary>
                public int y;

                /// <summary>
                /// Construct a point of coordinates (x,y).
                /// </summary>
                public POINT(int x, int y)
                {
                    this.x = x;
                    this.y = y;
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MINMAXINFO
            {
                public POINT ptReserved;
                public POINT ptMaxSize;
                public POINT ptMaxPosition;
                public POINT ptMinTrackSize;
                public POINT ptMaxTrackSize;
            };

            /// <summary>
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            public class MONITORINFO
            {
                /// <summary>
                /// </summary>            
                public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));

                /// <summary>
                /// </summary>            
                public RECT rcMonitor = new RECT();

                /// <summary>
                /// </summary>            
                public RECT rcWork = new RECT();

                /// <summary>
                /// </summary>            
                public int dwFlags = 0;
            }


            /// <summary> Win32 </summary>
            [StructLayout(LayoutKind.Sequential, Pack = 0)]
            public struct RECT
            {
                /// <summary> Win32 </summary>
                public int left;
                /// <summary> Win32 </summary>
                public int top;
                /// <summary> Win32 </summary>
                public int right;
                /// <summary> Win32 </summary>
                public int bottom;

                /// <summary> Win32 </summary>
                public static readonly RECT Empty = new RECT();

                /// <summary> Win32 </summary>
                public int width
                {
                    get { return Math.Abs(right - left); }  // Abs needed for BIDI OS
                }
                /// <summary> Win32 </summary>
                public int height
                {
                    get { return bottom - top; }
                }

                /// <summary> Win32 </summary>
                public RECT(int left, int top, int right, int bottom)
                {
                    this.left = left;
                    this.top = top;
                    this.right = right;
                    this.bottom = bottom;
                }


                /// <summary> Win32 </summary>
                public RECT(RECT rcSrc)
                {
                    this.left = rcSrc.left;
                    this.top = rcSrc.top;
                    this.right = rcSrc.right;
                    this.bottom = rcSrc.bottom;
                }

                /// <summary> Win32 </summary>
                public bool IsEmpty
                {
                    get
                    {
                        // BUGBUG : On Bidi OS (hebrew arabic) left > right
                        return left >= right || top >= bottom;
                    }
                }
                /// <summary> Return a user friendly representation of this struct </summary>
                public override string ToString()
                {
                    if (this == RECT.Empty) { return "RECT {Empty}"; }
                    return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
                }

                /// <summary> Determine if 2 RECT are equal (deep compare) </summary>
                public override bool Equals(object obj)
                {
                    if (!(obj is Rect)) { return false; }
                    return (this == (RECT)obj);
                }

                /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
                public override int GetHashCode()
                {
                    return left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
                }


                /// <summary> Determine if 2 RECT are equal (deep compare)</summary>
                public static bool operator ==(RECT rect1, RECT rect2)
                {
                    return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom);
                }

                /// <summary> Determine if 2 RECT are different(deep compare)</summary>
                public static bool operator !=(RECT rect1, RECT rect2)
                {
                    return !(rect1 == rect2);
                }
            }
        }

        public static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam, int minWidth, int minHeight)
        {
            OSInterop.MINMAXINFO mmi = (OSInterop.MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(OSInterop.MINMAXINFO));

            // Adjust the maximized size and position to fit the work area of the correct monitor
            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            IntPtr monitor = OSInterop.MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero)
            {
                OSInterop.MONITORINFO monitorInfo = new OSInterop.MONITORINFO();
                OSInterop.GetMonitorInfo(monitor, monitorInfo);
                OSInterop.RECT rcWorkArea = monitorInfo.rcWork;
                OSInterop.RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x += Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y += Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left) + Math.Abs(2 * mmi.ptMaxPosition.x);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top) + Math.Abs(2 * mmi.ptMaxPosition.y);
                mmi.ptMinTrackSize.x = minWidth;
                mmi.ptMinTrackSize.y = minHeight;
                mmi.ptMaxTrackSize.x = Math.Abs(rcWorkArea.width) + Math.Abs(2 * mmi.ptMaxPosition.x);
                mmi.ptMaxTrackSize.y = Math.Abs(rcWorkArea.height) + Math.Abs(2 * mmi.ptMaxPosition.y);
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        static Int32Rect _getOsInteropRect(Window w)
        {
            bool multimonSupported = OSInterop.GetSystemMetrics(OSInterop.SM_CMONITORS) != 0;
            if (!multimonSupported)
            {
                OSInterop.RECT rc = new OSInterop.RECT();
                OSInterop.SystemParametersInfo(48, 0, ref rc, 0);
                return new Int32Rect(rc.left, rc.top, rc.width, rc.height);
            }

            WindowInteropHelper helper = new WindowInteropHelper(w);
            IntPtr hmonitor = OSInterop.MonitorFromWindow(new HandleRef((object)null, helper.EnsureHandle()), 2);
            OSInterop.MONITORINFO info = new OSInterop.MONITORINFO();
            OSInterop.GetMonitorInfo(new HandleRef((object)null, hmonitor), info);
            return new Int32Rect(info.rcWork.left, info.rcWork.top, info.rcWork.width, info.rcWork.height);
        }

        public static Rect GetAbsoluteRect(this Window w)
        {
            if (w.WindowState != WindowState.Maximized)
                return new Rect(w.Left, w.Top, w.ActualWidth, w.ActualHeight);

            var r = _getOsInteropRect(w);
            return new Rect(r.X, r.Y, r.Width, r.Height);
        }

        public static Point GetAbsolutePosition(this Window w)
        {
            if (w.WindowState != WindowState.Maximized)
                return new Point(w.Left, w.Top);

            var r = _getOsInteropRect(w);
            return new Point(r.X, r.Y);
        }
    }
}
