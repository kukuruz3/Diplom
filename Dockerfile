FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 9080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SanatoriumIS/SanatoriumIS.csproj", "SanatoriumIS/"]
RUN dotnet restore "SanatoriumIS/SanatoriumIS.csproj"
COPY . .
WORKDIR "/src/SanatoriumIS"
RUN dotnet build "SanatoriumIS.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SanatoriumIS.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SanatoriumIS.dll"]
