using TestsManager.DTO;

namespace TestsManager.Repository;

public interface IRepository
{

    Task<List<Test>> GetAllTest();

    Task<Test> GetTestById(string id);

    Task AddTest(Test test);

    Task UpdateTest(string id, Test test);

    Task DeleteTest(string id);
}