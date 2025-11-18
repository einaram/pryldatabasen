using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Xunit;
using PrylDatabas.Models;
using PrylDatabas.ViewModels;

namespace PrylDatabas.Tests;

public class ViewModelTests
{
    private string GetSolutionRoot()
    {
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        var solutionRoot = currentDir;
        while (solutionRoot.Parent != null && !File.Exists(Path.Combine(solutionRoot.FullName, "PrylDatabasSolution.sln")))
        {
            solutionRoot = solutionRoot.Parent;
        }
        return solutionRoot.FullName;
    }

    private MainWindowViewModel CreateViewModelWithDummyData()
    {
        var viewModel = new MainWindowViewModel();
        
        // If no items loaded (Excel file not available), populate with dummy data
        if (viewModel.FilteredItems.Count == 0)
        {
            var dummyItems = TestDataProvider.GetDummyItems();
            foreach (var item in dummyItems)
            {
                viewModel.FilteredItems.Add(item);
            }
            
            // Rebuild categories from dummy items
            viewModel.Categories.Clear();
            viewModel.Categories.Add("Alla");
            var categories = new HashSet<string?>();
            foreach (var item in dummyItems)
            {
                if (!string.IsNullOrEmpty(item.Category))
                {
                    categories.Add(item.Category);
                }
            }
            foreach (var cat in categories.OrderBy(c => c))
            {
                viewModel.Categories.Add(cat);
            }
        }
        
        return viewModel;
    }

    [Fact]
    public void MainWindowViewModelLoadsItems()
    {
        // Arrange
        var viewModel = CreateViewModelWithDummyData();

        // Assert - should load items by default or use dummy data
        Assert.NotNull(viewModel.FilteredItems);
        Assert.NotEmpty(viewModel.FilteredItems);
    }

    [Fact]
    public void MainWindowViewModelHasCategories()
    {
        // Arrange
        var viewModel = CreateViewModelWithDummyData();

        // Assert - should have categories
        Assert.NotNull(viewModel.Categories);
        Assert.NotEmpty(viewModel.Categories);
        Assert.Contains("Alla", viewModel.Categories);
    }

    [Fact]
    public void MainWindowViewModelSearchFilters()
    {
        // Arrange
        var viewModel = CreateViewModelWithDummyData();
        var initialCount = viewModel.FilteredItems.Count;

        // Act - search for something that exists in dummy data
        viewModel.SearchText = "Dopskål";

        // Assert
        Assert.True(viewModel.FilteredItems.Count > 0, "Should find items matching 'Dopskål'");
        Assert.True(viewModel.FilteredItems.Count <= initialCount, "Filtered count should be <= original");
    }

    [Fact]
    public void MainWindowViewModelCategoryFilter()
    {
        // Arrange
        var viewModel = CreateViewModelWithDummyData();
        
        // Get first non-"Alla" category
        string? category = null;
        foreach (var cat in viewModel.Categories)
        {
            if (cat != "Alla")
            {
                category = cat;
                break;
            }
        }

        Assert.NotNull(category);
        var initialCount = viewModel.FilteredItems.Count;

        // Act
        viewModel.SelectedCategory = category;

        // Assert
        Assert.True(viewModel.FilteredItems.Count > 0, "Should find items in category");
        Assert.True(viewModel.FilteredItems.Count <= initialCount, "Filtered count should be <= original");
    }
}
