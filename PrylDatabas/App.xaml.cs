using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using QuestPDF.Infrastructure;

namespace PrylDatabas;

public partial class App : Application
{
    private static StreamWriter? _debugWriter;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Set up file-based debug logging
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "pryldb_debug.log");
        try
        {
            _debugWriter = new StreamWriter(logPath, false) { AutoFlush = true };
            _debugWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Application started");
            
            // Redirect Debug output to file
            System.Diagnostics.Trace.Listeners.Add(new TextWriterTraceListener(_debugWriter));
        }
        catch
        {
            // If file logging fails, continue anyway
        }
        
        // Configure QuestPDF Community License
        QuestPDF.Settings.License = LicenseType.Community;
        
        MainWindow = new MainWindow();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _debugWriter?.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Application closed");
        _debugWriter?.Flush();
        _debugWriter?.Dispose();
        base.OnExit(e);
    }
}

