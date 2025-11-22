using System;
using System.IO;

namespace PrylDatabas.Services;

/// <summary>
/// Centralized service for managing application settings stored in settings.txt
/// </summary>
public class SettingsService
{
    private readonly string _settingsPath;

    public SettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PrylDatabas");
        _settingsPath = Path.Combine(appDataPath, "settings.txt");
    }

    /// <summary>
    /// Get the Excel file path from settings
    /// </summary>
    public string GetExcelFilePath()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                var lines = File.ReadAllLines(_settingsPath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("ExcelFile=", StringComparison.OrdinalIgnoreCase))
                    {
                        return line.Substring("ExcelFile=".Length).Trim();
                    }
                }
            }
            catch { }
        }

        return "data/Gamla Prylar - dbs/Gamla Prylar.xlsx"; // Default fallback
    }

    /// <summary>
    /// Get the image folder path from settings
    /// </summary>
    public string GetImageFolderPath()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                var lines = File.ReadAllLines(_settingsPath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("ImageFolderPath=", StringComparison.OrdinalIgnoreCase))
                    {
                        return line.Substring("ImageFolderPath=".Length).Trim();
                    }
                }
            }
            catch { }
        }

        // Try to find images folder relative to solution root
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (currentDir.Parent != null && !File.Exists(Path.Combine(currentDir.FullName, "PrylDatabasSolution.sln")))
        {
            currentDir = currentDir.Parent;
        }
        return Path.Combine(currentDir.FullName, "data", "Gamla Prylar - foton i dbs");
    }

    /// <summary>
    /// Check if debug mode is enabled
    /// </summary>
    public bool IsDebugModeEnabled()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                var lines = File.ReadAllLines(_settingsPath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("DebugMode=", StringComparison.OrdinalIgnoreCase))
                    {
                        var debugValue = line.Substring("DebugMode=".Length).Trim();
                        return debugValue.Equals("true", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch { }
        }

        return false; // Default to false
    }

    /// <summary>
    /// Save all settings to file
    /// </summary>
    public void SaveSettings(string excelFilePath, string imageFolderPath, bool debugMode)
    {
        try
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PrylDatabas");

            Directory.CreateDirectory(appDataPath);

            var settings = new[]
            {
                $"ExcelFile={excelFilePath}",
                $"ImageFolderPath={imageFolderPath}",
                $"DebugMode={debugMode.ToString().ToLower()}"
            };
            File.WriteAllLines(_settingsPath, settings);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Error saving settings: {ex.Message}");
            throw;
        }
    }
}
