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

        public async Task<Course> GetCourseByIdAsync(string id)
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
                Console.WriteLine($"Failed to create course: {ex.Message}");
            }
        }

        public async Task<UpdateResult> UpdateAsync(string id, UpdateDefinition<Course> updateDefinition)
        {
            var result = await _courses.UpdateOneAsync(
                course => course.Id == id,
                updateDefinition
            );

            if (result.MatchedCount == 0)
            {
                throw new Exception("Failed to update the course. It may not exist.");
            }

            return result;
        }

        public async Task<UpdateResult> UpdateCourseAsync(string id, Course updatedCourse)
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
                .Set(c => c.Students, updatedCourse.Students);

            return await UpdateAsync(id, updateDefinition);
        }

        public async Task<UpdateResult> RemoveStudentAsync(string courseId, string studentId)
        {
            var updateDefinition = Builders<Course>.Update.Pull(c => c.Students, studentId);
            return await UpdateAsync(courseId, updateDefinition);
        }

        public async Task<UpdateResult> RemoveInstructorAsync(string courseId, string instructorId)
        {
            var updateDefinition = Builders<Course>.Update.Pull(c => c.Instructors, instructorId);
            return await UpdateAsync(courseId, updateDefinition);
        }

        public async Task<DeleteResult> DeleteCourseAsync(string id)
        {
            return await _courses.DeleteOneAsync(course => course.Id == id);
        }
    }
}
