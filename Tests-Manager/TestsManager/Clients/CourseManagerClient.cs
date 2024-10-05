using System.Net;

namespace TestsManager.Clients;

public class CourseServiceClient
{
    private readonly HttpClient _httpClient;

    public CourseServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> CheckCourseExists(string id)
    {
        // var response = await _httpClient.GetAsync($"http://localhost:5001/courses/{id}");
        var response = await _httpClient.GetAsync($"http://courses_manager_service:8080/courses/{id}");
        Console.WriteLine(response.StatusCode);
        return response.IsSuccessStatusCode; // Повертаємо true, якщо курс існує
    }

    public async Task<HttpStatusCode> DeleteTestFromCourses(string id, string requestBody)
    {
        var jsonContent = new StringContent(
           System.Text.Json.JsonSerializer.Serialize(requestBody),
           System.Text.Encoding.UTF8,
           "application/json");

        // var response = await _httpClient.PutAsync($"http://localhost:5001/courses/tests/{id}", jsonContent);
        var response = await _httpClient.PutAsync($"http://courses_manager_service:8080/courses/tests/{id}/delete", jsonContent);
        Console.WriteLine(response.StatusCode);
        return response.StatusCode;
    }
}
