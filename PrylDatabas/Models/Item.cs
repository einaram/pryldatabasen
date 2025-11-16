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

    public override string ToString() => Name ?? "Unknown";
}
