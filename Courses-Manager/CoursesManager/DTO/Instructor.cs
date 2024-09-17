using System.ComponentModel.DataAnnotations;

namespace CoursesManager.DTO;
public class Instructor
{
    [Key]
    public string? InstructorId { get; set; }

    [Required]
    public string? Name { get; set; }

    // Зв'язок з курсами
    public List<Course> Courses { get; set; } = new List<Course>();
}