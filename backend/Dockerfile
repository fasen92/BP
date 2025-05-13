# --- Build stage ---
    FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
    WORKDIR /src
    COPY . .
    
    # Go into the solution directory
    WORKDIR /src/WifiLocator
    
    # Restore and publish
    RUN dotnet restore WifiLocator.API.csproj
    RUN dotnet publish WifiLocator.API.csproj -c Release -o /app/publish
    
    # Final stage
    FROM mcr.microsoft.com/dotnet/sdk:8.0
    WORKDIR /app
    
    # Copy published app
    COPY --from=build /app/publish .
    
    ENTRYPOINT ["dotnet", "WifiLocator.API.dll"]