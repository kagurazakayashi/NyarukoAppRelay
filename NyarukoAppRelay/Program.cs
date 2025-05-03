using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace TrayMonitorApp
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 解析参数
            string cmdA = GetArgValue(args, "/A");
            string cmdE = GetArgValue(args, "/E");

            if (string.IsNullOrEmpty(cmdA) || string.IsNullOrEmpty(cmdE))
            {
                MessageBox.Show("用法示例：YourApp.exe /A \"notepad.exe\" /E \"cmd.exe\"", "参数缺失");
                return;
            }

            // 启动无窗口上下文
            Application.Run(new RelayContext(cmdA, cmdE));
        }

        static string GetArgValue(string[] args, string key)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }
}