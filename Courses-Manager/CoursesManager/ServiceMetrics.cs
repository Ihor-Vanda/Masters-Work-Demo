using Prometheus;

namespace CoursesManager;

public class ServiceMetrics
{
    private static readonly Counter GetCoursesRequestsCounter = Metrics
        .CreateCounter("courses_get_total", "Total number of GET requests to retrieve all courses.");

    private static readonly Counter GetCourseByIdRequestsCounter = Metrics
        .CreateCounter("course_get_by_id_total", "Total number of GET requests to retrieve a course by ID.");

    private static readonly Counter CreateCourseRequestsCounter = Metrics
        .CreateCounter("course_create_total", "Total number of POST requests to create a new course.");

    private static readonly Counter UpdateCourseRequestsCounter = Metrics
        .CreateCounter("course_update_total", "Total number of PUT requests to update a course.");

    private static readonly Counter DeleteCourseRequestsCounter = Metrics
        .CreateCounter("course_delete_total", "Total number of DELETE requests to delete a course.");

    private static readonly Counter AddStudentsToCourseCounter = Metrics
        .CreateCounter("add_students_total", "Total number of added students requests to course.");

    private static readonly Counter DeleteStudentsFromCourseCounter = Metrics
        .CreateCounter("delete_students_total", "Total number of deleted student requests from course.");

    private static readonly Counter DeleteStudentFromCoursesCounter = Metrics
        .CreateCounter("delete_student_from_courses_total", "Total number of deleted student requests from courses.");

    private static readonly Counter AddInstructorsToCourseCounter = Metrics
        .CreateCounter("add_instructors_total", "Total number of added instructors requests to course.");

    private static readonly Counter DeleteInstructorsFromCourseCounter = Metrics
        .CreateCounter("delete_instructors_total", "Total number of deleted instructors requests from course.");

    private static readonly Counter DeleteInstructorFromCoursesCounter = Metrics
           .CreateCounter("delete_instructor_from_courses_total", "Total number of deleted instructor requests from courses.");

    private static readonly Histogram RequestDuration = Metrics
        .CreateHistogram("course_request_duration_seconds", "Duration of requests for the course service in seconds.");

    public static void IncGetCoursesRequests()
    {
        GetCoursesRequestsCounter.Inc();
    }

    public static void IncGetCourseByIdRequests()
    {
        GetCourseByIdRequestsCounter.Inc();
    }

    public static void IncCreateCourseRequests()
    {
        CreateCourseRequestsCounter.Inc();
    }

    public static void IncUpdateCourseRequests()
    {
        UpdateCourseRequestsCounter.Inc();
    }

    public static void IncDeleteCourseRequests()
    {
        DeleteCourseRequestsCounter.Inc();
    }

    public static void IncAddStudentToCourseRequests()
    {
        AddStudentsToCourseCounter.Inc();
    }

    public static void IncDeleteStudentsFromCourseRequests()
    {
        DeleteStudentsFromCourseCounter.Inc();
    }

    public static void IncDeleteStudentFromCoursesRequests()
    {
        DeleteStudentFromCoursesCounter.Inc();
    }

    public static void IncAddInstructorsToCourseRequests()
    {
        AddInstructorsToCourseCounter.Inc();
    }

    public static void IncDeleteInstructorsFromCourseRequests()
    {
        DeleteInstructorsFromCourseCounter.Inc();
    }

    public static void IncDeleteInstructorFromCoursesRequests()
    {
        DeleteInstructorFromCoursesCounter.Inc();
    }

    public static IDisposable TrackRequestDuration()
    {
        return RequestDuration.NewTimer();
    }
}