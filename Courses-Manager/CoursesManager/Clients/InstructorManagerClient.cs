using System.Net;

namespace CoursesManager.Clients;

public class InstructorManagerClient
{
    private readonly HttpClient _httpClient;

    public InstructorManagerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HttpStatusCode> ModifyInstructorToCourse(string courseId, List<string> requestBody)
    {
        // Сериалізація тіла запиту у формат JSON
        var jsonContent = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PutAsync($"http://localhost:5217/InstructorManager/{courseId}/courses", jsonContent);

        Console.WriteLine(response.StatusCode);

        return response.StatusCode;
    }
}