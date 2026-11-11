# Build context is the backend/ folder:  docker build -f ParkEase.Api/Dockerfile -t parkease-api .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ParkEase.Api/ParkEase.Api.csproj ParkEase.Api/
RUN dotnet restore ParkEase.Api/ParkEase.Api.csproj
COPY ParkEase.Api/ ParkEase.Api/
RUN dotnet publish ParkEase.Api/ParkEase.Api.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ParkEase.Api.dll"]
