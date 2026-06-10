using DataBridge.Core.Models;

namespace DataBridge.Domain.Entities;

public class Product : IEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string? Description { get; set; }
}
