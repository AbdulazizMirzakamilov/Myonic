using Myonic.Core.Models;

namespace Myonic.Core.Services;

public interface IAiAnalysisService
{
    Task<AiConnectionTestResult> TestConnectionAsync(string apiKey, string modelName, CancellationToken cancellationToken = default);
    Task<AiAnalysisResult> AnalyzeWorkoutSessionAsync(int sessionId, CancellationToken cancellationToken = default);
    Task<AiAnalysisResult> AnalyzeExerciseProgressAsync(int exerciseId, int days = 90, CancellationToken cancellationToken = default);
}
