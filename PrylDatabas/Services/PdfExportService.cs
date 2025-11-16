using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using PrylDatabas.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace PrylDatabas.Services;

public class PdfExportService
{
    private readonly ImageService _imageService;
    private const string LOG_PREFIX = "[PdfExportService]";

    public PdfExportService(string? imageFolderPath = null)
    {
        // If no path provided, try to find it relative to the application directory
        if (string.IsNullOrEmpty(imageFolderPath))
        {
            // First try relative to app directory
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            imageFolderPath = Path.Combine(appDir, "data", "Gamla Prylar - foton i dbs");
            
            Debug.WriteLine($"{LOG_PREFIX} Trying app directory path: {imageFolderPath}");
            Debug.WriteLine($"{LOG_PREFIX} Path exists: {Directory.Exists(imageFolderPath)}");
            
            // If not found, try from current working directory
            if (!Directory.Exists(imageFolderPath))
            {
                imageFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "Gamla Prylar - foton i dbs");
                Debug.WriteLine($"{LOG_PREFIX} Trying current working directory path: {imageFolderPath}");
                Debug.WriteLine($"{LOG_PREFIX} Path exists: {Directory.Exists(imageFolderPath)}");
            }
        }
        
        _imageService = new ImageService(imageFolderPath);
        Debug.WriteLine($"{LOG_PREFIX} ImageService initialized with folder: {_imageService.ImageFolderPath}");
    }

    public void ExportItemsToPdf(IEnumerable<Item> items, string outputPath, bool includePhotos = true)
    {
        var itemList = items.ToList();
        Debug.WriteLine($"{LOG_PREFIX} Starting PDF export for {itemList.Count} items");
        Debug.WriteLine($"{LOG_PREFIX} Output path: {outputPath}");
        Debug.WriteLine($"{LOG_PREFIX} Include photos: {includePhotos}");

        var document = Document.Create(container =>
        {
            foreach (var item in itemList)
            {
                Debug.WriteLine($"{LOG_PREFIX} Processing item: Nr {item.Number} - {item.Name}");
                
                container.Page(page =>
                {
                    page.Margin(40);

                    page.Content().Column(column =>
                    {
                        // Header
                        column.Item().PaddingBottom(20).Column(header =>
                        {
                            header.Item().Text($"Nr {item.Number}").FontSize(24).Bold();
                            header.Item().Text(item.Name ?? "").FontSize(18);
                        });

                        // Divider
                        column.Item().BorderBottom(1).PaddingBottom(15);

                        // Details section
                        column.Item().PaddingBottom(20).Column(details =>
                        {
                            AddDetailRow(details, "Kategori", item.Category);
                            AddDetailRow(details, "Tillverkad", item.CreatedInfo);
                            AddDetailRow(details, "Stämpel", item.Stamp);
                            AddDetailRow(details, "Nuvarande Ägare", item.CurrentOwner);
                            AddDetailRow(details, "Proveniens", item.Provenance);
                        });

                        // Photos section
                        if (includePhotos)
                        {
                            Debug.WriteLine($"{LOG_PREFIX} Looking for photos for item {item.Number}");
                            Debug.WriteLine($"{LOG_PREFIX} Photos field value: '{item.Photos}'");
                            
                            if (!string.IsNullOrEmpty(item.Photos))
                            {
                                var images = _imageService.FindImages(item.Number.ToString(), item.Photos).ToList();
                                Debug.WriteLine($"{LOG_PREFIX} Found {images.Count} images for item {item.Number}");
                                
                                if (images.Count > 0)
                                {
                                    column.Item().PaddingTop(20).Text("Foton").FontSize(14).Bold();

                                    foreach (var imagePath in images)
                                    {
                                        Debug.WriteLine($"{LOG_PREFIX} Checking image file: {imagePath}");
                                        
                                        if (File.Exists(imagePath))
                                        {
                                            try
                                            {
                                                var fileInfo = new FileInfo(imagePath);
                                                Debug.WriteLine($"{LOG_PREFIX} Image file exists, size: {fileInfo.Length} bytes");
                                                Debug.WriteLine($"{LOG_PREFIX} Adding image to PDF: {Path.GetFileName(imagePath)}");
                                                
                                                column.Item().PaddingTop(10).MaxHeight(250).Image(imagePath);
                                                
                                                Debug.WriteLine($"{LOG_PREFIX} Image added successfully to PDF");
                                            }
                                            catch (Exception ex)
                                            {
                                                Debug.WriteLine($"{LOG_PREFIX} ERROR loading image {imagePath}: {ex.GetType().Name} - {ex.Message}");
                                                if (ex.InnerException != null)
                                                {
                                                    Debug.WriteLine($"{LOG_PREFIX} Inner exception: {ex.InnerException.Message}");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Debug.WriteLine($"{LOG_PREFIX} Image file NOT FOUND: {imagePath}");
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine($"{LOG_PREFIX} No images found for item {item.Number}");
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"{LOG_PREFIX} Photos field is empty for item {item.Number}");
                            }
                        }
                    });
                });
            }
        });

        try
        {
            Debug.WriteLine($"{LOG_PREFIX} Generating PDF document...");
            document.GeneratePdf(outputPath);
            Debug.WriteLine($"{LOG_PREFIX} PDF generated successfully: {outputPath}");
            
            var fileInfo = new FileInfo(outputPath);
            Debug.WriteLine($"{LOG_PREFIX} PDF file size: {fileInfo.Length} bytes");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{LOG_PREFIX} ERROR generating PDF: {ex.GetType().Name} - {ex.Message}");
            if (ex.InnerException != null)
            {
                Debug.WriteLine($"{LOG_PREFIX} Inner exception: {ex.InnerException.Message}");
            }
            throw;
        }
    }

    private void AddDetailRow(ColumnDescriptor column, string label, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        column.Item().PaddingBottom(8).Row(row =>
        {
            row.RelativeItem(2).Text(label + ":").Bold();
            row.RelativeItem(3).Text(value);
        });
    }
}
