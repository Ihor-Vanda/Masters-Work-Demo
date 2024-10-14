using InstructorsManager.Settings;
using MongoDB.Driver;

namespace InstructorsManager.Repository;

public class MongoDBRepository : IRepository
{
    private readonly IMongoCollection<Instructor> _instructors;

    public MongoDBRepository(MongoDBSettings mongoDBSettings)
    {
        var client = new MongoClient(mongoDBSettings.ConnectionString);
        var database = client.GetDatabase(mongoDBSettings.DatabaseName);
        _instructors = database.GetCollection<Instructor>("Instructors");
    }

    public async Task<List<Instructor>> GetInstructorsAsync()
    {
        return await _instructors.Find(instructor => true).ToListAsync();
    }

    public async Task<Instructor> GetInstructorByIdAsync(string id)
    {
        return await _instructors.Find(instructor => instructor.Id == id).FirstOrDefaultAsync();
    }

    public async Task AddInstructorAsync(Instructor instructor)
    {
        try
        {
            await _instructors.InsertOneAsync(instructor);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during adding new instructor: {ex.Message}");
        }
    }

    public async Task<UpdateResult> UpdateAsync(string id, UpdateDefinition<Instructor> updateDefinition)
    {
        var result = await _instructors.UpdateOneAsync(
            instructor => instructor.Id == id,
            updateDefinition
        );

        if (result.MatchedCount == 0)
        {
            throw new Exception("Failed to update the instructor. It may not exist");
        }

        return result;
    }

    public async Task<UpdateResult> UpdateInstructorAsync(string id, Instructor instructor)
    {
        var updateDefinition = Builders<Instructor>.Update
        .Set(c => c.FirstName, instructor.FirstName)
        .Set(c => c.LastName, instructor.LastName)
        .Set(c => c.Email, instructor.Email)
        .Set(c => c.PhoneNumber, instructor.PhoneNumber)
        .Set(c => c.BirthDate, instructor.BirthDate)
        .Set(c => c.Courses, instructor.Courses);

        return await UpdateAsync(id, updateDefinition);
    }

    public async Task<UpdateResult> AddCourseAsync(string id, string courseId)
    {
        var updateDefinition = Builders<Instructor>.Update.Push(c => c.Courses, courseId);
        return await UpdateAsync(id, updateDefinition);
    }

    public async Task<UpdateResult> DeleteCourseAsync(string id, string courseId)
    {
        var updateDefinition = Builders<Instructor>.Update.Pull(c => c.Courses, courseId);
        return await UpdateAsync(id, updateDefinition);
    }

    public async Task<DeleteResult> DeleteInstructorAsync(string id)
    {
        return await _instructors.DeleteOneAsync(instructor => instructor.Id == id);
    }
}