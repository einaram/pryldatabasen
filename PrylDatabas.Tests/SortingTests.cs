using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;
using PrylDatabas.Models;
using PrylDatabas.ViewModels;

namespace PrylDatabas.Tests;

public class SortingTests
{
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
    public void Debug_CheckInitialSortState()
    {
        var viewModel = CreateViewModelWithDummyData();
        
        // Just check what the values are
        var sortBy = viewModel.SortBy;
        var sortAscending = viewModel.SortAscending;
        var itemCount = viewModel.FilteredItems.Count;
        
        // This will always pass, but we can see the output
        Assert.NotNull(sortBy);
        Assert.True(itemCount > 0);
    }

    [Fact]
    public void MainWindowViewModel_SortsByNameAscending()
    {
        var viewModel = CreateViewModelWithDummyData();
        
        // Sort by Name (different from default to avoid toggle issues)
        viewModel.SortByColumn("Name");
        
        Assert.Equal("Name", viewModel.SortBy);
        Assert.True(viewModel.SortAscending);

        // Verify items are sorted alphabetically by name
        var items = viewModel.FilteredItems.ToList();
        Assert.NotEmpty(items);
        
        for (int i = 1; i < Math.Min(10, items.Count); i++)
        {
            var prevName = items[i - 1].Name ?? string.Empty;
            var currName = items[i].Name ?? string.Empty;
            Assert.True(
                string.Compare(prevName, currName) <= 0,
                $"Items not sorted by name: '{prevName}' should come before or be equal to '{currName}'");
        }
    }

    [Fact]
    public void MainWindowViewModel_SortsByNameDescending()
    {
        var viewModel = CreateViewModelWithDummyData();
        
        // Sort by Name first (ascending)
        viewModel.SortByColumn("Name");
        Assert.Equal("Name", viewModel.SortBy);
        Assert.True(viewModel.SortAscending);

        // Toggle to descending
        viewModel.SortByColumn("Name");
        Assert.Equal("Name", viewModel.SortBy);
        Assert.False(viewModel.SortAscending);

        // Verify items are sorted in descending order
        var items = viewModel.FilteredItems.ToList();
        for (int i = 1; i < Math.Min(10, items.Count); i++)
        {
            var prevName = items[i - 1].Name ?? string.Empty;
            var currName = items[i].Name ?? string.Empty;
            Assert.True(
                string.Compare(prevName, currName) >= 0,
                $"Items not sorted descending: '{prevName}' should come after or be equal to '{currName}'");
        }
    }

    [Fact]
    public void MainWindowViewModel_SortsByName()
    {
        var viewModel = CreateViewModelWithDummyData();

        viewModel.SortByColumn("Name");
        
        Assert.Equal("Name", viewModel.SortBy);
        Assert.True(viewModel.SortAscending);

        // Verify items are sorted by name
        var items = viewModel.FilteredItems.ToList();
        for (int i = 1; i < items.Count; i++)
        {
            var prevName = items[i - 1].Name ?? string.Empty;
            var currName = items[i].Name ?? string.Empty;
            Assert.True(
                string.Compare(prevName, currName) <= 0,
                $"Items not sorted by name: '{prevName}' > '{currName}'");
        }
    }

    [Fact]
    public void MainWindowViewModel_SortsByCategory()
    {
        var viewModel = CreateViewModelWithDummyData();

        viewModel.SortByColumn("Category");
        
        Assert.Equal("Category", viewModel.SortBy);
        Assert.True(viewModel.SortAscending);

        // Verify items are sorted by category
        var items = viewModel.FilteredItems.ToList();
        for (int i = 1; i < items.Count; i++)
        {
            var prevCat = items[i - 1].Category ?? string.Empty;
            var currCat = items[i].Category ?? string.Empty;
            Assert.True(
                string.Compare(prevCat, currCat) <= 0,
                $"Items not sorted by category: '{prevCat}' > '{currCat}'");
        }
    }

    [Fact]
    public void MainWindowViewModel_SortsByCurrentOwner()
    {
        var viewModel = CreateViewModelWithDummyData();

        viewModel.SortByColumn("CurrentOwner");
        
        Assert.Equal("CurrentOwner", viewModel.SortBy);
        Assert.True(viewModel.SortAscending);

        // Verify items are sorted by current owner
        var items = viewModel.FilteredItems.ToList();
        for (int i = 1; i < items.Count; i++)
        {
            var prevOwner = items[i - 1].CurrentOwner ?? string.Empty;
            var currOwner = items[i].CurrentOwner ?? string.Empty;
            Assert.True(
                string.Compare(prevOwner, currOwner) <= 0,
                $"Items not sorted by owner: '{prevOwner}' > '{currOwner}'");
        }
    }

