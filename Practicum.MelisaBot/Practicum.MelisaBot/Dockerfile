# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081



# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . .
RUN dotnet restore "./Practicum.MelisaBot/Practicum.MelisaBot.csproj"  -r linux-x64
WORKDIR "/src/."
RUN dotnet build "./Practicum.MelisaBot/Practicum.MelisaBot.csproj" -c $BUILD_CONFIGURATION  -r linux-x64 -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Practicum.MelisaBot/Practicum.MelisaBot.csproj" -c $BUILD_CONFIGURATION -r linux-x64 --self-contained false -o /app/publish /p:UseAppHost=false



# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final

USER root
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgomp1 \
    && rm -rf /var/lib/apt/lists/*
USER app

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Practicum.MelisaBot.dll"]