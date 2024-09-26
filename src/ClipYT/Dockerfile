# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:6.0.33-focal-arm64v8 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Install dependencies and create output directory
USER root
RUN apt-get update && apt-get install -y \
    #ffmpeg \
    python3.8 \
    python3.8-venv \
    python3-pip \
    pkg-config \
    libhdf5-dev 
    #python3-venv \
    #python3-setuptools \
    #python3-dev \
    #build-essential \
    #libffi-dev \
    #libssl-dev \
    #&& pip3 install numpy==1.23.5 --prefer-binary --break-system-packages \
    #&& pip3 install spleeter \
    #&& pip3 install yt-dlp \
    #&& apt-get clean \
    #&& rm -rf /var/lib/apt/lists/* \
    #&& mkdir -p /app/Output \
    #&& mkdir -p /app/Output/.cache \
    #&& chown -R 1001:1001 /app/Output

RUN python3.8 -m venv /app/venv
RUN /app/venv/bin/pip install spleeter==2.3.2
ENV PATH="/app/venv/bin:$PATH"

USER 1001

# Set the cache directory for yt-dlp
ENV XDG_CACHE_HOME=/app/Output/.cache

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ClipYT.csproj", "."]
RUN dotnet restore "./ClipYT.csproj"
COPY . .
WORKDIR "/src/."
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