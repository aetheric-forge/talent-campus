# Build from the repository root, including the initialized runtime submodule.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore src/TalentCampus.Web/TalentCampus.Web.csproj
RUN dotnet publish src/TalentCampus.Web/TalentCampus.Web.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/App_Data /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app /app/App_Data /home/app/.aspnet
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "TalentCampus.Web.dll"]
