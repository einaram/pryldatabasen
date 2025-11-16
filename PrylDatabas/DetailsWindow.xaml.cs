using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using PrylDatabas.Models;

namespace PrylDatabas;

public class ImageItemDisplay
{
    public string FileName { get; set; }
    public string FullPath { get; set; }
    public bool Found { get; set; }

    public ImageItemDisplay(string fileName, string fullPath, bool found)
    {
        FileName = fileName;
        FullPath = fullPath;
        Found = found;
    }
}

public partial class DetailsWindow : Window
{
    private List<string> _imagePaths = new();
    private List<ImageItemDisplay> _imageDisplayItems = new();
    private ImageService _imageService;

    public DetailsWindow(Item item)
    {
        InitializeComponent();
        _imageService = new ImageService();
        PopulateDetails(item);
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
        _imageDisplayItems.Clear();

        // Debug: Log image search info
        var debugInfo = $"Bildsökning för föremål {itemNumber}:\n";
        debugInfo += $"- Sökmapp: {_imageService.ImageFolderPath}\n";
        debugInfo += $"- Sökpar: {itemNumber}*\n";
        debugInfo += $"- Foton från DB: {item.Photos ?? "(tomt)"}\n";
        debugInfo += $"- Bilder hittade: {_imagePaths.Count}\n";
        
        // Parse photo file names from database
        var expectedPhotos = ParsePhotos(item.Photos);
        var foundFileNames = new HashSet<string>(
            _imagePaths.Select(p => Path.GetFileName(p)),
            StringComparer.OrdinalIgnoreCase
        );
        
        // Add found images first (green)
        foreach (var imagePath in _imagePaths)
        {
            var fileName = Path.GetFileName(imagePath);
            _imageDisplayItems.Add(new ImageItemDisplay(fileName, imagePath, true));
            debugInfo += $"  ✓ {fileName}\n";
        }

        // Add expected but missing images (orange)
        foreach (var expectedPhoto in expectedPhotos)
        {
            if (!foundFileNames.Contains(expectedPhoto))
            {
                _imageDisplayItems.Add(new ImageItemDisplay(expectedPhoto, "", false));
                debugInfo += $"  ✗ {expectedPhoto} (INTE HITTAD)\n";
            }
        }

        System.Diagnostics.Debug.WriteLine(debugInfo);
        MessageBox.Show(debugInfo, "Bildsökning - Debug Info", MessageBoxButton.OK, MessageBoxImage.Information);

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

    private List<string> ParsePhotos(string? photosField)
    {
        if (string.IsNullOrEmpty(photosField))
            return new List<string>();

        return photosField
            .Split(',')
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrEmpty(f))
            .ToList();
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
