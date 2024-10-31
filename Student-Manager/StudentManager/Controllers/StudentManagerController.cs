using System.Net;
using Microsoft.AspNetCore.Mvc;
using ModifiedCB;
using ModifiedCB.Settings;
using MongoDB.Bson;
using StudentManager.DTO;
using StudentManager.Repository;

namespace StudentManager.Controllers;

[ApiController]
[Route("students")]
public class StudentManagerController : ControllerBase
{
    private readonly IRepository _studentRepository;

    private readonly ICommunicationStrategy _communicationStrategy;

    public StudentManagerController(
        IRepository studentRepository,
        ICommunicationStrategy communicationStrategy)
    {
        _studentRepository = studentRepository;
        _communicationStrategy = communicationStrategy;
    }

    //GET: students
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetStudents()
    {
        ServiceMetrics.IncGetStudentsRequests();
        var start_time = DateTime.Now;
        double end_time;
        var students = await _studentRepository.GetAllStudents();

        end_time = (DateTime.Now - start_time).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(end_time, "GET");

        return Ok(students);
    }

    //GET: students/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Student>> GetStudentById(string id)
    {
        ServiceMetrics.IncGetStudentByIdRequests();

        if (!ObjectId.TryParse(id, out _)) return BadRequest("Invalid id");

        var start_time = DateTime.Now;
        double end_time;

        var student = await _studentRepository.GetStudentByIdAsync(id);

        if (student == null)
        {
            end_time = (DateTime.Now - start_time).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(end_time, "GET/id");
            return NotFound("Student not found");
        }

        end_time = (DateTime.Now - start_time).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(end_time, "GET/id");

        return Ok(student);
    }


    //POST: students
    [HttpPost]
    public async Task<ActionResult> AddStudent([FromBody] StudentDTO studentDTO)
    {
        ServiceMetrics.IncCreateStudentRequests();

        if (studentDTO == null) return BadRequest("Student can't be null");

        if (!DateTime.TryParseExact(studentDTO.BirthDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime birthDate))
        {
            return BadRequest("Date format is incorrect. Expected format: yyyy-MM-dd");
        }

        var start_time = DateTime.Now;
        double end_time;
        var student = new Student
        {
            FirstName = studentDTO.FirstName,
            LastName = studentDTO.LastName,
            BirthDate = birthDate,
            PhoneNumber = studentDTO.PhoneNumber,
            Email = studentDTO.Email
        };

        await _studentRepository.AddStudentAsync(student);

        end_time = (DateTime.Now - start_time).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(end_time, "POST/id");

        return CreatedAtAction(nameof(GetStudentById), new { id = student.Id }, student);
    }

    //PUT: students/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateStudent(string id, [FromBody] StudentDTO updatedStudent)
    {
        ServiceMetrics.IncUpdateStudentRequests();

        if (string.IsNullOrWhiteSpace(id) || updatedStudent == null) return BadRequest("Invalid request");
        if (!ObjectId.TryParse(id, out var _)) return BadRequest("Invalid id");

        if (string.IsNullOrWhiteSpace(updatedStudent.FirstName) || string.IsNullOrWhiteSpace(updatedStudent.LastName))
        {
            return BadRequest("Requeired filds are empty");
        }

        if (!DateTime.TryParseExact(updatedStudent.BirthDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime birthDate))
        {
            return BadRequest("Date format is invalid. Expected format: yyyy-MM-dd");
        }

        var start_time = DateTime.Now;
        double end_time;

        var student = await _studentRepository.GetStudentByIdAsync(id);
        if (student == null)
        {
            end_time = (DateTime.Now - start_time).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(end_time, "PUT/id");
            return NotFound("The student doesn't found");
        }

        student.FirstName = updatedStudent.FirstName;
        student.LastName = updatedStudent.LastName;
        student.BirthDate = birthDate;
        student.PhoneNumber = updatedStudent.PhoneNumber;
        student.Email = updatedStudent.Email;

        await _studentRepository.UpdateStudentAsync(id, student);

        end_time = (DateTime.Now - start_time).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(end_time, "PUT/id");

        return NoContent();
    }

