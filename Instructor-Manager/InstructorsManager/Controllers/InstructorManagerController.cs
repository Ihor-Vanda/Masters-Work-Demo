using System.Net;
using InstructorsManager.DTO;
using InstructorsManager.Repository;
using Microsoft.AspNetCore.Mvc;
using ModifiedCB;
using ModifiedCB.Settings;
using MongoDB.Bson;

namespace InstructorsManager.Controllers;

[ApiController]
[Route("instructors")]
public class InstructorManagerController : ControllerBase
{
    private readonly IRepository _instructorRepository;

    private readonly ICommunicationStrategy _communicationStrategy;

    public InstructorManagerController(
        IRepository instructorRepository,
        ICommunicationStrategy communicationStrategy)
    {
        _instructorRepository = instructorRepository;
        _communicationStrategy = communicationStrategy;
    }

    //GET: instructors
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Instructor>>> GetAllInstructors()
    {
        ServiceMetrics.IncGetInstructorsRequests();
        using (ServiceMetrics.TrackRequestDuration())
        {
            var instructors = await _instructorRepository.GetInstructorsAsync();

            Console.WriteLine($"Procecced request to get all instructors from{HttpContext.Connection.RemoteIpAddress}");

            return Ok(instructors);
        }
    }

    //GET: instructors/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Instructor>> GetInstructorById(string id)
    {
        ServiceMetrics.IncGetInstructorByIdRequests();
        using (ServiceMetrics.TrackRequestDuration())
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
    }

    //POST: instructors
    [HttpPost]
    public async Task<ActionResult> AddInstructor([FromBody] InstructorDTO instructorDTO)
    {
        ServiceMetrics.IncCreateInstructorRequests();
        using (ServiceMetrics.TrackRequestDuration())
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
    }

    //PUT: instructors
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateInstructor(string id, [FromBody] InstructorDTO instructorDTO)
    {
        ServiceMetrics.IncUpdateInstructorRequests();
        using (ServiceMetrics.TrackRequestDuration())
        {
            if (string.IsNullOrWhiteSpace(id) || instructorDTO == null)
            {
                return BadRequest("Invalid Request");
            }

            if (!ObjectId.TryParse(id, out var _))
            {
                return BadRequest("Invalid id");
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
    }

    //PUT: instructors/{id}/courses
    [HttpPut("courses/{id}/add")]
    public async Task<ActionResult> AddCourseToInstructors(string id, [FromBody] List<string> instructorIds)
    {
        ServiceMetrics.IncAddCourseToInstructorsRequests();
        using (ServiceMetrics.TrackRequestDuration())
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Invalid request");
            }

            if (!ObjectId.TryParse(id, out var _))
            {
                return BadRequest("Invalid id");
            }

            var instructorsList = new List<Instructor>();
            foreach (var instructorId in instructorIds)
            {
                if (!ObjectId.TryParse(instructorId, out var _))
                {
                    return BadRequest("Invalid instructor id");
                }
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
                ArgumentNullException.ThrowIfNull(instructor.Id);
                await _instructorRepository.AddCourseAsync(instructor.Id, id);
            }

            Console.WriteLine($"Processed request adding course {id} to instructors {string.Join(", ", instructorIds)} from {HttpContext.Connection.RemoteIpAddress}");

            return Ok(instructorsList.Select(i => i.Id).ToList());
        }
    }

    //PUT: instructors/courses/{id}/delete
    [HttpPut("courses/{id}/delete")]
    public async Task<ActionResult> DeleteInstructorsFromCourse(string id, [FromBody] List<string> instructorIds)
    {
        ServiceMetrics.IncDeleteInstructorRequests();
        using (ServiceMetrics.TrackRequestDuration())
        {
            if (string.IsNullOrWhiteSpace(id) || instructorIds == null || instructorIds.Count == 0)
            {
                return BadRequest("Invalid request");
            }

            if (!ObjectId.TryParse(id, out var _))
            {
                return BadRequest("Invalid id");
            }

            var instructorsList = new List<Instructor>();
            foreach (var instructorId in instructorIds)
            {
                if (!ObjectId.TryParse(instructorId, out var _))
                {
                    return BadRequest("Invalid id");
                }
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
                ArgumentNullException.ThrowIfNull(instructor.Id);
                await _instructorRepository.DeleteCourseAsync(instructor.Id, id);
            }

            Console.WriteLine($"Processed request deleting course {id} from students {string.Join(", ", instructorIds)} from {HttpContext.Connection.RemoteIpAddress}");

            return Ok(instructorsList.Select(i => i.Id).ToList());
        }
    }

    //DELETE: instructors/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteInstructor(string id)
    {
        ServiceMetrics.IncDeleteInstructorRequests();
        using (ServiceMetrics.TrackRequestDuration())
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Invalid id");
            }

            if (!ObjectId.TryParse(id, out var _))
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

            var settings = new CommunicationSettings
            {
                HttpSettings = new HttpCommunicationSettings
                {
                    Method = HttpMethod.Put,
                    DestinationURL = $"http://courses_manager_service:8080/instructors/{id}",
                    Message = null
                },
                RabbitMqSettings = new RabbitMqCommunicationSettings
                {
                    QueueName = "instructor-delete",
                    Message = id
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

            await _instructorRepository.DeleteInstructorAsync(id);

            Console.WriteLine($"Procecced request to delete instructors {id} from {HttpContext.Connection.RemoteIpAddress}");

            return NoContent();
        }
    }
}