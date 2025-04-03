using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace NyarukoAppRelay
{
    /// <summary>
    /// 程式主要入口類別
    /// </summary>
    static class Program
    {
        /// <summary>
        /// 應用程式的主要進入點
        /// </summary>
        /// <param name="args">命令列參數</param>
        [STAThread]
        static void Main(string[] args)
        {
            // 啟用應用程式視覺樣式與文字渲染設定
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run();
        }
    }
}