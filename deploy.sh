# Create project if it does not exist
oc new-project geneva-route-table
# OR Switch to project if it already exists.
oc project geneva-route-table

#
# Copy over Azure credentials
#
# NOTE: this is just a cheat, make sure that the service principal has contributor rights
# on the resource group, if it doesn't create another SP which has the appropriate permissions
# and set the following keys; azure_client_id, azure_client_secret in the azure-credentials secret.
#
oc get secret azure-credentials --namespace=kube-system --export -o yaml |\
   oc apply -f -

# Deploy job
oc apply -f route-table-creator.yaml
