
# Create pull secret from ACR
# NOT necesary if pulling image from dockerhub.
# oc create secret docker-registry wthacr-secret \
#     --docker-server=wthacr.azurecr.io \
#     --docker-username=wthAcr \
#     --docker-password='REDACTED' \
#     --docker-email='noreply@azurecr.io'

# Create project if it does not exist
oc new-project geneva-route-table
# OR Switch to project if it already exists.
oc project geneva-route-table

# Copy over Azure credentials
oc get secret azure-credentials --namespace=kube-system --export -o yaml |\
   oc apply -f -

# Deploy job
oc apply -f route-table-creator.yaml
