# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy and restore project
COPY WifiLocatorWeb/*.csproj ./WifiLocatorWeb/
RUN dotnet restore ./WifiLocatorWeb/WifiLocatorWeb.csproj

# Copy full source
COPY WifiLocatorWeb/ ./WifiLocatorWeb/

# Publish
WORKDIR /app/WifiLocatorWeb
RUN dotnet publish -c Release -o /app/out

# Runtime stage with Nginx
FROM nginx:alpine
COPY --from=build /app/out/wwwroot /usr/share/nginx/html
COPY ./WifiLocatorWeb/wwwroot/js /usr/share/nginx/html/js
COPY nginx.conf /etc/nginx/nginx.conf