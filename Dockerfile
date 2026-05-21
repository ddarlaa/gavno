# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем файл решения и .csproj файлы
COPY IceBreakerApp.sln .
COPY API/*.csproj API/
COPY Application/*.csproj Application/
COPY Domain/*.csproj Domain/
COPY Infrastructure/*.csproj Infrastructure/
COPY Migrations/*.csproj Migrations/
COPY Tests/*.csproj Tests/

# Восстанавливаем зависимости
RUN dotnet restore IceBreakerApp.sln

# Копируем весь код
COPY . .

# Публикуем API напрямую
WORKDIR /src/API
RUN dotnet publish API.csproj -c Release -o /app/publish

# Этап runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Устанавливаем curl для healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "API.dll"]