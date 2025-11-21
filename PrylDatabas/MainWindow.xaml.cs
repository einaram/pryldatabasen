using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using PrylDatabas.Services;
using PrylDatabas.ViewModels;
using PrylDatabas.Models;

namespace PrylDatabas;

public partial class MainWindow : Window
{
    private MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;
    }

    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_viewModel);
        settingsWindow.ShowDialog();
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Pryldatabas v1.0\n\nEn applikation för hantering av familjesamlingar.",
            "Om Pryldatabas",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedItem != null)
        {
            var detailWindow = new DetailsWindow(_viewModel.SelectedItem, _viewModel.ImageFolderPath);
            detailWindow.ShowDialog();
        }
    }

    private void ColumnHeader_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string columnName)
        {
            _viewModel.SortByColumn(columnName);
        }
    }

    private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
    {
        // Get selected items based on IsSelected property
        var selectedItems = _viewModel.FilteredItems
            .Where(item => item.IsSelected)
            .ToList();

        if (selectedItems.Count == 0)
        {
            MessageBox.Show(
                "Välj minst en pryl genom att markera checkboxen.",
                "Ingen val",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        // Debug logging
        System.Diagnostics.Debug.WriteLine($"[MainWindow] Exporting {selectedItems.Count} selected items:");
        foreach (var item in selectedItems)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow]   - Item {item.Number}: {item.Name} - Photos: '{item.Photos ?? "(null)"}'");
        }

        // Show export dialog
        var exportWindow = new PdfExportWindow(selectedItems, _viewModel.ImageFolderPath)
        {
            Owner = this
        };

        exportWindow.ShowDialog();
    }
}

public class PhotoStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not Item item)
            return System.Windows.Media.Brushes.Gray;

        if (string.IsNullOrEmpty(item.Photos))
            return System.Windows.Media.Brushes.Gray;

        // Check if images exist for this item
        var imageService = new ImageService();
        var images = imageService.FindImages(item.Number?.ToString(), item.Photos);
        
        return images.Count > 0 
            ? System.Windows.Media.Brushes.Green 
            : System.Windows.Media.Brushes.Orange;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
