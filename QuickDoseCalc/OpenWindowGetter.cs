using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HWND = System.IntPtr;
using System.Windows;

namespace SmartPlanning
{
    public static class OpenWindowGetter
    {
        /// <summary>Returns a dictionary that contains the handle and title of all the open windows.</summary>
        /// <returns>A dictionary that contains the handle and title of all the open windows.</returns>
        /// 

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);


        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint FindWindow(string className, string processId);
        public static IDictionary<HWND, string> GetOpenWindows()
        {
            HWND shellWindow = GetShellWindow();
            Dictionary<HWND, string> windows = new Dictionary<HWND, string>();

            EnumWindows(delegate (HWND hWnd, int lParam)
            {
                if (hWnd == shellWindow) return true;
                if (!IsWindowVisible(hWnd)) return true;

                int length = GetWindowTextLength(hWnd);
                //if (length == 0) return true;

                StringBuilder builder = new StringBuilder(length);
                GetWindowText(hWnd, builder, length + 1);

                GetWindowThreadProcessId(hWnd, out uint pid);

                windows[hWnd] = builder.ToString();
                return true;

            }, 0);

            return windows;
        }

        public static IDictionary<HWND, string> GetOpenChildWindows(HWND ptr)
        {
            HWND shellWindow = GetShellWindow();
            Dictionary<HWND, string> windows = new Dictionary<HWND, string>();

            EnumChildWindows(ptr, delegate (HWND hWnd, int lParam)
            {
                if (hWnd == shellWindow) return true;
                if (!IsWindowVisible(hWnd)) return true;

                int length = GetWindowTextLength(hWnd);
                //if (length == 0) return true;

                StringBuilder builder = new StringBuilder(length);
                GetWindowText(hWnd, builder, length + 1);

                GetWindowThreadProcessId(hWnd, out uint pid);

                windows[hWnd] = builder.ToString();
                return true;

            }, 0);

            return windows;
        }

        private delegate bool EnumWindowsProc(HWND hWnd, int lParam);

        [DllImport("USER32.DLL", CharSet = CharSet.Auto)]
        private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

        [DllImport("USER32.DLL", CharSet = CharSet.Auto)]
        private static extern bool EnumChildWindows(HWND ptr, EnumWindowsProc enumFunc, int lParam);

        [DllImport("USER32.DLL", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(HWND hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("USER32.DLL", CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(HWND hWnd);

        [DllImport("USER32.DLL", CharSet = CharSet.Auto)]
        private static extern bool IsWindowVisible(HWND hWnd);

        [DllImport("USER32.DLL", CharSet = CharSet.Auto)]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        private const UInt32 WM_CLOSE = 0x0010;

        public static void CloseWindow(IntPtr hwnd)
        {
            SendMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        public static void LaunchWindowsClosingThread(CancellationToken token)
        {
            Thread thread = new Thread(() =>
            {
                double timeoutDuration = 300; //min
                timeoutDuration *= 60; //min to seconds
                timeoutDuration *= 1000; //seconds to milliseconds

                CancellationToken ct = token;
                int pid = Process.GetCurrentProcess().Id;
                Stopwatch timer = new Stopwatch();
                timer.Start();
                while (true && timer.ElapsedMilliseconds < (timeoutDuration))
                {
                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }
                    foreach (var x in GetOpenWindows())
                    {

                        var a = GetOpenChildWindows(x.Key);

                        GetWindowThreadProcessId(x.Key, out uint windowPid);
                        if (windowPid == pid)
                        {
                            //sb.AppendLine($"{pid} - {x.Value}: {string.Join(";",a.Select(y=> $"{y.Key}= {y.Value}"))}");
                            foreach (var item in a)
                            {
                                string body = item.Value.ToLower();
                                Log.Debug(body);  // Debug
                                //if (body.Contains("warning:") && !body.Contains("error:") && (body.Contains("couch")
                                //            || body.Contains("electron")
                                //            || body.Contains("body"))
                                //            || body.Contains("machine model in treatment plan ")
                                //            || body.Contains("field has an opening that is smaller than the smallest measured")
                                //            || body.Contains("density")
                                //            || body.Contains("field")
                                //            || body.Contains("calculat")
                                //            || body.Contains("minimum hu value in the image")
                                //            || body.Contains("conversion curve is correctly calibrated")
                                //            || body.Contains("dose in isocenter") 
                                //            || body.Contains("gantry rotation")
                                //            || body.Contains("low")
                                //            )
                                if (body.Contains("collimator positions"))
                                {
                                    Log.Warning($"Popup Messages:\n{body}");
                                    // can not use console, conflict with close window
                                    //Console.WriteLine($"Popup Messages:\n{body}");
                                    //Console.WriteLine("before close window\n\n\n\n");
                                    Thread.Sleep(2000);
                                    CloseWindow(x.Key);
                                    //Console.WriteLine("after close window");
                                }
                                else if (body.Contains("warning"))
                                {
                                    Log.Warning($"Popup Messages:\n{body}");
                                    // can not use console, conflict with close window
                                    //Console.WriteLine($"Popup Messages:\n{body}");
                                    //Console.WriteLine("before close window\n\n\n\n");
                                    Thread.Sleep(2000);
                                    CloseWindow(x.Key);
                                    //Console.WriteLine("after close window");
                                }
                            }
                        }
                    }
                    Thread.Sleep(60);
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
    }
}
