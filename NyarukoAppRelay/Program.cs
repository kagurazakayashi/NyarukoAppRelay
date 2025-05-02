using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
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
                MessageBox.Show("参数缺失！用法示例：\n/A \"notepad.exe\" /E \"cmd.exe\"", "错误");
                return;
            }

            // 使用 ApplicationContext 运行，不显示窗体
            Application.Run(new MyCustomApplicationContext(cmdA, cmdE));
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