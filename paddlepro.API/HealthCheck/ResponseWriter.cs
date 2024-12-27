using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace paddlepro.API.HealthCheck;

public static class HealthCheckWriter
{
    public static async Task ResponseWriter(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            Status = report.Status.ToString(),
            Duration = report.TotalDuration,
            Services = report.Entries.Select(e => new
            {
                Service = e.Key,
                Status = e.Value.Status.ToString(),
                Duration = e.Value.Duration,
                Description = e.Value.Description,
                Data = e.Value.Data
            })
        };

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            response,
            new JsonSerializerOptions { WriteIndented = true }
            );
    }
}
