# https://hub.docker.com/_/microsoft-dotnet
#FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
FROM mcr.microsoft.com/dotnet/sdk:5.0.102-ca-patch-buster-slim-amd64 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.sln .
COPY BioMap/*.csproj ./BioMap/
COPY ImageSurveyor/*.csproj ./ImageSurveyor/
RUN dotnet restore

# copy everything else and build app
COPY BioMap/. ./BioMap/
COPY ImageSurveyor/. ./ImageSurveyor/
WORKDIR /source/BioMap
RUN dotnet publish -c debug -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
#RUN apt-get update
#RUN apt-get install openssh-server unzip curl -y
#RUN service ssh start
WORKDIR /var/www/dotnet/biomap/bin
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "BioMap.dll"]
