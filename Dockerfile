FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["ShieldOps.sln", "./"]
COPY ["ShieldOps/ShieldOps.csproj", "ShieldOps/"]

RUN dotnet restore "ShieldOps/ShieldOps.csproj"

# Copy remaining source code
COPY . .
WORKDIR "/src/ShieldOps"
RUN dotnet build "ShieldOps.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ShieldOps.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ShieldOps.dll"]