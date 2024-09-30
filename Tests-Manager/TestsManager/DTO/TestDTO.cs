using System.ComponentModel.DataAnnotations;

namespace TestsManager.DTO;

public class TestDTO
{
    [Required]
    public string? ReletedCourseId { get; set; }

    public List<Question>? Questions { get; set; }
}