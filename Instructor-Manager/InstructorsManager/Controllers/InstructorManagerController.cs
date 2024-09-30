using InstructorsManager.DTO;
using InstructorsManager.Repository;
using Microsoft.AspNetCore.Mvc;

namespace InstructorsManager.Controllers;

[ApiController]
[Route("[controller]")]
public class InstructorManagerController : ControllerBase
{
    private readonly IRepository _instructorRepository;

    public InstructorManagerController(IRepository instructorRepository)
    {
        _instructorRepository = instructorRepository;
    }

    //GET: api/instructors
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Instructor>>> GetAllInstructors()
    {
        var instructors = await _instructorRepository.GetAllInstructors();
        Console.WriteLine($"Procecced request to get all instructors from{HttpContext.Connection.RemoteIpAddress}");
        return Ok(instructors);
    }

    //GET: api/instructors/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Instructor>> GetInstructorById(string id)
    {
        var instructor = await _instructorRepository.GetInstructorById(id);

        if (instructor == null)
        {
            return NotFound();
        }

        Console.WriteLine($"Procecced request to get all instructor {id} from{HttpContext.Connection.RemoteIpAddress}");
        return Ok(instructor);
    }

    //POST: api/instructors
    [HttpPost]
    public async Task<ActionResult> AddInstructor([FromBody] InstructorDTO instructorDTO)
    {
        if (instructorDTO == null)
        {
            return BadRequest("Instructor can't be null");
        }

        if (DateTime.TryParseExact(instructorDTO.BirthDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var birthDate))
        {
            var instructor = new Instructor();
            instructor.FirstName = instructorDTO.FirstName;
            instructor.LastName = instructorDTO.LastName;
            instructor.BirthDate = birthDate;
            instructor.PhoneNumber = instructorDTO.PhoneNumber;
            instructor.Email = instructorDTO.Email;

            await _instructorRepository.AddInstructor(instructor);

            Console.WriteLine($"Procecced request to add instructors from{HttpContext.Connection.RemoteIpAddress}");
            return CreatedAtAction(nameof(GetInstructorById), new { id = instructor.Id }, instructor);
        }

        return BadRequest("Date format is incorrect");
    }

    //PUT: api/instrictors
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateInstructor(string id, [FromBody] InstructorDTO instructorDTO)
    {
        if (string.IsNullOrWhiteSpace(id) || instructorDTO == null)
        {
            return BadRequest("Invalid Request");
        }

        if (string.IsNullOrWhiteSpace(instructorDTO.FirstName) || string.IsNullOrWhiteSpace(instructorDTO.LastName) || string.IsNullOrWhiteSpace(instructorDTO.BirthDate))
        {
            return BadRequest("Reqired field are empty");
        }

        var instructor = await _instructorRepository.GetInstructorById(id);
        if (instructor == null)
        {
            return NotFound("The instructor not found");
        }

        if (DateTime.TryParseExact(instructorDTO.BirthDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var birthDate))
        {
            instructor.FirstName = instructorDTO.FirstName;
            instructor.LastName = instructorDTO.LastName;
            instructor.BirthDate = birthDate;
            instructor.PhoneNumber = instructorDTO.PhoneNumber;
            instructor.Email = instructorDTO.Email;

            await _instructorRepository.UpdateInstructor(id, instructor);

            Console.WriteLine($"Procecced request to update instructor {id} from{HttpContext.Connection.RemoteIpAddress}");
            return NoContent();
        }

        return BadRequest("Date format is invalid it must to be yyyy-mm-dd");
    }

    //PUT: api/instrctors/{id}/courses
    [HttpPut("{id}/courses")]
    public async Task<ActionResult> AddCourseToInstructors(string id, [FromBody] List<string> instructors)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid request");
        }

        var instructorsList = new List<Instructor>();
        foreach (var instructor in instructors)
        {
            var _instructor = await _instructorRepository.GetInstructorById(instructor);
            if (_instructor == null)
            {
                return BadRequest("The instructor doesn't exist");
            }
            instructorsList.Add(_instructor);
        }

        for (int i = 0; i < instructorsList.Count; i++)
        {
            var instr = instructorsList[i];
            instr.Courses.Add(id);
            await _instructorRepository.UpdateInstructor(instr.Id, instr);
        }

        Console.WriteLine($"Procecced request adding instructors {instructors} to course {id} from{HttpContext.Connection.RemoteIpAddress}");
        return Ok(instructors);
    }

    //DELETE: api/instructors/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteInstructor(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid id");
        }

        var instructor = _instructorRepository.GetInstructorById(id);
        if (instructor == null)
        {
            return NotFound("The instructor not foud");
        }

        await _instructorRepository.DeleteInstructor(id);

        Console.WriteLine($"Procecced request to delete instructors {id} from{HttpContext.Connection.RemoteIpAddress}");
        return NoContent();
    }
}
