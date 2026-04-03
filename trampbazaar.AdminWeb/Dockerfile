FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["trampbazaar.Shared/trampbazaar.Shared.csproj", "trampbazaar.Shared/"]
COPY ["trampbazaar.AdminWeb/trampbazaar.AdminWeb.csproj", "trampbazaar.AdminWeb/"]
RUN dotnet restore "trampbazaar.AdminWeb/trampbazaar.AdminWeb.csproj"

COPY . .
WORKDIR /src/trampbazaar.AdminWeb
RUN dotnet publish "trampbazaar.AdminWeb.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "trampbazaar.AdminWeb.dll"]
