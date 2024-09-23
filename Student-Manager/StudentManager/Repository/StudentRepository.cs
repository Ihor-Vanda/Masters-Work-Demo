namespace StudentManager.Repository;

public class StudentRepository : IRepository
{
    private readonly MongoDBRepository _mongoDBRepository;

    public StudentRepository(MongoDBRepository mongoDBRepository)
    {
        _mongoDBRepository = mongoDBRepository;
    }

    public async Task<List<Student>> GetAllStudents()
    {
        return await _mongoDBRepository.GetAllStudents();
    }

    public async Task<Student?> GetStudentByIdAsync(string id)
    {
        return await _mongoDBRepository.GetStudentByIdAsync(id);
    }

    public async Task AddStudentAsync(Student student)
    {
        await _mongoDBRepository.AddStudentAsync(student);
    }

    public async Task UpdateStudentAsync(string studentId, Student student)
    {
        await _mongoDBRepository.UpdateStudentAsync(studentId, student);
    }

    public async Task DeleteStudentAsync(string studentId)
    {
        await _mongoDBRepository.DeleteStudentAsync(studentId);
    }
}