using InstructorsManager.Clients;
using InstructorsManager.DTO;
using InstructorsManager.RabbitMQ;
using InstructorsManager.Repository;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Polly.CircuitBreaker;

namespace InstructorsManager.Controllers;

[ApiController]
[Route("instructors")]
public class InstructorManagerController : ControllerBase
{
    private readonly IRepository _instructorRepository;

    private readonly CourseServiceClient _courseServiceClient;

    private readonly RabbitMQClient _rabbitMQClient;

    public InstructorManagerController(IRepository instructorRepository, CourseServiceClient courseServiceClient, RabbitMQClient rabbitMQClient)
    {
        _instructorRepository = instructorRepository;
        _courseServiceClient = courseServiceClient;
        _rabbitMQClient = rabbitMQClient;
    }

    //GET: instructors
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Instructor>>> GetAllInstructors()
    {
        var instructors = await _instructorRepository.GetInstructorsAsync();

        Console.WriteLine($"Procecced request to get all instructors from{HttpContext.Connection.RemoteIpAddress}");

        return Ok(instructors);
    }

    //GET: instructors/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Instructor>> GetInstructorById(string id)
    {
        if (!ObjectId.TryParse(id, out var idValue))
        {
            return BadRequest("Invalid id");
        }

        var instructor = await _instructorRepository.GetInstructorByIdAsync(id);

        if (instructor == null)
        {
            return NotFound("Instructor not found");
        }

        Console.WriteLine($"Procecced request to get all instructor {id} from{HttpContext.Connection.RemoteIpAddress}");

        return Ok(instructor);
    }

    //POST: instructors
    [HttpPost]
    public async Task<ActionResult> AddInstructor([FromBody] InstructorDTO instructorDTO)
    {
        if (instructorDTO == null)
        {
            return BadRequest("Instructor can't be null");
        }

        if (!DateTime.TryParseExact(instructorDTO.BirthDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var birthDate))
        {
            return BadRequest("Date format is incorrect. Expected format: yyyy-MM-dd");
        }

        var instructor = new Instructor
        {
            FirstName = instructorDTO.FirstName,
            LastName = instructorDTO.LastName,
            BirthDate = birthDate,
            PhoneNumber = instructorDTO.PhoneNumber,
            Email = instructorDTO.Email
        };

        await _instructorRepository.AddInstructorAsync(instructor);

        Console.WriteLine($"Procecced request to add instructors from{HttpContext.Connection.RemoteIpAddress}");

        return CreatedAtAction(nameof(GetInstructorById), new { id = instructor.Id }, instructor);
    }

    //PUT: instructors
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

        var instructor = await _instructorRepository.GetInstructorByIdAsync(id);
        if (instructor == null)
        {
            return NotFound("The instructor not found");
        }

        if (!DateTime.TryParseExact(instructorDTO.BirthDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var birthDate))
        {
            return BadRequest("Date format is invalid. Expected format: yyyy-MM-dd");
        }

        instructor.FirstName = instructorDTO.FirstName;
        instructor.LastName = instructorDTO.LastName;
        instructor.BirthDate = birthDate;
        instructor.PhoneNumber = instructorDTO.PhoneNumber;
        instructor.Email = instructorDTO.Email;

        await _instructorRepository.UpdateInstructorAsync(id, instructor);

        Console.WriteLine($"Procecced request to update instructor {id} from{HttpContext.Connection.RemoteIpAddress}");

        return NoContent();
    }

    //PUT: instructors/{id}/courses
    [HttpPut("courses/{id}/add")]
    public async Task<ActionResult> AddCourseToInstructors(string id, [FromBody] List<string> instructorIds)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid request");
        }

        var instructorsList = new List<Instructor>();
        foreach (var instructorId in instructorIds)
        {
            var instructor = await _instructorRepository.GetInstructorByIdAsync(instructorId);
            if (instructor == null)
            {
                return BadRequest($"The instructor with id {instructorId} does not exist");
            }
            instructorsList.Add(instructor);
        }

        for (int i = 0; i < instructorsList.Count; i++)
        {
            var instructor = instructorsList[i];
            await _instructorRepository.AddCourseAsync(instructor.Id, id);
        }

        Console.WriteLine($"Processed request adding course {id} to instructors {string.Join(", ", instructorIds)} from {HttpContext.Connection.RemoteIpAddress}");

        return Ok(instructorsList.Select(i => i.Id).ToList());
    }

    //PUT: instructors/courses/{id}/delete
    [HttpPut("courses/{id}/delete")]
    public async Task<ActionResult> DeleteInstructorsFromCourse(string id, [FromBody] List<string> instructorIds)
    {
        if (string.IsNullOrWhiteSpace(id) || instructorIds == null || instructorIds.Count == 0)
        {
            return BadRequest("Invalid request");
        }

        var instructorsList = new List<Instructor>();
        foreach (var instructorId in instructorIds)
        {
            var instructor = await _instructorRepository.GetInstructorByIdAsync(instructorId);
            if (instructor == null)
            {
                return BadRequest($"The instructor with id {instructorId} does not exist");
            }
            instructorsList.Add(instructor);
        }

        for (int i = 0; i < instructorsList.Count; i++)
        {
            var instructor = instructorsList[i];
            await _instructorRepository.DeleteCourseAsync(instructor.Id, id);
        }

        Console.WriteLine($"Processed request deleting course {id} from students {string.Join(", ", instructorIds)} from {HttpContext.Connection.RemoteIpAddress}");

        return Ok(instructorsList.Select(i => i.Id).ToList());
    }

    //DELETE: instructors/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteInstructor(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid id");
        }

        var instructor = await _instructorRepository.GetInstructorByIdAsync(id);
        if (instructor == null)
        {
            return NotFound("The instructor not foud");
        }

        if (instructor.Courses.Count == 0)
        {
            await _instructorRepository.DeleteInstructorAsync(id);
            Console.WriteLine($"Procecced request to delete instructors {id} from {HttpContext.Connection.RemoteIpAddress}");

            return NoContent();
        }
        else
        {
            try
            {
                var response = await _courseServiceClient.DeleteInstructorFromCourses(id);

                if (response != System.Net.HttpStatusCode.OK)
                {
                    return StatusCode(503, "Remote service temporaly unavaible");
                }

                await _instructorRepository.DeleteInstructorAsync(id);

                Console.WriteLine($"Procecced request to delete instructors {id} from {HttpContext.Connection.RemoteIpAddress}");

                return NoContent();
            }
            catch (BrokenCircuitException)
            {
                _rabbitMQClient.PublishMessage("instructor-delete", id);
                await _instructorRepository.DeleteInstructorAsync(id);
                return StatusCode(200, "Sending request to delete instructor to queue");
            }
        }
    }
}
