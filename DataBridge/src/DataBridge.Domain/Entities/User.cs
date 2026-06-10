using DataBridge.Core.Models;

namespace DataBridge.Domain.Entities;

public class User : IEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Country { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTime SignupDate { get; set; }

    /// <summary>FK to Address (stored in a separate CSV data source).</summary>
    public Guid? AddressPrincipalId { get; set; }

    /// <summary>Navigation property resolved cross-source by the Unit of Work.</summary>
    public Address? AddressPrincipal { get; set; }
}
