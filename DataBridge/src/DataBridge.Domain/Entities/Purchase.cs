using DataBridge.Core.Models;

namespace DataBridge.Domain.Entities;

public class Purchase : IEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string Status { get; set; } = "completed";
}
