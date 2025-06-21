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
    /// 核心監控邏輯處理類別，繼承自 ApplicationContext 以實現無表單運行
    /// </summary>
    public class RelayContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private string _cmdA, _cmdE, _customTitle;
        private DateTime _startTime;
        private Timer _statusTimer;
        private Icon _managedIcon;
        private bool _isExiting = false;

        private Process _procA;
        private bool _useWindowMode;
        private bool _windowHasAppeared = false;

        /// <summary>
        /// 初始化監控上下文，設置系統匣圖示與啟動監控計時器
        /// </summary>
        public RelayContext(string cmdA, string cmdE, string iconPath, string title, bool useWindowMode)
        {
            _cmdA = cmdA;
            _cmdE = cmdE;
            _customTitle = title;
            _useWindowMode = useWindowMode;
            _startTime = DateTime.Now;

            // 初始化系統匣圖示與右鍵選單
            _trayIcon = new NotifyIcon()
            {
                Icon = LoadSmartIcon(iconPath, _cmdA),
                Visible = true,
                ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("详细信息(&D)", (s, e) => ShowDetailedMessage()),
                    new MenuItem("-"),
                    new MenuItem("退出程序(&X)", (s, e) => FinalExit()),
                })
            };

            // 註冊滑鼠左鍵點擊事件
            _trayIcon.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) ShowDetailedMessage(); };

            // 狀態更新計時器 (每秒執行)
            _statusTimer = new Timer { Interval = 1000 };
            _statusTimer.Tick += (s, e) => {
                UpdateTooltip();
                if (_useWindowMode) CheckWindowStatus();
            };
            _statusTimer.Start();

            // 延遲啟動處理序執行邏輯，確保訊息迴圈已啟動
            Timer startTimer = new Timer { Interval = 100 };
            startTimer.Tick += (s, e) => {
                startTimer.Stop();
                startTimer.Dispose();
                ExecuteRelay();
            };
            startTimer.Start();
        }

        /// <summary>
        /// 彈出詳細資訊對話框，顯示目前的監控狀態
        /// </summary>
        private void ShowDetailedMessage()
        {
            TimeSpan duration = DateTime.Now - _startTime;
            string timeStr = $"{(int)duration.TotalHours}小时 {duration.Minutes}分 {duration.Seconds}秒";

            int winCount = 0;
            string windowListText = "无可见窗口";
            const string emsp = "　"; // 全形空格用於排版縮排

            if (_procA != null && !_procA.HasExited)
            {
                var titles = Win32Helper.GetVisibleWindowTitles(_procA.Id);
                winCount = titles.Count;
                if (winCount > 0)
                    windowListText = string.Join("\n" + emsp, titles);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(_customTitle ?? "NyarukoAppRelay");
            sb.AppendLine();
            sb.AppendLine($"执行程序:");
            sb.AppendLine(emsp + _cmdA);
            sb.AppendLine($"程序结束后运行:");
            sb.AppendLine(emsp + (_cmdE ?? "未指定"));
            sb.AppendLine($"监控模式: {(_useWindowMode ? "窗口监控模式" : "进程监控模式")}");
            sb.AppendLine($"运行时长: {timeStr}");
            sb.AppendLine($"程序开启的窗口 ( {winCount} ):");
            sb.AppendLine(emsp + windowListText);
            sb.AppendLine();
            sb.AppendLine("要停止监控吗？");

            // 獲取目前的版本資訊
            string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

            if (MessageBox.Show(sb.ToString(), "NyarukoAppRelay v" + assemblyVersion + " by KagurazakaYashi", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                FinalExit();
            }
        }

        /// <summary>
        /// 檢查視窗狀態（僅在 /W 模式下使用）
        /// </summary>
        private void CheckWindowStatus()
        {
            if (_procA == null || _isExiting || !_useWindowMode) return;
            try
            {
                _procA.Refresh();
                if (_procA.HasExited)
                {
                    HandleProcAEnd();
                    return;
                }

                int windowCount = Win32Helper.GetVisibleWindowCount(_procA.Id);
                if (windowCount > 0) _windowHasAppeared = true;
                else if (_windowHasAppeared) HandleProcAEnd();
            }
            catch { }
        }

        /// <summary>
        /// 處理序 A 判定結束後的操作，嘗試執行 E 並退出
        /// </summary>
        private void HandleProcAEnd()
        {
            if (_isExiting) return;
            if (!string.IsNullOrEmpty(_cmdE))
            {
                try { Process.Start(ParseCommand(_cmdE)); }
                catch (Exception ex)
                {
                    MessageBox.Show($"/E 执行失败: {ex.Message}", "运行错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            FinalExit();
        }

        /// <summary>
        /// 載入合適的圖示（優先順序：自定義路徑 > 目標程式圖示 > 自身圖示）
        /// </summary>
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

        /// <summary>
        /// 從帶有參數的命令列中提取純執行檔路徑
        /// </summary>
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

        /// <summary>
        /// 更新系統匣圖示的懸停提示文字
        /// </summary>
        private void UpdateTooltip()
        {
            if (_isExiting) return;
            TimeSpan duration = DateTime.Now - _startTime;
            string timeStr = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";

            int winCount = (_procA != null && !_procA.HasExited) ? Win32Helper.GetVisibleWindowCount(_procA.Id) : 0;

            // 處理標題長度限制，防止溢出
            string displayTitle = string.IsNullOrEmpty(_customTitle) ? "NyarukoAppRelay" :
                (_customTitle.Length > 32 ? _customTitle.Substring(0, 28) + "..." : _customTitle);

            string info = $"{displayTitle}\n窗口数量: {winCount}\n运行时长: {timeStr}";

            // NotifyIcon.Text 限制在 64 字元內以確保舊版系統相容性
            _trayIcon.Text = info.Length > 63 ? info.Substring(0, 60) + "..." : info;
        }

        /// <summary>
        /// 啟動監控對象處理序 A
        /// </summary>
        private void ExecuteRelay()
        {
            try
            {
                _procA = new Process { StartInfo = ParseCommand(_cmdA) };
                _procA.EnableRaisingEvents = true;
                _procA.Exited += (s, e) => HandleProcAEnd();
                if (!_procA.Start()) throw new Exception("启动返回 false");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"/A 执行失败: {ex.Message}", "运行错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                FinalExit();
            }
        }

        /// <summary>
        /// 將命令列字串解析為處理序啟動資訊物件
        /// </summary>
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
        /// 執行清理工作並徹底結束應用程式處理序
        /// </summary>
        private void FinalExit()
        {
            if (_isExiting) return;
            _isExiting = true;
            if (_statusTimer != null) { _statusTimer.Stop(); _statusTimer.Dispose(); }
            if (_trayIcon != null) { _trayIcon.Visible = false; _trayIcon.Dispose(); }
            if (_managedIcon != null) { _managedIcon.Dispose(); }
            if (_procA != null) { _procA.Dispose(); }
            Environment.Exit(0);
        }
    }
}
