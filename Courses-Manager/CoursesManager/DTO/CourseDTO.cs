
using System.ComponentModel.DataAnnotations;

namespace CoursesManager.DTO;
public class CourseDto
{
    [Required]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [Required]
    public string? CourseCode { get; set; }

    public string? Duration { get; set; }

    public string? Language { get; set; }

    public string? Status { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int MaxStudents { get; set; }

    public string? DiscussionForum { get; set; }
}
