using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using PrylDatabas.Models;
using PrylDatabas.Services;
using QuestPDF.Infrastructure;
using Xunit;

namespace PrylDatabas.Tests;

public class PdfExportDebugTest
{
    static PdfExportDebugTest()
    {
        // Set QuestPDF license for tests
        QuestPDF.Settings.License = LicenseType.Community;
    }
    [Fact]
    public void TestPdfExportWithImages()
    {
        // Arrange
        var testItems = new List<Item>
        {
            new Item
            {
                Number = 302,
                Name = "Tullas ring",
                Category = "Smycken",
                CreatedBy = "Unknown",
                CreatedYear = "1900",
                CreatedPlace = "Sverige",
                Stamp = "—",
                CurrentOwner = "Test Owner",
                Provenance = "Test",
                Photos = "302-a.jpg, 302-b.jpg"
            },
            new Item
            {
                Number = 393,
                Name = "Dopskål",
                Category = "Keramik",
                CreatedBy = "—",
                CreatedYear = "—",
                CreatedPlace = "—",
                Stamp = "—",
                CurrentOwner = "Test Owner",
                Provenance = "Test",
                Photos = null // No photos
            }
        };

        var outputPath = Path.Combine(Path.GetTempPath(), "test_export_debug.pdf");
        
        // Get current directory for debugging
        var currentDir = Directory.GetCurrentDirectory();
        Console.WriteLine($"Current directory: {currentDir}");

        // Log app directory
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        Console.WriteLine($"App base directory: {appDir}");

        // Check paths exist
        var imagePath1 = Path.Combine(currentDir, "data", "Gamla Prylar - foton i dbs");
        var imagePath2 = Path.Combine(appDir, "data", "Gamla Prylar - foton i dbs");
        var imagePath3 = @"c:\prosjekter\pryldatabas\data\Gamla Prylar - foton i dbs";
        
        Console.WriteLine($"Path 1 exists: {Directory.Exists(imagePath1)} - {imagePath1}");
        Console.WriteLine($"Path 2 exists: {Directory.Exists(imagePath2)} - {imagePath2}");
        Console.WriteLine($"Path 3 exists: {Directory.Exists(imagePath3)} - {imagePath3}");

        // Create service with explicit path
        var pdfExportService = new PdfExportService(imagePath3);

        // Act
        Console.WriteLine($"\nExporting to: {outputPath}");
        pdfExportService.ExportItemsToPdf(testItems, outputPath, includePhotos: true);

        // Assert
        Assert.True(File.Exists(outputPath), $"PDF file was not created at {outputPath}");
        
        var fileInfo = new FileInfo(outputPath);
        Assert.True(fileInfo.Length > 0, "PDF file is empty");
        
        Console.WriteLine($"PDF created successfully: {outputPath}");
        Console.WriteLine($"File size: {fileInfo.Length} bytes");
        
        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }

    [Fact]
    public void TestImageServiceFindImages()
    {
        // Test the image service directly
        var imagePath = @"c:\prosjekter\pryldatabas\data\Gamla Prylar - foton i dbs";
        var service = new ImageService(imagePath);

        Console.WriteLine($"ImageService folder path: {service.ImageFolderPath}");
        Console.WriteLine($"Folder exists: {Directory.Exists(service.ImageFolderPath)}");

        // Test finding images for item 302
        var images302 = service.FindImages("302", "302-a.jpg, 302-b.jpg");
        Console.WriteLine($"Found {images302.Count} images for item 302");
        foreach (var img in images302)
        {
            var fileInfo = new FileInfo(img);
            Console.WriteLine($"  - {Path.GetFileName(img)} ({fileInfo.Length} bytes)");
        }

        Assert.NotEmpty(images302);

        // Test finding images by folder scan
        var images302NoPhotos = service.FindImages("302", null);
        Console.WriteLine($"Found {images302NoPhotos.Count} images for item 302 (folder scan)");
        foreach (var img in images302NoPhotos)
        {
            var fileInfo = new FileInfo(img);
            Console.WriteLine($"  - {Path.GetFileName(img)} ({fileInfo.Length} bytes)");
        }

        Assert.NotEmpty(images302NoPhotos);
    }
}
