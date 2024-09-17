using CoursesManager.DTO;

namespace CoursesManager.Repository
{
    public interface IRepository
    {
        Task<List<Course>> GetAllCoursesAsync();
        Task<Course?> GetCourseByIdAsync(string id);
        Task CreateCourseAsync(Course course);
        Task UpdateCourseAsync(string id, Course updatedCourse);
        Task DeleteCourseAsync(string id);
    }
}
