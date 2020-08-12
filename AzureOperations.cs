using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.Core;
using Azure.Identity;

namespace GenevaServiceTag
{
    public class AzureOperations
    {
        public const string ManagementUri = "https://management.azure.com";
        public string Region;
        public string Subscription;
        private TokenCredential _credential;
        private string _bearerToken;
        private IEnumerable<ServiceTagInformation> _serviceTags;

        public AzureOperations(string region, string subscription) 
        {
            Region = region;
            Subscription = subscription;
            _credential = new DefaultAzureCredential();
        }

        /// <summary>
        /// Queries Azure REST API to get a list of IPs represented by the AzureMonitor ServiceTag for the specified region.
        /// </summary>
        public async Task<IEnumerable<string>> GetAzureMonitorIPs(string regionName)
        {
            IEnumerable<string> addresses = await GetServiceTagsAsync("AzureMonitor", regionName);
            return addresses;
        }

        public async Task<IEnumerable<string>> GetAzureStorageIPs(string regionName)
        {
            IEnumerable<string> addresses = await GetServiceTagsAsync("Storage", regionName);
            return addresses;
        }

        public async Task<IEnumerable<string>> GetEventHubIPs(string regionName)
        {
            IEnumerable<string> addresses = await GetServiceTagsAsync("EventHub", regionName);
            return addresses;
        }

        public async Task<IEnumerable<string>> GetResourceManagerIPs(string regionName)
        {
            IEnumerable<string> addresses = await GetServiceTagsAsync("AzureResourceManager", null);
            addresses = addresses.Concat(await GetServiceTagsAsync("AzureResourceManager", regionName));
            return addresses;
        }

        /// <summary>
        /// Validates region is equal to the region name as returned from a list of all available regions.
        /// Formats the Display Name for the region, removing all spaces to match that of AzureMonitor.<RegionName> syntax
        /// found in the ServiceTag list.
        /// </summary>
        public async Task<string> GetRegionNameAsync()
        {
            Console.WriteLine($"Getting Azure Locations.");
            var locations = await GetLocationsAsync();

            var azureRegion = locations.Find(r => r.Key == Region);
            if (azureRegion.Value == null)
                throw new ApplicationException($"Unable to find an Azure region that matches {Region}.  Use region name, i.e. eastus");

            string formatted = azureRegion.Value.Replace(" ", "");
            return formatted;
        }

        /// <summary>
        /// Returns a list of KeyValuePair(s) where Key is Region Name, i.e. eastus and Value is the display name; "East US".
        /// </summary>
        public async Task<List<KeyValuePair<string,string>>> GetLocationsAsync()
        {
            // https://management.azure.com/subscriptions/{subscriptionId}/locations?api-version=2020-01-01           
            string uri = $"{ManagementUri}/subscriptions/{Subscription}/locations?api-version=2020-01-01";
            string response = await GetAzureResponseAsync(uri);

            List<KeyValuePair<string,string>> locations = new List<KeyValuePair<string,string>>();

            var o = JObject.Parse(response);
            var array = o.GetValue("value");
            foreach (var value in array)
            {
                string name = value.SelectToken("name").Value<string>();
                string displayName = value.SelectToken("displayName").Value<string>();
                locations.Add(new KeyValuePair<string, string>(name, displayName));
            }

            return locations;
        }

        /// <summary>
        /// Creates or updates an existing Azure Route Table with a list of IP addresses that will be bound for the Internet as
        /// next hop.
        /// <summary>
        public async Task CreateRouteTableAsync(
                string group, 
                string routeTableName, 
                IEnumerable<string> addresses, 
                string virtualFirewallIp,
                string vnet,
                string[] subnets)
        {            
            RouteTableFactory factory = new RouteTableFactory(
                Region, routeTableName, virtualFirewallIp, addresses);
            RouteTable table = factory.Create();
            table.Id = CreateRouteTableResourceId(group, routeTableName);

            if ( (table.Routes.Count() - 1) <= 0 )
            {
                System.Console.WriteLine("No routes to add.");
                return;
            }

            Console.WriteLine("Adding {0} routes.", table.Routes.Count());
            NetworkManagementClient client = new NetworkManagementClient(Subscription, _credential);
            await client.RouteTables.StartCreateOrUpdateAsync(group, routeTableName, table);

            if (subnets != null)
            {
                System.Console.WriteLine("Getting subnets to associate with route table.");
                AssociateSubnets(table, group, vnet, subnets);
            }
        }

