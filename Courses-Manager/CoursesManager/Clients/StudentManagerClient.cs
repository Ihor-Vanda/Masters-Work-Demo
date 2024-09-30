using System.Net;

namespace CoursesManager.Clients;

public class StudentManagerClient
{
    private readonly HttpClient _httpClient;

    public StudentManagerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HttpStatusCode> ModifyStudentToCourse(string courseId, List<string> requestBody)
    {
        // Сериалізація тіла запиту у формат JSON
        var jsonContent = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PutAsync($"http://localhost:5164/StudentManager/{courseId}/courses", jsonContent);

        Console.WriteLine(response.StatusCode);
        return response.StatusCode;
    }
}