FROM mcr.microsoft.com/dotnet/sdk:10.0.403-noble@sha256:de4fe90a98845d75910b9769662465c84de1a934665c431279d1382b2bddc02b AS build
WORKDIR /source
# The ignore file excludes local secrets and generated output from this context.
COPY . .

FROM build AS api-publish
RUN dotnet restore src/OrderInventory.Api/OrderInventory.Api.csproj \
    --locked-mode --disable-parallel -m:1 --nologo
RUN dotnet publish src/OrderInventory.Api/OrderInventory.Api.csproj \
    --configuration Release --no-restore --no-self-contained \
    -m:1 -p:UseSharedCompilation=false -p:UseAppHost=false --output /publish/api --nologo

FROM build AS migrator-publish
RUN dotnet restore tools/OrderInventory.DbMigrator/OrderInventory.DbMigrator.csproj \
    --locked-mode --disable-parallel -m:1 --nologo
RUN dotnet publish tools/OrderInventory.DbMigrator/OrderInventory.DbMigrator.csproj \
    --configuration Release --no-restore --no-self-contained \
    -m:1 -p:UseSharedCompilation=false -p:UseAppHost=false --output /publish/migrator --nologo

FROM mcr.microsoft.com/dotnet/aspnet:10.0.14-noble@sha256:de0ee358cdd672f7017a003bed21e9ac9ed48a27c922a59b18175b6e82fa7fee AS runtime
WORKDIR /app
USER app

FROM runtime AS migrator
COPY --from=migrator-publish /publish/migrator .
ENTRYPOINT ["dotnet", "OrderInventory.DbMigrator.dll"]

FROM runtime AS api
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=api-publish /publish/api .
HEALTHCHECK --interval=10s --timeout=5s --start-period=20s --retries=12 \
    CMD ["dotnet", "OrderInventory.Api.dll", "--healthcheck"]
ENTRYPOINT ["dotnet", "OrderInventory.Api.dll"]
