using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using PrylDatabas.Models;
using PrylDatabas.Services;

namespace PrylDatabas;

public partial class DetailsWindow : Window
{
    private List<string> _imagePaths = new();
    private List<ImageResult> _imageDisplayItems = new();
    private ImageService _imageService;
    private Item _currentItem;
    private string _imageFolderPath;
    private string _excelFilePath;

    public DetailsWindow(Item item)
    {
        InitializeComponent();
        _currentItem = item;
        var imageService = new ImageService();
        _imageFolderPath = imageService.ImageFolderPath;
        _excelFilePath = GetExcelFilePath();
        _imageService = imageService;
        PopulateDetails(item);
    }

    public DetailsWindow(Item item, string imageFolderPath, string excelFilePath = "")
    {
        InitializeComponent();
        _currentItem = item;
        _imageFolderPath = imageFolderPath;
        _excelFilePath = excelFilePath ?? GetExcelFilePath();
        System.Diagnostics.Debug.WriteLine($"[DetailsWindow] Created with imageFolderPath: {imageFolderPath}, excelFilePath: {_excelFilePath}");
        _imageService = new ImageService(imageFolderPath);
        PopulateDetails(item);
    }

    private string GetExcelFilePath()
    {
        return new SettingsService().GetExcelFilePath();
    }

    private void PopulateDetails(Item item)
    {
        ItemNameBlock.Text = item.Name ?? "Okänt föremål";
        NumberBlock.Text = item.Number?.ToString() ?? "—";
        CategoryBlock.Text = item.Category ?? "—";
        StampBlock.Text = item.Stamp ?? "—";
        OwnerBlock.Text = item.CurrentOwner ?? "—";
        PhotosBlock.Text = item.Photos ?? "—";
        ProvenanceBlock.Text = item.Provenance ?? "—";

        // Load and display images
        LoadImages(item);
        
        // Color-code the Photos field based on whether images were found
        if (_imagePaths.Count > 0)
        {
            PhotosBlock.Foreground = System.Windows.Media.Brushes.Green; // Found
        }
        else if (!string.IsNullOrEmpty(item.Photos))
        {
            PhotosBlock.Foreground = System.Windows.Media.Brushes.Orange; // Expected but not found
        }
        else
        {
            PhotosBlock.Foreground = System.Windows.Media.Brushes.Gray; // Not specified
        }
    }

    private void LoadImages(Item item)
    {
        var itemNumber = item.Number?.ToString() ?? "N/A";
        _imagePaths = _imageService.FindImages(itemNumber, item.Photos);
        _imageDisplayItems = _imageService.GetImageResults(itemNumber, item.Photos);

        // Debug: Log image search info
        var debugInfo = $"Bildsökning för föremål {itemNumber}:\n";
        debugInfo += $"- Mapp med föremålsmappar: {_imageService.ImageFolderPath}\n";
        debugInfo += $"- Sökmapp: {itemNumber}*\n";
        debugInfo += $"- Foton från DB: {item.Photos ?? "(tomt)"}\n";
        debugInfo += $"- Bilder hittade: {_imagePaths.Count}\n";
        
        foreach (var result in _imageDisplayItems)
        {
            if (result.Found)
                debugInfo += $"  ✓ {result.FileName}\n";
            else
                debugInfo += $"  ✗ {result.FileName} (INTE HITTAD)\n";
        }

        System.Diagnostics.Debug.WriteLine(debugInfo);
        
        // Show debug info only if debug mode is enabled
        if (IsDebugModeEnabled())
        {
            MessageBox.Show(debugInfo, "Bildsökning - Debug Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        if (_imagePaths.Count > 0 || _imageDisplayItems.Count > 0)
        {
            ImageListBox.ItemsSource = _imageDisplayItems;
            NoImagesText.Visibility = Visibility.Collapsed;
            ImageListBox.Visibility = Visibility.Visible;

            // Display the first found image by default
            if (_imagePaths.Count > 0)
            {
                DisplayImage(_imagePaths[0]);
                // Select the first found image in the list
                for (int i = 0; i < _imageDisplayItems.Count; i++)
                {
                    if (_imageDisplayItems[i].Found)
                    {
                        ImageListBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
        else
        {
            // No images found or expected
            ImageListBox.Visibility = Visibility.Collapsed;
            NoImagesText.Visibility = Visibility.Visible;
            MainImage.Source = null;
        }
    }

    private bool IsDebugModeEnabled()
    {
        return new SettingsService().IsDebugModeEnabled();
    }

    private void DisplayImage(string imagePath)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            MainImage.Source = bitmap;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kunde inte ladda bilden: {ex.Message}", "Fel", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImageListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ImageListBox.SelectedIndex >= 0 && ImageListBox.SelectedIndex < _imageDisplayItems.Count)
        {
            var selectedItem = _imageDisplayItems[ImageListBox.SelectedIndex];
            if (selectedItem.Found && !string.IsNullOrEmpty(selectedItem.FullPath))
            {
                DisplayImage(selectedItem.FullPath);
            }
            else
            {
                MainImage.Source = null;
            }
        }
    }

    private void AddImagesButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentItem == null)
        {
            MessageBox.Show("Inget föremål är valt.", "Fel", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var itemRepository = new ItemRepository(_excelFilePath);
            var imageManagementService = new ImageManagementService(_imageFolderPath, itemRepository);
            var selectedImages = imageManagementService.SelectMultipleImages();

            if (selectedImages != null && selectedImages.Count > 0)
            {
                if (imageManagementService.AddImagesToItem(_currentItem, selectedImages))
                {
                    MessageBox.Show(
                        $"Lade till {selectedImages.Count} bild(er) för '{_currentItem.Name}'.",
                        "Bilder tillagda",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    
                    // Reload images to display newly added ones
                    LoadImages(_currentItem);
                }
                else
                {
                    MessageBox.Show(
                        "Det gick inte att lägga till bilderna.",
                        "Fel",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DetailsWindow] Error in AddImagesButton_Click: {ex.Message}");
            MessageBox.Show(
                $"Ett fel uppstod: {ex.Message}",
                "Fel",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

public class ImageStatusConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool found)
        {
            return found 
                ? System.Windows.Media.Brushes.Green 
                : System.Windows.Media.Brushes.Orange; // Yellow-ish color
        }
        return System.Windows.Media.Brushes.Black;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
