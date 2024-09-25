using System.ComponentModel.DataAnnotations;

namespace InstructorsManager.DTO;

public class InstructorDTO
{
    [Required]
    public string? FirstName { get; set; }

    [Required]
    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    [Required]
    public string? BirthDate { get; set; }
}