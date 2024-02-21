FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /bin/Debug/net7.0

# Copy csproj and restore any dependencies (via NuGet)
COPY *.csproj .
RUN dotnet restore

RUN dotnet publish -c release -o /app --no-restore

# Final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
EXPOSE 8080

# Copy the build artifacts from the build stage to the final stage
COPY --from=build /app .

# Set the entry point for the application
ENTRYPOINT ["dotnet", "LabraryApi.dll"]