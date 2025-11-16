using System;
using System.IO;
using System.Linq;
using Xunit;
using PrylDatabas.Models;

namespace PrylDatabas.Tests;

public class ExcelFileReaderTests
{
    private readonly string _excelFilePath;
    private readonly string _solutionRoot;

    public ExcelFileReaderTests()
    {
        // Find solution root
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        _solutionRoot = currentDir.FullName;
        while (currentDir.Parent != null && !File.Exists(Path.Combine(currentDir.FullName, "PrylDatabasSolution.sln")))
        {
            currentDir = currentDir.Parent;
            _solutionRoot = currentDir.FullName;
        }

        // Path to the actual Excel file in the data directory
        _excelFilePath = Path.Combine(_solutionRoot, "data", "Gamla Prylar - dbs", "Gamla prylar 251115-xlsx.xlsx");
    }

    [Fact]
    public void ExcelFileExists()
    {
        Assert.True(File.Exists(_excelFilePath), 
            $"Excel file not found at: {_excelFilePath}\n" +
            $"Solution root: {_solutionRoot}");
    }

    [Fact]
    public void CanLoadItemsFromExcel()
    {
        // Arrange
        var repository = new ItemRepository(_excelFilePath);

        // Act
        var items = repository.LoadItems();

        // Assert
        Assert.NotNull(items);
        Assert.NotEmpty(items);
    }

    [Fact]
    public void LoadedItemsHaveExpectedProperties()
    {
        // Arrange
        var repository = new ItemRepository(_excelFilePath);

        // Act
        var items = repository.LoadItems();

        // Assert
        Assert.NotEmpty(items);
        
        var firstItem = items[0];
        Assert.NotNull(firstItem.Name);
        Assert.True(!string.IsNullOrWhiteSpace(firstItem.Name), 
            "First item should have a name");
    }

    [Fact]
    public void ItemCountIsGreaterThanZero()
    {
        // Arrange
        var repository = new ItemRepository(_excelFilePath);

        // Act
        var items = repository.LoadItems();

        // Assert
        Assert.True(items.Count > 0, 
            $"Expected at least 1 item, but got {items.Count}");
    }

    [Fact]
    public void AllLoadedItemsHaveNames()
    {
        // Arrange
        var repository = new ItemRepository(_excelFilePath);

        // Act
        var items = repository.LoadItems();

        // Assert
        foreach (var item in items)
        {
            Assert.NotNull(item.Name);
            Assert.NotEmpty(item.Name);
        }
    }

    [Fact]
    public void CategoriesArePopulatedCorrectly()
    {
        // Arrange
        var repository = new ItemRepository(_excelFilePath);

        // Act
        var items = repository.LoadItems();
        var categoriesWithContent = items
            .Where(i => !string.IsNullOrEmpty(i.Category))
            .Select(i => i.Category)
            .Distinct()
            .ToList();

        // Assert
        Assert.NotEmpty(categoriesWithContent);
    }
}
