# About this application

This sample application will pull a list of IP addresses from the Azure Monitor, Azure Storage, Azure Resource Manager, Azure Event Hubs Service Tags and update or create an Azure Route Table which directs the next hop for this ServiceTag to the Internet.

## Remarks

This code uses the latest Azure.Identity DefaultAzureCredential [https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet] module for authentication.

To use this with environment variables, set the following:

```bash

export AZURE_CLIENT_ID='xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'
export AZURE_CLIENT_SECRET='xxxx'
export AZURE_TENANT_ID='xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'
export AZURE_SUBSCRIPTION_ID='xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'
```

Sample use:

```bash

docker run -it jjdelorme/genevaroutetable:v1.2

./geneva -r eastus -f 10.0.1.4 -g my-resource-group

Getting Azure Locations.
Attempting to authenticate.
Getting Service Tags for geneva in eastus for subscription xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx.
Creating route table Firewall-route in my-resource-group with 373 address prefixes.
Adding 265 routes.
Done
```
