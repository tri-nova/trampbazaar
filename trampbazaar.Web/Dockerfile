FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["trampbazaar.Shared/trampbazaar.Shared.csproj", "trampbazaar.Shared/"]
COPY ["trampbazaar.Web/trampbazaar.Web.csproj", "trampbazaar.Web/"]
RUN dotnet restore "trampbazaar.Web/trampbazaar.Web.csproj"

COPY . .
WORKDIR /src/trampbazaar.Web
RUN dotnet publish "trampbazaar.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "trampbazaar.Web.dll"]
