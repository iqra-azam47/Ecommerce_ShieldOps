FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ShieldOps.csproj", "./"]
RUN dotnet restore "ShieldOps.csproj"
COPY . .
RUN dotnet publish "ShieldOps.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ShieldOps.dll"]