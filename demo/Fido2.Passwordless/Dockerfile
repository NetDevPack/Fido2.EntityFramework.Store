# Build context is the repository root (Directory.Build.props, Directory.Packages.props, global.json):
#   docker build -f demo/Fido2.Passwordless/Dockerfile .

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY ["Directory.Build.props", "Directory.Packages.props", "global.json", "README.MD", "./"]
COPY ["assets/", "assets/"]
COPY ["src/", "src/"]
COPY ["demo/", "demo/"]
RUN dotnet restore "demo/Fido2.Passwordless/Fido2.Passwordless.csproj"
WORKDIR "/repo/demo/Fido2.Passwordless"
RUN dotnet build "Fido2.Passwordless.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Fido2.Passwordless.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fido2.Passwordless.dll"]
