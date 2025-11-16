using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

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
            using (var spreadsheetDocument = SpreadsheetDocument.Open(fileStream, false))
            {
                var workbookPart = spreadsheetDocument.WorkbookPart;
                if (workbookPart == null)
                {
                    System.Diagnostics.Debug.WriteLine("No workbook part found");
                    return items;
                }

                // Find the first worksheet with data (skip empty sheets)
                WorksheetPart? worksheetPart = null;
                foreach (var wsPart in workbookPart.WorksheetParts)
                {
                    var sheetData = wsPart.Worksheet.Elements<SheetData>().FirstOrDefault();
                    var rows = sheetData?.Elements<Row>().ToList() ?? new List<Row>();
                    if (rows.Count > 1) // Has header + at least one data row
                    {
                        worksheetPart = wsPart;
                        break;
                    }
                }

                if (worksheetPart == null)
                {
                    System.Diagnostics.Debug.WriteLine("No worksheet with data found");
                    return items;
                }

                var sheetDataContent = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();
                if (sheetDataContent == null)
                {
                    System.Diagnostics.Debug.WriteLine("No sheet data found");
                    return items;
                }

                var rows2 = sheetDataContent.Elements<Row>().ToList();
                if (rows2.Count < 2)
                {
                    System.Diagnostics.Debug.WriteLine($"No data rows found (only {rows2.Count} rows)");
                    return items;
                }

                System.Diagnostics.Debug.WriteLine($"Found {rows2.Count} rows in Excel file");

                // Get column mapping from header row
                var headerRow = rows2[0];
                var columnMap = GetColumnMap(headerRow, workbookPart);

                System.Diagnostics.Debug.WriteLine($"Column map: {string.Join(", ", columnMap.Keys)}");

                // Process data rows
                for (int i = 1; i < rows2.Count; i++)
                {
                    var row = rows2[i];
                    var item = ParseRow(row, columnMap, workbookPart);
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

    private Dictionary<string, int> GetColumnMap(Row headerRow, WorkbookPart workbookPart)
    {
        var columnMap = new Dictionary<string, int>();
        var sharedStringsPart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();

        var headerCells = headerRow.Elements<Cell>().ToList();
        for (int i = 0; i < headerCells.Count; i++)
        {
            var cell = headerCells[i];
            var headerValue = GetCellValue(cell, sharedStringsPart)?.Trim().ToLower();

            if (!string.IsNullOrEmpty(headerValue))
            {
                columnMap[headerValue] = i;
            }
        }

        return columnMap;
    }

    private Item? ParseRow(Row row, Dictionary<string, int> columnMap, WorkbookPart workbookPart)
    {
        var sharedStringsPart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
        var cells = row.Elements<Cell>().ToList();
        var rowReference = row.RowIndex?.ToString() ?? "0";

        var item = new Item();

        if (TryGetCellValue(cells, columnMap, "nummer", sharedStringsPart, rowReference, out var numberStr))
            if (int.TryParse(numberStr, out var num)) item.Number = num;

        if (TryGetCellValue(cells, columnMap, "namn", sharedStringsPart, rowReference, out var name))
            item.Name = name;

        if (TryGetCellValue(cells, columnMap, "foto", sharedStringsPart, rowReference, out var photos))
            item.Photos = photos;

        if (TryGetCellValue(cells, columnMap, "kategori", sharedStringsPart, rowReference, out var category))
            item.Category = category;

        if (TryGetCellValue(cells, columnMap, "tillverkad_vem", sharedStringsPart, rowReference, out var createdBy))
            item.CreatedBy = createdBy;

        if (TryGetCellValue(cells, columnMap, "tillverkad_år", sharedStringsPart, rowReference, out var createdYear))
            item.CreatedYear = createdYear;

        if (TryGetCellValue(cells, columnMap, "tillverkad_plats", sharedStringsPart, rowReference, out var createdPlace))
            item.CreatedPlace = createdPlace;

        if (TryGetCellValue(cells, columnMap, "stämpel", sharedStringsPart, rowReference, out var stamp))
            item.Stamp = stamp;

        if (TryGetCellValue(cells, columnMap, "proveniens", sharedStringsPart, rowReference, out var provenance))
            item.Provenance = provenance;

        // Try both variations of the column name (with and without space after underscore)
        if (TryGetCellValue(cells, columnMap, "nuvarand_ägare", sharedStringsPart, rowReference, out var currentOwner) ||
            TryGetCellValue(cells, columnMap, "nuvarand_ ägare", sharedStringsPart, rowReference, out currentOwner))
            item.CurrentOwner = currentOwner;

        return item;
    }

    private bool TryGetCellValue(List<Cell> cells, Dictionary<string, int> columnMap, string columnName,
        SharedStringTablePart? sharedStringsPart, string rowReference, out string? value)
    {
        value = null;
        if (!columnMap.TryGetValue(columnName, out var columnIndex))
            return false;

        // Convert column index to Excel column letter
        var columnLetter = ConvertIndexToColumnLetter(columnIndex);
        var cellReference = columnLetter + rowReference;

        // Find the cell with the matching cell reference
        var cell = cells.FirstOrDefault(c => c.CellReference?.Value == cellReference);
        if (cell == null)
            return false;

        var cellValue = GetCellValue(cell, sharedStringsPart);
        if (!string.IsNullOrEmpty(cellValue))
        {
            value = cellValue.Trim();
            return true;
        }

        return false;
    }

    private string ConvertIndexToColumnLetter(int columnIndex)
    {
        // Convert 0-based column index to Excel column letter (A, B, C, ..., Z, AA, AB, ...)
        string columnLetter = "";
        int index = columnIndex + 1; // Excel columns are 1-based

        while (index > 0)
        {
            int remainder = (index - 1) % 26;
            columnLetter = (char)('A' + remainder) + columnLetter;
            index = (index - remainder - 1) / 26;
        }

        return columnLetter;
    }

    private string? GetCellValue(Cell? cell, SharedStringTablePart? sharedStringsPart)
    {
        if (cell == null)
            return null;

        var cellValue = cell.CellValue;
        if (cellValue == null)
            return null;

        if (cell.DataType?.Value == CellValues.SharedString)
        {
            if (int.TryParse(cellValue.Text, out var sharedStringIndex))
                return sharedStringsPart?.SharedStringTable.ElementAt(sharedStringIndex).InnerText;
        }

        return cellValue.Text;
    }
}
