using System.Net;

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

        var response = await _httpClient.PutAsync($"http://students_manager_service:8080/students/courses/{id}/add", jsonContent);
        // var response = await _httpClient.PutAsync($"http://localhost:5002/students/{id}/courses", jsonContent);

        Console.WriteLine($"Response from student-service: {response.StatusCode}");

        return response;
    }

    public async Task<HttpResponseMessage> DeleteStudentFromCourse(string id, List<string> requestBody)
    {
        var jsonContent = new StringContent(
        System.Text.Json.JsonSerializer.Serialize(requestBody),
        System.Text.Encoding.UTF8,
        "application/json");

        var response = await _httpClient.PutAsync($"http://students_manager_service:8080/students/courses/{id}/delete", jsonContent);
        // var response = await _httpClient.PutAsync($"http://localhost:5002/students/courses/{id}", null);

        Console.WriteLine($"Response from student-service: {response.StatusCode}");

        return response;
    }
}