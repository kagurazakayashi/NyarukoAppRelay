using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

/// <summary>
/// 核心邏輯處理類別：管理系統匣圖示與處理序生命週期
/// </summary>
public class RelayContext : ApplicationContext
{
    private NotifyIcon _trayIcon;
    private string _cmdA, _cmdE, _customTitle;
    private DateTime _startTime;
    private Timer _statusTimer;
    private Icon _managedIcon;
    private bool _isExiting = false;

    // 監控相關變數
    private Process _procA;
    private bool _useWindowMode;
    private bool _windowHasAppeared = false;

    /// <summary>
    /// 初始化監控環境
    /// </summary>
    public RelayContext(string cmdA, string cmdE, string iconPath, string title, bool useWindowMode)
    {
        _cmdA = cmdA;
        _cmdE = cmdE;
        _customTitle = title;
        _useWindowMode = useWindowMode;
        _startTime = DateTime.Now;

        // 配置系統匣通知區域圖示
        _trayIcon = new NotifyIcon()
        {
            Icon = LoadSmartIcon(iconPath, _cmdA),
            Visible = true,
            ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("退出程序", (s, e) => FinalExit())
                })
        };

        // 啟動狀態計時器，每秒執行一次
        _statusTimer = new Timer { Interval = 1000 };
        _statusTimer.Tick += (s, e) => {
            UpdateTooltip();
            if (_useWindowMode) CheckWindowStatus();
        };
        _statusTimer.Start();

        // 延後執行監控任務以確保環境就緒
        Timer startTimer = new Timer { Interval = 100 };
        startTimer.Tick += (s, e) => {
            startTimer.Stop();
            startTimer.Dispose();
            ExecuteRelay();
        };
        startTimer.Start();
    }

    /// <summary>
    /// 視窗模式下的核心監控邏輯
    /// </summary>
    private void CheckWindowStatus()
    {
        if (_procA == null || _isExiting || !_useWindowMode) return;

        try
        {
            // 重新刷新處理序快取資訊
            _procA.Refresh();

            if (_procA.HasExited)
            {
                HandleProcAEnd();
                return;
            }

            // 判斷主視窗控制代碼是否存在
            if (_procA.MainWindowHandle != IntPtr.Zero)
            {
                // 標記視窗已經成功出現過
                _windowHasAppeared = true;
            }
            else if (_windowHasAppeared)
            {
                // 若視窗出現後又消失，則判定為結束
                HandleProcAEnd();
            }
        }
        catch { }
    }

    /// <summary>
    /// 當 A 處理序被判定為結束時，執行 E 處理序並關閉本程式
    /// </summary>
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

    /// <summary>
    /// 智能載入圖示邏輯：/I > /A 執行檔 > 自身
    /// </summary>
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

    /// <summary>
    /// 提取命令列中的執行檔路徑
    /// </summary>
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

    /// <summary>
    /// 更新系統匣圖示的提示資訊與執行時間
    /// </summary>
    private void UpdateTooltip()
    {
        if (_isExiting) return;
        TimeSpan duration = DateTime.Now - _startTime;
        string timeStr = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        string info = $"{(string.IsNullOrEmpty(_customTitle) ? "NyarukoAppRelay" : _customTitle)}\n" +
                      $"A: {Path.GetFileName(ExtractExePath(_cmdA))}\n" +
                      $"E: {(string.IsNullOrEmpty(_cmdE) ? "无" : Path.GetFileName(ExtractExePath(_cmdE)))}\n" +
                      $"运行时长: {timeStr}";
        _trayIcon.Text = info.Length > 127 ? info.Substring(0, 124) + "..." : info;
    }

    /// <summary>
    /// 啟動被監控的處理序 A
    /// </summary>
    private void ExecuteRelay()
    {
        try
        {
            _procA = new Process { StartInfo = ParseCommand(_cmdA) };
            _procA.EnableRaisingEvents = true;

            // 無論是否開啟 /W，都保留處理序退出事件作為基礎保障
            _procA.Exited += (s, e) => HandleProcAEnd();

            if (!_procA.Start()) throw new Exception("启动返回 false");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"/A 执行失败: {ex.Message}", "运行错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            FinalExit();
        }
    }

    /// <summary>
    /// 解析帶有參數的命令列字串
    /// </summary>
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

    /// <summary>
    /// 清理資源並徹底終止程式
    /// </summary>
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