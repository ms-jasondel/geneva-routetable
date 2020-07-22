FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

ARG VERSION

COPY . /geneva
WORKDIR /geneva

RUN dotnet restore && \
    dotnet publish \
        -p:Version=$VERSION -c Release --self-contained true \
        -r linux-x64 -o ./bin

FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1 AS runtime
WORKDIR /geneva

COPY --from=build /geneva/bin /geneva
