using System;
using System.IO;
using Xunit;
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

    [Fact]
    public void MainWindowViewModelLoadsItems()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Assert - should load items by default
        Assert.NotNull(viewModel.FilteredItems);
        Assert.NotEmpty(viewModel.FilteredItems);
    }

    [Fact]
    public void MainWindowViewModelHasCategories()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Assert - should have categories
        Assert.NotNull(viewModel.Categories);
        Assert.NotEmpty(viewModel.Categories);
        Assert.Contains("Alla", viewModel.Categories);
    }

    [Fact]
    public void MainWindowViewModelSearchFilters()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        var initialCount = viewModel.FilteredItems.Count;

        // Act - search for something that likely exists
        viewModel.SearchText = "dopskål";

        // Assert
        Assert.True(viewModel.FilteredItems.Count > 0, "Should find items matching 'dopskål'");
        Assert.True(viewModel.FilteredItems.Count <= initialCount, "Filtered count should be <= original");
    }

    [Fact]
    public void MainWindowViewModelCategoryFilter()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        
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
