# About this application

This sample application will pull a list of IP addresses from the AzureMonitor (by RegionName)Service Tag and update or create an Azure Route Table which directs the next hop for this ServiceTag to the Internet.

## Remarks

This code uses the latest Azure.Identity DefaultAzureCredential [https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet] module for authentication.