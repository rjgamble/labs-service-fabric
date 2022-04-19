using System.ComponentModel.DataAnnotations;

namespace Customers.DataAccess;

public enum Level
{
    Bronze,
    Silver,
    Gold,
    Platinum
}

public record Customer(string Id, string Name, DateTimeOffset CreatedAt, Level Level = Level.Bronze);

public class CreateCustomer
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; }

    public Level Level { get; set; } = Level.Bronze;
}

public class UpdateCustomer
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; }

    public Level Level { get; set; } = Level.Bronze;
}

public class PatchCustomer
{
    public string? Name { get; set; }
    public Level? Level { get; set; }
}