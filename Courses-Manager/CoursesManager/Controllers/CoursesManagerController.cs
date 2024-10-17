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
        using (ServiceMetrics.TrackRequestDuration())
        {
            var courses = await _courseRepository.GetAllCoursesAsync();

            Console.WriteLine($"Procecced request to get all courses from{HttpContext.Connection.RemoteIpAddress}");

            return Ok(courses);
        }
    }

    // GET: courses/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Course>> GetCourseById(string id)
    {
        ServiceMetrics.IncGetCourseByIdRequests();
        using (ServiceMetrics.TrackRequestDuration())
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
    }

    // POST: courses
    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] CourseDto courseDto)
    {
        ServiceMetrics.IncCreateCourseRequests();
        using (ServiceMetrics.TrackRequestDuration())
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
    }

    // PUT: courses/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCourse(string id, [FromBody] CourseDto updatedCourseDTO)
    {
        ServiceMetrics.IncUpdateCourseRequests();
        using (ServiceMetrics.TrackRequestDuration())
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
    }

    // PUT: courses/students/{id}/add
    [HttpPut("/students/{id}/add")]
    public async Task<IActionResult> AddStudentsToCourse(string id, [FromBody] List<string> students)
    {
        ServiceMetrics.IncAddStudentToCourseRequests();
        using (ServiceMetrics.TrackRequestDuration())
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
                    Message = JsonSerializer.Serialize(new RabbitMQMessage
                    {
                        Type = "add",
                        CourseId = course.Id,
                        EntityIds = students
                    })
                }
            };

            try
            {
                await _communicationStrategy.SendMessage(settings);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, $"Error occurred: {ex.Message}");
            }

            course.Students.AddRange(students);
            await _courseRepository.UpdateCourseAsync(id, course);

            Console.WriteLine($"Processed request to add students to course {id} from {HttpContext.Connection.RemoteIpAddress}");

            return Ok(course);
        }
    }

    [HttpPut("/students/{id}/delete")]
    public async Task<ActionResult> DeleteStudentFromCourse(string id, [FromBody] List<string> students)
    {
        ServiceMetrics.IncDeleteStudentsFromCourseRequests();
        using (ServiceMetrics.TrackRequestDuration())
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
                    Message = JsonSerializer.Serialize(new RabbitMQMessage
                    {
                        Type = "delete",
                        CourseId = course.Id,
                        EntityIds = students
                    })
                }
            };

            try
            {
                await _communicationStrategy.SendMessage(settings);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, $"Error occurred: {ex.Message}");
            }

            course.Students.RemoveAll(students.Contains);
            await _courseRepository.UpdateCourseAsync(id, course);

            Console.WriteLine($"Procecced request to delete students from course {id} from {HttpContext.Connection.RemoteIpAddress}");

            return Ok(course);
        }
    }

    [HttpPut("/students/{id}")]
    public async Task<ActionResult> DeleteStudentFromAllCourses(string id)
    {
        ServiceMetrics.IncDeleteStudentFromCoursesRequests();
        using (ServiceMetrics.TrackRequestDuration())
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
                var course = coursesList[i];

                ArgumentNullException.ThrowIfNull(course.Id);
                var res = await _courseRepository.RemoveStudentAsync(course.Id, id);

                Console.WriteLine($"Updated course {course.Id} - Success: {res.ModifiedCount > 0}");
                Console.WriteLine($"Deleted student {id} from course {course.Id}");
            }

            return Ok(id);
        }
    }

    // PUT: courses/instructors/{id}/add
    [HttpPut("/instructors/{id}/add")]
    public async Task<ActionResult> AddInstructorsToCourse(string id, [FromBody] List<string> instructors)
    {
        ServiceMetrics.IncAddInstructorsToCourseRequests();
        using (ServiceMetrics.TrackRequestDuration())
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
                    Message = JsonSerializer.Serialize(new RabbitMQMessage
                    {
                        Type = "add",
                        CourseId = course.Id,
                        EntityIds = instructors
                    })
                }
            };

            try
            {
                await _communicationStrategy.SendMessage(settings);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, $"Error occurred: {ex.Message}");
            }

            course.Instructors.AddRange(instructors);
            await _courseRepository.UpdateCourseAsync(id, course);

            Console.WriteLine($"Procecced request to add instructors to course {id} from {HttpContext.Connection.RemoteIpAddress}");

            return Ok(course);
        }
    }

    [HttpPut("/instructors/{id}/delete")]
    public async Task<ActionResult> DeleteInstructorsFromCourse(string id, [FromBody] List<string> instructors)
    {
        ServiceMetrics.IncDeleteInstructorsFromCourseRequests();
        using (ServiceMetrics.TrackRequestDuration())
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
                    Message = JsonSerializer.Serialize(new RabbitMQMessage
                    {
                        Type = "delete",
                        CourseId = course.Id,
                        EntityIds = instructors
                    })
                }
            };

            try
            {
                await _communicationStrategy.SendMessage(settings);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, $"Error occurred: {ex.Message}");
            }

            course.Instructors.RemoveAll(instructors.Contains);
            await _courseRepository.UpdateCourseAsync(id, course);

            Console.WriteLine($"Procecced request to delete instructors from course {id} from {HttpContext.Connection.RemoteIpAddress}");

            return Ok(course);
        }
    }

    [HttpPut("/instructors/{id}")]
    public async Task<ActionResult> DeleteInstructorFromAllCourses(string id)
    {
        ServiceMetrics.IncDeleteInstructorFromCoursesRequests();
        using (ServiceMetrics.TrackRequestDuration())
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
                var course = coursesList[i];

                ArgumentNullException.ThrowIfNull(course.Id);
                var res = await _courseRepository.RemoveInstructorAsync(course.Id, id);

                Console.WriteLine($"Updated course {course.Id} - Success: {res.ModifiedCount > 0}");
                Console.WriteLine($"Deleted instructor {id} from course {course.Id}");
            }

            return Ok(id);
        }
    }

    // DELETE: api/courses/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCourse(string id)
    {
        ServiceMetrics.IncDeleteCourseRequests();
        using (ServiceMetrics.TrackRequestDuration())
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

            bool studentRequestSuccess = existingCourse.Students.Count == 0;
            bool instructorRequestSuccess = existingCourse.Instructors.Count == 0;

            ArgumentNullException.ThrowIfNull(existingCourse.Id);

            if (!studentRequestSuccess)
            {
                var studentDeletionResult = await DeleteStudentFromCourse(existingCourse.Id, existingCourse.Students);
                studentRequestSuccess = studentDeletionResult is OkResult;
            }

            if (!instructorRequestSuccess)
            {
                var instructorDeletionResult = await DeleteInstructorsFromCourse(existingCourse.Id, existingCourse.Instructors);
                instructorRequestSuccess = instructorDeletionResult is OkResult;
            }

            if (!studentRequestSuccess || !instructorRequestSuccess)
            {
                return StatusCode(503, "Remote services temporarily unavailable.");
            }

            await _courseRepository.DeleteCourseAsync(id);
            Console.WriteLine($"Processed request to delete course {id} from {HttpContext.Connection.RemoteIpAddress}");

            return NoContent();
        }
    }
}