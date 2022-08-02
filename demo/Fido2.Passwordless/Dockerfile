#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Fido2.Passwordless/Fido2.Passwordless.csproj", "Fido2.Passwordless/"]
RUN dotnet restore "Fido2.Passwordless/Fido2.Passwordless.csproj"
COPY . .
WORKDIR "/src/Fido2.Passwordless"
RUN dotnet build "Fido2.Passwordless.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Fido2.Passwordless.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fido2.Passwordless.dll"]