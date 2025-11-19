FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj(s) and restore
COPY *.sln .
COPY *.csproj .
RUN dotnet restore --no-cache

# copy everything else and publish
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Let Render provide the PORT environment variable
ENV ASPNETCORE_URLS=http://+:${PORT:-10000}

EXPOSE 10000
ENTRYPOINT ["dotnet", "login.dll"]
