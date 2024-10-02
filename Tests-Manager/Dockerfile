# Використовуємо офіційний образ .NET для запуску
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Використовуємо офіційний образ для SDK (для збірки проекту)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копіюємо файл проекту та відновлюємо залежності
COPY ["TestsManager/TestsManager.csproj", "TestsManager/"]
RUN dotnet restore "TestsManager/TestsManager.csproj"

# Копіюємо весь вихідний код і будуємо проект
COPY . .
WORKDIR "/src/TestsManager"
RUN dotnet build "TestsManager.csproj" -c Release -o /app/build

# Публікація проекту
FROM build AS publish
RUN dotnet publish "TestsManager.csproj" -c Release -o /app/publish

# Створення фінального образу
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TestsManager.dll"]
