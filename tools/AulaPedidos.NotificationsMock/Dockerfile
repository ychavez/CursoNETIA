FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY global.json Directory.Build.props NuGet.Config ./
COPY tools/AulaPedidos.NotificationsMock/ tools/AulaPedidos.NotificationsMock/
RUN dotnet restore tools/AulaPedidos.NotificationsMock/AulaPedidos.NotificationsMock.csproj --locked-mode
RUN dotnet publish tools/AulaPedidos.NotificationsMock/AulaPedidos.NotificationsMock.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN mkdir /data && chown $APP_UID:$APP_UID /data
COPY --from=build /app/publish .
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AulaPedidos.NotificationsMock.dll"]
