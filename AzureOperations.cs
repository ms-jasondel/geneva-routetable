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

        private string _bearerToken = null;

        public AzureOperations(string region, string subscription) 
        {
            Region = region;
            Subscription = subscription;
        }

        /// <summary>
        /// Queries Azure REST API to get a list of IPs represented by the AzureMonitor ServiceTag for the specified region.
        /// </summary>
        public async Task<string[]> GetAzureMonitorIPs()
        {
            // https://management.azure.com/subscriptions/{subscriptionId}/providers/Microsoft.Network/locations/{location}/serviceTags?api-version=2020-05-01        
            string uri = $"{ManagementUri}/subscriptions/{Subscription}/providers/Microsoft.Network/locations/{Region}/serviceTags?api-version=2020-05-01";

            string response = await GetAzureResponseAsync(uri);

            var o = JObject.Parse(response);
            var v = o.GetValue("values");
            string regionName = await GetRegionNameAsync();
            string jQuery = $"[?(@.name == 'AzureMonitor.{regionName}')].properties.addressPrefixes";
            JToken adresses = v.SelectToken(jQuery);
            IEnumerable<string> values = adresses?.Values().Values<string>(); 
  
            return values.ToArray();
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
        public async Task CreateRouteTableAsync(string group, string routeTableName, string[] addresses, string virtualFirewallIp)
        {
            RouteTable table = new Azure.ResourceManager.Network.Models.RouteTable();
            table.Location = Region.ToLower();
            table.Routes = new List<Route>();
            table.Id = routeTableName;

            // Add subnet.
            // table.Subnets = new List<Subnet>();
            // table.Subnets.Add(new Subnet() {  })
            
            // Add default route.
            Route defaultRoute = new Route();
            defaultRoute.Id = "default";
            defaultRoute.Name = "default";
            defaultRoute.AddressPrefix = "0.0.0.0/0";
            defaultRoute.NextHopIpAddress = virtualFirewallIp;
            defaultRoute.NextHopType = RouteNextHopType.VirtualAppliance;
            table.Routes.Add(defaultRoute);

            // Add route for each of the IPs represented by the ServiceTag.
            foreach (string address in addresses)
            {
                Route route = new Route();
                route.Id = FormatName(routeTableName, address);
                route.Name = FormatName(routeTableName, address);
                route.AddressPrefix = address;
                route.NextHopType = RouteNextHopType.Internet;
                table.Routes.Add(route);
            }          
            
            NetworkManagementClient client = new NetworkManagementClient(Subscription, new DefaultAzureCredential());
            await client.RouteTables.StartCreateOrUpdateAsync(group, routeTableName, table, new System.Threading.CancellationToken());
        }

        /// <summary>
        /// Helper method to come up with route names.
        /// </summary>
        private string FormatName(string routeTableName, string name)
        {
            string formatted = string.Format("{0}-{1:X16}", 
                routeTableName,
                name.GetHashCode());

            return formatted;
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
            }

            return value;            
        }

        /// <summary>
        /// Authenticates to Azure and gets a bearer token.
        /// </summary>
        private async Task<string> GetIdentityTokenAsync()
        {
            Console.WriteLine($"Attempting to authenticate.");
            var cred = new DefaultAzureCredential();
            string[] scopes = new string[]{ ManagementUri + "/.default"};
            var accessToken = await cred.GetTokenAsync(new TokenRequestContext(scopes));
            var token = accessToken.Token;
            
            return token;
        }                
    }
}