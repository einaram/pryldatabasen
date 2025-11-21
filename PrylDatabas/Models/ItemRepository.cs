using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace PrylDatabas.Models;

public class ItemRepository
{
    private readonly string _filePath;

    public ItemRepository(string filePath)
    {
        _filePath = filePath;
    }

    public ObservableCollection<Item> LoadItems()
    {
        var items = new ObservableCollection<Item>();

        // Resolve the file path if it's relative
        var resolvedPath = ResolvePath(_filePath);
        
        if (!File.Exists(resolvedPath))
        {
            System.Diagnostics.Debug.WriteLine($"Excel file not found: {resolvedPath}");
            return items;
        }

        try
        {
            // Open with FileShare.ReadWrite to allow other processes (like Excel) to have the file open
            using (var fileStream = new FileStream(resolvedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var workbook = new XLWorkbook(fileStream))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    System.Diagnostics.Debug.WriteLine("No worksheet found");
                    return items;
                }

                var rows = worksheet.RangeUsed()?.Rows().ToList();
                if (rows == null || rows.Count < 2)
                {
                    System.Diagnostics.Debug.WriteLine($"No data rows found (only {rows?.Count ?? 0} rows)");
                    return items;
                }

                System.Diagnostics.Debug.WriteLine($"Found {rows.Count} rows in Excel file");

                // Get column mapping from header row
                var headerRow = rows[0];
                var columnMap = GetColumnMap(headerRow);

                System.Diagnostics.Debug.WriteLine($"Column map: {string.Join(", ", columnMap.Keys)}");

                // Process data rows
                for (int i = 1; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var item = ParseRow(row, columnMap);
                    if (item != null && !string.IsNullOrEmpty(item.Name))
                    {
                        items.Add(item);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Successfully loaded {items.Count} items");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading items: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        return items;
    }

    /// <summary>
    /// Update the Photos cell for an item and save the workbook.
    /// </summary>
    public void UpdateItemPhotos(int itemNumber, string photoFileNames)
    {
        var resolvedPath = ResolvePath(_filePath);
        
        if (!File.Exists(resolvedPath))
        {
            System.Diagnostics.Debug.WriteLine($"Excel file not found: {resolvedPath}");
            return;
        }

        try
        {
            using (var workbook = new XLWorkbook(resolvedPath))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    System.Diagnostics.Debug.WriteLine("No worksheet found");
                    return;
                }

                var rows = worksheet.RangeUsed()?.Rows().ToList();
                if (rows == null || rows.Count < 2)
                {
                    System.Diagnostics.Debug.WriteLine("No data rows found");
                    return;
                }

                // Get column mapping from header row
                var headerRow = rows[0];
                var columnMap = GetColumnMap(headerRow);

                if (!columnMap.TryGetValue("foto", out var photoColumnIndex))
                {
                    System.Diagnostics.Debug.WriteLine("Photo column not found");
                    return;
                }

                // Find the row with matching item number
                for (int i = 1; i < rows.Count; i++)
                {
                    var row = rows[i];
                    if (TryGetCellValue(row, columnMap, "nummer", out var numberStr) &&
                        int.TryParse(numberStr, out var number) && number == itemNumber)
                    {
                        // Update the photo cell
                        var photoCell = row.Cell(photoColumnIndex + 1); // ClosedXML uses 1-based indexing
                        photoCell.SetValue(photoFileNames);
                        System.Diagnostics.Debug.WriteLine($"Updated photos for item {itemNumber} to: {photoFileNames}");
                        break;
                    }
                }

                workbook.Save();
                System.Diagnostics.Debug.WriteLine("Workbook saved successfully");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating photos: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private string ResolvePath(string filePath)
    {
        // If it's already an absolute path and exists, return it
        if (Path.IsPathRooted(filePath) && File.Exists(filePath))
            return filePath;

        // Try to find it relative to the solution root
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        var solutionRoot = currentDir;

        // Walk up the directory tree to find the solution file
        while (solutionRoot.Parent != null && !File.Exists(Path.Combine(solutionRoot.FullName, "PrylDatabasSolution.sln")))
        {
            solutionRoot = solutionRoot.Parent;
        }

        var resolvedPath = Path.Combine(solutionRoot.FullName, filePath);
        if (File.Exists(resolvedPath))
            return resolvedPath;

        // If still not found, return the original path
        return filePath;
    }

    private Dictionary<string, int> GetColumnMap(IXLRangeRow headerRow)
    {
        var columnMap = new Dictionary<string, int>();

        var lastCell = headerRow.LastCellUsed();
        var columnCount = lastCell?.Address.ColumnNumber ?? 0;
        
        for (int i = 1; i <= columnCount; i++)
        {
            var headerValue = headerRow.Cell(i).GetString().Trim().ToLower();

            if (!string.IsNullOrEmpty(headerValue))
            {
                columnMap[headerValue] = i - 1; // Store as 0-based index
            }
        }

        return columnMap;
    }

    private Item? ParseRow(IXLRangeRow row, Dictionary<string, int> columnMap)
    {
        var item = new Item();

        if (TryGetCellValue(row, columnMap, "nummer", out var numberStr))
            if (int.TryParse(numberStr, out var num)) item.Number = num;

        if (TryGetCellValue(row, columnMap, "namn", out var name))
            item.Name = name;

        if (TryGetCellValue(row, columnMap, "foto", out var photos))
            item.Photos = photos;

        if (TryGetCellValue(row, columnMap, "kategori", out var category))
            item.Category = category;

        if (TryGetCellValue(row, columnMap, "tillverkad_vem", out var createdBy))
            item.CreatedBy = createdBy;

        if (TryGetCellValue(row, columnMap, "tillverkad_år", out var createdYear))
            item.CreatedYear = createdYear;

        if (TryGetCellValue(row, columnMap, "tillverkad_plats", out var createdPlace))
            item.CreatedPlace = createdPlace;

        if (TryGetCellValue(row, columnMap, "stämpel", out var stamp))
            item.Stamp = stamp;

        if (TryGetCellValue(row, columnMap, "proveniens", out var provenance))
            item.Provenance = provenance;

        // Try both variations of the column name (with and without space)
        if (TryGetCellValue(row, columnMap, "nuvarand_ägare", out var currentOwner) ||
            TryGetCellValue(row, columnMap, "nuvarand_ ägare", out currentOwner))
            item.CurrentOwner = currentOwner;

        return item;
    }

    private bool TryGetCellValue(IXLRangeRow row, Dictionary<string, int> columnMap, string columnName, out string? value)
    {
        value = null;
        if (!columnMap.TryGetValue(columnName, out var columnIndex))
            return false;

        var cellValue = row.Cell(columnIndex + 1).GetString(); // ClosedXML uses 1-based indexing
        if (!string.IsNullOrEmpty(cellValue))
        {
            value = cellValue.Trim();
            return true;
        }

        return false;
    }
}
