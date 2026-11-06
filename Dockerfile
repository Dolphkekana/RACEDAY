FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG PROJECT=src/RaceDay.Web/RaceDay.Web.csproj
WORKDIR /src
COPY . .
RUN dotnet publish "$PROJECT" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
ARG ASSEMBLY_NAME=RaceDay.Web.dll
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    ASSEMBLY_NAME=${ASSEMBLY_NAME}
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "dotnet \"$ASSEMBLY_NAME\""]
