<p align="center">
  <img height="100" src="https://github.com/pawelptak/clip-yt/assets/52631916/5f6176d5-a57d-4095-bd4d-9f6405b457c3">
</p>

# ClipYT
[![Build](https://github.com/pawelptak/clip-yt/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/pawelptak/clip-yt/actions/workflows/build.yml)

An ASP.NET Core MVC web application for creating clips from YouTube videos.

## Getting Started
### Windows
- Download yt-dlp and FFmpeg [[yt-dlp Download]](https://github.com/yt-dlp/yt-dlp/releases/latest) [[FFmpeg Download]](https://ffmpeg.org/download.html) and put them into the `Utilites` directory.
- The default path configurations can be changed in `appsettings.json`.
- For the "Extract stems" option to work you need Python 3.9, FFmpeg and FFprobe installed and added to Path. Then install spleeter using the following commands:
  ```
  pip install spleeter
  pip install numpy==1.26.4
  ```
  Finally, add the path to you Python.exe in `appsettings.json`.
- Build and run the solution.


### Linux
- Download and install yt-dlp and FFmpeg on your system.
- Replace the contents of `appsettings.json` with the contents of `appsettings.Raspberrypi.json`.
- For the "Extract stems" option to work you need to install spleeter using the following command:
  ```
  pip install spleeter
  pip install numpy==1.26.4
  ```
- Build and run the solution.

### Linux Docker
- Build and run from Visual Studio using the Dockerfile.

## Usage
- Paste the URL address of a YouTube video.
- Select the desired output file format.
- Select the start and end timestamp if you'd like to download a section of the whole video.
- Press the "Start" button to start processing and download the file.

 <img height="500" src="https://github.com/user-attachments/assets/58f322d0-e635-4a62-ad96-0bc4d92a0cc0">


## Credits
Special thanks to the creators of [FFmpeg](https://ffmpeg.org/) and [yt-dlp](https://github.com/yt-dlp/yt-dlp) :).
