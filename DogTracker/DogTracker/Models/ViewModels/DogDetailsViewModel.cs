using DogTracker.Models;
using DogTracker.Web.Models; // Access the main models

namespace DogTracker.Web.Models.ViewModels; // Adjust namespace if needed

public class DogDetailsViewModel
{
    public Dog Dog { get; set; } = null!; // The specific dog's details
    public List<EatingHabit> EatingHabits { get; set; } = new();
    public List<ToiletHabit> ToiletHabits { get; set; } = new();
    public List<WeightMeasurement> WeightMeasurements { get; set; } = new();
}