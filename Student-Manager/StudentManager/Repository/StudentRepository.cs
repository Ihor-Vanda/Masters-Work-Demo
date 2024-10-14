using MongoDB.Driver;

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

    public async Task<Student> GetStudentByIdAsync(string id)
    {
        return await _mongoDBRepository.GetStudentByIdAsync(id);
    }

    public async Task AddStudentAsync(Student student)
    {
        await _mongoDBRepository.AddStudentAsync(student);
    }

    public async Task<UpdateResult> UpdateAsync(string id, UpdateDefinition<Student> updateDefinition)
    {
        return await _mongoDBRepository.UpdateAsync(id, updateDefinition);
    }

    public async Task<UpdateResult> UpdateStudentAsync(string studentId, Student student)
    {
        return await _mongoDBRepository.UpdateStudentAsync(studentId, student);
    }

    public async Task<UpdateResult> AddCourseAsync(string studentId, string courseId)
    {
        return await _mongoDBRepository.AddCourseAsync(studentId, courseId);
    }

    public async Task<UpdateResult> DeleteCourseAsync(string studentId, string courseId)
    {
        return await _mongoDBRepository.DeleteCourseAsync(studentId, courseId);
    }

    public async Task<DeleteResult> DeleteStudentAsync(string studentId)
    {
        return await _mongoDBRepository.DeleteStudentAsync(studentId);
    }
}