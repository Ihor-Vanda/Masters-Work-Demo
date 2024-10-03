using System.Net;

namespace StudentManager.Clients;

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

    public async Task<HttpStatusCode> DeleteStudentFromCourses(string id, List<string> requestBody)
    {
        var jsonContent = new StringContent(
           System.Text.Json.JsonSerializer.Serialize(requestBody),
           System.Text.Encoding.UTF8,
           "application/json");

        var response = await _httpClient.PutAsync($"http://localhost:5001/courses/students/{id}", jsonContent);

        return response.StatusCode;
    }
}