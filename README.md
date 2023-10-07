<p align="center">
  <img height="100" src="https://github.com/pawelptak/clip-yt/assets/52631916/5f6176d5-a57d-4095-bd4d-9f6405b457c3">
</p>

# ClipYT
[![Build](https://github.com/pawelptak/clip-yt/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/pawelptak/clip-yt/actions/workflows/build.yml)

An ASP.NET Core MVC web application for creating clips from YouTube videos.

## Getting Started
- Download yt-dlp and FFmpeg [[yt-dlp Download]](https://github.com/yt-dlp/yt-dlp/releases/latest) [[FFmpeg Download]](https://ffmpeg.org/download.html) and put them into the `Utilites` directory.
- Build and run the solution using Visual Studio.
- The default path configurations can be changed in `appsettings.json`.

## Usage
- Paste the URL address of a YouTube video.
- Select the desired output file format.
- Select the start and end timestamp if you'd like to download a section of the whole video.
- Press the "Start" button to start processing and download the file.

 <img height="500" src="https://github.com/pawelptak/clip-yt/assets/52631916/b1d956f8-bf3d-4a20-9e57-cbc239e9e090">

 ## Run the app locally and redirect to a public address
1. Create an [ngrok](https://ngrok.com/) account.
2. Install ngrok.
3. Connect your account:
```
ngrok config add-authtoken <your_auth_token>
```
4. Open cmd and go to the ClipYT solution directory.
5. Run the app locally:
```
dotnet run
```
6. Run ngrok:
```
ngrok http https://localhost:<clipyt_port> --host-header="localhost:<clipyt_port>"
```
7. The running app will be redirected to a public web address.

## Credits
Special thanks to the creators of [FFmpeg](https://ffmpeg.org/) and [yt-dlp](https://github.com/yt-dlp/yt-dlp) :).
