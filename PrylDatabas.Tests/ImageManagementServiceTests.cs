using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using PrylDatabas.Models;
using PrylDatabas.Services;

namespace PrylDatabas.Tests;

public class ImageManagementServiceTests
{
    private readonly string _testImageFolder;
    private readonly string _solutionRoot;
    private readonly string _excelFilePath;

    public ImageManagementServiceTests()
    {
        // Find solution root
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        _solutionRoot = currentDir.FullName;
        while (currentDir.Parent != null && !File.Exists(Path.Combine(currentDir.FullName, "PrylDatabasSolution.sln")))
        {
            currentDir = currentDir.Parent;
            _solutionRoot = currentDir.FullName;
        }

        // Use a temporary test folder
        _testImageFolder = Path.Combine(Path.GetTempPath(), "PrylDatabas_ImageTest_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testImageFolder);

        // Path to the actual Excel file for testing
        _excelFilePath = Path.Combine(_solutionRoot, "data", "Gamla Prylar - dbs", "Gamla prylar 251115.xlsx");
    }

    [Fact]
    public void AddImagesToItem_WithValidInputs_CreatesItemFolder()
    {
        // Arrange
        var repository = new ItemRepository(_excelFilePath);
        var service = new ImageManagementService(_testImageFolder, repository);
        
        var testItem = new Item 
        { 
            Number = 999,
            Name = "Test Pryl"
        };

        // Create test image files
        var sourceImagePath = Path.Combine(Path.GetTempPath(), "test_image.jpg");
        File.WriteAllBytes(sourceImagePath, new byte[] { 0xFF, 0xD8, 0xFF }); // Minimal JPEG header

        var sourceImages = new List<string> { sourceImagePath };

        try
        {
            // Act
            var result = service.AddImagesToItem(testItem, sourceImages);

            // Assert
            Assert.True(result);
            
            var expectedFolderPath = Path.Combine(_testImageFolder, "999 Test Pryl");
            Assert.True(Directory.Exists(expectedFolderPath), 
                $"Item folder should be created at: {expectedFolderPath}");
            
            var copiedFile = Path.Combine(expectedFolderPath, "test_image.jpg");
            Assert.True(File.Exists(copiedFile), 
                $"Image should be copied to: {copiedFile}");
        }
        finally
        {
            // Cleanup
            if (File.Exists(sourceImagePath))
                File.Delete(sourceImagePath);
            if (Directory.Exists(_testImageFolder))
                Directory.Delete(_testImageFolder, recursive: true);
        }
    }

    [Fact]
    public void AddImagesToItem_WithMultipleImages_CopiesAllFiles()
    {
        // Arrange
        var repository = new ItemRepository(_excelFilePath);
        var service = new ImageManagementService(_testImageFolder, repository);
        
        var testItem = new Item 
        { 
            Number = 998,
            Name = "Multi Image Test"
        };

        var tempDir = Path.Combine(Path.GetTempPath(), "test_images_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var sourceImages = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var imagePath = Path.Combine(tempDir, $"test_image_{i}.jpg");
            File.WriteAllBytes(imagePath, new byte[] { 0xFF, 0xD8, 0xFF });
            sourceImages.Add(imagePath);
        }

        try
        {
            // Act
            var result = service.AddImagesToItem(testItem, sourceImages);

            // Assert
            Assert.True(result);
            
            var itemFolderPath = Path.Combine(_testImageFolder, "998 Multi Image Test");
            var copiedFiles = Directory.GetFiles(itemFolderPath);
            
            Assert.Equal(3, copiedFiles.Length);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
            if (Directory.Exists(_testImageFolder))
                Directory.Delete(_testImageFolder, recursive: true);
        }
    }

    [Fact]
    public void AddImagesToItem_WithDuplicateFilenames_HandlesDuplicates()
    {
        // Arrange
        var repository = new ItemRepository(_excelFilePath);
        var service = new ImageManagementService(_testImageFolder, repository);
        
        var testItem = new Item 
        { 
            Number = 997,
            Name = "Duplicate Test"
        };

        var tempDir = Path.Combine(Path.GetTempPath(), "test_images_dup_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        // Create item folder with an existing file
        var itemFolderPath = Path.Combine(_testImageFolder, "997 Duplicate Test");
        Directory.CreateDirectory(itemFolderPath);
        File.WriteAllBytes(Path.Combine(itemFolderPath, "image.jpg"), new byte[] { 0xFF, 0xD8, 0xFF });

        // Create duplicate source file
        var sourcePath = Path.Combine(tempDir, "image.jpg");
        File.WriteAllBytes(sourcePath, new byte[] { 0xFF, 0xD8, 0xFF });

        var sourceImages = new List<string> { sourcePath };

        try
        {
            // Act
            var result = service.AddImagesToItem(testItem, sourceImages);

            // Assert
            Assert.True(result);
            
            var filesInFolder = Directory.GetFiles(itemFolderPath);
            Assert.Equal(2, filesInFolder.Length);
            
            // Check that the duplicate was renamed with suffix
            Assert.True(filesInFolder.Any(f => f.Contains("image_1.jpg")), 
                "Duplicate file should be renamed with _1 suffix");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
            if (Directory.Exists(_testImageFolder))
                Directory.Delete(_testImageFolder, recursive: true);
        }
    }

    [Fact]
    public void AddImagesToItem_WithNullItem_ReturnsFalse()
    {
        // Arrange
        var repository = new ItemRepository(_excelFilePath);
        var service = new ImageManagementService(_testImageFolder, repository);
        
        var sourceImages = new List<string> { "dummy.jpg" };

        try
        {
            // Act
            var result = service.AddImagesToItem(null!, sourceImages);

            // Assert
            Assert.False(result);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(_testImageFolder))
                Directory.Delete(_testImageFolder, recursive: true);
        }
    }

    [Fact]
    public void AddImagesToItem_WithEmptyImageList_ReturnsFalse()
    {
        // Arrange
        var repository = new ItemRepository(_excelFilePath);
        var service = new ImageManagementService(_testImageFolder, repository);
        
        var testItem = new Item 
        { 
            Number = 996,
            Name = "Empty List Test"
        };

        var sourceImages = new List<string>();

        try
        {
            // Act
            var result = service.AddImagesToItem(testItem, sourceImages);

            // Assert
            Assert.False(result);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(_testImageFolder))
                Directory.Delete(_testImageFolder, recursive: true);
        }
    }

