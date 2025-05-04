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
    private Icon _customIcon;

    public RelayContext(string cmdA, string cmdE, string iconPath, string title)
    {
        _cmdA = cmdA;
        _cmdE = cmdE;

        // 尝试加载自定义图标
        Icon displayIcon = SystemIcons.Application; // 默认图标
        if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
        {
            try
            {
                _customIcon = new Icon(iconPath);
                displayIcon = _customIcon;
            }
            catch
            {
                // 如果图标文件格式错误，保持默认
            }
        }

        _trayIcon = new NotifyIcon()
        {
            Icon = displayIcon,
            // Windows 通知栏 Text 长度限制为 127 字符
            Text = title.Length > 127 ? title.Substring(0, 124) + "..." : title,
            ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("退出 NyarukoAppRelay", (s, e) => ExitThread())
                }),
            Visible = true
        };

        ExecuteRelay();
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
                try
                {
                    Process.Start(ParseCommand(_cmdE));
                }
                catch { }
                finally
                {
                    ExitThread();
                }
            };

            if (!procA.Start())
            {
                ExitThread();
            }
        }
        catch
        {
            ExitThread();
        }
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
            _customIcon.Dispose(); // 释放外部图标资源
        }
        base.ExitThreadCore();
    }
}