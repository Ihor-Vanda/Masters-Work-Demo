using MongoDB.Driver;
using TestsManager.DTO;
using TestsManager.Settings;

namespace TestsManager.Repository;

public class MongoDBRepository : IRepository
{
    private readonly IMongoCollection<Test> _tests;

    public MongoDBRepository(MongoDBSettings mongoDBSettings)
    {
        var client = new MongoClient(mongoDBSettings.ConnectionString);
        var database = client.GetDatabase(mongoDBSettings.DatabaseName);
        _tests = database.GetCollection<Test>("Tests");
    }

    public async Task AddTest(Test test)
    {
        try
        {
            await _tests.InsertOneAsync(test);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during adding new instructor: {ex.Message}");
        }

    }

    public async Task DeleteTest(string id)
    {
        await _tests.DeleteOneAsync(test => test.Id == id);
    }

    public async Task<List<Test>> GetAllTest()
    {
        return await _tests.Find(test => true).ToListAsync();
    }

    public async Task<Test> GetTestById(string id)
    {
        return await _tests.Find(test => test.Id == id).FirstOrDefaultAsync();
    }

    public async Task UpdateTest(string id, Test test)
    {
        var updatedTest = Builders<Test>.Update
        .Set(c => c.ReletedCourseId, test.ReletedCourseId)
        .Set(c => c.Questions, test.Questions);

        var result = await _tests.UpdateOneAsync(
            test => test.Id == id,
            updatedTest
        );

        if (result.MatchedCount == 0)
        {
            throw new Exception("Failed to update the test. It may not exist");
        }
    }
}