    [Fact]
    public void AddImagesToItem_WithNonexistentSourceFile_SkipsFile()
    {
        // Arrange
        var repository = new ItemRepository(_excelFilePath);
        var service = new ImageManagementService(_testImageFolder, repository);
        
        var testItem = new Item 
        { 
            Number = 995,
            Name = "Missing File Test"
        };

        var sourceImages = new List<string> { "C:\\nonexistent\\file.jpg" };

        try
        {
            // Act
            var result = service.AddImagesToItem(testItem, sourceImages);

            // Assert
            // Should return false because no files were successfully added
            Assert.False(result);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(_testImageFolder))
                Directory.Delete(_testImageFolder, recursive: true);
        }
    }

    [Fact]
    public void AddImagesToItem_UpdatesItemPhotosProperty()
    {
        // Arrange
        var repository = new ItemRepository(_excelFilePath);
        var service = new ImageManagementService(_testImageFolder, repository);
        
        var testItem = new Item 
        { 
            Number = 994,
            Name = "Photo Update Test",
            Photos = "existing.jpg"
        };

        var tempDir = Path.Combine(Path.GetTempPath(), "test_images_update_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var imagePath = Path.Combine(tempDir, "new_image.jpg");
        File.WriteAllBytes(imagePath, new byte[] { 0xFF, 0xD8, 0xFF });

        var sourceImages = new List<string> { imagePath };

        try
        {
            // Act
            var result = service.AddImagesToItem(testItem, sourceImages);

            // Assert
            Assert.True(result);
            Assert.NotNull(testItem.Photos);
            Assert.Contains("existing.jpg", testItem.Photos);
            Assert.Contains("new_image", testItem.Photos); // Without extension
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
            if (Directory.Exists(_testImageFolder))
                Directory.Delete(_testImageFolder, recursive: true);
        }
    }
}
