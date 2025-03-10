using System.Net.Http.Json;

namespace DeepfaceTest;

public class EmbeddingGenerator
{
    private readonly HttpClient _client;

    public EmbeddingGenerator()
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5005")
        };
    }

    public async Task<FaceAnalysisResponse> GenerateAsync(string filePath)
    {
        var response = await _client.PostAsJsonAsync("/represent", new
        {
            model_name = "VGG-Face",
            img = $"/img_db/{filePath}"
        });

        response.EnsureSuccessStatusCode();
        var parsedResponse = await response.Content.ReadFromJsonAsync<FaceAnalysisResponse>()
                             ?? throw new InvalidOperationException("Response was not parsed");

        return parsedResponse;
    }

    public record FaceAnalysisResponse(FacialAnalysisResult[] results);

    public record FacialAnalysisResult(
        ReadOnlyMemory<float> embedding,
        double face_confidence,
        FacialArea facial_area
    );

    public record FacialArea(
        int h,
        int w,
        int x,
        int y,
        List<int> left_eye,
        List<int> right_eye
    );
}