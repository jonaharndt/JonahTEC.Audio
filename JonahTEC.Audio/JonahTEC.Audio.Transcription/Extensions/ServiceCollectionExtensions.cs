using JonahTEC.Audio.Transcription.Backends;
using JonahTEC.Audio.Transcription.Configurations;
using JonahTEC.Audio.Transcription.Interfaces;
using JonahTEC.Audio.Transcription.Runners;
using JonahTEC.Audio.Transcription.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JonahTEC.Audio.Transcription.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures and registers the Whisper transcription services and related dependencies.
        /// </summary>
        /// <remarks>This method binds configuration sections for Whisper and application settings,
        /// registers them as singletons,  and sets up the necessary services for Whisper-based transcription. It
        /// includes the Whisper runner,  transcription backend, and parsing service.</remarks>
        /// <param name="services">The <see cref="IServiceCollection"/> to which the Whisper services will be added.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> instance used to retrieve application and Whisper-specific settings.</param>
        public static void AddWhisper(this IServiceCollection services, IConfiguration configuration)
        {
            WhisperConfiguration whisperConfiguration = new();
            configuration.GetSection(WhisperConfiguration.SECTION_NAME).Bind(whisperConfiguration);
            services.AddSingleton(whisperConfiguration);

            AppConfiguration appConfiguration = new();
            configuration.GetSection(AppConfiguration.SECTION_NAME).Bind(appConfiguration);
            services.AddSingleton(appConfiguration);

            var backendConfiguration = whisperConfiguration.GetBackendConfiguration();
            backendConfiguration.TargetPhrase = appConfiguration.SearchPhrase;
            services.AddSingleton(backendConfiguration);

            services.AddSingleton<WhisperRunner>();
            services.AddSingleton<ITranscriptionBackend, WhisperCppRunner>();
            services.AddSingleton<ParsingService>();
        }
    }
}