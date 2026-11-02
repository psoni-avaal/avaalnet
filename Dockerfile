FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY BasicDotNetPostgresWebsite.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BasicDotNetPostgresWebsite.dll"]
