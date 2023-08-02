FROM mcr.microsoft.com/dotnet/sdk:6.0 as build-env
WORKDIR /src

COPY . .
RUN dotnet restore ./src/Moonglade.Web/MoongladePure.Web.csproj --no-cache --configfile nuget.config
RUN dotnet publish ./src/Moonglade.Web/MoongladePure.Web.csproj --no-restore --configuration Release --output /build

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY ./assets/OpenSans-Regular.ttf /usr/share/fonts/OpenSans-Regular.ttf
COPY --from=build-env /build .
EXPOSE 80
ENTRYPOINT ["dotnet","./MoongladePure.Web.dll"]
