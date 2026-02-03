using Prometheus;

namespace webapi_docker_demo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var app = builder.Build();

            // Prometheus metrics middleware
            app.UseHttpMetrics();
            app.MapMetrics("/metrics");

            // K8s-style probes
            app.MapGet("/healthz", () => Results.Ok("ok"));
            app.MapGet("/readyz", () => Results.Ok("ready"));

            app.MapGet("/", () =>
            {
                var msg = Environment.GetEnvironmentVariable("MESSAGE") ?? "Hello from .NET 10 in Kubernetes!";
                var pod = Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown-host";
                return Results.Ok(new
                {
                    message = msg,
                    pod,
                    utc = DateTime.UtcNow
                });
            });

            // Endpoint to generate CPU load (useful for HPA demos)
            app.MapGet("/work", (int? ms) =>
            {
                var durationMs = Math.Clamp(ms ?? 250, 1, 5000);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < durationMs)
                {
                    // burn some CPU
                    _ = Math.Sqrt(sw.ElapsedMilliseconds * 123.456);
                }
                return Results.Ok(new { workedMs = durationMs });
            });

            app.Run();
        }
    }
}
