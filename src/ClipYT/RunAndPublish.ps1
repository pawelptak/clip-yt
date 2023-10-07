param (
    [int]$appPort = 7068  # Default app port if not specified
)

# Get the directory where the script is located
$scriptDir = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

# Set the working directory to the script's location
Set-Location -Path $scriptDir

# Start your .NET app using "dotnet run"
Start-Process -NoNewWindow -FilePath "dotnet" -ArgumentList "run"

# Wait for a few seconds to ensure the app is running
Start-Sleep -Seconds 5

# Start ngrok to expose your app to a public address
$ngrokCmd = "http https://localhost:$appPort --host-header=localhost:$appPort"
Start-Process -NoNewWindow -FilePath "ngrok" -ArgumentList $ngrokCmd

# Keep the script running until you manually stop it (Ctrl+C)
Write-Host "Press Ctrl+C to stop ngrok and the .NET app."
Wait-Event -SourceIdentifier PowerShell.ProcessExit

# Clean up by stopping ngrok and the .NET app
Stop-Process -Name "ngrok" -Force
Stop-Process -Name "dotnet" -Force
