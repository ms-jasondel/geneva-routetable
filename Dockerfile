FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

ARG VERSION

COPY . /geneva
WORKDIR /geneva

#Optionally install curl in debug container.
#RUN apt-get update && apt-get install -y curl

RUN dotnet restore && \
    dotnet publish \
        -p:Version=$VERSION -c Release --self-contained true \
        -r linux-x64 -o ./publish

FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1 AS runtime
WORKDIR /geneva

COPY --from=build /geneva/publish /geneva
