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

    public MyCustomApplicationContext(string cmdA, string cmdE)
    {
        _cmdA = cmdA;
        _cmdE = cmdE;

        // 初始化通知栏图标
        notifyIcon = new NotifyIcon()
        {
            // 使用系统默认图标，你也可以通过 Icon.ExtractAssociatedIcon 提取图标
            Icon = SystemIcons.Shield,
            ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("退出", (s, e) => ExitThread())
                }),
            Text = $"监控中: {cmdA}",
            Visible = true
        };

        StartMonitoring();
    }

    private async void StartMonitoring()
    {
        try
        {
            // 1. 启动第一个进程 (A)
            ProcessStartInfo startInfoA = ParseCommand(_cmdA);
            using (Process procA = Process.Start(startInfoA))
            {
                if (procA == null) throw new Exception("无法启动进程 A");

                // 2. 持续监控直到退出
                // 使用异步等待，避免阻塞 UI 线程（保证通知栏图标响应）
                await procA.WaitForExitAsync();
            }

            // 3. 执行第二个进程 (E)
            ProcessStartInfo startInfoE = ParseCommand(_cmdE);
            Process.Start(startInfoE);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"运行出错: {ex.Message}");
        }
        finally
        {
            // 4. 完成任务后退出本程序
            ExitThread();
        }
    }

    private ProcessStartInfo ParseCommand(string command)
    {
        // 处理带参数的字符串（例如：notepad.exe C:\1.txt）
        // 简单处理：取第一个空格前的作为文件名，剩下的作为参数
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
        notifyIcon.Visible = false; // 退出前隐藏图标，防止图标残留在通知栏
        base.ExitThreadCore();
    }
}