using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using PrylDatabas.Models;

namespace PrylDatabas.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private ItemRepository _itemRepository;
    private ObservableCollection<Item> _allItems;
    private ObservableCollection<Item> _filteredItems;
    private Item? _selectedItem;
    private string _searchText = string.Empty;
    private string? _selectedCategory;
    private string _excelFilePath;
    private string _imageFolderPath;
    private string _sortBy = string.Empty; // Start empty so first sort sets it
    private bool _sortAscending = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<Item> FilteredItems
    {
        get => _filteredItems;
        set => SetProperty(ref _filteredItems, value);
    }

    public Item? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilters();
        }
    }

    public string? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
                ApplyFilters();
        }
    }

    public ObservableCollection<string> Categories { get; }

    public string SortBy
    {
        get => _sortBy;
        set
        {
            if (SetProperty(ref _sortBy, value))
                ApplyFilters();
        }
    }

    public bool SortAscending
    {
        get => _sortAscending;
        set
        {
            if (SetProperty(ref _sortAscending, value))
                ApplyFilters();
        }
    }

    public MainWindowViewModel()
    {
        _excelFilePath = GetExcelFilePath();
        _imageFolderPath = GetImageFolderPath();
        _allItems = new ObservableCollection<Item>();
        _filteredItems = new ObservableCollection<Item>();
        Categories = new ObservableCollection<string>();
        _itemRepository = new ItemRepository(_excelFilePath);

        _sortBy = "Number";  // Set default without triggering toggle
        LoadItems();
        // LoadItems calls ApplyFilters which will apply the default sort
    }

    public void LoadItems()
    {
        _allItems = _itemRepository.LoadItems();
        
        // Debug logging for photos
        System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Loaded {_allItems.Count} items");
        foreach (var item in _allItems.Take(10))
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Item {item.Number}: {item.Name} - Photos: '{item.Photos ?? "(null)"}'");
        }
        
        UpdateCategories();
        ApplyFilters();
    }

    public void SetExcelFilePath(string filePath)
    {
        _excelFilePath = filePath;
        _itemRepository = new ItemRepository(filePath);
        LoadItems();
    }

    public void SetImageFolderPath(string folderPath)
    {
        _imageFolderPath = folderPath;
    }

    private void UpdateCategories()
    {
        Categories.Clear();
        Categories.Add("Alla"); // "All" in Swedish

        var uniqueCategories = _allItems
            .Where(i => !string.IsNullOrEmpty(i.Category))
            .Select(i => i.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        foreach (var category in uniqueCategories)
        {
            if (!string.IsNullOrEmpty(category))
                Categories.Add(category);
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrEmpty(_searchText))
        {
            var searchLower = _searchText.ToLower();
            filtered = filtered.Where(i =>
                (i.Name?.ToLower().Contains(searchLower) ?? false) ||
                (i.Number?.ToString().Contains(searchLower) ?? false) ||
                (i.CurrentOwner?.ToLower().Contains(searchLower) ?? false));
        }

        // Apply category filter
        if (!string.IsNullOrEmpty(_selectedCategory) && _selectedCategory != "Alla")
        {
            filtered = filtered.Where(i => i.Category == _selectedCategory);
        }

        // Apply sorting
        filtered = SortItems(filtered);

        FilteredItems = new ObservableCollection<Item>(filtered);
    }

    private IEnumerable<Item> SortItems(IEnumerable<Item> items)
    {
        return _sortBy switch
        {
            "Number" => _sortAscending ? items.OrderBy(i => i.Number) : items.OrderByDescending(i => i.Number),
            "Name" => _sortAscending ? items.OrderBy(i => i.Name) : items.OrderByDescending(i => i.Name),
            "Category" => _sortAscending ? items.OrderBy(i => i.Category) : items.OrderByDescending(i => i.Category),
            "CurrentOwner" => _sortAscending ? items.OrderBy(i => i.CurrentOwner) : items.OrderByDescending(i => i.CurrentOwner),
            _ => items.OrderBy(i => i.Number)
        };
    }

    public void SortByColumn(string columnName)
    {
        // If clicking the same column, toggle sort direction
        if (_sortBy == columnName)
        {
            SortAscending = !SortAscending;
        }
        else
        {
            // New column, sort ascending
            SortBy = columnName;
            SortAscending = true;
        }
    }

    private string GetExcelFilePath()
    {
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PrylDatabas",
            "settings.txt");

        if (File.Exists(settingsPath))
        {
            try
            {
                var lines = File.ReadAllLines(settingsPath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("ExcelFile=", StringComparison.OrdinalIgnoreCase))
                    {
                        var path = line.Substring("ExcelFile=".Length).Trim();
                        if (File.Exists(path))
                            return path;
                    }
                }
            }
            catch { }
        }

        // Default path relative to solution root
        return Path.Combine("data", "Gamla Prylar - dbs", "Gamla prylar 251115-xlsx.xlsx");
    }

    private string GetImageFolderPath()
    {
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PrylDatabas",
            "settings.txt");

        if (File.Exists(settingsPath))
        {
            try
            {
                var lines = File.ReadAllLines(settingsPath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("ImageFolderPath=", StringComparison.OrdinalIgnoreCase))
                    {
                        var path = line.Substring("ImageFolderPath=".Length).Trim();
                        if (!string.IsNullOrEmpty(path))
                            return path;
                    }
                }
            }
            catch { }
        }

        // Default path - resolve to absolute path
        var solutionRoot = ResolveSolutionRoot();
        return Path.Combine(solutionRoot, "data", "Gamla Prylar - foton i dbs");
    }

    private string ResolveSolutionRoot()
    {
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        var searchDir = currentDir;

        while (searchDir.Parent != null && !File.Exists(Path.Combine(searchDir.FullName, "PrylDatabasSolution.sln")))
        {
            searchDir = searchDir.Parent;
        }
        return searchDir.FullName;
    }

    public string ImageFolderPath => _imageFolderPath;

    public string ExcelFilePath => _excelFilePath;

    public ItemRepository ItemRepository => _itemRepository;

    public void RefreshItems()
    {
        LoadItems();
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
