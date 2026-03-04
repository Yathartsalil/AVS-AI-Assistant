FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN rm -rf bin obj *.json *.runtimeconfig.* *.deps.*
RUN dotnet publish -c Release -o /publish /p:GenerateAssemblyInfo=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT
CMD ["./AVS"]
