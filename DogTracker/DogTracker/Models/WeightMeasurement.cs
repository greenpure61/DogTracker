using DogTracker.Models;

namespace DogTracker.Web.Models;

public class WeightMeasurement
{
    public int Id { get; set; } // Primary Key
    public int DogId { get; set; } // Foreign Key to Dog
    public DateTime Timestamp { get; set; }
    public decimal Weight { get; set; }
    public string Unit { get; set; } // e.g., "kg", "lbs"

    public Dog? Dog { get; set; } // Navigation property (optional)
}