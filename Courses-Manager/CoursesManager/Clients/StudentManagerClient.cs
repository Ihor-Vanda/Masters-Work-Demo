using System.Net;
using System.Net.Sockets;

namespace CoursesManager.Clients;

public class StudentManagerClient
{
    private readonly HttpClient _httpClient;

    public StudentManagerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HttpResponseMessage> AddStudentToCourse(string id, List<string> requestBody)
    {
        // Сериалізація тіла запиту у формат JSON
        var jsonContent = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        try
        {
            var response = await _httpClient.PutAsync($"http://students_manager_service:8080/students/courses/{id}/add", jsonContent);
            // var response = await _httpClient.PutAsync($"http://localhost:5002/students/{id}/courses", jsonContent);

            Console.WriteLine($"Response from student-service: {response.StatusCode}");

            return response;
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException)
        {
            // This happens if the service is unreachable or DNS fails
            Console.WriteLine("Service is unavailable or DNS failure: " + ex.Message);
            return new HttpResponseMessage { StatusCode = HttpStatusCode.ServiceUnavailable };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            throw; // rethrow if it's an unknown error, it should be handled by Circuit Breaker or other mechanisms
        }
    }

    public async Task<HttpResponseMessage> DeleteStudentFromCourse(string id, List<string> requestBody)
    {
        var jsonContent = new StringContent(
        System.Text.Json.JsonSerializer.Serialize(requestBody),
        System.Text.Encoding.UTF8,
        "application/json");

        try
        {
            var response = await _httpClient.PutAsync($"http://students_manager_service:8080/students/courses/{id}/delete", jsonContent);
            // var response = await _httpClient.PutAsync($"http://localhost:5002/students/courses/{id}", null);

            Console.WriteLine($"Response from student-service: {response.StatusCode}");

            return response;
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException)
        {
            // This happens if the service is unreachable or DNS fails
            Console.WriteLine("Service is unavailable or DNS failure: " + ex.Message);
            return new HttpResponseMessage { StatusCode = HttpStatusCode.ServiceUnavailable };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            throw; // rethrow if it's an unknown error, it should be handled by Circuit Breaker or other mechanisms
        }
    }
}