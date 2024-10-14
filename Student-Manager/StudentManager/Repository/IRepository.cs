using MongoDB.Driver;

namespace StudentManager.Repository;

public interface IRepository
{
    Task<List<Student>> GetAllStudents();

    Task<Student> GetStudentByIdAsync(string id);

    Task AddStudentAsync(Student student);

    Task<UpdateResult> UpdateAsync(string id, UpdateDefinition<Student> updateDefinition);

    Task<UpdateResult> UpdateStudentAsync(string id, Student student);

    Task<UpdateResult> AddCourseAsync(string studentId, string courseId);

    Task<UpdateResult> DeleteCourseAsync(string studentId, string courseId);

    Task<DeleteResult> DeleteStudentAsync(string id);
}
