using System.Net;

namespace TestsManager.Clients;

public class CourseServiceClient
{
    private readonly HttpClient _httpClient;

    public CourseServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> CheckCourseExists(string courseId)
    {
        var response = await _httpClient.GetAsync($"http://localhost:5001/courses/{courseId}");
        Console.WriteLine(response.StatusCode);
        return response.IsSuccessStatusCode; // Повертаємо true, якщо курс існує
    }

    public async Task<HttpStatusCode> DeleteTestFromCourses(string id, string requestBody)
    {
        var jsonContent = new StringContent(
           System.Text.Json.JsonSerializer.Serialize(requestBody),
           System.Text.Encoding.UTF8,
           "application/json");

        var response = await _httpClient.PutAsync($"http://localhost:5001/courses/tests/{id}", jsonContent);
        Console.WriteLine(response.StatusCode);
        return response.StatusCode;
    }
}
