using System.Runtime.Serialization;
using Microsoft.AspNetCore.Mvc;
using StudentManager.DTO;
using StudentManager.Repository;

namespace StudentManager.Controllers;

[ApiController]
[Route("[controller]")]
public class StudentManagerController : ControllerBase
{
    private readonly IRepository _studentRepository;

    public StudentManagerController(IRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    //GET: api/students
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetStudents()
    {
        var students = await _studentRepository.GetAllStudents();

        Console.WriteLine($"Procecced request to get all students from {HttpContext.Connection.RemoteIpAddress}");
        return Ok(students);
    }

    //GET: api/students/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Student>> GetStudentById(string id)
    {
        var student = await _studentRepository.GetStudentByIdAsync(id);

        if (student == null)
        {
            return NotFound();
        }

        Console.WriteLine($"Procecced request to get stuent {id} from {HttpContext.Connection.RemoteIpAddress}");
        return Ok(student);
    }

    //POST: api/students
    [HttpPost]
    public async Task<ActionResult> AddStudent([FromBody] StudentDTO studentDTO)
    {
        if (studentDTO == null)
        {
            return BadRequest("Student can't be null");
        }

        if (DateTime.TryParseExact(studentDTO.BirthDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime birthDate))
        {
            var student = new Student
            {
                FirstName = studentDTO.FirstName,
                LastName = studentDTO.LastName,
                BirthDate = birthDate,
                PhoneNumber = studentDTO.PhoneNumber,
                Email = studentDTO.Email,
            };

            await _studentRepository.AddStudentAsync(student);

            Console.WriteLine($"Procecced request to add student from {HttpContext.Connection.RemoteIpAddress}");
            return CreatedAtAction(nameof(GetStudentById), new { id = student.Id }, student);
        }

        return BadRequest("Date format is incorecct");
    }

    //PUT: api/students/{id}
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

        if (DateTime.TryParseExact(updatedStudent.BirthDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime birthDate))
        {
            student.FirstName = updatedStudent.FirstName;
            student.LastName = updatedStudent.LastName;
            student.BirthDate = birthDate;
            student.PhoneNumber = updatedStudent.PhoneNumber;
            student.Email = updatedStudent.Email;

            await _studentRepository.UpdateStudentAsync(id, student);

            Console.WriteLine($"Procecced request to update stuent {id} from {HttpContext.Connection.RemoteIpAddress}");
            return NoContent();
        }
        return BadRequest("Date format is invalid it must to be yyyy-mm-dd");

    }

    //PUT: api/students/{id}/courses
    [HttpPut("{id}/courses")]
    public async Task<ActionResult> AddCoursesToStudent(string id, [FromBody] List<string> students)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid request");
        }

        var studentsList = new List<Student>();
        foreach (var student in students)
        {
            var _student = await _studentRepository.GetStudentByIdAsync(student);
            if (_student == null)
            {
                return BadRequest("The student doesn't exist");
            }
            studentsList.Add(_student);
        }

        for (int i = 0; i < studentsList.Count; i++)
        {
            var std = studentsList[i];
            std.Courses.Add(id);
            await _studentRepository.UpdateStudentAsync(std.Id, std);
        }
        Console.WriteLine($"Procecced request adding course {id} to students {students.ToArray()} from {HttpContext.Connection.RemoteIpAddress}");
        return Ok(students);
    }

    //DELETE: api/student/{id}
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

        await _studentRepository.DeleteStudentAsync(id);

        return NoContent();
    }
}