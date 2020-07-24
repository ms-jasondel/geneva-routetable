
# Create pull secret from ACR
oc create secret docker-registry wthacr-secret \
    --docker-server=wthacr.azurecr.io \
    --docker-username=wthAcr \
    --docker-password='REDACTED' \
    --docker-email='noreply@azurecr.io'

# Copy over Azure credentials
oc get secret azure-credentials --namespace=kube-system --export -o yaml |\
   oc apply -f -

# Deploy job
oc apply -f route-table-creator.yaml
