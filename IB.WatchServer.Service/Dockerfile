FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine AS base
WORKDIR /app
EXPOSE 80


FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build
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
ENTRYPOINT ["dotnet", "IB.WatchServer.Service.dll"]