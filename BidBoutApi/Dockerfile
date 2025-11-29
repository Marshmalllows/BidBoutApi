FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

COPY *.sln .
COPY BidBoutApi/*.csproj ./BidBoutApi/

RUN dotnet restore

COPY BidBoutApi/. ./BidBoutApi/

WORKDIR /source/BidBoutApi
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BidBoutApi.dll"]
