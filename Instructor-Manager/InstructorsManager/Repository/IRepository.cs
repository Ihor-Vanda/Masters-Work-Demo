namespace InstructorsManager.Repository;

public interface IRepository
{
    Task<List<Instructor>> GetAllInstructors();

    Task<Instructor> GetInstructorById(string id);

    Task AddInstructor(Instructor instructor);

    Task UpdateInstructor(string id, Instructor instructor);

    Task DeleteInstructor(string id);
}