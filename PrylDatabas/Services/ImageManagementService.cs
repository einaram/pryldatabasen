using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrylDatabas.Models;

namespace PrylDatabas.Services;

public class ImageManagementService
{
    private readonly string _imageFolderPath;
    private readonly ItemRepository _itemRepository;
    private readonly ImageService _imageService;

    public ImageManagementService(string imageFolderPath, ItemRepository itemRepository)
    {
        _imageFolderPath = imageFolderPath;
        _itemRepository = itemRepository;
        _imageService = new ImageService(imageFolderPath);
    }

    /// <summary>
    /// Add multiple images to an item. Copies files to item folder and updates Excel.
    /// </summary>
    public bool AddImagesToItem(Item item, List<string> sourceImagePaths)
    {
        if (item == null || sourceImagePaths == null || sourceImagePaths.Count == 0)
            return false;

        try
        {
            // First, try to find an existing folder for this item (matching the item number)
            string? itemFolderPath = _imageService.FindItemFolder(item.Number?.ToString());

            // If no existing folder found, create a new one with standard naming
            if (itemFolderPath == null)
            {
                var itemFolderName = $"{item.Number} {item.Name}";
                itemFolderPath = Path.Combine(_imageFolderPath, itemFolderName);

                if (!Directory.Exists(itemFolderPath))
                {
                    Directory.CreateDirectory(itemFolderPath);
                    System.Diagnostics.Debug.WriteLine($"Created folder: {itemFolderPath}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Using existing folder: {itemFolderPath}");
            }

            // Copy files and collect filenames (without extensions for Excel)
            var copiedFileNames = new List<string>();

            foreach (var sourcePath in sourceImagePaths)
            {
                if (!File.Exists(sourcePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Source file not found: {sourcePath}");
                    continue;
                }

                var fileName = Path.GetFileName(sourcePath);
                var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                var ext = Path.GetExtension(fileName);
                var destPath = Path.Combine(itemFolderPath, fileName);

                // If file already exists, create a unique name
                if (File.Exists(destPath))
                {
                    var counter = 1;
                    while (File.Exists(Path.Combine(itemFolderPath, $"{nameWithoutExt}_{counter}{ext}")))
                        counter++;
                    fileName = $"{nameWithoutExt}_{counter}{ext}";
                    nameWithoutExt = $"{nameWithoutExt}_{counter}";
                    destPath = Path.Combine(itemFolderPath, fileName);
                }

                File.Copy(sourcePath, destPath, overwrite: false);
                copiedFileNames.Add(nameWithoutExt); // Add name WITHOUT extension to Excel
                System.Diagnostics.Debug.WriteLine($"Copied image: {destPath}");
            }

            // Update Excel with new photo filenames
            if (copiedFileNames.Count > 0)
            {
                var existingPhotos = item.Photos;
                var allPhotos = new List<string>();

                if (!string.IsNullOrEmpty(existingPhotos))
                {
                    allPhotos.AddRange(existingPhotos.Split(',').Select(p => p.Trim()));
                }

                allPhotos.AddRange(copiedFileNames);
                var photoString = string.Join(", ", allPhotos.Distinct());

                if (item.Number.HasValue)
                {
                    _itemRepository.UpdateItemPhotos(item.Number.Value, photoString);
                    item.Photos = photoString;

                    System.Diagnostics.Debug.WriteLine($"Updated item {item.Number} photos to: {photoString}");
                }
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding images: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Open file dialog to select multiple image files.
    /// </summary>
    public List<string>? SelectMultipleImages()
    {
        var dialog = new System.Windows.Forms.OpenFileDialog
        {
            Title = "Välj bilder att lägga till",
            Filter = "Bildfiler (*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff)|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff|Alla filer (*.*)|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            return dialog.FileNames.ToList();
        }

        return null;
    }
}