        /// <summary>
        /// Formats the Azure resource id string for the route table.
        /// </summary>
        private string CreateRouteTableResourceId(string group, string routeTableName)
        {
            string resourceId = $"/subscriptions/{Subscription}/resourceGroups/{group}/providers/Microsoft.Network/routeTables/{routeTableName}";
            return resourceId;
        }

        /// <summary>
        /// Lazy load all service tags, and return results of json query.
        /// </summary>
        private async Task<IEnumerable<string>> GetServiceTagsAsync(string service, string regionName)
        {
            string location = regionName?.ToLower() ?? Region.ToLower();

            if (_serviceTags == null)
            {
                NetworkManagementClient client = new NetworkManagementClient(Subscription, _credential);
                var response = await client.ServiceTags.ListAsync(location);

                _serviceTags = response?.Value.Values;
                
                #if DEBUG
                    Azure.Response http = response?.GetRawResponse();
                    if (http != null)
                    {
                        using (var reader = new System.IO.StreamReader(http.ContentStream))
                        {
                            System.Console.WriteLine(http.ReasonPhrase + "\n");
                            System.Console.WriteLine(reader.ReadToEnd());
                        }
                    }
                    else 
                    {
                        System.Console.WriteLine($"Response was empty.");
                    }
                #endif

                if (_serviceTags == null || _serviceTags.Count() <= 0)
                    throw new ApplicationException($"Unable to retrieve service tags for {service} in {regionName}.");
            }

            var tags =_serviceTags.Where(t => 
                t.Properties.Region == location && 
                t.Properties.SystemService == service);

            IEnumerable<string> addresses = tags.SelectMany(t => t.Properties.AddressPrefixes);
            
            return addresses;
        }

        /// <summary>
        /// Helper method for making Azure Management REST API calls.
        /// <summary>
        private async Task<string> GetAzureResponseAsync(string uri)
        {
            if (_bearerToken == null)
                _bearerToken = await GetIdentityTokenAsync();

            string value = null;

            using (var client = new HttpClient())
            {              
                client.DefaultRequestHeaders.Add("Authorization", new string[] {"Bearer " + _bearerToken});
                var response = await client.GetAsync(uri);
                value = await response.Content.ReadAsStringAsync();
                #if DEBUG
                    System.Console.WriteLine($"Using bearer: {_bearerToken}\n\t{uri} returned \n\t{response.StatusCode}:\n\t{value}");
                #endif
            }

            return value;            
        }

        /// <summary>
        /// Authenticates to Azure and gets a bearer token.
        /// </summary>
        private async Task<string> GetIdentityTokenAsync()
        {
            Console.WriteLine($"Attempting to authenticate.");

            DefaultAzureCredential cred = (DefaultAzureCredential)_credential;
            string[] scopes = new string[]{ ManagementUri + "/.default" };
            var accessToken = await cred.GetTokenAsync(new TokenRequestContext(scopes));
            var token = accessToken.Token;
            
            return token;
        }   

        private void AssociateSubnets(RouteTable table, string group, string vnetName, string[] subnetNames)
        {
            NetworkManagementClient client = new NetworkManagementClient(Subscription, 
                new DefaultAzureCredential());
            
            foreach (string name in subnetNames)
            {
                Subnet subnet = client.Subnets.Get(group, vnetName, name)?.Value;
                if (subnet == null)
                {
                    throw new ArgumentException(
                        string.Format("Subnet {0} was not found in {1}.", name, vnetName));
                }

                subnet.RouteTable = table;
                client.Subnets.StartCreateOrUpdate(group, vnetName, name, subnet);
            }
        }
    }         
}