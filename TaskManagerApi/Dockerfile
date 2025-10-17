FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY *.sln .
COPY TaskManagerApi/*.csproj ./TaskManagerApi/
RUN dotnet restore

COPY TaskManagerApi/. ./TaskManagerApi/
WORKDIR /app/TaskManagerApi
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/TaskManagerApi/out ./
EXPOSE 5000
ENTRYPOINT ["dotnet", "TaskManagerApi.dll"]
