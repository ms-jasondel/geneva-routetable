if [ -z "$VERSION" ]
then 
    VERSION=$(git describe --abbrev=1 --tags)
fi
container=jjdelorme/genevaroutetable:v$VERSION

if [ $1 == "debug" ]; then
    target=" --target build "
    dockerrun="dotnet run -- -r eastus -f 10.0.1.4 -g jasondel-aro-rg -v aro-vnet -t aro-route -n worker2-subnet"
fi

#
# Build container
#
echo "Building container."
docker build -t $container \
    --build-arg VERSION=$VERSION \
    --force-rm $target \
    -f ./Dockerfile .

if [ $1 == "debug" ]; then
    docker run --rm --name geneva -it \
        $container $dockerrun    
fi        