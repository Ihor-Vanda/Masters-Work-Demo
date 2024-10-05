using System.Net;

namespace CoursesManager.Clients;

public class InstructorManagerClient
{
    private readonly HttpClient _httpClient;

    public InstructorManagerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HttpResponseMessage> AddInstructorToCourse(string id, List<string> requestBody)
    {
        // Сериалізація тіла запиту у формат JSON
        var jsonContent = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PutAsync($"http://instructors_manager_service:8080/instructors/courses/{id}/add", jsonContent);
        // var response = await _httpClient.PutAsync($"http://localhost:5003/instructors/{id}/courses", jsonContent);

        Console.WriteLine($"Respose from instructors-service: {response.StatusCode}");

        return response;
    }

    public async Task<HttpResponseMessage> DeleteInstructorFromCourse(string id, List<string> requestBody)
    {
        var jsonContent = new StringContent(
           System.Text.Json.JsonSerializer.Serialize(requestBody),
           System.Text.Encoding.UTF8,
           "application/json");

        // var response = await _httpClient.PutAsync($"http://localhost:5003/instructors/courses/{id}", null);
        var response = await _httpClient.PutAsync($"http://instructors_manager_service:8080/instructors/courses/{id}/delete", jsonContent);

        Console.WriteLine($"Respose from instructors-service: {response.StatusCode}");

        return response;
    }
}