using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

/// <summary>
/// 核心邏輯處理類別
/// </summary>
public class RelayContext : ApplicationContext
{
    // --- Win32 API 宣告 ---
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

    /// <summary>
    /// 建構式：初始化系統匣圖示與事件監聽
    /// </summary>
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
                    new MenuItem("详细信息(&D)", (s, e) => ShowDetailedMessage()),
                    new MenuItem("退出程序(&X)", (s, e) => FinalExit()),
                })
        };

        // 註冊滑鼠點擊事件處理
        _trayIcon.MouseClick += TrayIcon_MouseClick;

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
    /// 處理系統匣圖示點擊事件
    /// </summary>
    private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
    {
        // 僅回應滑鼠左鍵點擊
        if (e.Button == MouseButtons.Left)
        {
            ShowDetailedMessage();
        }
    }

    /// <summary>
    /// 顯示詳細資訊彈窗（簡體中文顯示）
    /// </summary>
    private void ShowDetailedMessage()
    {
        TimeSpan duration = DateTime.Now - _startTime;
        string timeStr = $"{(int)duration.TotalHours}小时 {duration.Minutes}分 {duration.Seconds}秒";

        int winCount = 0;
        string windowListText = "无可见窗口";
        const string emsp = "　";
        if (_procA != null && !_procA.HasExited)
        {
            var titles = GetWindowDetails(_procA.Id);
            winCount = titles.Count;
            if (winCount > 0)
                windowListText = string.Join("\n" + emsp, titles);
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(_customTitle ?? "NyarukoAppRelay");
        sb.AppendLine();
        sb.AppendLine($"执行程序:");
        sb.AppendLine(emsp + _cmdA);
        sb.AppendLine($"程序结束后运行:");
        sb.AppendLine(emsp + (_cmdE ?? "未指定"));
        sb.AppendLine($"监控模式: {(_useWindowMode ? "窗口监控模式" : "进程监控模式")}");
        sb.AppendLine($"运行时长: {timeStr}");
        sb.AppendLine($"程序开启的窗口 ( {winCount} ):");
        sb.AppendLine(emsp + windowListText);
        sb.AppendLine();
        sb.AppendLine("要停止监控吗？");

        Assembly assembly = Assembly.GetExecutingAssembly();
        AssemblyName assemblyName = assembly.GetName();
        string assemblyVersion = assemblyName.Version?.ToString() ?? "";

        if (MessageBox.Show(sb.ToString(), "NyarukoAppRelay v" + assemblyVersion + " by KagurazakaYashi", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            FinalExit();
        }
    }

    /// <summary>
    /// 獲取指定 PID 的所有可見視窗標題列表
    /// </summary>
    private List<string> GetWindowDetails(int processId)
    {
        List<string> titles = new List<string>();
        EnumWindows((hWnd, lParam) =>
        {
            uint windowPid;
            GetWindowThreadProcessId(hWnd, out windowPid);
            if (windowPid == processId && IsWindowVisible(hWnd))
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

    private int GetWindowCount(int processId)
    {
        int count = 0;
        EnumWindows((hWnd, lParam) =>
        {
            uint windowPid;
            GetWindowThreadProcessId(hWnd, out windowPid);
            if (windowPid == processId && IsWindowVisible(hWnd)) count++;
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
            int windowCount = GetWindowCount(_procA.Id);
            if (windowCount > 0) _windowHasAppeared = true;
            else if (_windowHasAppeared) HandleProcAEnd();
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
        string info = $"{(string.IsNullOrEmpty(_customTitle) ? "NyarukoAppRelay" : (_customTitle.Length > 32 ? _customTitle.Substring(0, 28) + "..." : _customTitle))}\n" +
                      //$"{fname1}\n" +
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