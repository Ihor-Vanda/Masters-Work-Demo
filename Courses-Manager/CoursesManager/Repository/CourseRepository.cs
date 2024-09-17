using CoursesManager.DTO;

namespace CoursesManager.Repository
{
    public class CourseRepository : IRepository
    {
        private readonly MongoDBRepository _mongoDBRepository;

        public CourseRepository(MongoDBRepository mongoDBRepository)
        {
            _mongoDBRepository = mongoDBRepository;
        }

        public async Task<List<Course>> GetAllCoursesAsync()
        {
            return await _mongoDBRepository.GetAllCoursesAsync();
        }

        public async Task<Course?> GetCourseByIdAsync(string id)
        {
            return await _mongoDBRepository.GetCourseByIdAsync(id);
        }

        public async Task CreateCourseAsync(Course course)
        {
            await _mongoDBRepository.CreateCourseAsync(course);
        }

        public async Task UpdateCourseAsync(string id, Course updatedCourse)
        {
            await _mongoDBRepository.UpdateCourseAsync(id, updatedCourse);
        }

        public async Task DeleteCourseAsync(string id)
        {
            await _mongoDBRepository.DeleteCourseAsync(id);
        }
    }
}
