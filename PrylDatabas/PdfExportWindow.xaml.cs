using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using PrylDatabas.Models;
using PrylDatabas.Services;

namespace PrylDatabas;

public partial class PdfExportWindow : Window
{
    private List<Item> _selectedItems;
    private PdfExportService _pdfExportService;

    public PdfExportWindow(List<Item> selectedItems)
    {
        InitializeComponent();
        _selectedItems = selectedItems;
        _pdfExportService = new PdfExportService();
        
        // Bind selected items to ListBox
        ItemsListBox.ItemsSource = _selectedItems.Select(i => $"Nr {i.Number} - {i.Name}").ToList();
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        // Show save file dialog
        var saveDialog = new SaveFileDialog
        {
            Filter = "PDF-filer (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            FileName = $"prylar_export_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                bool includePhotos = IncludeImagesCheckBox.IsChecked == true;
                bool openPdf = OpenPdfCheckBox.IsChecked == true;
                
                _pdfExportService.ExportItemsToPdf(_selectedItems, saveDialog.FileName, includePhotos);
                
                // Open PDF if checkbox is checked
                if (openPdf)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = saveDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Kunde inte öppna PDF: {ex.Message}", "Varning", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                
                MessageBox.Show($"PDF exporterad:\n{saveDialog.FileName}", "Klart", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fel vid export: {ex.Message}", "Exportfel", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

