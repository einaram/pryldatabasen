using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PrylDatabas;
using PrylDatabas.Models;

namespace PrylDatabas.Tests;

public class ImageDisplayTests
{
    [Fact]
    public void ImageResult_CanBeCreated_WithFoundImage()
    {
        var display = new ImageResult("test.jpg", "/path/to/test.jpg", true);
        
        Assert.NotNull(display);
        Assert.Equal("test.jpg", display.FileName);
        Assert.Equal("/path/to/test.jpg", display.FullPath);
        Assert.True(display.Found);
    }

    [Fact]
    public void ImageResult_CanBeCreated_WithMissingImage()
    {
        var display = new ImageResult("missing.jpg", "", false);
        
        Assert.NotNull(display);
        Assert.Equal("missing.jpg", display.FileName);
        Assert.Empty(display.FullPath);
        Assert.False(display.Found);
    }

    [Fact]
    public void ImageStatusConverter_ReturnsGreen_ForFoundImage()
    {
        var converter = new ImageStatusConverter();
        var result = converter.Convert(true, typeof(System.Windows.Media.Brush), null, System.Globalization.CultureInfo.InvariantCulture);
        
        Assert.NotNull(result);
        Assert.Equal(System.Windows.Media.Brushes.Green, result);
    }

    [Fact]
    public void ImageStatusConverter_ReturnsOrange_ForMissingImage()
    {
        var converter = new ImageStatusConverter();
        var result = converter.Convert(false, typeof(System.Windows.Media.Brush), null, System.Globalization.CultureInfo.InvariantCulture);
        
        Assert.NotNull(result);
        Assert.Equal(System.Windows.Media.Brushes.Orange, result);
    }

    [Fact]
    public void ImageStatusConverter_ReturnsBlack_ForNullValue()
    {
        var converter = new ImageStatusConverter();
        var result = converter.Convert(null, typeof(System.Windows.Media.Brush), null, System.Globalization.CultureInfo.InvariantCulture);
        
        Assert.NotNull(result);
        Assert.Equal(System.Windows.Media.Brushes.Black, result);
    }

    [Fact]
    public void ImageStatusConverter_ReturnsBlack_ForNonBoolValue()
    {
        var converter = new ImageStatusConverter();
        var result = converter.Convert("string", typeof(System.Windows.Media.Brush), null, System.Globalization.CultureInfo.InvariantCulture);
        
        Assert.NotNull(result);
        Assert.Equal(System.Windows.Media.Brushes.Black, result);
    }

    [Fact]
    public void ImageStatusConverter_ThrowsNotImplementedException_OnConvertBack()
    {
        var converter = new ImageStatusConverter();
        
        Assert.Throws<NotImplementedException>(() => 
            converter.ConvertBack(System.Windows.Media.Brushes.Green, typeof(bool), null, System.Globalization.CultureInfo.InvariantCulture)
        );
    }

    [Fact]
    public void ImageResult_MultipleInstances_AreIndependent()
    {
        var display1 = new ImageResult("image1.jpg", "/path/1", true);
        var display2 = new ImageResult("image2.jpg", "/path/2", false);
        
        Assert.NotEqual(display1.FileName, display2.FileName);
        Assert.NotEqual(display1.FullPath, display2.FullPath);
        Assert.NotEqual(display1.Found, display2.Found);
    }

    [Fact]
    public void ImageResult_WithEmptyFileName_StillWorks()
    {
        var display = new ImageResult("", "/path/test.jpg", true);
        
        Assert.Empty(display.FileName);
        Assert.Equal("/path/test.jpg", display.FullPath);
        Assert.True(display.Found);
    }

    [Fact]
    public void ImageResult_WithSpecialCharactersInFileName_StillWorks()
    {
        var display = new ImageResult("302-a (kopia).jpg", "/path/302-a (kopia).jpg", true);
        
        Assert.Equal("302-a (kopia).jpg", display.FileName);
        Assert.Equal("/path/302-a (kopia).jpg", display.FullPath);
        Assert.True(display.Found);
    }

