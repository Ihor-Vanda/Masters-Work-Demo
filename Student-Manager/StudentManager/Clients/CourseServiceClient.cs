using System.Net;

namespace StudentManager.Clients;

public class CourseServiceClient
{
    private readonly HttpClient _httpClient;

    public CourseServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> CheckCourseExists(string id)
    {
        // var response = await _httpClient.GetAsync($"http://localhost:5001/courses/{courseId}");
        var response = await _httpClient.GetAsync($"http://courses_manager_service:8080/courses/{id}");

        Console.WriteLine($"Response from courses-service: {response.StatusCode}");

        return response.IsSuccessStatusCode;
    }

    public async Task<HttpStatusCode> DeleteStudentFromCourses(string id)
    {
        // var jsonContent = new StringContent(
        //    System.Text.Json.JsonSerializer.Serialize(requestBody),
        //    System.Text.Encoding.UTF8,
        //    "application/json");

        var response = await _httpClient.PutAsync($"http://courses_manager_service:8080/students/{id}", null);
        // var response = await _httpClient.PutAsync($"http://localhost:5001/courses/students/{id}/delete", jsonContent);

        Console.WriteLine($"Response from courses-service: {response.StatusCode}");

        return response.StatusCode;
    }
}