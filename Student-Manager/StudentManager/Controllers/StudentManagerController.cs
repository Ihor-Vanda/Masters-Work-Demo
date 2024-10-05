using System.Runtime.Serialization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using StudentManager.Clients;
using StudentManager.DTO;
using StudentManager.Repository;

namespace StudentManager.Controllers;

[ApiController]
[Route("students")]
public class StudentManagerController : ControllerBase
{
    private readonly IRepository _studentRepository;

    private readonly CourseServiceClient _courseServiceClient;

    public StudentManagerController(IRepository studentRepository, CourseServiceClient courseServiceClient)
    {
        _studentRepository = studentRepository;
        _courseServiceClient = courseServiceClient;
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
            return NotFound();
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

    [HttpPut("courses/{id}/add")]
    public async Task<ActionResult> AddCoursesToStudent(string id, [FromBody] List<string> students)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid request");
        }

        var courseExists = await _courseServiceClient.CheckCourseExists(id);
        if (!courseExists)
        {
            return BadRequest($"Course with id {id} does not exist.");
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
            if (!std.Courses.Contains(id) && std.Id != null)
            {
                std.Courses.Add(id);
                await _studentRepository.UpdateStudentAsync(std.Id, std);
            }
            else
            {
                studentsList.RemoveAt(i);
                i--;
            }
        }

        Console.WriteLine($"Procecced request adding course {id} to students {students.ToList()} from {HttpContext.Connection.RemoteIpAddress}");

        return Ok(studentsList.Select(s => s.Id).ToList());
    }

    [HttpPut("courses/{id}/delete")]
    public async Task<ActionResult> DeleteCourseFromStudent(string id, [FromBody] List<string> students)
    {
        if (string.IsNullOrWhiteSpace(id) || students == null)
        {
            return BadRequest("Invalid request");
        }

        // var courseExists = await _courseServiceClient.CheckCourseExists(id);
        // if (!courseExists)
        // {
        //     return BadRequest($"Course with id {id} does not exist.");
        // }

        var _students = await _studentRepository.GetAllStudents();

        if (_students == null)
        {
            return BadRequest("Can't get students from repo");
        }

        var studentsList = _students.FindAll(s => students.Contains(s.Id)).ToList();

        if (studentsList == null)
        {
            return BadRequest("The students doesn't exist");
        }

        for (int i = 0; i < studentsList.Count; i++)
        {
            studentsList[i].Courses.Remove(id);
            await _studentRepository.UpdateStudentAsync(studentsList[i].Id, studentsList[i]);
        }

        Console.WriteLine($"Procecced request adding course {id} to students {students.ToList()} from {HttpContext.Connection.RemoteIpAddress}");

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
            var response = await _courseServiceClient.DeleteStudentFromCourses(id);

            if (response == System.Net.HttpStatusCode.OK)
            {
                await _studentRepository.DeleteStudentAsync(id);

                Console.WriteLine($"Procecced request to delete student {id} from {HttpContext.Connection.RemoteIpAddress}");

                return NoContent();
            }

            return BadRequest("Can't delete student from releted courses");
        }

        await _studentRepository.DeleteStudentAsync(id);

        Console.WriteLine($"Procecced request to delete student {id} from {HttpContext.Connection.RemoteIpAddress}");

        return NoContent();
    }
}