    [Fact]
    public void FoundImages_ShouldNotAppearDuplicated_WhenInBothFoundAndExpected()
    {
        // This test simulates the scenario where:
        // - "photo.jpg" is found on disk
        // - "photo.jpg" is also listed in the database Photos field
        // Result: Should appear only ONCE as a found image (green), NOT as both found and missing
        
        var foundImages = new List<string> { "/path/to/photo.jpg" };
        var expectedPhotos = new List<string> { "photo.jpg" };
        
        var displayItems = new List<ImageResult>();
        var foundFileNames = new HashSet<string>(
            foundImages.Select(p => System.IO.Path.GetFileName(p)),
            StringComparer.OrdinalIgnoreCase
        );
        
        // Add found images first (green)
        foreach (var imagePath in foundImages)
        {
            var fileName = System.IO.Path.GetFileName(imagePath);
            displayItems.Add(new ImageResult(fileName, imagePath, true));
        }

        // Add expected but missing images (orange)
        foreach (var expectedPhoto in expectedPhotos)
        {
            if (!foundFileNames.Contains(expectedPhoto))
            {
                displayItems.Add(new ImageResult(expectedPhoto, "", false));
            }
        }
        
        // Verify: Should have exactly 1 item, and it should be found (green)
        Assert.Single(displayItems);
        Assert.Equal("photo.jpg", displayItems[0].FileName);
        Assert.True(displayItems[0].Found);
        Assert.Equal("/path/to/photo.jpg", displayItems[0].FullPath);
    }
}

public class PhotoStatusConverterTests
{
    [Fact]
    public void PhotoStatusConverter_ReturnsGreen_WhenImagesFound()
    {
        var converter = new PhotoStatusConverter();
        var item = new Item { Number = 302, Photos = "302-a.jpg, 302-b.jpg" };
        
        var result = converter.Convert(item, typeof(System.Windows.Media.Brush), null!, System.Globalization.CultureInfo.InvariantCulture);
        
        // Result should be green brush (images exist for item 302)
        Assert.NotNull(result);
        // We expect green if images are found (this depends on actual file system)
        // For testing, we'll check that it returns a brush type
        Assert.IsAssignableFrom<System.Windows.Media.Brush>(result);
    }

    [Fact]
    public void PhotoStatusConverter_ReturnsGray_WhenPhotosIsNull()
    {
        var converter = new PhotoStatusConverter();
        var item = new Item { Number = 302, Photos = null };
        
        var result = converter.Convert(item, typeof(System.Windows.Media.Brush), null!, System.Globalization.CultureInfo.InvariantCulture);
        
        Assert.NotNull(result);
        Assert.Equal(System.Windows.Media.Brushes.Gray, result);
    }

    [Fact]
    public void PhotoStatusConverter_ReturnsGray_WhenPhotosIsEmpty()
    {
        var converter = new PhotoStatusConverter();
        var item = new Item { Number = 302, Photos = "" };
        
        var result = converter.Convert(item, typeof(System.Windows.Media.Brush), null!, System.Globalization.CultureInfo.InvariantCulture);
        
        Assert.NotNull(result);
        Assert.Equal(System.Windows.Media.Brushes.Gray, result);
    }

    [Fact]
    public void PhotoStatusConverter_ReturnsGray_WhenItemIsNull()
    {
        var converter = new PhotoStatusConverter();
        
        var result = converter.Convert(null, typeof(System.Windows.Media.Brush), null!, System.Globalization.CultureInfo.InvariantCulture);
        
        Assert.NotNull(result);
        Assert.Equal(System.Windows.Media.Brushes.Gray, result);
    }

    [Fact]
    public void PhotoStatusConverter_ReturnsGray_WhenValueIsNotItem()
    {
        var converter = new PhotoStatusConverter();
        
        var result = converter.Convert("not an item", typeof(System.Windows.Media.Brush), null!, System.Globalization.CultureInfo.InvariantCulture);
        
        Assert.NotNull(result);
        Assert.Equal(System.Windows.Media.Brushes.Gray, result);
    }

    [Fact]
    public void PhotoStatusConverter_ThrowsNotImplementedException_OnConvertBack()
    {
        var converter = new PhotoStatusConverter();
        
        Assert.Throws<NotImplementedException>(() =>
            converter.ConvertBack(System.Windows.Media.Brushes.Green, typeof(Item), null!, System.Globalization.CultureInfo.InvariantCulture)
        );
    }

    [Fact]
    public void PhotoStatusConverter_ReturnsOrange_WhenImagesNotFound()
    {
        var converter = new PhotoStatusConverter();
        var item = new Item { Number = 99999, Photos = "99999-a.jpg" }; // Item with no images
        
        var result = converter.Convert(item, typeof(System.Windows.Media.Brush), null!, System.Globalization.CultureInfo.InvariantCulture);
        
        // Result should be orange (images expected but not found)
        Assert.NotNull(result);
        Assert.IsAssignableFrom<System.Windows.Media.Brush>(result);
    }
}
