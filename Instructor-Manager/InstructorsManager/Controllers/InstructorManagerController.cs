using System.Linq;
using System.Net;
using InstructorsManager.DTO;
using InstructorsManager.Repository;
using Microsoft.AspNetCore.Mvc;
using ModifiedCB;
using ModifiedCB.Settings;
using MongoDB.Bson;
using MongoDB.Driver.Linq;

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
        var start_time = DateTime.Now;
        double end_time;

        var instructors = await _instructorRepository.GetInstructorsAsync();

        end_time = (DateTime.Now - start_time).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(end_time, "GET");

        return Ok(instructors);
    }

    //GET: instructors/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Instructor>> GetInstructorById(string id)
    {
        ServiceMetrics.IncGetInstructorByIdRequests();

        if (!ObjectId.TryParse(id, out var _)) return BadRequest("Invalid id");

        var start_time = DateTime.Now;
        double end_time;

        var instructor = await _instructorRepository.GetInstructorByIdAsync(id);

        if (instructor == null)
        {
            end_time = (DateTime.Now - start_time).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(end_time, "GET/id");
            return NotFound("Instructor not found");
        }

        end_time = (DateTime.Now - start_time).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(end_time, "GET/id");

        return Ok(instructor);

    }

    //POST: instructors
    [HttpPost]
    public async Task<ActionResult> AddInstructor([FromBody] InstructorDTO instructorDTO)
    {
        ServiceMetrics.IncCreateInstructorRequests();
        if (instructorDTO == null) return BadRequest("Instructor can't be null");

        if (!DateTime.TryParseExact(instructorDTO.BirthDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var birthDate))
        {
            return BadRequest("Date format is incorrect. Expected format: yyyy-MM-dd");
        }

        var start_time = DateTime.Now;
        double end_time;

        var instructor = new Instructor
        {
            FirstName = instructorDTO.FirstName,
            LastName = instructorDTO.LastName,
            BirthDate = birthDate,
            PhoneNumber = instructorDTO.PhoneNumber,
            Email = instructorDTO.Email
        };

        await _instructorRepository.AddInstructorAsync(instructor);

        end_time = (DateTime.Now - start_time).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(end_time, "POST");

        return CreatedAtAction(nameof(GetInstructorById), new { id = instructor.Id }, instructor);
    }


    //PUT: instructors
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateInstructor(string id, [FromBody] InstructorDTO instructorDTO)
    {
        ServiceMetrics.IncUpdateInstructorRequests();

        if (string.IsNullOrWhiteSpace(id) || instructorDTO == null) return BadRequest("Invalid Request");
        if (!ObjectId.TryParse(id, out var _)) return BadRequest("Invalid id");

        if (string.IsNullOrWhiteSpace(instructorDTO.FirstName) || string.IsNullOrWhiteSpace(instructorDTO.LastName) || string.IsNullOrWhiteSpace(instructorDTO.BirthDate))
        {
            return BadRequest("Reqired field are empty");
        }

        if (!DateTime.TryParseExact(instructorDTO.BirthDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var birthDate))
        {
            return BadRequest("Date format is invalid. Expected format: yyyy-MM-dd");
        }

        var start_time = DateTime.Now;
        double end_time;

        var instructor = await _instructorRepository.GetInstructorByIdAsync(id);
        if (instructor == null)
        {
            end_time = (DateTime.Now - start_time).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(end_time, "PUT/id");
            return NotFound("The instructor not found");
        }

        instructor.FirstName = instructorDTO.FirstName;
        instructor.LastName = instructorDTO.LastName;
        instructor.BirthDate = birthDate;
        instructor.PhoneNumber = instructorDTO.PhoneNumber;
        instructor.Email = instructorDTO.Email;

        await _instructorRepository.UpdateInstructorAsync(id, instructor);

        end_time = (DateTime.Now - start_time).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(end_time, "PUT/id");

        return NoContent();

    }

    //PUT: instructors/courses/{id}/add
    [HttpPut("courses/{id}/add")]
    public async Task<ActionResult> AddCourseToInstructors(string id, [FromBody] List<string> instructorIds)
    {
        ServiceMetrics.IncAddCourseToInstructorsRequests();

        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out var _)) return BadRequest("Invalid course id");

        var start_time = DateTime.Now;
        double end_time;

        var validInstructorIds = instructorIds
            .Where(instructorId => ObjectId.TryParse(instructorId, out _))
            .ToList();

        if (validInstructorIds.Count != instructorIds.Count)
        {
            end_time = (DateTime.Now - start_time).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(end_time, "PUT/courses/id/add");
            return BadRequest("One or more instructor IDs are invalid");
        }

        var instructorTasks = validInstructorIds.Select(_instructorRepository.GetInstructorByIdAsync);
        var instructorsList = (await Task.WhenAll(instructorTasks))
            .Where(instructor => instructor != null)
            .ToList();

        if (instructorsList.Count == 0)
        {
            end_time = (DateTime.Now - start_time).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(end_time, "PUT/courses/id/add");
            return BadRequest("No valid instructors found");
        }

        var addCourseTasks = instructorsList.Select(instructor =>
            _instructorRepository.AddCourseAsync(instructor.Id, id));

        await Task.WhenAll(addCourseTasks);


        // var instructorsList = new List<Instructor>();
        // foreach (var instructorId in instructorIds)
        // {
        //     if (!ObjectId.TryParse(instructorId, out var _))
        //     {
        //         return BadRequest("Invalid instructor id");
        //     }
        //     var instructor = await _instructorRepository.GetInstructorByIdAsync(instructorId);
        //     if (instructor == null)
        //     {
        //         return BadRequest($"The instructor with id {instructorId} does not exist");
        //     }
        //     instructorsList.Add(instructor);
        // }

        // for (int i = 0; i < instructorsList.Count; i++)
        // {
        //     var instructor = instructorsList[i];
        //     ArgumentNullException.ThrowIfNull(instructor.Id);
        //     await _instructorRepository.AddCourseAsync(instructor.Id, id);
        // }

        end_time = (DateTime.Now - start_time).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(end_time, "PUT/courses/id/add");

        return Ok(instructorsList.Select(i => i.Id).ToList());
    }

    //PUT: instructors/courses/{id}/delete
    [HttpPut("courses/{id}/delete")]
    public async Task<ActionResult> DeleteInstructorsFromCourse(string id, [FromBody] List<string> instructorIds)
    {
        ServiceMetrics.IncDeleteInstructorRequests();

        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out var _)) return BadRequest("Invalid course id");

        var start_time = DateTime.Now;
        double end_time;

        var validInstructorIds = instructorIds
           .Where(instructorId => ObjectId.TryParse(instructorId, out _))
           .ToList();

        if (validInstructorIds.Count != instructorIds.Count)
        {
            end_time = (DateTime.Now - start_time).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(end_time, "PUT/courses/id/delete");
            return BadRequest("One or more instructor IDs are invalid");
        }

        var instructorTasks = validInstructorIds.Select(_instructorRepository.GetInstructorByIdAsync);
        var instructorsList = (await Task.WhenAll(instructorTasks))
            .Where(instructor => instructor != null)
            .ToList();

        if (instructorsList.Count == 0)
        {
            end_time = (DateTime.Now - start_time).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(end_time, "PUT/courses/id/delete");
            return BadRequest("No valid instructors found");
        }

        var deleteCourseTasks = instructorsList.Select(instructor =>
            _instructorRepository.DeleteCourseAsync(instructor.Id, id));

        await Task.WhenAll(deleteCourseTasks);

        // var instructorsList = new List<Instructor>();
        // foreach (var instructorId in instructorIds)
        // {
        //     if (!ObjectId.TryParse(instructorId, out var _))
        //     {
        //         return BadRequest("Invalid id");
        //     }
        //     var instructor = await _instructorRepository.GetInstructorByIdAsync(instructorId);
        //     if (instructor == null)
        //     {
        //         return BadRequest($"The instructor with id {instructorId} does not exist");
        //     }
        //     instructorsList.Add(instructor);
        // }

        // for (int i = 0; i < instructorsList.Count; i++)
        // {
        //     var instructor = instructorsList[i];
        //     ArgumentNullException.ThrowIfNull(instructor.Id);
        //     await _instructorRepository.DeleteCourseAsync(instructor.Id, id);
        // }

        end_time = (DateTime.Now - start_time).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(end_time, "courses/id/delete");

        return Ok(instructorsList.Select(i => i.Id).ToList());
    }

    //DELETE: instructors/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteInstructor(string id)
    {
        ServiceMetrics.IncDeleteInstructorRequests();

        if (string.IsNullOrWhiteSpace(id)) return BadRequest("Invalid id");
        if (!ObjectId.TryParse(id, out var _)) return BadRequest("Invalid id");

        var start_time = DateTime.Now;
        double end_time;

        var instructor = await _instructorRepository.GetInstructorByIdAsync(id);
        if (instructor == null)
        {
            end_time = (DateTime.Now - start_time).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(end_time, "DELETE/id");
            return NotFound("The instructor not foud");
        }

        if (instructor.Courses.Count == 0)
        {
            await _instructorRepository.DeleteInstructorAsync(id);

            end_time = (DateTime.Now - start_time).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(end_time, "DELETE/id");
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
            end_time = (DateTime.Now - start_time).TotalSeconds;
            ServiceMetrics.TrackRequestDuration(end_time, "DELETE/id");
            return StatusCode((int)HttpStatusCode.InternalServerError, $"Error occurred: {ex.Message}");
        }

        await _instructorRepository.DeleteInstructorAsync(id);

        end_time = (DateTime.Now - start_time).TotalSeconds;
        ServiceMetrics.TrackRequestDuration(end_time, "DELETE/id");

        return NoContent();
    }
}
