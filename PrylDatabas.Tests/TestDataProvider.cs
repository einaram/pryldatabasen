using System;
using System.Collections.ObjectModel;
using PrylDatabas.Models;

namespace PrylDatabas.Tests;

/// <summary>
/// Provides dummy test data for unit tests that don't have access to the Excel file in CI/CD environments.
/// </summary>
public static class TestDataProvider
{
    public static ObservableCollection<Item> GetDummyItems()
    {
        var items = new ObservableCollection<Item>
        {
            new Item
            {
                Number = 302,
                Name = "Tullas ring",
                Category = "Smycken",
                CurrentOwner = "Anna",
                CreatedYear = "2020",
                Photos = "302"
            },
            new Item
            {
                Number = 393,
                Name = "Dopskål",
                Category = "Doppöppersaker",
                CurrentOwner = "Maria",
                CreatedYear = "2018",
                Photos = "393-a,393-b"
            },
            new Item
            {
                Number = 399,
                Name = "Ymnighetshorn salt & peppar",
                Category = "Kök",
                CurrentOwner = "Erik",
                CreatedYear = "2019",
                Photos = null
            },
            new Item
            {
                Number = 401,
                Name = "Antik spegel",
                Category = "Möbler",
                CurrentOwner = "Johanna",
                CreatedYear = "2021",
                Photos = "401"
            },
            new Item
            {
                Number = 402,
                Name = "Kopparkruka",
                Category = "Kök",
                CurrentOwner = "Peter",
                CreatedYear = "2017",
                Photos = "402-a,402-b,402-c"
            }
        };

        return items;
    }
}
