FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["trampbazaar.Shared/trampbazaar.Shared.csproj", "trampbazaar.Shared/"]
COPY ["trampbazaar.Api/trampbazaar.Api.csproj", "trampbazaar.Api/"]
RUN dotnet restore "trampbazaar.Api/trampbazaar.Api.csproj"

COPY . .
WORKDIR /src/trampbazaar.Api
RUN dotnet publish "trampbazaar.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "trampbazaar.Api.dll"]
