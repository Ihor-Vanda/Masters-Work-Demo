using CoursesManager.DTO;
using MongoDB.Driver;

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

        public async Task<Course> GetCourseByIdAsync(string id)
        {
            return await _mongoDBRepository.GetCourseByIdAsync(id);
        }

        public async Task CreateCourseAsync(Course course)
        {
            await _mongoDBRepository.CreateCourseAsync(course);
        }

        public async Task<UpdateResult> UpdateAsync(string id, UpdateDefinition<Course> updateDefinition)
        {
            return await _mongoDBRepository.UpdateAsync(id, updateDefinition);
        }

        public async Task<UpdateResult> UpdateCourseAsync(string id, Course updatedCourse)
        {
            return await _mongoDBRepository.UpdateCourseAsync(id, updatedCourse);
        }

        public async Task<UpdateResult> RemoveStudentAsync(string courseId, string studentId)
        {
            return await _mongoDBRepository.RemoveStudentAsync(courseId, studentId);
        }

        public async Task<UpdateResult> RemoveInstructorAsync(string courseId, string instructorId)
        {
            return await _mongoDBRepository.RemoveInstructorAsync(courseId, instructorId);
        }

        public async Task<DeleteResult> DeleteCourseAsync(string id)
        {
            return await _mongoDBRepository.DeleteCourseAsync(id);
        }
    }
}