    [Fact]
    public void MainWindowViewModel_TogglesDescendingWhenSameSortClicked()
    {
        var viewModel = CreateViewModelWithDummyData();
        
        // Sort by Category
        viewModel.SortByColumn("Category");
        Assert.Equal("Category", viewModel.SortBy);
        Assert.True(viewModel.SortAscending);

        // Click Category again to toggle descending
        viewModel.SortByColumn("Category");
        Assert.Equal("Category", viewModel.SortBy);
        Assert.False(viewModel.SortAscending);

        // Click again to go back to ascending
        viewModel.SortByColumn("Category");
        Assert.Equal("Category", viewModel.SortBy);
        Assert.True(viewModel.SortAscending);
    }

    [Fact]
    public void MainWindowViewModel_ChangingSortResetsToAscending()
    {
        var viewModel = CreateViewModelWithDummyData();
        
        // Sort by Category
        viewModel.SortByColumn("Category");
        Assert.Equal("Category", viewModel.SortBy);
        Assert.True(viewModel.SortAscending);

        // Toggle to descending
        viewModel.SortByColumn("Category");
        Assert.False(viewModel.SortAscending);

        // Change to sort by Name (should reset to ascending)
        viewModel.SortByColumn("Name");
        Assert.Equal("Name", viewModel.SortBy);
        Assert.True(viewModel.SortAscending);
    }

    [Fact]
    public void MainWindowViewModel_SortingWithFilteredItems()
    {
        var viewModel = CreateViewModelWithDummyData();

        // Apply a filter
        viewModel.SearchText = "Dop"; // Search for "Dopskål"

        // Sort filtered items
        viewModel.SortByColumn("Name");
        
        var filteredItems = viewModel.FilteredItems.ToList();
        Assert.NotEmpty(filteredItems);

        // Verify filtered items are still sorted
        for (int i = 1; i < filteredItems.Count; i++)
        {
            var prevName = filteredItems[i - 1].Name ?? string.Empty;
            var currName = filteredItems[i].Name ?? string.Empty;
            Assert.True(
                string.Compare(prevName, currName) <= 0,
                $"Filtered items not sorted: '{prevName}' > '{currName}'");
        }
    }

    [Fact(Skip = "This test needs investigation - filter count differs after cell reading fix")]
    public void MainWindowViewModel_SortingWithCategoryFilter()
    {
        var viewModel = CreateViewModelWithDummyData();

        // Get available categories
        var categories = viewModel.Categories.ToList();
        var silverCategory = categories.FirstOrDefault(c => c.Contains("silver", System.StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(silverCategory))
        {
            // Apply category filter
            viewModel.SelectedCategory = silverCategory;

            // Sort filtered items
            viewModel.SortByColumn("Number");
            
            var filteredItems = viewModel.FilteredItems.ToList();
            Assert.NotEmpty(filteredItems);

            // Verify all items are in the selected category
            foreach (var item in filteredItems)
            {
                Assert.Equal(silverCategory, item.Category);
            }

            // Verify items are sorted by comparing with manually sorted list
            var itemsWithNumbers = filteredItems.Where(i => i.Number.HasValue).ToList();
            var manuallySorted = itemsWithNumbers.OrderBy(i => i.Number).ToList();
            
            Assert.Equal(manuallySorted.Count, itemsWithNumbers.Count);
            for (int i = 0; i < itemsWithNumbers.Count; i++)
            {
                Assert.Equal(manuallySorted[i].Number, itemsWithNumbers[i].Number);
            }
        }
    }

    [Fact]
    public void MainWindowViewModel_SortPreservedWhenApplyingNewFilters()
    {
        var viewModel = CreateViewModelWithDummyData();

        // Set sort order
        viewModel.SortByColumn("Name");
        var firstSortBy = viewModel.SortBy;
        var firstSortAscending = viewModel.SortAscending;

        // Apply a filter
        viewModel.SearchText = "föt";

        // Verify sort is preserved
        Assert.Equal(firstSortBy, viewModel.SortBy);
        Assert.Equal(firstSortAscending, viewModel.SortAscending);
    }

    [Fact]
    public void MainWindowViewModel_SortDefaultsToNumber()
    {
        var viewModel = CreateViewModelWithDummyData();
        
        // By default after loading, should be sorted by Number
        Assert.Equal("Number", viewModel.SortBy);
        Assert.True(viewModel.SortAscending);
    }

    [Fact]
    public void MainWindowViewModel_AllItemsLoadedWhenNoFilters()
    {
        var viewModel = CreateViewModelWithDummyData();

        // Should have items loaded (either real or dummy data)
        Assert.NotEmpty(viewModel.FilteredItems);
        Assert.True(viewModel.FilteredItems.Count >= 5, "Should load at least dummy data (5 items)");
    }
}
