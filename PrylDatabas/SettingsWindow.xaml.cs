using System;
using System.Windows;
using Microsoft.Win32;
using PrylDatabas.ViewModels;
using PrylDatabas.Services;

namespace PrylDatabas;

public partial class SettingsWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly SettingsService _settingsService;

    public SettingsWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _settingsService = new SettingsService();
        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            FilePathBox.Text = _settingsService.GetExcelFilePath();
            ImageFolderPathBox.Text = _settingsService.GetImageFolderPath();
            DebugModeCheckBox.IsChecked = _settingsService.IsDebugModeEnabled();
        }
        catch
        {
            FilePathBox.Text = string.Empty;
            ImageFolderPathBox.Text = string.Empty;
            DebugModeCheckBox.IsChecked = false;
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
            _settingsService.SaveSettings(
                FilePathBox.Text,
                ImageFolderPathBox.Text,
                DebugModeCheckBox.IsChecked ?? false);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kunde inte spara inställningar: {ex.Message}", "Fel",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
