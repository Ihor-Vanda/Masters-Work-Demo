using Microsoft.AspNetCore.Mvc;
using TestsManager.Clients;
using TestsManager.DTO;
using TestsManager.Repository;

namespace TestsManager.Controllers;

[ApiController]
[Route("[controller]")]
public class TestsManagerController : ControllerBase
{
    private readonly IRepository _testRepository;
    private readonly CourseServiceClient _courseServiceClient;

    public TestsManagerController(IRepository testRepository, CourseServiceClient courseServiceClient)
    {
        _testRepository = testRepository;
        _courseServiceClient = courseServiceClient;
    }

    //GET: api/test
    [HttpGet]
    public async Task<ActionResult<List<Test>>> GetAllTest()
    {
        var test = await _testRepository.GetAllTest();
        return Ok(test);
    }

    //GET: api/test/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Test>> GetTest(string id)
    {
        var test = await _testRepository.GetTestById(id);

        if (test == null)
        {
            return NotFound();
        }

        return Ok(test);
    }

    //POST: api/test
    [HttpPost]
    public async Task<ActionResult> AddTest([FromBody] TestDTO testDTO)
    {
        if (testDTO == null || testDTO.ReletedCourseId == null)
        {
            return BadRequest("Test can't be null");
        }

        // Перевіряємо, чи існує курс з вказаним ReletedCourseId
        var courseExists = await _courseServiceClient.CheckCourseExists(testDTO.ReletedCourseId);
        if (!courseExists)
        {
            return BadRequest($"Course with id {testDTO.ReletedCourseId} does not exist.");
        }

        var test = new Test();
        test.ReletedCourseId = testDTO.ReletedCourseId;
        test.Questions = testDTO.Questions;

        await _testRepository.AddTest(test);

        return CreatedAtAction(nameof(GetTest), new { id = test.Id }, test);
    }

    //PUT: api/test/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateTest(string id, [FromBody] TestDTO testDTO)
    {
        if (string.IsNullOrWhiteSpace(id) || testDTO == null || testDTO.ReletedCourseId == null)
        {
            return BadRequest("Invalid request");
        }

        var test = await _testRepository.GetTestById(id);
        if (test == null)
        {
            return NotFound();
        }

        // Перевіряємо, чи існує курс з вказаним ReletedCourseId
        var courseExists = await _courseServiceClient.CheckCourseExists(testDTO.ReletedCourseId);
        if (!courseExists)
        {
            return BadRequest($"Course with id {testDTO.ReletedCourseId} does not exist.");
        }

        test.ReletedCourseId = testDTO.ReletedCourseId;
        test.Questions = testDTO.Questions;

        await _testRepository.UpdateTest(id, test);

        return NoContent();
    }

    [HttpPut("{id}/courses")]
    public async Task<ActionResult> ChageCourseToTest(string id, [FromBody] string test)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(test))
        {
            return BadRequest("Invalid request");
        }

        var _test = await _testRepository.GetTestById(test);
        if (_test == null)
        {
            return BadRequest("The test doesn't exist");
        }

        var courseExists = await _courseServiceClient.CheckCourseExists(id);
        if (!courseExists)
        {
            return BadRequest($"Course with id {id} does not exist.");
        }

        _test.ReletedCourseId = id;
        await _testRepository.UpdateTest(test, _test);

        Console.WriteLine($"Procecced request adding course {id} to test {_test} from {HttpContext.Connection.RemoteIpAddress}");
        return Ok(_test);
    }

    //DELETE: api/test/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTest(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid request");
        }

        var test = await _testRepository.GetTestById(id);
        if (test == null)
        {
            return NotFound();
        }

        await _testRepository.DeleteTest(id);

        return NoContent();
    }
}
