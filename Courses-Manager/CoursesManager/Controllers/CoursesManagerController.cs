using CoursesManager.DTO;
using CoursesManager.Repository;
using Microsoft.AspNetCore.Mvc;

namespace CoursesManager.Controllers;

[ApiController]
[Route("[controller]")]
public class CoursesManagerController : ControllerBase
{
    private readonly IRepository _courseRepository;

    public CoursesManagerController(IRepository courseService)
    {
        _courseRepository = courseService;
    }

    // GET: api/courses
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Course>>> GetCourses()
    {
        var courses = await _courseRepository.GetAllCoursesAsync();
        return Ok(courses);
    }

    // GET: api/courses/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Course>> GetCourseById(string id)
    {
        var course = await _courseRepository.GetCourseByIdAsync(id);

        if (course == null)
        {
            return NotFound();
        }

        return Ok(course);
    }

    // POST: api/courses
    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] CourseDto courseDto)
    {
        if (courseDto == null)
        {
            return BadRequest("Курс не може бути порожнім.");
        }

        // Map CourseDto to Course
        var course = new Course
        {
            Title = courseDto.Title,
            Description = courseDto.Description,
            CourseCode = courseDto.CourseCode,
            Duration = courseDto.Duration,
            Language = courseDto.Language,
            Status = courseDto.Status,
            StartDate = courseDto.StartDate,
            EndDate = courseDto.EndDate,
            MaxStudents = courseDto.MaxStudents,
            DiscussionForum = courseDto.DiscussionForum,
            // Instructors, Students, Tests are omitted
        };

        await _courseRepository.CreateCourseAsync(course);

        return CreatedAtAction(nameof(GetCourseById), new { id = course.Id }, course);
    }

    // PUT: api/courses/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCourse(string id, [FromBody] CourseDto updatedCourseDTO)
    {
        if (string.IsNullOrWhiteSpace(id) || updatedCourseDTO == null)
        {
            return BadRequest("Invalid request.");
        }

        // Перевіряємо, чи обов'язкові поля заповнені
        if (string.IsNullOrWhiteSpace(updatedCourseDTO.Title) || string.IsNullOrWhiteSpace(updatedCourseDTO.CourseCode))
        {
            return BadRequest("Назва курсу та код курсу є обов'язковими.");
        }

        // Перевіряємо, чи існує курс з таким Id
        var existingCourse = await _courseRepository.GetCourseByIdAsync(id);
        if (existingCourse == null)
        {
            return NotFound("Курс не знайдено.");
        }

        var updatedCourse = new Course
        {
            Title = updatedCourseDTO.Title,
            Description = updatedCourseDTO.Description,
            CourseCode = updatedCourseDTO.CourseCode,
            Duration = updatedCourseDTO.Duration,
            Language = updatedCourseDTO.Language,
            Status = updatedCourseDTO.Status,
            StartDate = updatedCourseDTO.StartDate,
            EndDate = updatedCourseDTO.EndDate,
            MaxStudents = updatedCourseDTO.MaxStudents,
            DiscussionForum = updatedCourseDTO.DiscussionForum,
            // Instructors, Students, Tests are omitted
        };

        // Оновлюємо курс у базі даних
        await _courseRepository.UpdateCourseAsync(id, updatedCourse);

        return NoContent(); // Повертає статус 204 No Content
    }

    // PUT: api/courses/{id}/students
    [HttpPut("{id}/students")]
    public async Task<IActionResult> AddStudentsToCourse(string id, [FromBody] List<int> students)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid course ID.");
        }

        var course = await _courseRepository.GetCourseByIdAsync(id);
        if (course == null)
        {
            return NotFound("Курс не знайдено.");
        }

        students ??= [];

        // Add students to the existing course
        course.Students.AddRange(students);
        await _courseRepository.UpdateCourseAsync(id, course);

        return Ok(course);
    }

    // PUT: api/courses/{id}/instructors
    [HttpPut("{id}/instructors")]
    public async Task<IActionResult> AddInstructorsToCourse(string id, [FromBody] List<int> instructors)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid course ID.");
        }

        var course = await _courseRepository.GetCourseByIdAsync(id);
        if (course == null)
        {
            return NotFound("Курс не знайдено.");
        }

        course.Instructors ??= [];

        course.Instructors.AddRange(instructors);
        await _courseRepository.UpdateCourseAsync(id, course);

        return Ok(course);
    }

    // PUT: api/courses/{id}/tests
    [HttpPut("{id}/tests")]
    public async Task<IActionResult> AddTestsToCourse(string id, [FromBody] List<int> tests)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid course ID.");
        }

        var course = await _courseRepository.GetCourseByIdAsync(id);
        if (course == null)
        {
            return NotFound("Курс не знайдено.");
        }

        tests ??= [];

        course.Tests.AddRange(tests);
        await _courseRepository.UpdateCourseAsync(id, course);

        return Ok(course);
    }

    // DELETE: api/courses/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCourse(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid Id.");
        }

        // Перевіряємо, чи існує курс з таким Id
        var existingCourse = await _courseRepository.GetCourseByIdAsync(id);
        if (existingCourse == null)
        {
            return NotFound("Курс не знайдено.");
        }

        // Видаляємо курс з бази даних
        await _courseRepository.DeleteCourseAsync(id);

        return NoContent(); // Повертає статус 204 No Content
    }

}

