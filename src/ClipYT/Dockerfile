# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0.8-bookworm-slim-arm64v8 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


## Install dependencies and create output directory
#USER root
#RUN apt-get update && apt-get install -y \
    #ffmpeg \
    #python3-pip \
    #&& pip3 install yt-dlp --break-system-packages \
    #&& apt-get clean \
    #&& rm -rf /var/lib/apt/lists/* \
    #&& mkdir -p /app/Output \
    #&& mkdir -p /app/Output/.cache \
    #&& chown -R 1001:1001 /app/Output
#USER 1001
#
## Set the cache directory for yt-dlp
#ENV XDG_CACHE_HOME=/app/Output/.cache

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ClipYT.csproj", "./"]
RUN dotnet restore "./ClipYT.csproj"

COPY . .

RUN dotnet build "./ClipYT.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ClipYT.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClipYT.dll"]