    [HttpPut("courses/{id}/add")]
    public async Task<ActionResult> AddCourseToStudents(string id, [FromBody] List<string> studentIds)
    {
        ServiceMetrics.IncAddCourseToStudentsRequests();

        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out var _)) return BadRequest("Invalid course id");

        var start_time = DateTime.Now;
        double end_time;

        var validStudentIds = studentIds
            .Where(studentId => ObjectId.TryParse(studentId, out _))
            .ToList();

        if (validStudentIds.Count != studentIds.Count)
        {
            end_time = (DateTime.Now - start_time).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(end_time, "PUT/courses/id/add");
            return BadRequest("One or more student IDs are invalid");
        }

        var studentTasks = validStudentIds.Select(_studentRepository.GetStudentByIdAsync);
        var studentsList = (await Task.WhenAll(studentTasks))
            .Where(student => student != null)
            .ToList();

        if (studentsList.Count == 0)
        {
            end_time = (DateTime.Now - start_time).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(end_time, "PUT/courses/id/add");
            return BadRequest("No valid students found");
        }

        var addCourseTasks = studentsList.Select(student =>
            _studentRepository.AddCourseAsync(student.Id, id));

        await Task.WhenAll(addCourseTasks);

        // var studentsList = new List<Student>();
        // foreach (var studentId in studentIds)
        // {
        //     if (!ObjectId.TryParse(studentId, out var _))
        //     {
        //         end_time = (DateTime.Now - start_time).TotalSeconds;
        //         ServiceMetrics.TrackRequestDuration(end_time, "PUT/courses/id/add");
        //         return BadRequest("Invalid id");
        //     }
        //     var student = await _studentRepository.GetStudentByIdAsync(studentId);
        //     if (student == null)
        //     {
        //         end_time = (DateTime.Now - start_time).TotalSeconds;
        //         ServiceMetrics.TrackRequestDuration(end_time, "PUT/courses/id/add");
        //         return BadRequest($"Student with id {studentId} does not exist.");
        //     }
        //     studentsList.Add(student);
        // }

        // for (int i = 0; i < studentsList.Count; i++)
        // {
        //     var student = studentsList[i];
        //     ArgumentNullException.ThrowIfNull(student.Id);
        //     var res = await _studentRepository.AddCourseAsync(student.Id, id);
        // }

        end_time = (DateTime.Now - start_time).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(end_time, "PUT/courses/id/add");
        return Ok(studentsList.Select(s => s.Id).ToList());
    }


    [HttpPut("courses/{id}/delete")]
    public async Task<ActionResult> DeleteCourseFromStudent(string id, [FromBody] List<string> studentIds)
    {
        ServiceMetrics.IncDeleteCourseFromStudentsRequests();

        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out var _)) return BadRequest("Invalid course id");

        var start_time = DateTime.Now;
        double end_time;

        var validStudentIds = studentIds
            .Where(studentId => ObjectId.TryParse(studentId, out _))
            .ToList();

        if (validStudentIds.Count != studentIds.Count)
        {
            end_time = (DateTime.Now - start_time).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(end_time, "courses/id/delete");
            return BadRequest("One or more student IDs are invalid");
        }

        var studentTasks = validStudentIds.Select(_studentRepository.GetStudentByIdAsync);
        var studentsList = (await Task.WhenAll(studentTasks))
            .Where(student => student != null)
            .ToList();

        if (studentsList.Count == 0)
        {
            end_time = (DateTime.Now - start_time).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(end_time, "PUT/courses/id/delete");
            return BadRequest("No valid students found");
        }

        var addCourseTasks = studentsList.Select(student =>
            _studentRepository.DeleteCourseAsync(student.Id, id));

        await Task.WhenAll(addCourseTasks);

        // var studentsList = new List<Student>();
        // foreach (var studentId in studentIds)
        // {
        //     if (!ObjectId.TryParse(studentId, out var _))
        //     {
        //         end_time = (DateTime.Now - start_time).TotalSeconds;
        //         ServiceMetrics.TrackRequestDuration(end_time, "PUT/courses/id/delete");
        //         return BadRequest("Invalid id");
        //     }
        //     var student = await _studentRepository.GetStudentByIdAsync(studentId);
        //     if (student == null)
        //     {
        //         end_time = (DateTime.Now - start_time).TotalSeconds;
        //         ServiceMetrics.TrackRequestDuration(end_time, "PUT/courses/id/delete");
        //         return BadRequest($"Student with id {studentId} does not exist.");
        //     }
        //     studentsList.Add(student);
        // }

        // for (int i = 0; i < studentsList.Count; i++)
        // {
        //     var student = studentsList[i];
        //     ArgumentNullException.ThrowIfNull(student.Id);
        //     var res = await _studentRepository.DeleteCourseAsync(student.Id, id);
        // }

        end_time = (DateTime.Now - start_time).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(end_time, "PUT/courses/id/delete");

        return Ok(studentsList.Select(s => s.Id).ToList());
    }

    //DELETE: student/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteStudent(string id)
    {
        ServiceMetrics.IncDeleteStudentRequests();

        if (string.IsNullOrWhiteSpace(id)) return BadRequest("Invalid id");
        if (!ObjectId.TryParse(id, out var _)) return BadRequest("Invalid id");

        var start_time = DateTime.Now;
        double end_time;

        var student = await _studentRepository.GetStudentByIdAsync(id);
        if (student == null)
        {
            end_time = (DateTime.Now - start_time).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(end_time, "DELETE/id");
            return NotFound("The student doesn't found");
        }

        if (student.Courses.Count == 0)
        {
            await _studentRepository.DeleteStudentAsync(id);
            end_time = (DateTime.Now - start_time).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(end_time, "DELETE/id");

            return NoContent();
        }

        var settings = new CommunicationSettings
        {
            HttpSettings = new HttpCommunicationSettings
            {
                Method = HttpMethod.Put,
                DestinationURL = $"http://courses_manager_service:8080/students/{id}",
                Message = null
            },
            RabbitMqSettings = new RabbitMqCommunicationSettings
            {
                QueueName = "student-delete",
                Message = id
            }
        };

        try
        {
            await _communicationStrategy.SendMessage(settings);
        }
        catch (Exception ex)
        {
            end_time = (DateTime.Now - start_time).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(end_time, "DELETE/id");
            return StatusCode((int)HttpStatusCode.InternalServerError, $"Error occurred: {ex.Message}");
        }

        await _studentRepository.DeleteStudentAsync(id);

        end_time = (DateTime.Now - start_time).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(end_time, "DELETE/id");

        return NoContent();
    }
}