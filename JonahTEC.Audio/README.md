# JonahTEC.Audio

JonahTEC.Audio is a .NET project for audio transcription and processing. It consists of two main components:

- **JonahTEC.Audio.Console**: A console application for running audio transcription tasks.
- **JonahTEC.Audio.Transcription**: A library providing transcription backends, configuration, models, and services.

## Features

- Audio transcription using various backends (e.g., WhisperCpp).
- Configurable via `appsettings.json`.
- Extensible architecture for adding new transcription backends.
- Includes models for transcription results, timestamps, and word hits.

## Project Structure

```
JonahTEC.Audio.sln
JonahTEC.Audio.Console/
  Program.cs
  appsettings.json
JonahTEC.Audio.Transcription/
  Backends/
  Configurations/
  Extensions/
  Interfaces/
  Models/
  Runners/
  Services/
```

## Getting Started

1. **Clone the repository**
2. **Restore NuGet packages**
   ```powershell
   dotnet restore JonahTEC.Audio.sln
   ```
3. **Build the solution**
   ```powershell
   dotnet build JonahTEC.Audio.sln
   ```
4. **Run the console app**
   ```powershell
   dotnet run --project JonahTEC.Audio.Console/JonahTEC.Audio.Console.csproj
   ```

## Configuration

Edit `JonahTEC.Audio.Console/appsettings.json` to configure transcription backends and other settings.

## Example appsettings.json

```json
{
  "Whisper": {
    "ExecutablePath": "PathTo\\whisper-cli.exe",
    "ModelPath": "PathTo\\models\\ggml-base.en.bin",
    "Args": "-ml 1 -oj",
    "ExtraOutputArgs": "-otxt",
    "SkipIfJsonExists": true,
    "OutputDirectory": "C:\\Audio\\Transcripts"
  },
  "App": {
    "ScanRoot": "C:\\AudioToScan",
    "SearchPhrase": "search word",
    "OutputCsv": "C:\\Audio\\Results",
    "CopyDirectory": "C:\\Audio\\Found"
  }
}
```

## Installing Whisper

To use the Whisper backend, you need to install the Whisper CLI executable and download a compatible model file.

### Steps

1. **Download Whisper CLI**

   - Visit the [whisper.cpp GitHub releases page](https://github.com/ggerganov/whisper.cpp/releases).
   - Download the appropriate binary for your operating system (e.g., `whisper-cli.exe` for Windows).
   - Place the executable in a directory of your choice (e.g., `C:\Users\Jonah\source\repos\WavWordScan\whisper-bin-x64\Release`).

2. **Download a Whisper Model**

   - From the same repository or documentation, download a model file such as `ggml-base.en.bin`.
   - Place the model file in a directory (e.g., `C:\Users\Jonah\source\repos\WavWordScan\whisper-bin-x64\Release\models`).

3. **Update appsettings.json**
   - Set the `ExecutablePath` and `ModelPath` in your `appsettings.json` to the locations of the downloaded files.

### Example

```json
"Whisper": {
  "ExecutablePath": "C:\\Path\\To\\whisper-cli.exe",
  "ModelPath": "C:\\Path\\To\\models\\ggml-base.en.bin"
}
```

For more details, see the [whisper.cpp documentation](https://github.com/ggerganov/whisper.cpp).
