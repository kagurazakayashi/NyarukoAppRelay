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
    private string _cmdA, _cmdE, _customTitle;

    /// <summary>
    /// 初始化監控上下文
    /// </summary>
    public RelayContext(string cmdA, string cmdE, string iconPath, string title)
    {
        _cmdA = cmdA;
        _cmdE = cmdE;
        _customTitle = title;
        _startTime = DateTime.Now;
    }
}