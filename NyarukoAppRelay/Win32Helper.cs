using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NyarukoAppRelay
{
    /// <summary>
    /// 封裝 Windows 底層 API 的工具類別
    /// </summary>
    public static class Win32Helper
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        /// <summary>
        /// 獲取指定處理序 ID (PID) 的所有可見視窗標題
        /// </summary>
        /// <param name="processId">目標處理序 ID</param>
        /// <returns>標題字串清單</returns>
        public static List<string> GetVisibleWindowTitles(int processId)
        {
            List<string> titles = new List<string>();
            EnumWindows((hWnd, lParam) =>
            {
                uint windowPid;
                GetWindowThreadProcessId(hWnd, out windowPid);

                // 檢查是否屬於該處理序且視窗目前為可見狀態
                if (windowPid == (uint)processId && IsWindowVisible(hWnd))
                {
                    int length = GetWindowTextLength(hWnd);
                    if (length > 0)
                    {
                        StringBuilder sb = new StringBuilder(length + 1);
                        GetWindowText(hWnd, sb, sb.Capacity);
                        titles.Add(sb.ToString());
                    }
                    else
                    {
                        titles.Add("(无标题窗口)");
                    }
                }
                return true;
            }, IntPtr.Zero);
            return titles;
        }

        /// <summary>
        /// 獲取指定處理序的可見視窗數量
        /// </summary>
        public static int GetVisibleWindowCount(int processId)
        {
            int count = 0;
            EnumWindows((hWnd, lParam) =>
            {
                uint windowPid;
                GetWindowThreadProcessId(hWnd, out windowPid);
                if (windowPid == (uint)processId && IsWindowVisible(hWnd)) count++;
                return true;
            }, IntPtr.Zero);
            return count;
        }
    }
}
