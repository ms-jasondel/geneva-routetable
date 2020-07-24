# deploys cronjob
oc get secret azure-credentials --namespace=kube-system --export -o yaml |\
   oc apply -f -

oc apply -f route-table-creator.yaml
