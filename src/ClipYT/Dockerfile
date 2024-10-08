# Use slim base image for ASP.NET Core runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0.8-bookworm-slim-arm64v8 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the project file and restore any dependencies (using a distinct layer for better caching)
COPY ["ClipYT.csproj", "./"]
RUN dotnet nuget locals all --clear
RUN dotnet restore "./ClipYT.csproj"

# Copy the rest of the application source code
COPY . .

# Build the project
RUN dotnet build "./ClipYT.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ClipYT.csproj" -c $BUILD_CONFIGURATION -o /app/publish

# Use the base image as the final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Use the non-root user for security
USER app

ENTRYPOINT ["dotnet", "ClipYT.dll"]