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

        // 图标加载逻辑
        Icon displayIcon = LoadSmartIcon(iconPath, _cmdA);

        _trayIcon = new NotifyIcon()
        {
            Icon = displayIcon,
            Visible = true,
            ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("退出程序", (s, e) => ExitThread())
                })
        };

        _updateTimer = new Timer { Interval = 1000 };
        _updateTimer.Tick += (s, e) => UpdateTooltip();
        _updateTimer.Start();
        UpdateTooltip();

        ExecuteRelay();
    }

    private Icon LoadSmartIcon(string iPath, string aCmd)
    {
        try
        {
            // 1. 优先使用 /I 指定的图标
            if (!string.IsNullOrEmpty(iPath) && File.Exists(iPath))
            {
                if (Path.GetExtension(iPath).ToLower() == ".ico") return _managedIcon = new Icon(iPath);
                return _managedIcon = Icon.ExtractAssociatedIcon(iPath);
            }

            // 2. 其次尝试提取 /A 中的 EXE 图标
            string exePath = ExtractExePath(aCmd);
            if (File.Exists(exePath))
            {
                return _managedIcon = Icon.ExtractAssociatedIcon(exePath);
            }
        }
        catch { }

        // 3. 最后使用本程序自身的图标
        try
        {
            return _managedIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        }
        catch
        {
            return SystemIcons.Application; // 极度兜底
        }
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
            procA.Exited += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_cmdE))
                {
                    try
                    {
                        Process.Start(ParseCommand(_cmdE));
                    }
                    catch (Exception ex)
                    {
                        // 如果 /E 执行失败，弹出提示并退出
                        MessageBox.Show($"/E 执行失败: {ex.Message}", "运行错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                ExitThread();
            };

            if (!procA.Start()) throw new Exception("进程 A 启动返回 false");
        }
        catch (Exception ex)
        {
            // 如果 /A 执行失败，弹出提示并退出
            MessageBox.Show($"/A 执行失败: {ex.Message}", "运行错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ExitThread();
        }
    }

    private ProcessStartInfo ParseCommand(string cmd)
    {
        string path = ExtractExePath(cmd).Replace("\"", "");
        string args = cmd.Length > path.Length ? cmd.Substring(cmd.IndexOf(path) + path.Length).Trim().Trim('\"') : "";
        // 如果命令行整体有引号包裹逻辑，这里需要微调确保参数正确
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