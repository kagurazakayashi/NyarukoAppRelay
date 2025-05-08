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
    private bool _isExiting = false; // 防止重复调用的标记

    public RelayContext(string cmdA, string cmdE, string iconPath, string title)
    {
        _cmdA = cmdA;
        _cmdE = cmdE;
        _customTitle = title;
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
        _statusTimer.Tick += (s, e) => UpdateTooltip();
        _statusTimer.Start();

        // 延迟启动任务
        Timer startTimer = new Timer { Interval = 100 };
        startTimer.Tick += (s, e) => {
            startTimer.Stop();
            startTimer.Dispose();
            ExecuteRelay();
        };
        startTimer.Start();
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
        string info = $"{(string.IsNullOrEmpty(_customTitle) ? "NyarukoAppRelay" : _customTitle)}\n" +
                      $"A: {Path.GetFileName(ExtractExePath(_cmdA))}\n" +
                      $"E: {(string.IsNullOrEmpty(_cmdE) ? "无" : Path.GetFileName(ExtractExePath(_cmdE)))}\n" +
                      $"运行时长: {timeStr}";
        _trayIcon.Text = info.Length > 127 ? info.Substring(0, 124) + "..." : info;
    }

    private void ExecuteRelay()
    {
        try
        {
            Process procA = new Process { StartInfo = ParseCommand(_cmdA) };
            procA.EnableRaisingEvents = true;
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
    /// 统一的退出入口
    /// </summary>
    private void FinalExit()
    {
        if (_isExiting) return; // 确保清理逻辑只走一遍
        _isExiting = true;

        if (_statusTimer != null) { _statusTimer.Stop(); _statusTimer.Dispose(); }
        if (_trayIcon != null) { _trayIcon.Visible = false; _trayIcon.Dispose(); }
        if (_managedIcon != null) { _managedIcon.Dispose(); }

        // 直接终结进程，不再调用 ExitThread() 以防递归
        Environment.Exit(0);
    }

    // 彻底移除对 ExitThreadCore 的重写，避免死循环
}