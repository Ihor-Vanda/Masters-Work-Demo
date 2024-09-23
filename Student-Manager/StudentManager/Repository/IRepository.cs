namespace StudentManager.Repository;

public interface IRepository
{
    Task<List<Student>> GetAllStudents();
    Task<Student?> GetStudentByIdAsync(string id);
    Task AddStudentAsync(Student student);
    Task UpdateStudentAsync(string id, Student student);
    Task DeleteStudentAsync(string id);
}
