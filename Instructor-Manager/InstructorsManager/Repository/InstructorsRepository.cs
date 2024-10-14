using MongoDB.Driver;

namespace InstructorsManager.Repository;

public class InstructorRepository : IRepository
{
    private readonly MongoDBRepository _mongoDBRepository;

    public InstructorRepository(MongoDBRepository mongoDBRepository)
    {
        _mongoDBRepository = mongoDBRepository;
    }

    public async Task<List<Instructor>> GetInstructorsAsync()
    {
        return await _mongoDBRepository.GetInstructorsAsync();
    }

    public async Task<Instructor> GetInstructorByIdAsync(string id)
    {
        return await _mongoDBRepository.GetInstructorByIdAsync(id);
    }

    public async Task AddInstructorAsync(Instructor instructor)
    {
        await _mongoDBRepository.AddInstructorAsync(instructor);
    }

    public async Task<UpdateResult> UpdateAsync(string id, UpdateDefinition<Instructor> updateDefinition)
    {
        return await _mongoDBRepository.UpdateAsync(id, updateDefinition);
    }

    public async Task<UpdateResult> UpdateInstructorAsync(string id, Instructor instructor)
    {
        return await _mongoDBRepository.UpdateInstructorAsync(id, instructor);
    }

    public async Task<UpdateResult> AddCourseAsync(string instructorId, string courseId)
    {
        return await _mongoDBRepository.AddCourseAsync(instructorId, courseId);
    }

    public async Task<UpdateResult> DeleteCourseAsync(string instructorId, string courseId)
    {
        return await _mongoDBRepository.DeleteCourseAsync(instructorId, courseId);
    }

    public async Task<DeleteResult> DeleteInstructorAsync(string id)
    {
        return await _mongoDBRepository.DeleteInstructorAsync(id);
    }
}