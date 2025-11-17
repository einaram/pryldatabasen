using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PrylDatabas.Models;

public class ImageService
{
    private readonly string _imageFolder;

    public ImageService(string? imageFolder = null)
    {
        // Default to "data/Gamla Prylar - foton i dbs" relative to solution root
        if (string.IsNullOrEmpty(imageFolder))
        {
            _imageFolder = GetDefaultImageFolder();
        }
        else
        {
            _imageFolder = imageFolder;
        }
    }

    /// <summary>
    /// Find image files for an item based on the Photos column and item number.
    /// </summary>
    public List<string> FindImages(string? itemNumber, string? photoFileNames)
    {
        var images = new List<string>();

        if (string.IsNullOrEmpty(itemNumber))
            return images;

        // Parse the photoFileNames field (can be comma-separated or single file)
        var fileNames = ParsePhotoFileNames(photoFileNames);

        foreach (var fileName in fileNames)
        {
            var imagePath = FindImageFile(itemNumber, fileName);
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                images.Add(imagePath);
            }
        }

        // If no images found from photo names, look for any images in item folder
        if (images.Count == 0)
        {
            images.AddRange(FindImagesByItemNumber(itemNumber));
        }

        return images;
    }

    /// <summary>
    /// Find a specific image file for an item.
    /// </summary>
    private string? FindImageFile(string itemNumber, string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;

        // Search in item-specific folder first (e.g., "302 Tullas ring")
        var itemFolder = Path.Combine(_imageFolder, itemNumber + "*");
        try
        {
            var folders = Directory.GetDirectories(_imageFolder, itemNumber + "*", SearchOption.TopDirectoryOnly);
            if (folders.Length > 0)
            {
                var folder = folders[0];
                var imagePath = Path.Combine(folder, fileName);
                if (File.Exists(imagePath))
                    return imagePath;

                // Also check with common image extensions if not found exactly
                var imageWithExtensions = FindImageWithExtension(folder, Path.GetFileNameWithoutExtension(fileName));
                if (!string.IsNullOrEmpty(imageWithExtensions))
                    return imageWithExtensions;
            }
        }
        catch
        {
            // Folder search failed, continue
        }

        return null;
    }

    /// <summary>
    /// Find all images in the folder for a given item number.
    /// </summary>
    private List<string> FindImagesByItemNumber(string itemNumber)
    {
        var images = new List<string>();

        try
        {
            System.Diagnostics.Debug.WriteLine($"[ImageService] Searching for images in folder: {_imageFolder}");
            System.Diagnostics.Debug.WriteLine($"[ImageService] Looking for directories matching: {itemNumber}*");
            
            // First, list all directories in the folder for debugging
            var allFolders = Directory.GetDirectories(_imageFolder);
            System.Diagnostics.Debug.WriteLine($"[ImageService] All folders in image directory:");
            foreach (var folder in allFolders)
            {
                System.Diagnostics.Debug.WriteLine($"  - {Path.GetFileName(folder)}");
            }
            
            var folders = Directory.GetDirectories(_imageFolder, itemNumber + "*", SearchOption.TopDirectoryOnly);
            
            System.Diagnostics.Debug.WriteLine($"[ImageService] Found {folders.Length} matching folders");
            
            if (folders.Length > 0)
            {
                var folder = folders[0];
                System.Diagnostics.Debug.WriteLine($"[ImageService] Using folder: {folder}");
                
                var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff" };

                var allFiles = Directory.GetFiles(folder);
                System.Diagnostics.Debug.WriteLine($"[ImageService] All files in folder ({allFiles.Length} total):");
                foreach (var file in allFiles)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {Path.GetFileName(file)}");
                    if (imageExtensions.Contains(Path.GetExtension(file).ToLower()))
                    {
                        System.Diagnostics.Debug.WriteLine($"[ImageService] Found image: {file}");
                        images.Add(file);
                    }
                }

                // Sort images by name (e.g., 302-a.jpg, 302-b.jpg)
                images.Sort();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ImageService] No folders found matching pattern");
            }
        }
        catch (Exception ex)
        {
            // Folder search failed
            System.Diagnostics.Debug.WriteLine($"[ImageService] Error searching for images: {ex.Message}");
        }

        return images;
    }

    /// <summary>
    /// Try to find an image file with various extensions.
    /// </summary>
    private string? FindImageWithExtension(string folder, string fileName)
    {
        var extensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff" };

        foreach (var ext in extensions)
        {
            var path = Path.Combine(folder, fileName + ext);
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    /// <summary>
    /// Parse comma-separated photo file names from the Photos column.
    /// </summary>
    private List<string> ParsePhotoFileNames(string? photoField)
    {
        if (string.IsNullOrEmpty(photoField))
            return new List<string>();

        return photoField
            .Split(',')
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrEmpty(f))
            .ToList();
    }

    /// <summary>
    /// Get the default image folder path relative to the solution root.
    /// </summary>
    private string GetDefaultImageFolder()
    {
        var solutionRoot = ResolveSolutionRoot();
        return Path.Combine(solutionRoot, "data", "Gamla Prylar - foton i dbs");
    }

    /// <summary>
    /// Find the solution root by looking for PrylDatabasSolution.sln file.
    /// </summary>
    private string ResolveSolutionRoot()
    {
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        var searchDir = currentDir;

        System.Diagnostics.Debug.WriteLine($"[ImageService] Current working directory: {currentDir.FullName}");

        while (searchDir.Parent != null)
        {
            var slnPath = Path.Combine(searchDir.FullName, "PrylDatabasSolution.sln");
            System.Diagnostics.Debug.WriteLine($"[ImageService] Checking for: {slnPath}");
            
            if (File.Exists(slnPath))
            {
                System.Diagnostics.Debug.WriteLine($"[ImageService] Found solution root at: {searchDir.FullName}");
                return searchDir.FullName;
            }
            searchDir = searchDir.Parent;
        }

        // Fallback to current directory if not found
        System.Diagnostics.Debug.WriteLine($"[ImageService] Solution not found, using current directory: {Directory.GetCurrentDirectory()}");
        return Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Check if images exist for the given item.
    /// </summary>
    public bool HasImages(string? itemNumber, string? photoFileNames)
    {
        return FindImages(itemNumber, photoFileNames).Count > 0;
    }

    /// <summary>
    /// Get the configured image folder path.
    /// </summary>
    public string ImageFolderPath => _imageFolder;

    /// <summary>
    /// Get images with their found/expected status. Useful for UI display and PDF export.
    /// Compares found files with expected photo names, matching on filename without extension.
    /// </summary>
    public List<ImageResult> GetImageResults(string? itemNumber, string? photoFileNames)
    {
        var results = new List<ImageResult>();
        var foundImages = FindImages(itemNumber, photoFileNames);
        var expectedPhotos = ParsePhotoFileNames(photoFileNames);

        // Create a set of found file names WITHOUT extensions for comparison
        var foundFileNamesWithoutExt = new HashSet<string>(
            foundImages.Select(p => Path.GetFileNameWithoutExtension(p)),
            StringComparer.OrdinalIgnoreCase
        );

        // Add found images first
        foreach (var imagePath in foundImages)
        {
            var fileName = Path.GetFileName(imagePath);
            results.Add(new ImageResult(fileName, imagePath, true));
        }

        // Add expected but missing images
        foreach (var expectedPhoto in expectedPhotos)
        {
            // Compare without extensions since Photos field may not include them
            if (!foundFileNamesWithoutExt.Contains(Path.GetFileNameWithoutExtension(expectedPhoto)))
            {
                results.Add(new ImageResult(expectedPhoto, "", false));
            }
        }

        return results;
    }
}

/// <summary>
/// Represents an image result with its found status.
/// </summary>
public class ImageResult
{
    public string FileName { get; }
    public string FullPath { get; }
    public bool Found { get; }

    public ImageResult(string fileName, string fullPath, bool found)
    {
        FileName = fileName;
        FullPath = fullPath;
        Found = found;
    }
}
