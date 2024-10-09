using System.Net;
using CoursesManager.Clients;
using CoursesManager.DTO;
using CoursesManager.RabbitMQ;
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

    private readonly RabbitMQClient _rabbitMQClient;

    public CoursesManagerController(
        IRepository courseService,
        InstructorManagerClient instructorManagerClient,
        StudentManagerClient studentManagerClient,
        RabbitMQClient rabbitMQClient)
    {
        _courseRepository = courseService;
        _instructorManagerClient = instructorManagerClient;
        _studentManagerClient = studentManagerClient;
        _rabbitMQClient = rabbitMQClient;
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
            return NotFound("Course not found");
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
            return BadRequest("Course can't be null");
        }

        if (!DateTime.TryParseExact(courseDto.StartDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var startDate))
        {
            return BadRequest("Date format is incorrect. Expected format: yyyy-MM-dd");
        }

        if (!DateTime.TryParseExact(courseDto.EndDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var endDate))
        {
            return BadRequest("Date format is incorrect. Expected format: yyyy-MM-dd");
        }

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
        };

        await _courseRepository.CreateCourseAsync(course);

        Console.WriteLine($"Procecced request to add course from{HttpContext.Connection.RemoteIpAddress}");
        return CreatedAtAction(nameof(GetCourseById), new { id = course.Id }, course);
    }

    // PUT: courses/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCourse(string id, [FromBody] CourseDto updatedCourseDTO)
    {
        if (string.IsNullOrWhiteSpace(id) || updatedCourseDTO == null)
        {
            return BadRequest("Invalid request.");
        }

        if (string.IsNullOrWhiteSpace(updatedCourseDTO.Title) || string.IsNullOrWhiteSpace(updatedCourseDTO.CourseCode))
        {
            return BadRequest("Reqired field are empty");
        }

        var course = await _courseRepository.GetCourseByIdAsync(id);
        if (course == null)
        {
            return NotFound("The course not found");
        }

        if (!DateTime.TryParseExact(updatedCourseDTO.StartDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var startDate))
        {
            return BadRequest("Date format is incorrect. Expected format: yyyy-MM-dd");
        }

        if (!DateTime.TryParseExact(updatedCourseDTO.EndDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var endDate))
        {
            return BadRequest("Date format is incorrect. Expected format: yyyy-MM-dd");
        }

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
            return NotFound("The course not found");
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
            _rabbitMQClient.PublishMessage("student-course-add", id + "," + String.Join(",", students));
            course.Students.AddRange(students);
            await _courseRepository.UpdateCourseAsync(id, course);
            return StatusCode(503, "Sending request add students to course to queue");
        }

        return Ok(course);
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
            _rabbitMQClient.PublishMessage("student-course-delete", id + "," + String.Join(",", students));
            course.Students.RemoveAll(students.Contains);
            await _courseRepository.UpdateCourseAsync(id, course);
            return StatusCode(503, "Sending request add students to course to queue");
        }

        return Ok(course);
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

        var updateTasks = coursesList
        .Select(async x =>
        {
            x.Students.Remove(id);
            await _courseRepository.UpdateCourseAsync(x.Id, x);
        });

        await Task.WhenAll(updateTasks);

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
            _rabbitMQClient.PublishMessage("instructor-course-add", id + "," + String.Join(",", instructors));
            course.Students.AddRange(instructors);
            await _courseRepository.UpdateCourseAsync(id, course);
            return StatusCode(503, "Sending request add instructors to course to queue");
        }

        return Ok(course);
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
            _rabbitMQClient.PublishMessage("instructor-course-delete", id + "," + String.Join(",", instructors));
            course.Students.RemoveAll(instructors.Contains);
            await _courseRepository.UpdateCourseAsync(id, course);
            return StatusCode(503, "Sending request delete instructors from course to queue");
        }

        return Ok(course);
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

        var updateTasks = coursesList.Select(async x =>
        {
            x.Instructors.Remove(id);
            await _courseRepository.UpdateCourseAsync(x.Id, x);
        });

        await Task.WhenAll(updateTasks);

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

        var existingCourse = await _courseRepository.GetCourseByIdAsync(id);
        if (existingCourse == null)
        {
            return NotFound("Course not found");
        }

        var studentRequestSuccess = false;
        var instructorRequestSuccess = false;

        // Перевірка, чи є студенти на курсі
        if (existingCourse.Students.Count > 0)
        {
            try
            {
                // Використання HTTP-клієнта для видалення студентів з курсу
                var s_response = await _studentManagerClient.DeleteStudentFromCourse(id, existingCourse.Students);

                if (s_response.StatusCode == HttpStatusCode.OK)
                {
                    studentRequestSuccess = true;
                }
                else
                {
                    return BadRequest($"Failed to delete students from course. Status: {s_response.StatusCode}");
                }
            }
            catch (BrokenCircuitException)
            {
                // Якщо спрацьовує Circuit Breaker, публікуємо повідомлення до RabbitMQ
                _rabbitMQClient.PublishMessage("student-course-delete", id + "," + String.Join(",", existingCourse.Students));
                Console.WriteLine("Send request to delete students from course to queue");
            }
        }

        // Перевірка, чи є інструктори на курсі
        if (existingCourse.Instructors.Count > 0)
        {
            try
            {
                // Використання HTTP-клієнта для видалення інструкторів з курсу
                var i_response = await _instructorManagerClient.DeleteInstructorFromCourse(id, existingCourse.Instructors);

                if (i_response.StatusCode == HttpStatusCode.OK)
                {
                    instructorRequestSuccess = true;
                }
                else
                {
                    return BadRequest($"Failed to delete instructors from course. Status: {i_response.StatusCode}");
                }
            }
            catch (BrokenCircuitException)
            {
                // Якщо спрацьовує Circuit Breaker, публікуємо повідомлення до RabbitMQ
                _rabbitMQClient.PublishMessage("instructor-course-delete", id + "," + String.Join(",", existingCourse.Instructors));
                Console.WriteLine("Send request to delete instructors from course to queue");
            }
        }

        // Перевіряємо, чи хоча б одне з HTTP-з'єднань було успішним.
        // Якщо обидва з'єднання не вдалися, курс не буде видалено
        if (!studentRequestSuccess && !instructorRequestSuccess)
        {
            return StatusCode(500, "Both student and instructor services are unavailable.");
        }

        // Якщо студенти і інструктори були видалені, видаляємо курс
        await _courseRepository.DeleteCourseAsync(id);
        Console.WriteLine($"Processed request to delete course {id} from {HttpContext.Connection.RemoteIpAddress}");

        return NoContent();
    }
}