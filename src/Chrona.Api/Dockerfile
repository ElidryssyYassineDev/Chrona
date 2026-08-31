FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Chrona.Api/Chrona.Api.csproj", "src/Chrona.Api/"]
COPY ["src/Chrona.Shared/Chrona.Shared.csproj", "src/Chrona.Shared/"]
COPY ["src/Chrona.Modules.Workforce/Workforce.csproj", "src/Chrona.Modules.Workforce/"]

RUN dotnet restore "src/Chrona.Api/Chrona.Api.csproj"

COPY . .

WORKDIR "/src/src/Chrona.Api"
RUN dotnet publish "Chrona.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Chrona.Api.dll"]