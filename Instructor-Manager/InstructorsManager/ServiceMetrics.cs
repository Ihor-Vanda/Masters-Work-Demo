using Prometheus;

namespace InstructorsManager;

public class ServiceMetrics
{
    private static readonly Counter GetInstructorsRequestsCounter = Metrics
        .CreateCounter("instructors_get_total", "Total number of GET requests to retrieve all instructors.");

    private static readonly Counter GetInstructorByIdRequestsCounter = Metrics
        .CreateCounter("instructor_get_by_id_total", "Total number of GET requests to retrieve a instructor by ID.");

    private static readonly Counter CreateInstructorRequestsCounter = Metrics
        .CreateCounter("instructor_create_total", "Total number of POST requests to create a new instructor.");

    private static readonly Counter UpdateInstructorRequestsCounter = Metrics
        .CreateCounter("instructor_update_total", "Total number of PUT requests to update a instructor.");

    private static readonly Counter DeleteInstructorRequestsCounter = Metrics
        .CreateCounter("instructor_delete_total", "Total number of DELETE requests to delete a instructor.");

    private static readonly Counter AddCourseToInstructorsCounter = Metrics
        .CreateCounter("add_course_total", "Total number of added course requests to instructors.");

    private static readonly Counter DeleteCourseFromInstructorsCounter = Metrics
        .CreateCounter("delete_course_total", "Total number of deleted course requests from instructors.");

    private static readonly Histogram RequestDuration = Metrics
           .CreateHistogram("course_request_duration_seconds", "Duration of requests for the instructor service in seconds.");

    public static void IncGetInstructorsRequests() => GetInstructorsRequestsCounter.Inc();
    public static void IncGetInstructorByIdRequests() => GetInstructorByIdRequestsCounter.Inc();
    public static void IncCreateInstructorRequests() => CreateInstructorRequestsCounter.Inc();
    public static void IncUpdateInstructorRequests() => UpdateInstructorRequestsCounter.Inc();
    public static void IncDeleteInstructorRequests() => DeleteInstructorRequestsCounter.Inc();
    public static void IncAddCourseToInstructorsRequests() => AddCourseToInstructorsCounter.Inc();
    public static void IncDeleteCourseFromInstructorsRequests() => DeleteCourseFromInstructorsCounter.Inc();

    public static IDisposable TrackRequestDuration()
    {
        return RequestDuration.NewTimer();
    }

}