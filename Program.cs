using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.Core;
using Azure.Identity;

namespace geneva
{
    class Program
    {
        static string region = "EastUS";

        static void Main(string[] args)
        {
            string[] address = GetAzureMonitorServiceTag();
            CreateRouteTableAsync(address).Wait();
        }

        static string[] GetAzureMonitorServiceTag()
        {
            Task<string> t = GetServiceTagsAsync();
            t.Wait();
            string result = t.Result;
            //string result = System.IO.File.ReadAllText("output.json");
            
            var o = JObject.Parse(result);
            var v = o.GetValue("values");

            JToken adresses = v.SelectToken($"[?(@.name == 'AzureMonitor.{region}')].properties.addressPrefixes");
            List<string> values = new List<string>();
            foreach (var address in adresses?.Values())
            {
                values.Add(address.Value<string>());
            }

            return values.ToArray();
        }

        static async Task<string> GetIdentityTokenAsync()
        {
            var cred = new DefaultAzureCredential();
            string[] scopes = new string[]{"https://management.azure.com/.default"};
            var accessToken = await cred.GetTokenAsync(new TokenRequestContext(scopes));
            var token = accessToken.Token;
            return token;
        }

        static async Task<string> GetServiceTagsAsync()
        {
            // https://management.azure.com/subscriptions/{subscriptionId}/providers/Microsoft.Network/locations/{location}/serviceTags?api-version=2020-05-01        
            string json;
            string bearerToken = await GetIdentityTokenAsync();

            using (var client = new HttpClient())
            {
                string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
                string location = region.ToLower();
                string uri = $"https://management.azure.com/subscriptions/{subscriptionId}/providers/Microsoft.Network/locations/{location}/serviceTags?api-version=2020-05-01";
                
                client.DefaultRequestHeaders.Add("Authorization", new string[] {"Bearer " + bearerToken});
                var response = await client.GetAsync(uri);

                json = await response.Content.ReadAsStringAsync();
            }

            return json;
        }

        static async Task CreateRouteTableAsync(string[] addresses)
        {
            string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            string group = "jasondel-aro-rg";
            string routeTableName = "aro-geneva-route";
            RouteTable table = new Azure.ResourceManager.Network.Models.RouteTable();
            table.Location = region.ToLower();
            table.Routes = new List<Route>();
            table.Id = routeTableName;
            
            foreach (string address in addresses)
            {
                Route route = new Route();
                route.Id = FormatName(routeTableName, address);
                route.Name = FormatName(routeTableName, address);
                route.AddressPrefix = address;
                route.NextHopType = RouteNextHopType.Internet;
                table.Routes.Add(route);
            }          
            
            NetworkManagementClient client = new NetworkManagementClient(subscriptionId, new DefaultAzureCredential());
            await client.RouteTables.StartCreateOrUpdateAsync(group, routeTableName, table, new System.Threading.CancellationToken());
        }

        static string FormatName(string routeTableName, string name)
        {
            string formatted = string.Format("{0}-{1:X16}", 
                routeTableName,
                name.GetHashCode());

            return formatted;
        }
    }
}
