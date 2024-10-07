using System.Net;
using CoursesManager.Clients;
using CoursesManager.DTO;
using CoursesManager.Repository;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Polly.CircuitBreaker;

namespace CoursesManager.Controllers;

[ApiController]
[Route("/courses")]
public class CoursesManagerController : ControllerBase
{
    private readonly IRepository _courseRepository;

    private readonly InstructorManagerClient _instructorManagerClient;
    private readonly StudentManagerClient _studentManagerClient;
    // private readonly TestManagerClient _testManagerClient;

    public CoursesManagerController(
        IRepository courseService,
        InstructorManagerClient instructorManagerClient,
        StudentManagerClient studentManagerClient)
    {
        _courseRepository = courseService;
        _instructorManagerClient = instructorManagerClient;
        _studentManagerClient = studentManagerClient;
        // _testManagerClient = testManagerClient;
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
        if (!ObjectId.TryParse(id, out var idValue))
        {
            return BadRequest("Invalid id");
        }

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
            return BadRequest("Reqired field are empty");
        }

        // Перевіряємо, чи існує курс з таким Id
        var course = await _courseRepository.GetCourseByIdAsync(id);
        if (course == null)
        {
            return NotFound("The course not found");
        }

        if (DateTime.TryParseExact(updatedCourseDTO.StartDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var startDate)
       && DateTime.TryParseExact(updatedCourseDTO.EndDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var endDate))
        {
            course.Title = updatedCourseDTO.Title;
            course.Description = updatedCourseDTO.Description;
            course.CourseCode = updatedCourseDTO.CourseCode;
            course.Language = updatedCourseDTO.Language;
            course.Status = updatedCourseDTO.Status;
            course.StartDate = startDate;
            course.EndDate = endDate;
            course.MaxStudents = updatedCourseDTO.MaxStudents;

            await _courseRepository.UpdateCourseAsync(id, course);

            Console.WriteLine($"Procecced request to update course {id} from {HttpContext.Connection.RemoteIpAddress}");
            return NoContent();
        }

