FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .
WORKDIR "/src/src/UI"
RUN dotnet restore "UI.csproj"
RUN dotnet publish "UI.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

COPY --from=build /app/publish/wwwroot .
COPY src/UI/nginx.conf /etc/nginx/nginx.conf

EXPOSE 80