using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Polly.CircuitBreaker;
using StudentManager.Clients;
using StudentManager.DTO;
using StudentManager.RabbitMQ;
using StudentManager.Repository;

namespace StudentManager.Controllers;

[ApiController]
[Route("students")]
public class StudentManagerController : ControllerBase
{
    private readonly IRepository _studentRepository;

    private readonly CourseServiceClient _courseServiceClient;

    private readonly RabbitMQClient _rabbitMQClient;

    public StudentManagerController(IRepository studentRepository, CourseServiceClient courseServiceClient, RabbitMQClient rabbitMQClient)
    {
        _studentRepository = studentRepository;
        _courseServiceClient = courseServiceClient;
        _rabbitMQClient = rabbitMQClient;
    }

    //GET: students
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetStudents()
    {
        var students = await _studentRepository.GetAllStudents();

        Console.WriteLine($"Procecced request to get all students from {HttpContext.Connection.RemoteIpAddress}");

        return Ok(students);
    }

    //GET: students/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Student>> GetStudentById(string id)
    {

        if (!ObjectId.TryParse(id, out var idValue))
        {
            return BadRequest("Invalid id");
        }

        var student = await _studentRepository.GetStudentByIdAsync(id);

        if (student == null)
        {
            return NotFound("Student not found");
        }

        Console.WriteLine($"Procecced request to get stuent {id} from {HttpContext.Connection.RemoteIpAddress}");

        return Ok(student);
    }

    //POST: students
    [HttpPost]
    public async Task<ActionResult> AddStudent([FromBody] StudentDTO studentDTO)
    {
        if (studentDTO == null)
        {
            return BadRequest("Student can't be null");
        }

        if (!DateTime.TryParseExact(studentDTO.BirthDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime birthDate))
        {
            return BadRequest("Date format is incorrect. Expected format: yyyy-MM-dd");
        }

        var student = new Student
        {
            FirstName = studentDTO.FirstName,
            LastName = studentDTO.LastName,
            BirthDate = birthDate,
            PhoneNumber = studentDTO.PhoneNumber,
            Email = studentDTO.Email
        };

        await _studentRepository.AddStudentAsync(student);

        Console.WriteLine($"Procecced request to add student from {HttpContext.Connection.RemoteIpAddress}");

        return CreatedAtAction(nameof(GetStudentById), new { id = student.Id }, student);

    }

    //PUT: students/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateStudent(string id, [FromBody] StudentDTO updatedStudent)
    {
        if (string.IsNullOrWhiteSpace(id) || updatedStudent == null)
        {
            return BadRequest("Invalid request");
        }

        if (string.IsNullOrWhiteSpace(updatedStudent.FirstName) || string.IsNullOrWhiteSpace(updatedStudent.LastName))
        {
            return BadRequest("Requeired filds are empty");
        }

        var student = await _studentRepository.GetStudentByIdAsync(id);
        if (student == null)
        {
            return NotFound("The student doesn't found");
        }

        if (!DateTime.TryParseExact(updatedStudent.BirthDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime birthDate))
        {
            return BadRequest("Date format is invalid. Expected format: yyyy-MM-dd");
        }

        student.FirstName = updatedStudent.FirstName;
        student.LastName = updatedStudent.LastName;
        student.BirthDate = birthDate;
        student.PhoneNumber = updatedStudent.PhoneNumber;
        student.Email = updatedStudent.Email;

        await _studentRepository.UpdateStudentAsync(id, student);

        Console.WriteLine($"Procecced request to update stuent {id} from {HttpContext.Connection.RemoteIpAddress}");

        return NoContent();
    }

    [HttpPut("courses/{id}/add")]
    public async Task<ActionResult> AddCoursesToStudent(string id, [FromBody] List<string> studentIds)
    {
        if (string.IsNullOrWhiteSpace(id) || studentIds == null || studentIds.Count == 0)
        {
            return BadRequest("Invalid request");
        }

        // try
        // {
        //     var courseExists = await _courseServiceClient.CheckCourseExists(id);
        //     if (!courseExists)
        //     {
        //         return BadRequest($"Course with id {id} does not exist.");
        //     }
        // }
        // catch (BrokenCircuitException)
        // {
        //     // Circuit breaker is open, return 503 Service Unavailable
        //     return StatusCode(503, "Course service is temporarily unavailable due to a circuit breaker.");
        // }

        var studentsList = new List<Student>();
        foreach (var studentId in studentIds)
        {
            var student = await _studentRepository.GetStudentByIdAsync(studentId);
            if (student == null)
            {
                return BadRequest($"Student with id {studentId} does not exist.");
            }
            studentsList.Add(student);
        }

        // Add course to each student
        var updateTasks = studentsList
            .Where(std => !std.Courses.Contains(id))
            .Select(std =>
            {
                std.Courses.Add(id);
                return _studentRepository.UpdateStudentAsync(std.Id, std);
            });

        await Task.WhenAll(updateTasks);

        Console.WriteLine($"Processed request adding course {id} to students {string.Join(", ", studentIds)} from {HttpContext.Connection.RemoteIpAddress}");

        return Ok(studentsList.Select(s => s.Id).ToList());
    }

    [HttpPut("courses/{id}/delete")]
    public async Task<ActionResult> DeleteCourseFromStudent(string id, [FromBody] List<string> studentIds)
    {
        if (string.IsNullOrWhiteSpace(id) || studentIds == null || studentIds.Count == 0)
        {
            return BadRequest("Invalid request");
        }

        // var courseExists = await _courseServiceClient.CheckCourseExists(id);
        // if (!courseExists)
        // {
        //     return BadRequest($"Course with id {id} does not exist.");
        // }

        var studentsList = await _studentRepository.GetAllStudents();
        studentsList.FindAll(s => studentIds.Contains(s.Id));
        if (studentsList == null || studentsList.Count == 0)
        {
            return BadRequest("The specified students do not exist.");
        }

        var updateTasks = studentsList.Select(std =>
        {
            std.Courses.Remove(id);
            return _studentRepository.UpdateStudentAsync(std.Id, std);
        });

        await Task.WhenAll(updateTasks);

        Console.WriteLine($"Processed request deleting course {id} from students {string.Join(", ", studentIds)} from {HttpContext.Connection.RemoteIpAddress}");

        return Ok(studentsList.Select(s => s.Id).ToList());
    }

    //DELETE: student/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteStudent(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid id");
        }

        var student = await _studentRepository.GetStudentByIdAsync(id);
        if (student == null)
        {
            return NotFound("The student doesn't found");
        }

        if (student.Courses.Count > 0)
        {
            try
            {
                var response = await _courseServiceClient.DeleteStudentFromCourses(id);

                if (response != System.Net.HttpStatusCode.OK)
                {
                    return StatusCode(503,"Remote service temporaly unavaible");
                }

                await _studentRepository.DeleteStudentAsync(id);

                Console.WriteLine($"Procecced request to delete student {id} from {HttpContext.Connection.RemoteIpAddress}");

                return NoContent();
            }
            catch (BrokenCircuitException)
            {
                _rabbitMQClient.PublishMessage("student-delete", id);
                await _studentRepository.DeleteStudentAsync(id);
                return StatusCode(503,"Sending request to delete student to queue");
            }
        }

        await _studentRepository.DeleteStudentAsync(id);
        Console.WriteLine($"Processed request deleting course {id} from students {string.Join(", ", id)} from {HttpContext.Connection.RemoteIpAddress}");

        return NoContent();
    }
}