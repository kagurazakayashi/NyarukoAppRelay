using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

/// <summary>
/// 處理系統匣圖示與處理序監控的應用程式上下文
/// </summary>
public class RelayContext : ApplicationContext
{
    private NotifyIcon _trayIcon;
    private string _cmdA, _cmdE, _customTitle;
    private DateTime _startTime;
    private Timer _statusTimer;
    private Icon _managedIcon;
    private bool _isExiting = false;

    /// <summary>
    /// 初始化監控上下文並設置系統匣圖示
    /// </summary>
    public RelayContext(string cmdA, string cmdE, string iconPath, string title)
    {
        _cmdA = cmdA;
        _cmdE = cmdE;
        _customTitle = title;
        _startTime = DateTime.Now;

        // 初始化系統匣通知圖示
        _trayIcon = new NotifyIcon()
        {
            Icon = LoadSmartIcon(iconPath, _cmdA),
            Visible = true,
            ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("退出程序", (s, e) => FinalExit())
                })
        };

        // 設置每秒更新一次工具提示資訊的計時器
        _statusTimer = new Timer { Interval = 1000 };
        _statusTimer.Tick += (s, e) => UpdateTooltip();
        _statusTimer.Start();

        // 延遲執行核心邏輯，確保訊息迴圈已完全啟動
        Timer startTimer = new Timer { Interval = 100 };
        startTimer.Tick += (s, e) => {
            startTimer.Stop();
            startTimer.Dispose();
            ExecuteRelay();
        };
        startTimer.Start();
    }

    /// <summary>
    /// 根據優先權載入合適的圖示
    /// </summary>
    private Icon LoadSmartIcon(string iPath, string aCmd)
    {
        try
        {
            // 1. 優先使用使用者指定的圖示檔案
            if (!string.IsNullOrEmpty(iPath) && File.Exists(iPath))
            {
                if (Path.GetExtension(iPath).ToLower() == ".ico") return _managedIcon = new Icon(iPath);
                return _managedIcon = Icon.ExtractAssociatedIcon(iPath);
            }
            // 2. 其次從目標執行檔中提取圖示
            string exePath = ExtractExePath(aCmd);
            if (File.Exists(exePath)) return _managedIcon = Icon.ExtractAssociatedIcon(exePath);
        }
        catch { }

        try
        {
            // 3. 最後嘗試使用本程式自身的圖示
            return _managedIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        }
        catch { return SystemIcons.Application; }
    }

    /// <summary>
    /// 從命令列字串中提取執行檔的路徑
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
    /// 更新系統匣圖示的懸停提示文字
    /// </summary>
    private void UpdateTooltip()
    {
        if (_isExiting) return;
        TimeSpan duration = DateTime.Now - _startTime;
        string timeStr = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";

        // 組合顯示資訊，包含 A/E 處理序名稱與執行時間
        string info = $"{(string.IsNullOrEmpty(_customTitle) ? "NyarukoAppRelay" : _customTitle)}\n" +
                      $"A: {Path.GetFileName(ExtractExePath(_cmdA))}\n" +
                      $"E: {(string.IsNullOrEmpty(_cmdE) ? "无" : Path.GetFileName(ExtractExePath(_cmdE)))}\n" +
                      $"运行时长: {timeStr}";

        // 確保字串長度不超過 WinForms NotifyIcon 的限制
        _trayIcon.Text = info.Length > 127 ? info.Substring(0, 124) + "..." : info;
    }

    /// <summary>
    /// 執行主監控任務：啟動 A 並在退出後執行 E
    /// </summary>
    private void ExecuteRelay()
    {
        try
        {
            Process procA = new Process { StartInfo = ParseCommand(_cmdA) };
            procA.EnableRaisingEvents = true;

            // 監聽 A 處理序退出事件
            procA.Exited += (s, e) => {
                if (!string.IsNullOrEmpty(_cmdE))
                {
                    try { Process.Start(ParseCommand(_cmdE)); }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"/E 执行失败: {ex.Message}", "运行错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                FinalExit();
            };

            if (!procA.Start()) throw new Exception("启动返回 false");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"/A 执行失败: {ex.Message}", "运行错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            FinalExit();
        }
    }

    /// <summary>
    /// 將包含參數的命令列字串解析為 ProcessStartInfo
    /// </summary>
    private ProcessStartInfo ParseCommand(string cmd)
    {
        string path = ExtractExePath(cmd).Replace("\"", "");
        string args = "";
        string trimmed = cmd.Trim();

        // 分離執行檔路徑與後續參數
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
    /// 執行最終清理並強制結束程式
    /// </summary>
    private void FinalExit()
    {
        if (_isExiting) return;
        _isExiting = true;

        // 停止計時器並釋放 UI 資源
        if (_statusTimer != null) { _statusTimer.Stop(); _statusTimer.Dispose(); }
        if (_trayIcon != null) { _trayIcon.Visible = false; _trayIcon.Dispose(); }
        if (_managedIcon != null) { _managedIcon.Dispose(); }

        // 終止當前處理序，確保不殘留背景執行緒
        Environment.Exit(0);
    }
}