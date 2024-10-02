namespace InstructorsManager.Clients;

public class CourseServiceClient
{
    private readonly HttpClient _httpClient;

    public CourseServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> CheckCourseExists(string courseId)
    {
        var response = await _httpClient.GetAsync($"http://localhost:5001/CoursesManager/{courseId}");
        Console.WriteLine(response.StatusCode);
        return response.IsSuccessStatusCode; // Повертаємо true, якщо курс існує
    }
}