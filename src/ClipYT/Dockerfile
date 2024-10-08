# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ClipYT.csproj", "."]
RUN dotnet restore "./ClipYT.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./ClipYT.csproj" -c Release -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build-env AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ClipYT.csproj" -c Release -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClipYT.dll"]
