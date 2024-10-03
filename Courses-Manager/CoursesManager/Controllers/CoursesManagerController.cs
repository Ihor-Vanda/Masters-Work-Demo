using System.Runtime.Serialization;
using CoursesManager.Clients;
using CoursesManager.DTO;
using CoursesManager.Repository;
using Microsoft.AspNetCore.Mvc;

namespace CoursesManager.Controllers;

[ApiController]
[Route("/courses")]
public class CoursesManagerController : ControllerBase
{
    private readonly IRepository _courseRepository;

    private readonly InstructorManagerClient _instructorManagerClient;
    private readonly StudentManagerClient _studentManagerClient;
    private readonly TestManagerClient _testManagerClient;

    public CoursesManagerController(
        IRepository courseService,
        InstructorManagerClient instructorManagerClient,
        StudentManagerClient studentManagerClient,
        TestManagerClient testManagerClient)
    {
        _courseRepository = courseService;
        _instructorManagerClient = instructorManagerClient;
        _studentManagerClient = studentManagerClient;
        _testManagerClient = testManagerClient;
    }

    // GET: /courses
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Course>>> GetCourses()
    {
        var courses = await _courseRepository.GetAllCoursesAsync();

        Console.WriteLine($"Procecced request to get all courses from{HttpContext.Connection.RemoteIpAddress}");
        return Ok(courses);
    }

    // GET: courses/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Course>> GetCourseById(string id)
    {
        var course = await _courseRepository.GetCourseByIdAsync(id);

        if (course == null)
        {
            return NotFound();
        }

        Console.WriteLine($"Procecced request to get course {id} from{HttpContext.Connection.RemoteIpAddress}");
        return Ok(course);
    }

    // POST: courses
    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] CourseDto courseDto)
    {
        if (courseDto == null)
        {
            return BadRequest("Курс не може бути порожнім.");
        }

        if (DateTime.TryParseExact(courseDto.StartDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var startDate)
        && DateTime.TryParseExact(courseDto.EndDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var endDate))
        {


            // Map CourseDto to Course
            var course = new Course
            {
                Title = courseDto.Title,
                Description = courseDto.Description,
                CourseCode = courseDto.CourseCode,
                Language = courseDto.Language,
                Status = courseDto.Status,
                StartDate = startDate,
                EndDate = endDate,
                MaxStudents = courseDto.MaxStudents,
                // Instructors, Students, Tests are omitted
            };

            await _courseRepository.CreateCourseAsync(course);

            Console.WriteLine($"Procecced request to add course from{HttpContext.Connection.RemoteIpAddress}");
            return CreatedAtAction(nameof(GetCourseById), new { id = course.Id }, course);
        }

        return BadRequest("Date format must be yyyy-MM-dd");
    }

    // PUT: courses/{id}
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

        if (DateTime.TryParseExact(updatedCourseDTO.StartDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var startDate)
       && DateTime.TryParseExact(updatedCourseDTO.EndDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var endDate))
        {

            var updatedCourse = new Course
            {
                Title = updatedCourseDTO.Title,
                Description = updatedCourseDTO.Description,
                CourseCode = updatedCourseDTO.CourseCode,
                Language = updatedCourseDTO.Language,
                Status = updatedCourseDTO.Status,
                StartDate = startDate,
                EndDate = endDate,
                MaxStudents = updatedCourseDTO.MaxStudents,
            };

            // Оновлюємо курс у базі даних
            await _courseRepository.UpdateCourseAsync(id, updatedCourse);

            Console.WriteLine($"Procecced request to update course {id} from{HttpContext.Connection.RemoteIpAddress}");
            return NoContent(); // Повертає статус 204 No Content
        }

        return BadRequest("Date format must be yyyy-MM-dd");
    }

    // PUT: courses/students/{id}
    [HttpPut("/students/{id}")]
    public async Task<IActionResult> AddStudentsToCourse(string id, [FromBody] List<string> students)
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

        if (!_studentManagerClient.AddStudentToCourse(id, students).IsCompletedSuccessfully)
        {
            course.Students.AddRange(students);
            await _courseRepository.UpdateCourseAsync(id, course);

            return Ok(course);
        }

        return BadRequest("Invalid students id");
    }

    [HttpDelete("/students/{id}")]
    public async Task<ActionResult> DeleteStudentFromCourses(string id, [FromBody] List<string> courses)
    {

        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid id");
        }

        var coursesList = new List<Course>();
        foreach (var course in courses)
        {
            var c = await _courseRepository.GetCourseByIdAsync(course);
            if (c != null)
            {
                coursesList.Add(c);
            }
        }

        for (int i = 0; i < coursesList.Count; i++)
        {
            coursesList[i].Students.Remove(id);
            await _courseRepository.UpdateCourseAsync(coursesList[i].Id, coursesList[i]);
        }

        return Ok(id);
    }

    // PUT: courses/instructors/{id}
    [HttpPut("/instructors/{id}")]
    public async Task<ActionResult> AddInstructorsToCourse(string id, [FromBody] List<string> instructors)
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

        if (!_instructorManagerClient.ModifyInstructorToCourse(id, instructors).IsCompletedSuccessfully)
        {
            course.Instructors.AddRange(instructors);
            await _courseRepository.UpdateCourseAsync(id, course);

            return Ok(course);
        }

        return BadRequest("Invalid instructors id");
    }

    [HttpDelete("/instructors/{id}")]
    public async Task<ActionResult> DeleteInstructorFromCourses(string id, [FromBody] List<string> courses)
    {

        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid id");
        }

        var coursesList = new List<Course>();
        foreach (var course in courses)
        {
            var c = await _courseRepository.GetCourseByIdAsync(course);
            if (c != null)
            {
                coursesList.Add(c);
            }
        }

        for (int i = 0; i < coursesList.Count; i++)
        {
            coursesList[i].Instructors.Remove(id);
            await _courseRepository.UpdateCourseAsync(coursesList[i].Id, coursesList[i]);
        }

        return Ok(id);
    }

    // PUT: courses/tests/{id}
    [HttpPut("/tests/{id}")]
    public async Task<IActionResult> AddTestsToCourse(string id, [FromBody] List<string> tests)
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

    [HttpDelete("/tests/{id}")]
    public async Task<ActionResult> DeleteTestFromCourses(string id, [FromBody] string course)
    {

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(course))
        {
            return BadRequest("Invalid id");
        }

        var c = await _courseRepository.GetCourseByIdAsync(course);
        if (c == null)
        {
            return BadRequest("Course id is invalid");
        }

        c.Tests.Remove(id);
        await _courseRepository.UpdateCourseAsync(course, c);

        return Ok(id);
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

        await _studentManagerClient.DeleteStudentFromCourse(id);
        await _instructorManagerClient.DeleteInstructorFromCourse(id);
        await _testManagerClient.DeleteTestFromCourse(id);

        // Видаляємо курс з бази даних
        await _courseRepository.DeleteCourseAsync(id);

        Console.WriteLine($"Procecced request to delete course {id} from {HttpContext.Connection.RemoteIpAddress}");
        return NoContent(); // Повертає статус 204 No Content
    }

}

