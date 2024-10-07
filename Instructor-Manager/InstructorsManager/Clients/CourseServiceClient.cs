using System.Net;
using System.Net.Sockets;

namespace InstructorsManager.Clients;

public class CourseServiceClient
{
    private readonly HttpClient _httpClient;

    public CourseServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> CheckCourseExists(string id)
    {
        try
        {
            // var response = await _httpClient.GetAsync($"http://localhost:5001/courses/{courseId}");
            var response = await _httpClient.GetAsync($"http://courses_manager_service:8080/courses/{id}");

            Console.WriteLine($"Response from courses-service: {response.StatusCode}");
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException)
        {
            // This happens if the service is unreachable or DNS fails
            Console.WriteLine("Service is unavailable or DNS failure: " + ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            throw; // rethrow if it's an unknown error, it should be handled by Circuit Breaker or other mechanisms
        }
    }

    public async Task<HttpStatusCode> DeleteInstructorFromCourses(string id)
    {
        // var jsonContent = new StringContent(
        //    System.Text.Json.JsonSerializer.Serialize(requestBody),
        //    System.Text.Encoding.UTF8,
        //    "application/json");

        try
        {
            var response = await _httpClient.PutAsync($"http://courses_manager_service:8080/instructors/{id}", null);
            // var response = await _httpClient.PutAsync($"http://localhost:5001/courses/instructor/{id}/delete", jsonContent);

            Console.WriteLine($"Response from courses-service: {response.StatusCode}");

            return response.StatusCode;
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException)
        {
            // This happens if the service is unreachable or DNS fails
            Console.WriteLine("Service is unavailable or DNS failure: " + ex.Message);
            return HttpStatusCode.ServiceUnavailable;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            throw; // rethrow if it's an unknown error, it should be handled by Circuit Breaker or other mechanisms
        }
    }
}