        return BadRequest("Date format must be yyyy-MM-dd");
    }

    // PUT: courses/students/{id}/add
    [HttpPut("/students/{id}/add")]
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

        students = students
            .Distinct()
            .Where(student => !course.Students.Contains(student))
            .ToList();

        if (students.Count == 0)
        {
            return BadRequest("The course already have the students");
        }

        try
        {
            var service_response = await _studentManagerClient.AddStudentToCourse(id, students);

            if (service_response.StatusCode == HttpStatusCode.OK)
            {
                var studentsList = await service_response.Content.ReadFromJsonAsync<List<string>>();

                if (studentsList == null)
                {
                    return Ok("The students already have the course");

                }

                course.Students.AddRange(studentsList);
                await _courseRepository.UpdateCourseAsync(id, course);

                Console.WriteLine($"Procecced request to add students to course {id} from {HttpContext.Connection.RemoteIpAddress}");

                return Ok(course);
            }
        }
        catch (BrokenCircuitException)
        {
            // Circuit breaker is open, return 503 Service Unavailable
            return StatusCode(503, "Students service is temporarily unavailable due to a circuit breaker.");
        }

        return BadRequest("Invalid students id");
    }

    [HttpPut("/students/{id}/delete")]
    public async Task<ActionResult> DeleteStudentFromCourses(string id, [FromBody] List<string> students)
    {

        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid id");
        }

        var course = await _courseRepository.GetCourseByIdAsync(id);
        if (course == null)
        {
            return NotFound("The course not found");
        }

        students = students
           .Distinct()
           .Where(course.Students.Contains)
           .ToList();

        if (students.Count == 0)
        {
            return BadRequest("The course don't have the students");
        }

        try
        {
            var service_response = await _studentManagerClient.DeleteStudentFromCourse(id, students);
            if (service_response.StatusCode == HttpStatusCode.OK)
            {
                var studentsList = await service_response.Content.ReadFromJsonAsync<List<string>>();

                if (studentsList == null)
                {
                    return Ok("The students don't have the course");

                }

                course.Students.RemoveAll(studentsList.Contains);
                await _courseRepository.UpdateCourseAsync(id, course);

                Console.WriteLine($"Procecced request to delete students from course {id} from {HttpContext.Connection.RemoteIpAddress}");

                return Ok(course);
            }
        }
        catch (BrokenCircuitException)
        {
            // Circuit breaker is open, return 503 Service Unavailable
            return StatusCode(503, "Students service is temporarily unavailable due to a circuit breaker.");
        }

        return BadRequest("Invalid students id");

        // var coursesList = new List<Course>();
        // foreach (var course in courses)
        // {
        //     var c = await _courseRepository.GetCourseByIdAsync(course);
        //     if (c != null)
        //     {
        //         coursesList.Add(c);
        //     }
        // }

        // for (int i = 0; i < coursesList.Count; i++)
        // {
        //     coursesList[i].Students.Remove(id);
        //     await _courseRepository.UpdateCourseAsync(coursesList[i].Id, coursesList[i]);
        // }

        // return Ok(id);
    }

    [HttpPut("/students/{id}")]
    public async Task<ActionResult> DeleteStudentFromAllCourses(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid id");
        }

        var courses = await _courseRepository.GetAllCoursesAsync();
        if (courses == null)
        {
            return NotFound("The course not found");
        }

        var coursesList = courses.FindAll(c => c.Students.Contains(id));

        if (coursesList == null)
        {
            return Ok(id);
        }

        for (int i = 0; i < coursesList.Count; i++)
        {
            coursesList[i].Students.Remove(id);
            await _courseRepository.UpdateCourseAsync(coursesList[i].Id, coursesList[i]);
        }

        return Ok(id);
    }

    // PUT: courses/instructors/{id}/add
    [HttpPut("/instructors/{id}/add")]
    public async Task<ActionResult> AddInstructorsToCourse(string id, [FromBody] List<string> instructors)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid course ID.");
        }

        var course = await _courseRepository.GetCourseByIdAsync(id);
        if (course == null)
        {
            return NotFound("The course not found");
        }

        instructors = instructors
            .Distinct()
            .Where(instructor => !course.Instructors.Contains(instructor))
            .ToList();

        if (instructors.Count == 0)
        {
            return BadRequest("The course already have the instructors");
        }

        try
        {
            var service_response = await _instructorManagerClient.AddInstructorToCourse(id, instructors);
            if (service_response.StatusCode == HttpStatusCode.OK)
            {
                var instructorsList = await service_response.Content.ReadFromJsonAsync<List<string>>();

                if (instructorsList == null)
                {
                    return Ok("Instructors already have the course");

                }

                course.Instructors.AddRange(instructorsList);
                await _courseRepository.UpdateCourseAsync(id, course);

                Console.WriteLine($"Procecced request to add instructors to course {id} from {HttpContext.Connection.RemoteIpAddress}");
                return Ok(course);
            }
        }
        catch (BrokenCircuitException)
        {
            // Circuit breaker is open, return 503 Service Unavailable
            return StatusCode(503, "Instructors service is temporarily unavailable due to a circuit breaker.");
        }

        return BadRequest("Invalid instructors id");
    }

    [HttpPut("/instructors/{id}/delete")]
    public async Task<ActionResult> DeleteInstructorsFromCourse(string id, [FromBody] List<string> instructors)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid course ID.");
        }

        var course = await _courseRepository.GetCourseByIdAsync(id);
        if (course == null)
        {
            return NotFound("The course not found");
        }

        instructors = instructors
           .Distinct()
           .Where(course.Instructors.Contains)
           .ToList();

        if (instructors.Count == 0)
        {
            return BadRequest("The course don't have the instructors");
        }

        try
        {
            var service_response = await _instructorManagerClient.DeleteInstructorFromCourse(id, instructors);
            if (service_response.StatusCode == HttpStatusCode.OK)
            {
                var instructorsList = await service_response.Content.ReadFromJsonAsync<List<string>>();

                if (instructorsList == null)
                {
                    return Ok("The Instructors don't have the course");

                }

                course.Instructors.RemoveAll(instructorsList.Contains);
                await _courseRepository.UpdateCourseAsync(id, course);

                Console.WriteLine($"Procecced request to delete instructors from course {id} from {HttpContext.Connection.RemoteIpAddress}");
                return Ok(course);
            }
        }
        catch (BrokenCircuitException)
        {
            // Circuit breaker is open, return 503 Service Unavailable
            return StatusCode(503, "Instructors service is temporarily unavailable due to a circuit breaker.");
        }

        return BadRequest("Invalid instructors id");

        // var coursesList = new List<Course>();
        // foreach (var course in courses)
        // {
        //     var c = await _courseRepository.GetCourseByIdAsync(course);
        //     if (c != null)
        //     {
        //         coursesList.Add(c);
        //     }
        // }

        // for (int i = 0; i < coursesList.Count; i++)
        // {
        //     coursesList[i].Instructors.Remove(id);
        //     await _courseRepository.UpdateCourseAsync(coursesList[i].Id, coursesList[i]);
        // }

        // return Ok(id);
    }

    [HttpPut("/instructors/{id}")]
    public async Task<ActionResult> DeleteInstructorFromAllCourses(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid id");
        }

        var courses = await _courseRepository.GetAllCoursesAsync();
        if (courses == null)
        {
            return NotFound("The course not found");
        }

        var coursesList = courses.FindAll(c => c.Instructors.Contains(id));

        if (coursesList == null)
        {
            return Ok(id);
        }

        for (int i = 0; i < coursesList.Count; i++)
        {
            coursesList[i].Instructors.Remove(id);
            await _courseRepository.UpdateCourseAsync(coursesList[i].Id, coursesList[i]);
        }

        return Ok(id);
    }

    // PUT: courses/tests/{id}
    // [HttpPut("/tests/{id}/add")]
    // public async Task<IActionResult> AddTestsToCourse(string id, [FromBody] List<string> tests)
    // {
    //     if (string.IsNullOrWhiteSpace(id))
    //     {
    //         return BadRequest("Invalid course ID.");
    //     }

    //     var course = await _courseRepository.GetCourseByIdAsync(id);
    //     if (course == null)
    //     {
    //         return NotFound("Курс не знайдено.");
    //     }

    //     tests ??= [];

    //     course.Tests.AddRange(tests);
    //     await _courseRepository.UpdateCourseAsync(id, course);

    //     return Ok(course);
    // }

    // [HttpPut("/tests/{id}/delete")]
    // public async Task<ActionResult> DeleteTestFromCourses(string id, [FromBody] string course)
    // {

    //     if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(course))
    //     {
    //         return BadRequest("Invalid id");
    //     }

    //     var c = await _courseRepository.GetCourseByIdAsync(course);
    //     if (c == null)
    //     {
    //         return BadRequest("Course id is invalid");
    //     }

    //     c.Tests.Remove(id);
    //     await _courseRepository.UpdateCourseAsync(course, c);

    //     return Ok(id);
    // }


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

        if (existingCourse.Students.Count > 0)
        {
            try
            {
                var s_response = await _studentManagerClient.DeleteStudentFromCourse(id, existingCourse.Students);

                if (s_response.StatusCode != HttpStatusCode.OK)
                {
                    return BadRequest("Can't delete students from the course");
                }
            }
            catch (BrokenCircuitException)
            {
                // Circuit breaker is open, return 503 Service Unavailable
                return StatusCode(503, "Student service is temporarily unavailable due to a circuit breaker.");
            }
        }

        if (existingCourse.Instructors.Count > 0)
        {
            try
            {
                var i_response = await _instructorManagerClient.DeleteInstructorFromCourse(id, existingCourse.Instructors);

                if (i_response.StatusCode != HttpStatusCode.OK)
                {
                    return BadRequest("Can't delete instructors from the course");
                }
            }
            catch (BrokenCircuitException)
            {
                // Circuit breaker is open, return 503 Service Unavailable
                return StatusCode(503, "Instructors service is temporarily unavailable due to a circuit breaker.");
            }
        }

        // if (existingCourse.Tests.Count > 0)
        // {
        //     await _testManagerClient.DeleteTestFromCourse(id);
        // }

        await _courseRepository.DeleteCourseAsync(id);

        Console.WriteLine($"Procecced request to delete course {id} from {HttpContext.Connection.RemoteIpAddress}");

        return NoContent();
    }

}