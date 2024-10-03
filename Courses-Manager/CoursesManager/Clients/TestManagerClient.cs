using System.Net;

namespace CoursesManager.Clients;

public class TestManagerClient
{
    private readonly HttpClient _httpClient;

    public TestManagerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HttpStatusCode> ModifyTestToCourse(string courseId, List<string> requestBody)
    {
        // Сериалізація тіла запиту у формат JSON
        var jsonContent = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PutAsync($"http://localhost:5004/tests/{courseId}/courses", jsonContent);

        Console.WriteLine(response.StatusCode);
        return response.StatusCode;
    }

    public async Task<HttpStatusCode> DeleteTestFromCourse(string testId)
    {
        var response = await _httpClient.DeleteAsync($"http://localhost:5004/tests/{testId}");

        Console.WriteLine(response.StatusCode);
        return response.StatusCode;
    }
}