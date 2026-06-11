using Sbc.Domain.Common;

namespace Sbc.Domain.Entities;

public class Simulator : AuditableEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Location { get; set; }

    public ICollection<ProtectedSystem> Systems { get; set; } = new List<ProtectedSystem>();
}