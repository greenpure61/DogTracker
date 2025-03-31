using System.ComponentModel.DataAnnotations; // Add this namespace

namespace DogTracker.Web.Models; // Or DogTracker.Models

public class Dog
{
    public int Id { get; set; } // Primary Key

    [Required(ErrorMessage = "Dog name is required.")] // Make name required
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")] // Set length limits
    public string Name { get; set; } = string.Empty; // Initialize to satisfy nullable warning if not using 'required' keyword

    [StringLength(100, ErrorMessage = "Breed cannot exceed 100 characters.")]
    public string? Breed { get; set; } // Nullable string

    [Display(Name = "Date of Birth")] // Set display name for labels
    [DataType(DataType.Date)] // Helps with date input rendering
    public DateTime? DateOfBirth { get; set; } // Nullable DateTime
}