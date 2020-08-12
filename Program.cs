using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace GenevaServiceTag
{
    class Program
    {
        const string DefaultRouteTableName = "Firewall-route";
        
        static void Main(string[] args)
        {
            System.Console.WriteLine("Debug");
            System.Console.ReadLine();
            var createRouteTableCommand = CreateRouteTableCommand();
            createRouteTableCommand.InvokeAsync(args).Wait();
        }

        /// <summary>
        /// Creates or update a route table to route all IPs represented by the Azure Monitor 
        /// Service Tag to the internet.
        /// </summary>
        static async Task CreateOrUpdateRouteTableAsync(
                string group, 
                string table, 
                string firewall, 
                string region, 
                string subscription, 
                string vnet,
                string[] subnets)
        {
            if (table == null)
                table = DefaultRouteTableName;

            if (subscription == null)
                subscription = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

            if (subscription == null)
                throw new ArgumentException("Must provide Azure subscription ID.");

            if (region == null)
                region = Environment.GetEnvironmentVariable("AZURE_REGION");
            
            if (region == null)
                throw new ArgumentException("Must provide Azure region.");

            #if DEBUG
                System.Console.WriteLine($"AZURE_REGION: {region}");
                System.Console.WriteLine($"AZURE_SUBSCRIPTION_ID: {subscription}");
                System.Console.WriteLine($"AZURE_TENANT_ID: {Environment.GetEnvironmentVariable("AZURE_TENANT_ID")}");
                System.Console.WriteLine($"AZURE_CLIENT_ID: {Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")}");
                //System.Console.WriteLine($"AZURE_CLIENT_SECRET: {Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET")}");
            #endif

            AzureOperations azure = new AzureOperations(region, subscription);

            string regionName = await azure.GetRegionNameAsync();

            Console.WriteLine($"Getting Service Tags for geneva in {region} for subscription {subscription}.");
            
            IEnumerable<string> addresses = await azure.GetAzureMonitorIPs(regionName);
            addresses = addresses.Concat(await azure.GetAzureStorageIPs(regionName));
            addresses = addresses.Concat(await azure.GetEventHubIPs(regionName));
            addresses = addresses.Concat(await azure.GetResourceManagerIPs(null));

            Console.WriteLine($"Creating route table {table} in {group} with {addresses.Count()} address prefixes.");
            await azure.CreateRouteTableAsync(group, table, addresses, firewall, vnet, subnets);

            Console.WriteLine("Done");
        }

        /// <summary>
        /// Leverages the System.CommandLine package to parse and validate arguments and build a command.
        /// </summary>
        static RootCommand CreateRouteTableCommand()
        {
            var command = new RootCommand();
            
            var regionOption = new Option(
                new string[] {"-r", "--region"}, 
                "Azure Region abbreviation.");
            regionOption.Argument = new Argument<string>();
            regionOption.Required = false;
            command.AddOption(regionOption);

            var tableOption = new Option(
                new string[] {"-t", "--table"}, 
                "Route Table resource name.");
            tableOption.Argument = new Argument<string>();
            tableOption.Required = false;
            command.AddOption(tableOption);

            var firewallOption = new Option(
                new string[] {"-f", "--firewall"}, 
                "IP Address of the Firewall.");
            firewallOption.Argument = new Argument<string>();
            firewallOption.Required = true;
            command.AddOption(firewallOption);

            var groupOption = new Option(
                new string[] {"-g", "--group"}, 
                "Azure Resource Group to create Route Table.");
            groupOption.Argument = new Argument<string>();
            groupOption.Required = true;
            command.AddOption(groupOption);

            var subscriptionOption = new Option(
                new string[] {"-s", "--subscription"}, 
                "Azure Subscription ID.");
            subscriptionOption.Argument = new Argument<string>();
            subscriptionOption.Required = false;
            command.AddOption(subscriptionOption);

            var vnetOption = new Option(
                new string[] {"-v", "--vnet"}, 
                "Azure vnet name");
            vnetOption.Argument = new Argument<string>();
            vnetOption.Required = true;
            command.AddOption(vnetOption);         

            var subnetsOption = new Option(
                new string[] {"-n", "--subnets"}, 
                "1 or more Azure subnet names, separated by spaces.");
            subnetsOption.Argument = new Argument<string[]>();
            subnetsOption.Required = false;
            command.AddOption(subnetsOption);            

            command.Handler = 
                CommandHandler.Create<string, string, string, string, string, string, string[]>(CreateOrUpdateRouteTableAsync);

            return command;
        }
    }
}
