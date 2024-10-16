# Використовуємо офіційний образ .NET для запуску
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Використовуємо офіційний образ для SDK (для збірки проекту)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копіюємо тільки файл проекту та csproj файли для оптимізації кешування
COPY ["CoursesManager/CoursesManager.csproj", "CoursesManager/"]

# Копіюємо бібліотеку DLL до контейнера
COPY CoursesManager/lib/ModifiedCB.dll ./lib/

# Виконуємо відновлення залежностей
RUN dotnet restore "CoursesManager/CoursesManager.csproj"

# Тепер копіюємо весь вихідний код
COPY . .

# Збираємо проект
WORKDIR "/src/CoursesManager"
RUN dotnet build "CoursesManager.csproj" -c Release -o /app/build

# Публікація проекту
FROM build AS publish
RUN dotnet publish "CoursesManager.csproj" -c Release -o /app/publish

# Створення фінального образу
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoursesManager.dll"]
