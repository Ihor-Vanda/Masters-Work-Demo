using System.Net;
using System.Text.Json;
using CoursesManager.DTO;
using CoursesManager.RabbitMQ;
using CoursesManager.Repository;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using ModifiedCB;
using ModifiedCB.Settings;

namespace CoursesManager.Controllers;

[ApiController]
[Route("/courses")]
public class CoursesManagerController : ControllerBase
{
    private readonly IRepository _courseRepository;

    private readonly ICommunicationStrategy _communicationStrategy;

    public CoursesManagerController(
        IRepository courseService,
        ICommunicationStrategy communicationStrategy)
    {
        _courseRepository = courseService;
        _communicationStrategy = communicationStrategy;
    }

    // GET: /courses
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Course>>> GetCourses()
    {
        ServiceMetrics.IncGetCoursesRequests();
        var startTime = DateTime.Now;
        double endTime;
        var courses = await _courseRepository.GetAllCoursesAsync();

        endTime = (DateTime.Now - startTime).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(endTime, "GET");
        // Console.WriteLine($"Procecced request to get all courses from{HttpContext.Connection.RemoteIpAddress}");
        return Ok(courses);
    }

    // GET: courses/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Course>> GetCourseById(string id)
    {
        ServiceMetrics.IncGetCourseByIdRequests();

        if (!ObjectId.TryParse(id, out var _)) return BadRequest("Invalid id");

        var startTime = DateTime.Now;
        double endTime;

        var course = await _courseRepository.GetCourseByIdAsync(id);

        if (course == null)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "GET/id");
            return NotFound("Course not found");
        }

        endTime = (DateTime.Now - startTime).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(endTime, "GET/id");

        return Ok(course);
    }

    // POST: courses
    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] CourseDto courseDto)
    {
        ServiceMetrics.IncCreateCourseRequests();

        if (courseDto == null)
        {
            return BadRequest("Invalid request body");
        }

        if (!DateTime.TryParseExact(courseDto.StartDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var startDate) ||
            !DateTime.TryParseExact(courseDto.EndDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var endDate))
        {
            return BadRequest("Invalid data format. Expect yyyy-mm-dd format");
        }

        var startTime = DateTime.Now;
        double endTime;

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

        endTime = (DateTime.Now - startTime).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(endTime, "POST");

        return CreatedAtAction(nameof(GetCourseById), new { id = course.Id }, course);
    }

    // PUT: courses/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCourse(string id, [FromBody] CourseDto updatedCourseDTO)
    {
        ServiceMetrics.IncUpdateCourseRequests();

        if (string.IsNullOrWhiteSpace(id) || updatedCourseDTO == null || !ObjectId.TryParse(id, out var _)) return BadRequest("Invalid request.");

        if (string.IsNullOrWhiteSpace(updatedCourseDTO.Title) || string.IsNullOrWhiteSpace(updatedCourseDTO.CourseCode))
        {
            return BadRequest("Reqired field are empty");
        }

        if (!DateTime.TryParseExact(updatedCourseDTO.StartDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var startDate) ||
            !DateTime.TryParseExact(updatedCourseDTO.EndDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var endDate))
        {
            return BadRequest("Date format is incorrect. Expected format: yyyy-MM-dd");
        }

        var startTime = DateTime.Now;
        double endTime;

        var course = await _courseRepository.GetCourseByIdAsync(id);
        if (course == null)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "PUT/id");
            return NotFound("The course not found");
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
        endTime = (DateTime.Now - startTime).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(endTime, "PUT/id");

        return NoContent();
    }

    // PUT: courses/students/{id}/add
    [HttpPut("/students/{id}/add")]
    public async Task<IActionResult> AddStudentsToCourse(string id, [FromBody] List<string> students)
    {
        ServiceMetrics.IncAddStudentToCourseRequests();
        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out var _)) return BadRequest("Invalid course ID.");

        var startTime = DateTime.Now;
        double endTime;

        var course = await _courseRepository.GetCourseByIdAsync(id);
        if (course == null)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "PUT/students/id/add");
            return NotFound("The course not found");
        }

        students = students
            .Distinct()
            .Where(student => !course.Students.Contains(student) && ObjectId.TryParse(student, out var _))
            .ToList();

        if (students.Count == 0)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "PUT/students/id/add");
            return BadRequest("The course already have the students");
        }

        var settings = new CommunicationSettings
        {
            HttpSettings = new HttpCommunicationSettings
            {
                Method = HttpMethod.Put,
                DestinationURL = $"http://students_manager_service:8080/students/courses/{id}/add",
                Message = JsonSerializer.Serialize(students)
            },
            RabbitMqSettings = new RabbitMqCommunicationSettings
            {
                QueueName = "student-course",
                Message = $"add;{course.Id};{string.Join(",", students)}"
            }
        };

        try
        {
            await _communicationStrategy.SendMessage(settings);
        }
        catch (Exception ex)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "PUT/students/id/add");
            return StatusCode((int)HttpStatusCode.InternalServerError, $"Error occurred: {ex.Message}");
        }

        course.Students.AddRange(students);
        await _courseRepository.UpdateCourseAsync(id, course);
        endTime = (DateTime.Now - startTime).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(endTime, "PUT/students/id/add");

        return Ok(course);
    }


    [HttpPut("/students/{id}/delete")]
    public async Task<ActionResult> DeleteStudentFromCourse(string id, [FromBody] List<string> students)
    {
        ServiceMetrics.IncDeleteStudentsFromCourseRequests();

        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out var _)) return BadRequest("Invalid id");

        var startTime = DateTime.Now;
        double endTime;

        var course = await _courseRepository.GetCourseByIdAsync(id);
        if (course == null)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "PUT/students/id/delete");
            return NotFound("The course not found");
        }

        students = students
            .Distinct()
            .Where(student => course.Students.Contains(student) && ObjectId.TryParse(student, out var _))
            .ToList();

        if (students.Count == 0)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "PUT/students/id/delete");
            return BadRequest("The course doesn't have the students");
        }

        var settings = new CommunicationSettings
        {
            HttpSettings = new HttpCommunicationSettings
            {
                Method = HttpMethod.Put,
                DestinationURL = $"http://students_manager_service:8080/students/courses/{id}/delete",
                Message = JsonSerializer.Serialize(students)
            },
            RabbitMqSettings = new RabbitMqCommunicationSettings
            {
                QueueName = "student-course",
                Message = $"delete;{course.Id};{string.Join(",", students)}"
            }
        };

        try
        {
            await _communicationStrategy.SendMessage(settings);
        }
        catch (Exception ex)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "PUT/students/id/delete");
            return StatusCode((int)HttpStatusCode.InternalServerError, $"Error occurred: {ex.Message}");
        }

        course.Students.RemoveAll(students.Contains);
        await _courseRepository.UpdateCourseAsync(id, course);
        endTime = (DateTime.Now - startTime).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(endTime, "PUT/students/id/delete");

        return Ok(course);
    }


    [HttpPut("/students/{id}")]
    public async Task<ActionResult> DeleteStudentFromAllCourses(string id)
    {
        ServiceMetrics.IncDeleteStudentFromCoursesRequests();

        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out var _)) return BadRequest("Invalid id");

        var startTime = DateTime.Now;
        double endTime;

        var courses = await _courseRepository.GetAllCoursesAsync();
        if (courses == null)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "PUT/students/id");
            return NotFound("The course not found");
        }

        var coursesList = courses.FindAll(c => c.Students.Contains(id));

        if (coursesList == null)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "PUT/students/id");
            return Ok(id);
        }

        for (int i = 0; i < coursesList.Count; i++)
        {
            var course = coursesList[i];
            ArgumentNullException.ThrowIfNull(course.Id);
            var res = await _courseRepository.RemoveStudentAsync(course.Id, id);
        }

        endTime = (DateTime.Now - startTime).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(endTime, "PUT/students/id");

        return Ok(id);
    }

    // PUT: courses/instructors/{id}/add
    [HttpPut("/instructors/{id}/add")]
    public async Task<ActionResult> AddInstructorsToCourse(string id, [FromBody] List<string> instructors)
    {
        ServiceMetrics.IncAddInstructorsToCourseRequests();

        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out var _)) return BadRequest("Invalid course ID.");

        var startTime = DateTime.Now;
        double endTime;

        var course = await _courseRepository.GetCourseByIdAsync(id);
        if (course == null)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "PUT/instructors/id/add");
            return NotFound("The course not found");
        }

        instructors = instructors
            .Distinct()
            .Where(instructor => !course.Instructors.Contains(instructor) && ObjectId.TryParse(instructor, out var _))
            .ToList();

        if (instructors.Count == 0)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "PUT/instructors/id/add");
            return BadRequest("The course already have the instructors");
        }

        var settings = new CommunicationSettings
        {
            HttpSettings = new HttpCommunicationSettings
            {
                Method = HttpMethod.Put,
                DestinationURL = $"http://instructors_manager_service:8080/instructors/courses/{id}/add",
                Message = JsonSerializer.Serialize(instructors)
            },
            RabbitMqSettings = new RabbitMqCommunicationSettings
            {
                QueueName = "instructor-course",
                Message = $"add;{course.Id};{string.Join(",", instructors)}"
            }
        };

        try
        {
            await _communicationStrategy.SendMessage(settings);
        }
        catch (Exception ex)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "PUT/instructors/id/add");
            return StatusCode((int)HttpStatusCode.InternalServerError, $"Error occurred: {ex.Message}");
        }

        course.Instructors.AddRange(instructors);
        await _courseRepository.UpdateCourseAsync(id, course);
        endTime = (DateTime.Now - startTime).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(endTime, "PUT/instructors/id/add");

        return Ok(course);
    }

    [HttpPut("/instructors/{id}/delete")]
    public async Task<ActionResult> DeleteInstructorsFromCourse(string id, [FromBody] List<string> instructors)
    {
        ServiceMetrics.IncDeleteInstructorsFromCourseRequests();
        var startTime = DateTime.Now;
        double endTime;
        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out var _)) return BadRequest("Invalid course ID.");

        var course = await _courseRepository.GetCourseByIdAsync(id);
        if (course == null)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "PUT/instructors/id/delete");
            return NotFound("The course not found");
        }

        instructors = instructors
           .Distinct()
           .Where(instructor => course.Instructors.Contains(instructor) && ObjectId.TryParse(instructor, out var _))
           .ToList();

        if (instructors.Count == 0)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "PUT/instructors/id/delete");
            return BadRequest("The course don't have the instructors");
        }

        var settings = new CommunicationSettings
        {
            HttpSettings = new HttpCommunicationSettings
            {
                Method = HttpMethod.Put,
                DestinationURL = $"http://instructors_manager_service:8080/instructors/courses/{id}/delete",
                Message = JsonSerializer.Serialize(instructors)
            },
            RabbitMqSettings = new RabbitMqCommunicationSettings
            {
                QueueName = "instructor-course",
                Message = $"delete;{course.Id};{string.Join(",", instructors)}"
            }
        };

        try
        {
            await _communicationStrategy.SendMessage(settings);
        }
        catch (Exception ex)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "PUT/instructors/id/delete");
            return StatusCode((int)HttpStatusCode.InternalServerError, $"Error occurred: {ex.Message}");
        }

        course.Instructors.RemoveAll(instructors.Contains);
        await _courseRepository.UpdateCourseAsync(id, course);
        endTime = (DateTime.Now - startTime).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(endTime, "PUT/instructors/id/delete");

        return Ok(course);
    }

    [HttpPut("/instructors/{id}")]
    public async Task<ActionResult> DeleteInstructorFromAllCourses(string id)
    {
        ServiceMetrics.IncDeleteInstructorFromCoursesRequests();
        var startTime = DateTime.Now;
        double endTime;
        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out var _)) return BadRequest("Invalid id");

        var courses = await _courseRepository.GetAllCoursesAsync();
        if (courses == null)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "PUT/instructors/id");
            return NotFound("The course not found");
        }

        var coursesList = courses.FindAll(c => c.Instructors.Contains(id));

        if (coursesList == null)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "PUT/instructors/id");
            return Ok(id);
        }

        for (int i = 0; i < coursesList.Count; i++)
        {
            var course = coursesList[i];
            ArgumentNullException.ThrowIfNull(course.Id);
            var res = await _courseRepository.RemoveInstructorAsync(course.Id, id);
        }

        endTime = (DateTime.Now - startTime).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(endTime, "PUT/instructors/id");

        return Ok(id);
    }

    // DELETE: courses/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCourse(string id)
    {
        ServiceMetrics.IncDeleteCourseRequests();

        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out var _)) return BadRequest("Invalid Id.");

        var startTime = DateTime.Now;
        double endTime;

        var existingCourse = await _courseRepository.GetCourseByIdAsync(id);
        if (existingCourse == null)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "DELETE");
            return NotFound("Course not found");
        }

        bool studentRequestSuccess = existingCourse.Students.Count == 0;
        bool instructorRequestSuccess = existingCourse.Instructors.Count == 0;

        ArgumentNullException.ThrowIfNull(existingCourse.Id);

        if (!studentRequestSuccess)
        {
            var studentDeletionResult = await DeleteStudentFromCourse(existingCourse.Id, existingCourse.Students);
            studentRequestSuccess = studentDeletionResult is OkObjectResult;
        }

        if (!instructorRequestSuccess)
        {
            var instructorDeletionResult = await DeleteInstructorsFromCourse(existingCourse.Id, existingCourse.Instructors);
            instructorRequestSuccess = instructorDeletionResult is OkObjectResult;
        }

        if (!studentRequestSuccess || !instructorRequestSuccess)
        {
            endTime = (DateTime.Now - startTime).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(endTime, "DELETE");
            return StatusCode(503, "Remote services temporarily unavailable.");
        }

        await _courseRepository.DeleteCourseAsync(id);
        endTime = (DateTime.Now - startTime).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(endTime, "DELETE");

        return NoContent();
    }
}