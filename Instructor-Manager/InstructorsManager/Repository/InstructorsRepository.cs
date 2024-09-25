namespace InstructorsManager.Repository;

public class InstructorRepository : IRepository
{
    private readonly MongoDBRepository _mongoDBRepository;

    public InstructorRepository(MongoDBRepository mongoDBRepository)
    {
        _mongoDBRepository = mongoDBRepository;
    }

    public async Task<List<Instructor>> GetAllInstructors()
    {
        return await _mongoDBRepository.GetAllInstructors();
    }

    public async Task<Instructor> GetInstructorById(string id)
    {
        return await _mongoDBRepository.GetInstructorById(id);
    }

    public async Task AddInstructor(Instructor instructor)
    {
        await _mongoDBRepository.AddInstructor(instructor);
    }

    public async Task UpdateInstructor(string id, Instructor instructor)
    {
        await _mongoDBRepository.UpdateInstructor(id, instructor);
    }

    public async Task DeleteInstructor(string id)
    {
        await _mongoDBRepository.DeleteInstructor(id);
    }
}