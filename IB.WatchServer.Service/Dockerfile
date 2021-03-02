FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["IB.WatchServer.Service/IB.WatchServer.Service.csproj", "IB.WatchServer.Service/"]
RUN dotnet restore "IB.WatchServer.Service/IB.WatchServer.Service.csproj"
COPY . .
WORKDIR "/src/IB.WatchServer.Service"
RUN dotnet build "IB.WatchServer.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IB.WatchServer.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IB.WatchServer.dll"]