FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json ./
COPY Template.Api/Template.Api.csproj Template.Api/
COPY Template.Data/Template.Data.csproj Template.Data/
COPY Template.Model/Template.Model.csproj Template.Model/
COPY Template.Services/Template.Services.csproj Template.Services/
COPY Template.Common/Template.Common.csproj Template.Common/
RUN dotnet restore Template.Api/Template.Api.csproj
COPY . .
RUN dotnet publish Template.Api/Template.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "Template.Api.dll"]
