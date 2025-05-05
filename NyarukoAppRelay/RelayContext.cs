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
    private string _cmdA;
    private string _cmdE;
    private Icon _customIcon; // 用于存储需要手动释放的图标资源

    public RelayContext(string cmdA, string cmdE, string iconPath, string title)
    {
        _cmdA = cmdA;
        _cmdE = cmdE;

        // 调用改进后的图标加载逻辑
        Icon displayIcon = LoadDisplayIcon(iconPath);

        _trayIcon = new NotifyIcon()
        {
            Icon = displayIcon,
            Text = title.Length > 127 ? title.Substring(0, 124) + "..." : title,
            ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("退出 NyarukoAppRelay", (s, e) => ExitThread())
                }),
            Visible = true
        };

        ExecuteRelay();
    }

    private Icon LoadDisplayIcon(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return SystemIcons.Application;

        try
        {
            string extension = Path.GetExtension(path).ToLower();

            if (extension == ".exe" || extension == ".dll" || extension == ".lnk")
            {
                // 从可执行文件或快捷方式中提取关联图标
                // 注意：ExtractAssociatedIcon 返回的是一个新的 Icon 对象
                _customIcon = Icon.ExtractAssociatedIcon(path);
                return _customIcon;
            }
            else
            {
                // 按标准图标文件加载
                _customIcon = new Icon(path);
                return _customIcon;
            }
        }
        catch
        {
            // 如果文件损坏或不是合法的图标源，返回系统默认图标
            return SystemIcons.Application;
        }
    }

    private void ExecuteRelay()
    {
        try
        {
            ProcessStartInfo startInfoA = ParseCommand(_cmdA);
            Process procA = new Process { StartInfo = startInfoA };
            procA.EnableRaisingEvents = true;

            procA.Exited += (s, e) =>
            {
                try { Process.Start(ParseCommand(_cmdE)); }
                catch { }
                finally { ExitThread(); }
            };

            if (!procA.Start()) ExitThread();
        }
        catch { ExitThread(); }
    }

    private ProcessStartInfo ParseCommand(string command)
    {
        command = command.Trim();
        string fileName;
        string arguments = "";

        if (command.StartsWith("\""))
        {
            int nextQuote = command.IndexOf("\"", 1);
            if (nextQuote != -1)
            {
                fileName = command.Substring(1, nextQuote - 1);
                arguments = command.Substring(nextQuote + 1).Trim();
            }
            else { fileName = command; }
        }
        else
        {
            int firstSpace = command.IndexOf(" ");
            if (firstSpace > 0)
            {
                fileName = command.Substring(0, firstSpace);
                arguments = command.Substring(firstSpace + 1).Trim();
            }
            else { fileName = command; }
        }
        return new ProcessStartInfo(fileName, arguments) { UseShellExecute = true };
    }

    protected override void ExitThreadCore()
    {
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        if (_customIcon != null)
        {
            _customIcon.Dispose();
        }
        base.ExitThreadCore();
    }
}