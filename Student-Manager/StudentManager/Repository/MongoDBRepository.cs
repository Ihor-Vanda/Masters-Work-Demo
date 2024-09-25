using MongoDB.Driver;
using StudentManager.Settings;

namespace StudentManager.Repository;

public class MongoDBRepository : IRepository
{
    private readonly IMongoCollection<Student> _students;

    public MongoDBRepository(MongoDBSettings settings)
    {
        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);
        _students = database.GetCollection<Student>("Students");
    }

    public async Task<List<Student>> GetAllStudents()
    {
        return await _students.Find(student => true).ToListAsync();
    }

    public async Task<Student?> GetStudentByIdAsync(string id)
    {
        return await _students.Find(student => student.Id == id).FirstOrDefaultAsync();
    }

    public async Task AddStudentAsync(Student student)
    {
        try
        {
            await _students.InsertOneAsync(student);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during added new student: {ex.Message}");
        }
    }

    public async Task UpdateStudentAsync(string id, Student student)
    {
        var updatedStudent = Builders<Student>.Update
        .Set(c => c.FirstName, student.FirstName)
        .Set(c => c.LastName, student.LastName)
        .Set(c => c.Email, student.Email)
        .Set(c => c.PhoneNumber, student.PhoneNumber)
        .Set(c => c.BirthDate, student.BirthDate)
        .Set(c => c.Courses, student.Courses);

        var result = await _students.UpdateOneAsync(
            student => student.Id == id,
            updatedStudent
        );

        if (result.ModifiedCount == 0)
        {
            throw new Exception("Failed to update the student. It may not exist");
        }
    }

    public async Task DeleteStudentAsync(string id)
    {
        await _students.DeleteOneAsync(student => student.Id == id);
    }
}