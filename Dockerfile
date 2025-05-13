# syntax=docker/dockerfile:1
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG TARGETARCH
WORKDIR /src

RUN apt-get update && apt install gcc --yes

COPY src .

WORKDIR /src

ARG TARGETPLATFORM
ARG BUILDPLATFORM
#RUN echo "I am running on $BUILDPLATFORM, building for $TARGETPLATFORM and $TARGETARCH" > /log

RUN dotnet restore Service/Service.csproj --arch $TARGETARCH
RUN dotnet publish Service/Service.csproj \
    --no-restore \
    --configuration Release \
    --output /app \
    --self-contained true \
    /p:NoWarn=RS0041 \
    --arch $TARGETARCH \
    -p:SKIP_OPENAPI_GENERATION=true
    

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS app

LABEL vendor="Innago"
LABEL com.innago.image="Innago.HeapService"

RUN apk update && apk upgrade

RUN addgroup --gid 10001 notroot \
    && adduser --uid 10001 --ingroup notroot notroot --disabled-password --no-create-home

WORKDIR /app
COPY --from=build /app .

USER notroot:notroot
ENV ASPNETCORE_URLS="http://*:8080"
ENV DOTNET_EnableDiagnostics=1
ENV DOTNET_EnableDiagnostics_IPC=0
ENV DOTNET_EnableDiagnostics_Debugger=0
ENV DOTNET_EnableDiagnostics_Profiler=1
ENV ASPNETCORE_HOSTBUILDER_RELOADCONFIGONCHANGE="false"

ENTRYPOINT ["dotnet","Innago.Shared.HeapService.dll"]
