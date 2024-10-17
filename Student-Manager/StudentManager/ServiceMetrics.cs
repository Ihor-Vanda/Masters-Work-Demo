using Prometheus;

namespace StudentManager;

public class ServiceMetrics
{
    private static readonly Counter GetStudentsRequestsCounter = Metrics
        .CreateCounter("students_get_total", "Total number of GET requests to retrieve all students.");

    private static readonly Counter GetStudentByIdRequestsCounter = Metrics
        .CreateCounter("student_get_by_id_total", "Total number of GET requests to retrieve a student by ID.");

    private static readonly Counter CreateStudentRequestsCounter = Metrics
        .CreateCounter("student_create_total", "Total number of POST requests to create a new student.");

    private static readonly Counter UpdateStudentRequestsCounter = Metrics
        .CreateCounter("student_update_total", "Total number of PUT requests to update a student.");

    private static readonly Counter DeleteStudentRequestsCounter = Metrics
        .CreateCounter("student_delete_total", "Total number of DELETE requests to delete a student.");

    private static readonly Counter AddCourseToStudentsCounter = Metrics
        .CreateCounter("add_course_total", "Total number of added course requests to students.");

    private static readonly Counter DeleteCourseFromStudentsCounter = Metrics
        .CreateCounter("delete_course_total", "Total number of deleted course requests from students.");

    private static readonly Histogram RequestDuration = Metrics
           .CreateHistogram("course_request_duration_seconds", "Duration of requests for the student service in seconds.");

    public static void IncGetStudentsRequests() => GetStudentsRequestsCounter.Inc();
    public static void IncGetStudentByIdRequests() => GetStudentByIdRequestsCounter.Inc();
    public static void IncCreateStudentRequests() => CreateStudentRequestsCounter.Inc();
    public static void IncUpdateStudentRequests() => UpdateStudentRequestsCounter.Inc();
    public static void IncDeleteStudentRequests() => DeleteStudentRequestsCounter.Inc();
    public static void IncAddCourseToStudentsRequests() => AddCourseToStudentsCounter.Inc();
    public static void IncDeleteCourseFromStudentsRequests() => DeleteCourseFromStudentsCounter.Inc();

    public static IDisposable TrackRequestDuration()
    {
        return RequestDuration.NewTimer();
    }

}