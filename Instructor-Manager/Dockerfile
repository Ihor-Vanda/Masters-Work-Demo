# Використовуємо офіційний образ .NET для запуску
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Використовуємо офіційний образ для SDK (для збірки проекту)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копіюємо файл проекту та відновлюємо залежності
COPY ["InstructorsManager/InstructorsManager.csproj", "InstructorsManager/"]

# Копіюємо бібліотеку DLL до контейнера
COPY InstructorsManager/lib/ModifiedCB.dll ./lib/

RUN dotnet restore "InstructorsManager/InstructorsManager.csproj"

# Копіюємо весь вихідний код і будуємо проект
COPY . .
WORKDIR "/src/InstructorsManager"
RUN dotnet build "InstructorsManager.csproj" -c Release -o /app/build

# Публікація проекту
FROM build AS publish
RUN dotnet publish "InstructorsManager.csproj" -c Release -o /app/publish

# Створення фінального образу
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InstructorsManager.dll"]
