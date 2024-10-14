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
    public async Task<ActionResult> AddCourseToStudents(string id, [FromBody] List<string> studentIds)
    {
        if (string.IsNullOrWhiteSpace(id) || studentIds == null || studentIds.Count == 0)
        {
            return BadRequest("Invalid request");
        }

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

        for (int i = 0; i < studentsList.Count; i++)
        {
            var student = studentsList[i];

            var res = await _studentRepository.AddCourseAsync(student.Id, id);

            Console.WriteLine($"Updated student {student.Id} - Success: {res.ModifiedCount > 0}");
        }

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

        for (int i = 0; i < studentsList.Count; i++)
        {
            var student = studentsList[i];

            var res = await _studentRepository.DeleteCourseAsync(student.Id, id);

            Console.WriteLine($"Updated student {student.Id} - Success: {res.ModifiedCount > 0}");
        }

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

        if (student.Courses.Count == 0)
        {
            await _studentRepository.DeleteStudentAsync(id);
            Console.WriteLine($"Processed request deleting course {id} from students {string.Join(", ", id)} from {HttpContext.Connection.RemoteIpAddress}");

            return NoContent();
        }

        try
        {
            var response = await _courseServiceClient.DeleteStudentFromCourses(id);

            if (response != System.Net.HttpStatusCode.OK)
            {
                return StatusCode(503, "Remote service temporaly unavaible");
            }

            await _studentRepository.DeleteStudentAsync(id);

            Console.WriteLine($"Procecced request to delete student {id} from {HttpContext.Connection.RemoteIpAddress}");

            return NoContent();
        }
        catch (BrokenCircuitException)
        {
            _rabbitMQClient.PublishMessage("student-delete", id);
            await _studentRepository.DeleteStudentAsync(id);
            return StatusCode(200, "Sending request to delete student to queue");
        }
    }
}