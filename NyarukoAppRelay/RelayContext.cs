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
    private NotifyIcon notifyIcon;
    private string _cmdA;
    private string _cmdE;

    public RelayContext(string cmdA, string cmdE)
    {
        _cmdA = cmdA;
        _cmdE = cmdE;

        notifyIcon = new NotifyIcon()
        {
            Icon = SystemIcons.Application,
            ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("退出监控", (s, e) => ExitThread())
                }),
            Text = "进程监控工具运行中",
            Visible = true
        };

        StartMonitoring();
    }

    private void StartMonitoring()
    {
        try
        {
            // 解析命令和参数
            var startInfoA = ParseCommand(_cmdA);
            Process procA = new Process { StartInfo = startInfoA };

            // 重要：在 .NET Framework 中开启事件支持
            procA.EnableRaisingEvents = true;

            // 订阅退出事件
            procA.Exited += (sender, e) =>
            {
                // 进程 A 退出后，执行进程 E
                try
                {
                    Process.Start(ParseCommand(_cmdE));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("启动结束进程失败: " + ex.Message);
                }
                finally
                {
                    // 结束本程序
                    ExitThread();
                }
            };

            procA.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show("监控启动失败: " + ex.Message);
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
            fileName = command.Substring(1, nextQuote - 1);
            arguments = command.Substring(nextQuote + 1).Trim();
        }
        else
        {
            int firstSpace = command.IndexOf(" ");
            if (firstSpace > 0)
            {
                fileName = command.Substring(0, firstSpace);
                arguments = command.Substring(firstSpace + 1).Trim();
            }
            else
            {
                fileName = command;
            }
        }
        return new ProcessStartInfo(fileName, arguments) { UseShellExecute = true };
    }

    protected override void ExitThreadCore()
    {
        if (notifyIcon != null) notifyIcon.Visible = false;
        base.ExitThreadCore();
    }
}