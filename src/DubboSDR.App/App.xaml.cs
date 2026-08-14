using System;
using System.IO;
using System.Windows;
using System.Threading;

namespace DubboSDR.App
{
    public partial class App : Application
    {
        private int _crashCount = 0;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash(e.Exception);
            
            // Prevent recursive layout exceptions from spawning infinite message boxes
            if (Interlocked.Increment(ref _crashCount) == 1)
            {
                e.Handled = true; 
                MessageBox.Show($"DubboSDR crashed: {e.Exception.Message}\n\nCheck diagnostics/crash.log for details.", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
            else
            {
                // We're already crashing, just terminate immediately without another message box
                e.Handled = false;
                Environment.Exit(1);
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogCrash(ex);
            }
        }

        private void LogCrash(Exception ex)
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "diagnostics");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, "crash.log");
                File.AppendAllText(file, $"[{DateTime.Now:O}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n");
            }
            catch { }
        }
    }
}
