
using DogTracker.Models;

namespace DogTracker.Web.Models;

public class EatingHabit
{
    public int Id { get; set; } // Primary Key
    public int DogId { get; set; } // Foreign Key to Dog
    public DateTime Timestamp { get; set; }
    public string FoodType { get; set; }
    public decimal? Amount { get; set; } // e.g., grams, cups (make nullable if sometimes unknown)
    public string? Unit { get; set; } // e.g., "grams", "cups" (make nullable)
    public string? Notes { get; set; }

    public Dog? Dog { get; set; } // Navigation property (optional)
}