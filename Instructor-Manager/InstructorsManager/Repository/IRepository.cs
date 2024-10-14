using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;

namespace InstructorsManager.Repository;

public interface IRepository
{
    Task<List<Instructor>> GetInstructorsAsync();

    Task<Instructor> GetInstructorByIdAsync(string id);

    Task AddInstructorAsync(Instructor instructor);

    Task<UpdateResult> UpdateAsync(string id, UpdateDefinition<Instructor> updateDefinition);

    Task<UpdateResult> UpdateInstructorAsync(string id, Instructor instructor);

    Task<UpdateResult> AddCourseAsync(string instructorId, string courseId);

    Task<UpdateResult> DeleteCourseAsync(string instructorId, string courseId);

    Task<DeleteResult> DeleteInstructorAsync(string id);
}