using System;
using System.Collections.Generic;

namespace PrylDatabas.Models;

public class Item
{
    public int? Number { get; set; }
    public string? Name { get; set; }
    public string? Photos { get; set; }
    public string? Category { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedYear { get; set; }
    public string? CreatedPlace { get; set; }
    public string? Stamp { get; set; }
    public string? Provenance { get; set; }
    public string? CurrentOwner { get; set; }
    public bool IsSelected { get; set; }

    /// <summary>
    /// Groups creation details (by, year, place) into a single formatted string
    /// </summary>
    public string CreatedInfo
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(CreatedBy)) parts.Add(CreatedBy);
            if (!string.IsNullOrEmpty(CreatedYear)) parts.Add(CreatedYear);
            if (!string.IsNullOrEmpty(CreatedPlace)) parts.Add(CreatedPlace);
            return string.Join(", ", parts);
        }
    }

    public override string ToString() => Name ?? "Unknown";
}
