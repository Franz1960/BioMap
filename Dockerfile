FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY build /app/
ENTRYPOINT ["dotnet", "BioMap.dll"]
