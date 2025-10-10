FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AllowanceTracker/AllowanceTracker.csproj", "AllowanceTracker/"]
RUN dotnet restore "AllowanceTracker/AllowanceTracker.csproj"
COPY src/AllowanceTracker/. AllowanceTracker/
WORKDIR "/src/AllowanceTracker"
RUN dotnet build "AllowanceTracker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AllowanceTracker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AllowanceTracker.dll"]
