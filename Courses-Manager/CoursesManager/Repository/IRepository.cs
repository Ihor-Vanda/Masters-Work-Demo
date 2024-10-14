using MongoDB.Driver;

namespace CoursesManager.Repository
{
    public interface IRepository
    {
        Task<List<Course>> GetAllCoursesAsync();

        Task<Course> GetCourseByIdAsync(string id);

        Task CreateCourseAsync(Course course);

        Task<UpdateResult> UpdateAsync(string id, UpdateDefinition<Course> updateDefinition);

        Task<UpdateResult> UpdateCourseAsync(string id, Course updatedCourse);

        Task<UpdateResult> RemoveStudentAsync(string courseId, string studentId);

        Task<UpdateResult> RemoveInstructorAsync(string courseId, string instructorId);

        Task<DeleteResult> DeleteCourseAsync(string id);
    }
}
