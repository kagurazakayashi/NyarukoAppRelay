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
    private Timer _updateTimer;
    private Icon _managedIcon;

    public RelayContext(string cmdA, string cmdE, string iconPath, string title)
    {
        _cmdA = cmdA;
        _cmdE = cmdE;
        _customTitle = title;
        _startTime = DateTime.Now;

        // 图标逻辑：优先 /I，其次提取 /A 的图标，最后默认
        Icon displayIcon = LoadSmartIcon(iconPath, _cmdA);

        _trayIcon = new NotifyIcon()
        {
            Icon = displayIcon,
            Visible = true,
            ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("退出程序", (s, e) => ExitThread())
                })
        };

        // 启动定时器更新提示信息
        _updateTimer = new Timer { Interval = 1000 }; // 每秒更新一次
        _updateTimer.Tick += (s, e) => UpdateTooltip();
        _updateTimer.Start();
        UpdateTooltip(); // 立即执行一次

        ExecuteRelay();
    }

    private Icon LoadSmartIcon(string iPath, string aCmd)
    {
        try
        {
            // 情况 1: 用户指定了图标
            if (!string.IsNullOrEmpty(iPath) && File.Exists(iPath))
            {
                if (Path.GetExtension(iPath).ToLower() == ".ico") return _managedIcon = new Icon(iPath);
                return _managedIcon = Icon.ExtractAssociatedIcon(iPath);
            }

            // 情况 2: 尝试从 /A 的 EXE 中提取
            string exePath = ExtractExePath(aCmd);
            if (File.Exists(exePath))
            {
                return _managedIcon = Icon.ExtractAssociatedIcon(exePath);
            }
        }
        catch { }

        return SystemIcons.Application; // 兜底默认图标
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
        TimeSpan duration = DateTime.Now - _startTime;
        string timeStr = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";

        string info = $"{(string.IsNullOrEmpty(_customTitle) ? "NyarukoRelay" : _customTitle)}\n" +
                      $"A: {Path.GetFileName(ExtractExePath(_cmdA))}\n" +
                      $"E: {(string.IsNullOrEmpty(_cmdE) ? "无" : Path.GetFileName(ExtractExePath(_cmdE)))}\n" +
                      $"运行时长: {timeStr}";

        // 截断以防超过通知栏长度限制
        _trayIcon.Text = info.Length > 127 ? info.Substring(0, 124) + "..." : info;
    }

    private void ExecuteRelay()
    {
        try
        {
            Process procA = new Process { StartInfo = ParseCommand(_cmdA) };
            procA.EnableRaisingEvents = true;
            procA.Exited += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_cmdE))
                {
                    try { Process.Start(ParseCommand(_cmdE)); } catch { }
                }
                ExitThread();
            };

            if (!procA.Start()) ExitThread();
        }
        catch (Exception ex)
        {
            MessageBox.Show("执行失败: " + ex.Message);
            ExitThread();
        }
    }

    private ProcessStartInfo ParseCommand(string cmd)
    {
        string path = ExtractExePath(cmd);
        string args = cmd.Length > path.Length ? cmd.Substring(cmd.IndexOf(path) + path.Length).Trim() : "";
        // 如果路径带引号，需要去掉引号后传给 ProcessStartInfo
        path = path.Replace("\"", "");
        return new ProcessStartInfo(path, args) { UseShellExecute = true };
    }

    protected override void ExitThreadCore()
    {
        _updateTimer?.Stop();
        if (_trayIcon != null) { _trayIcon.Visible = false; _trayIcon.Dispose(); }
        _managedIcon?.Dispose();
        base.ExitThreadCore();
    }
}