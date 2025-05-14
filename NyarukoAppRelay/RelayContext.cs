using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

/// <summary>
/// 核心邏輯處理類別
/// </summary>
public class RelayContext : ApplicationContext
{
    // 匯入 Win32 API 用於枚舉視窗
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private NotifyIcon _trayIcon;
    private string _cmdA, _cmdE, _customTitle;
    private DateTime _startTime;
    private Timer _statusTimer;
    private Icon _managedIcon;
    private bool _isExiting = false;

    private Process _procA;
    private bool _useWindowMode;
    private bool _windowHasAppeared = false;

    public RelayContext(string cmdA, string cmdE, string iconPath, string title, bool useWindowMode)
    {
        _cmdA = cmdA;
        _cmdE = cmdE;
        _customTitle = title;
        _useWindowMode = useWindowMode;
        _startTime = DateTime.Now;

        _trayIcon = new NotifyIcon()
        {
            Icon = LoadSmartIcon(iconPath, _cmdA),
            Visible = true,
            ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("退出程序", (s, e) => FinalExit())
                })
        };

        _statusTimer = new Timer { Interval = 1000 };
        _statusTimer.Tick += (s, e) => {
            UpdateTooltip();
            if (_useWindowMode) CheckWindowStatus();
        };
        _statusTimer.Start();

        Timer startTimer = new Timer { Interval = 100 };
        startTimer.Tick += (s, e) => {
            startTimer.Stop();
            startTimer.Dispose();
            ExecuteRelay();
        };
        startTimer.Start();
    }

    /// <summary>
    /// 計算指定處理序當前開啟的視窗數量
    /// </summary>
    private int GetWindowCount(int processId)
    {
        int count = 0;
        EnumWindows((hWnd, lParam) =>
        {
            uint windowPid;
            GetWindowThreadProcessId(hWnd, out windowPid);
            // 僅計算 PID 匹配且目前可見的視窗
            if (windowPid == processId && IsWindowVisible(hWnd))
            {
                count++;
            }
            return true;
        }, IntPtr.Zero);
        return count;
    }

    private void CheckWindowStatus()
    {
        if (_procA == null || _isExiting || !_useWindowMode) return;
        try
        {
            _procA.Refresh();
            if (_procA.HasExited)
            {
                HandleProcAEnd();
                return;
            }

            // 透過 Win32 API 檢查視窗是否存在
            int windowCount = GetWindowCount(_procA.Id);
            if (windowCount > 0)
            {
                _windowHasAppeared = true;
            }
            else if (_windowHasAppeared)
            {
                HandleProcAEnd();
            }
        }
        catch { }
    }

    private void HandleProcAEnd()
    {
        if (_isExiting) return;
        if (!string.IsNullOrEmpty(_cmdE))
        {
            try { Process.Start(ParseCommand(_cmdE)); }
            catch (Exception ex)
            {
                MessageBox.Show($"/E 执行失败: {ex.Message}", "运行错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        FinalExit();
    }

    private Icon LoadSmartIcon(string iPath, string aCmd)
    {
        try
        {
            if (!string.IsNullOrEmpty(iPath) && File.Exists(iPath))
            {
                if (Path.GetExtension(iPath).ToLower() == ".ico") return _managedIcon = new Icon(iPath);
                return _managedIcon = Icon.ExtractAssociatedIcon(iPath);
            }
            string exePath = ExtractExePath(aCmd);
            if (File.Exists(exePath)) return _managedIcon = Icon.ExtractAssociatedIcon(exePath);
        }
        catch { }
        try { return _managedIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location); }
        catch { return SystemIcons.Application; }
    }

    private string ExtractExePath(string command)
    {
        command = command.Trim();
        if (command.StartsWith("\""))
        {
            int nextQuote = command.IndexOf("\"", 1);
            return nextQuote != -1 ? command.Substring(1, nextQuote - 1) : command;
        }
        int space = command.IndexOf(" ");
        return space != -1 ? command.Substring(0, space) : command;
    }

    private void UpdateTooltip()
    {
        if (_isExiting) return;
        TimeSpan duration = DateTime.Now - _startTime;
        string timeStr = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";

        // 獲取當前視窗數量
        int winCount = 0;
        if (_procA != null && !_procA.HasExited)
        {
            winCount = GetWindowCount(_procA.Id);
        }
        string fname1 = Path.GetFileName(ExtractExePath(_cmdA));
        fname1 = fname1.Length > 16 ? fname1.Substring(0, 12) + "..." : fname1;
        string fname2 = string.IsNullOrEmpty(_cmdE) ? "无" : Path.GetFileName(ExtractExePath(_cmdE));
        fname2 = fname2.Length > 16 ? fname2.Substring(0, 12) + "..." : fname2;
        string info = $"{(string.IsNullOrEmpty(_customTitle) ? "NyarukoAppRelay" : (_customTitle.Length > 16 ? _customTitle.Substring(0, 12) + "..." : _customTitle))}\n" +
                      $"{fname1}\n" +
                      //$"结束: {fname2}\n" +
                      $"窗口数量: {winCount}\n" +
                      $"运行时长: {timeStr}";

        _trayIcon.Text = info.Length > 64 ? info.Substring(0, 60) + "..." : info;
    }

    private void ExecuteRelay()
    {
        try
        {
            _procA = new Process { StartInfo = ParseCommand(_cmdA) };
            _procA.EnableRaisingEvents = true;
            _procA.Exited += (s, e) => HandleProcAEnd();
            if (!_procA.Start()) throw new Exception("启动返回 false");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"/A 执行失败: {ex.Message}", "运行错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            FinalExit();
        }
    }

    private ProcessStartInfo ParseCommand(string cmd)
    {
        string path = ExtractExePath(cmd).Replace("\"", "");
        string args = "";
        string trimmed = cmd.Trim();
        if (trimmed.StartsWith("\""))
        {
            int endQuote = trimmed.IndexOf("\"", 1);
            if (endQuote != -1 && trimmed.Length > endQuote + 1)
                args = trimmed.Substring(endQuote + 1).Trim();
        }
        else
        {
            int space = trimmed.IndexOf(" ");
            if (space != -1) args = trimmed.Substring(space + 1).Trim();
        }
        return new ProcessStartInfo(path, args) { UseShellExecute = true };
    }

    private void FinalExit()
    {
        if (_isExiting) return;
        _isExiting = true;
        if (_statusTimer != null) { _statusTimer.Stop(); _statusTimer.Dispose(); }
        if (_trayIcon != null) { _trayIcon.Visible = false; _trayIcon.Dispose(); }
        if (_managedIcon != null) { _managedIcon.Dispose(); }
        if (_procA != null) { _procA.Dispose(); }
        Environment.Exit(0);
    }
}