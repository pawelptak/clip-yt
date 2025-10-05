<p align="center">
  <img height="100" src="src/ClipYT/wwwroot/images/clipyt_logo_gradient.svg">
</p>

# ClipYT
[![Build](https://github.com/pawelptak/clip-yt/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/pawelptak/clip-yt/actions/workflows/build.yml)

An ASP.NET Core MVC web application for creating clips from YouTube videos.

## Getting Started
### Windows
- Download yt-dlp and FFmpeg [[yt-dlp Download]](https://github.com/yt-dlp/yt-dlp/releases/latest) [[FFmpeg Download]](https://ffmpeg.org/download.html) and put them into the `Utilites` directory.
- The default path configurations can be changed in `appsettings.json`.
- Build and run the solution.

### Linux
- Download and install yt-dlp and FFmpeg on your system.
- Change the path configurations of yt-dlp and FFmpeg in `appsettings.json` to `yt-dlp` and `ffmpeg`.
- Build and run the solution.

### Linux Docker
- Build and run from Visual Studio using the Dockerfile.

## Usage
- Paste the URL address of a YouTube video.
- Select the desired output file format.
- Select the start and end timestamp if you'd like to download a section of the whole video.
- Press the "Start" button to start processing and download the file.

 <img height="500" src="https://github.com/user-attachments/assets/a684d4e1-7a8b-43e5-bcbb-8cdc41863217">


## Credits
Special thanks to the creators of [FFmpeg](https://ffmpeg.org/) and [yt-dlp](https://github.com/yt-dlp/yt-dlp) :).
