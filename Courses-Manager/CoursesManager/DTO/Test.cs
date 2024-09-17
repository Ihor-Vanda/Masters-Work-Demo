using System.ComponentModel.DataAnnotations;

namespace CoursesManager.DTO;

public class Test
{
    [Key]
    public string? TestId { get; set; }

    [Required]
    public string? Title { get; set; }

    // Зв'язок з курсом
    public string? CourseId { get; set; }
    public Course? Course { get; set; }
}