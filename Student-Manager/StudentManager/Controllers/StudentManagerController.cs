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

            return NoContent();
        }
        return BadRequest("Date format is invalid it must to be yyyy-mm-dd");

    }

    //PUT: api/students/{id}/courses
    [HttpPut("{id}/courses")]
    public async Task<ActionResult> AddCoursesToStudent(string id, [FromBody] List<string> courses)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid request");
        }

        var student = await _studentRepository.GetStudentByIdAsync(id);

        if (student == null)
        {
            return NotFound("The student doesn't found");
        }

        courses ??= [];

        student.Courses.AddRange(courses);
        await _studentRepository.UpdateStudentAsync(id, student);

        return Ok(student);
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