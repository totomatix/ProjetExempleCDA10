FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5293

ENV ASPNETCORE_URLS=http://+:5293

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["ProjetExempleCDA10.csproj", "./"]
RUN dotnet restore "ProjetExempleCDA10.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "ProjetExempleCDA10.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "ProjetExempleCDA10.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProjetExempleCDA10.dll"]
