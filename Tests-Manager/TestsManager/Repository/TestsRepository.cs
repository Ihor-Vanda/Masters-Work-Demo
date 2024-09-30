
namespace TestsManager.Repository;

public class TestsRepository : IRepository
{
    public readonly MongoDBRepository _test;

    public TestsRepository(MongoDBRepository test)
    {
        _test = test;
    }

    public async Task AddTest(Test test)
    {
        await _test.AddTest(test);
    }

    public async Task DeleteTest(string id)
    {
        await _test.DeleteTest(id);
    }

    public async Task<List<Test>> GetAllTest()
    {
        return await _test.GetAllTest();
    }

    public async Task<Test> GetTestById(string id)
    {
        return await _test.GetTestById(id);
    }

    public async Task UpdateTest(string id, Test test)
    {
        await _test.UpdateTest(id, test);
    }
}