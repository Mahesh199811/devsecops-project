FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY DevSecOpsApp.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY --from=build /app/publish .
RUN mkdir -p /app/instance

ENV ASPNETCORE_URLS=http://+:5001
EXPOSE 5001

ENTRYPOINT ["dotnet", "DevSecOpsApp.dll"]
