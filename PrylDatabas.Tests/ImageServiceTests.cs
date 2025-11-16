using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using PrylDatabas.Models;

namespace PrylDatabas.Tests;

public class ImageServiceTests
{
    private readonly string _solutionRoot;
    private readonly string _imageFolderPath;

    public ImageServiceTests()
    {
        _solutionRoot = ResolveSolutionRoot();
        _imageFolderPath = Path.Combine(_solutionRoot, "data", "Gamla Prylar - foton i dbs");
    }

    [Fact]
    public void ImageFolderExists()
    {
        Assert.True(Directory.Exists(_imageFolderPath), $"Image folder not found at: {_imageFolderPath}");
    }

    [Fact]
    public void ImageServiceCanBeInstantiated()
    {
        var service = new ImageService(_imageFolderPath);
        Assert.NotNull(service);
    }

    [Fact]
    public void FindImages_WithValidItemNumber_ReturnsImages()
    {
        var service = new ImageService(_imageFolderPath);
        var images = service.FindImages("302", null);

        Assert.NotEmpty(images);
        Assert.True(images.Count > 0, "Should find images for item 302");
    }

    [Fact]
    public void FindImages_WithInvalidItemNumber_ReturnsEmpty()
    {
        var service = new ImageService(_imageFolderPath);
        var images = service.FindImages("99999", null);

        Assert.Empty(images);
    }

    [Fact]
    public void FindImages_ReturnsValidFilePaths()
    {
        var service = new ImageService(_imageFolderPath);
        var images = service.FindImages("302", null);

        foreach (var imagePath in images)
        {
            Assert.True(File.Exists(imagePath), $"Image file not found: {imagePath}");
        }
    }

    [Fact]
    public void FindImages_ReturnsJpgFiles()
    {
        var service = new ImageService(_imageFolderPath);
        var images = service.FindImages("302", null);

        foreach (var imagePath in images)
        {
            var extension = Path.GetExtension(imagePath).ToLower();
            Assert.True(
                extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp" || extension == ".gif",
                $"Unexpected file extension: {extension}");
        }
    }

    [Fact]
    public void FindImages_SortsImagesByName()
    {
        var service = new ImageService(_imageFolderPath);
        var images = service.FindImages("302", null);

        // Check if images are sorted
        for (int i = 1; i < images.Count; i++)
        {
            var fileName1 = Path.GetFileName(images[i - 1]);
            var fileName2 = Path.GetFileName(images[i]);
            Assert.True(
                string.Compare(fileName1, fileName2) <= 0,
                $"Images not sorted: {fileName1} should come before {fileName2}");
        }
    }

    [Fact]
    public void FindImages_WithNullItemNumber_ReturnsEmpty()
    {
        var service = new ImageService(_imageFolderPath);
        var images = service.FindImages(null, null);

        Assert.Empty(images);
    }

    [Fact]
    public void FindImages_WithEmptyItemNumber_ReturnsEmpty()
    {
        var service = new ImageService(_imageFolderPath);
        var images = service.FindImages(string.Empty, null);

        Assert.Empty(images);
    }

    [Fact]
    public void HasImages_WithItemThatHasImages_ReturnsTrue()
    {
        var service = new ImageService(_imageFolderPath);
        var hasImages = service.HasImages("302", null);

        Assert.True(hasImages);
    }

    [Fact]
    public void HasImages_WithItemWithoutImages_ReturnsFalse()
    {
        var service = new ImageService(_imageFolderPath);
        var hasImages = service.HasImages("99999", null);

        Assert.False(hasImages);
    }

    [Fact]
    public void ImageServiceFolderPathProperty_IsSet()
    {
        var service = new ImageService(_imageFolderPath);
        Assert.Equal(_imageFolderPath, service.ImageFolderPath);
    }

    [Fact]
    public void FindImages_WithSpecificPhotoFileName_FindsCorrectFile()
    {
        var service = new ImageService(_imageFolderPath);
        
        // First, find what images exist for item 302
        var allImages = service.FindImages("302", null);
        Assert.NotEmpty(allImages);

        // Get the filename of the first image
        var firstImageName = Path.GetFileName(allImages[0]);

        // Now search for it specifically
        var images = service.FindImages("302", firstImageName);
        Assert.NotEmpty(images);
        Assert.Contains(firstImageName, images[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FindImages_WithMultipleItemsHavingImages_FindsCorrectImages()
    {
        var service = new ImageService(_imageFolderPath);
        
        var images302 = service.FindImages("302", null);
        var images393 = service.FindImages("393", null);

        // Both should have images
        Assert.NotEmpty(images302);
        Assert.NotEmpty(images393);

        // They should be different
        Assert.NotEqual(images302[0], images393[0]);
    }

    [Fact]
    public void FindImages_DefaultImageFolderUsesRelativePath()
    {
        // Create service without specifying image folder (uses default)
        var service = new ImageService();
        
        // Try to find images - should work with default path
        var images = service.FindImages("302", null);
        Assert.NotEmpty(images);
    }

    [Fact]
    public void FindImages_AllImagesAreInImageFolder()
    {
        var service = new ImageService(_imageFolderPath);
        var images = service.FindImages("302", null);

        foreach (var imagePath in images)
        {
            // All images should be under the image folder
            Assert.StartsWith(_imageFolderPath, imagePath, StringComparison.OrdinalIgnoreCase);
        }
    }

    private string ResolveSolutionRoot()
    {
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        var searchDir = currentDir;

        while (searchDir.Parent != null)
        {
            if (File.Exists(Path.Combine(searchDir.FullName, "PrylDatabasSolution.sln")))
            {
                return searchDir.FullName;
            }
            searchDir = searchDir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
