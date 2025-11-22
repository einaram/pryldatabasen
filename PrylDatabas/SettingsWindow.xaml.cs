using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using PrylDatabas.ViewModels;

namespace PrylDatabas;

public partial class SettingsWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public SettingsWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PrylDatabas",
            "settings.txt");

        if (File.Exists(settingsPath))
        {
            try
            {
                var lines = File.ReadAllLines(settingsPath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("ExcelFile=", StringComparison.OrdinalIgnoreCase))
                    {
                        FilePathBox.Text = line.Substring("ExcelFile=".Length).Trim();
                    }
                    else if (line.StartsWith("ImageFolderPath=", StringComparison.OrdinalIgnoreCase))
                    {
                        ImageFolderPathBox.Text = line.Substring("ImageFolderPath=".Length).Trim();
                    }
                    else if (line.StartsWith("DebugMode=", StringComparison.OrdinalIgnoreCase))
                    {
                        var debugValue = line.Substring("DebugMode=".Length).Trim();
                        DebugModeCheckBox.IsChecked = debugValue.Equals("true", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch
            {
                FilePathBox.Text = string.Empty;
                ImageFolderPathBox.Text = string.Empty;
                DebugModeCheckBox.IsChecked = false;
            }
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
            Title = "Välj Excel-fil"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            FilePathBox.Text = openFileDialog.FileName;
        }
    }

    private void BrowseImageButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Välj bildmapp"
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            ImageFolderPathBox.Text = dialog.SelectedPath;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        _viewModel.SetExcelFilePath(FilePathBox.Text);
        _viewModel.SetImageFolderPath(ImageFolderPathBox.Text);
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SaveSettings()
    {
        try
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PrylDatabas");

            Directory.CreateDirectory(appDataPath);

            var settingsPath = Path.Combine(appDataPath, "settings.txt");
            var settings = new[]
            {
                $"ExcelFile={FilePathBox.Text}",
                $"ImageFolderPath={ImageFolderPathBox.Text}",
                $"DebugMode={DebugModeCheckBox.IsChecked?.ToString().ToLower() ?? "false"}"
            };
            File.WriteAllLines(settingsPath, settings);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kunde inte spara inställningar: {ex.Message}", "Fel",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
