#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WebApiWithDocker/WebApiWithDocker.csproj", "WebApiWithDocker/"]
RUN dotnet restore "WebApiWithDocker/WebApiWithDocker.csproj"
COPY . .
WORKDIR "/src/WebApiWithDocker"
RUN dotnet build "WebApiWithDocker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebApiWithDocker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApiWithDocker.dll", "--launch-profile Docker"]