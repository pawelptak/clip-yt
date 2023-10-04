#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ClipYT.csproj", "."]
RUN dotnet restore "./ClipYT.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "ClipYT.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ClipYT.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClipYT.dll"]

# Install ngrok
RUN apt-get update && apt-get install -y wget unzip
RUN wget https://bin.equinox.io/c/4VmDzA7iaHb/ngrok-stable-linux-amd64.zip
RUN unzip ngrok-stable-linux-amd64.zip
RUN ./ngrok authtoken <auth_token>