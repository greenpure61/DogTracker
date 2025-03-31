using DogTracker.Models;

namespace DogTracker.Web.Models;

public class ToiletHabit
{
    public int Id { get; set; } // Primary Key
    public int DogId { get; set; } // Foreign Key to Dog
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } // e.g., "Pee", "Poop", "Both"
    public string? Location { get; set; } // e.g., "Indoors", "Outdoors - Garden", "Walk"
    public string? Notes { get; set; }

    public Dog? Dog { get; set; } // Navigation property (optional)
}