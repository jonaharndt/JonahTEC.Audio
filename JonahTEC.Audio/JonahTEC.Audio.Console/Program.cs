using JonahTEC.Audio.Transcription.Extensions;
using JonahTEC.Audio.Transcription.Runners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace JonahTEC.Audio.Console
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string? environment = Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT");
            if (string.IsNullOrEmpty(environment))
            {
                environment = "Development";
            }

            IHost host = CreateHostBuilder(args, environment).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    var runner = services.GetRequiredService<WhisperRunner>();

                    Stopwatch stopwatch = Stopwatch.StartNew();

                    await runner.RunAsync();

                    stopwatch.Stop();

                    logger.LogInformation("Execution completed in {Time}", stopwatch.Elapsed.ToString(@"hh\:mm\:ss"));

                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred during execution.");
                }
            }

        }

        private static IHostBuilder CreateHostBuilder(string[] args, string environment)
        {
            return Host.CreateDefaultBuilder()
                .UseEnvironment(environment)
                .UseDefaultServiceProvider((ctx, opt) =>
                {
                    opt.ValidateScopes = true;
                    opt.ValidateOnBuild = true;
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddWhisper(context.Configuration);

                });
        }
    }
}