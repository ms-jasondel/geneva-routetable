if [ -z "$VERSION" ]
then 
    VERSION=$(git describe --abbrev=1 --tags)
fi
container=jjdelorme/genevaroutetable:v$VERSION

#
# Build container
#
echo "Building container."
docker build -t $container \
    --build-arg VERSION=$VERSION \
    --force-rm \
    -f ./Dockerfile .