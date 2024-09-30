using MongoDB.Driver;
using CoursesManager.DTO;
using CoursesManager.Settings;
using MongoDB.Bson;

namespace CoursesManager.Repository
{
    public class MongoDBRepository : IRepository
    {
        private readonly IMongoCollection<Course> _courses;

        public MongoDBRepository(MongoDBSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _courses = database.GetCollection<Course>("Courses");
        }

        public async Task<List<Course>> GetAllCoursesAsync()
        {
            return await _courses.Find(course => true).ToListAsync();
        }

        public async Task<Course?> GetCourseByIdAsync(string id)
        {
            return await _courses.Find(course => course.Id == id).FirstOrDefaultAsync();
        }

        public async Task CreateCourseAsync(Course course)
        {
            try
            {
                await _courses.InsertOneAsync(course);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка під час додавання курсу: {ex.Message}");
            }
        }

        public async Task UpdateCourseAsync(string id, Course updatedCourse)
        {
            var updateDefinition = Builders<Course>.Update
                .Set(c => c.Title, updatedCourse.Title)
                .Set(c => c.Description, updatedCourse.Description)
                .Set(c => c.CourseCode, updatedCourse.CourseCode)
                .Set(c => c.Language, updatedCourse.Language)
                .Set(c => c.Status, updatedCourse.Status)
                .Set(c => c.StartDate, updatedCourse.StartDate)
                .Set(c => c.EndDate, updatedCourse.EndDate)
                .Set(c => c.MaxStudents, updatedCourse.MaxStudents)
                .Set(c => c.Instructors, updatedCourse.Instructors)
                .Set(c => c.Students, updatedCourse.Students)
                .Set(c => c.Tests, updatedCourse.Tests);

            var result = await _courses.UpdateOneAsync(
                course => course.Id == id,
                updateDefinition
            );

            if (result.ModifiedCount == 0)
            {
                throw new Exception("Failed to update the course. It may not exist.");
            }
        }

        public async Task DeleteCourseAsync(string id)
        {
            await _courses.DeleteOneAsync(course => course.Id == id);
        }
    }
}
