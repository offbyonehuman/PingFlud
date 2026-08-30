using System;
using System.Windows.Forms;
using WinFormsApplication = System.Windows.Forms.Application;

namespace PingFlud.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        WinFormsApplication.Run(new MainForm());
    }
}
