FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app
# Copy csproj and restore any dependencies (via NuGet)
COPY *.csproj ./

COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out


# Final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
EXPOSE 8080

# Copy the build artifacts from the build stage to the final stage
COPY --from=build /app/out .

# Set the entry point for the application
ENTRYPOINT ["dotnet", "LabraryApi.dll"]