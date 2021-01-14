# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.sln .
COPY BioMap/*.csproj ./BioMap/
RUN dotnet restore

# copy everything else and build app
COPY BioMap/. ./BioMap/
WORKDIR /source/BioMap
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /var/www/dotnet/biomap/bin
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "BioMap.dll"]