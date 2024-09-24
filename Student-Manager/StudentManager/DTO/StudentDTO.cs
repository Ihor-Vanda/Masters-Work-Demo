using System.ComponentModel.DataAnnotations;

namespace StudentManager.DTO;

public class StudentDTO
{
    [Required]
    public string? FirstName { get; set; }

    [Required]
    public string? LastName { get; set; }

    [Required]
    public string? BirthDate { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }
}