using System;
using System.Linq;
using System.Threading.Tasks;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace geneva
{
    class Program
    {
        static void Main(string[] args)
        {
            var createRouteTableCommand = CreateRouteTableCommand();
            createRouteTableCommand.InvokeAsync(args).Wait();
        }

        static RootCommand CreateRouteTableCommand()
        {
            var command = new RootCommand();
            
            var regionOption = new Option(
                new string[] {"-r", "--region"}, 
                "Azure Region abbreviation.");
            regionOption.Argument = new Argument<string>();
            regionOption.Required = true;
            command.AddOption(regionOption);

            var tableOption = new Option(
                new string[] {"-t", "--table"}, 
                "Route Table resource name.");
            tableOption.Argument = new Argument<string>();
            tableOption.Required = true;
            command.AddOption(tableOption);

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

            command.Handler = 
                CommandHandler.Create<string, string, string, string>(CreateOrUpdateRouteTable);

            return command;
        }

        /// <summary>
        /// Top level function to create or update a route table to route all IPs represented by the Azure Monitor 
        /// Service Tag to the internet.
        /// </summary>
        static async Task CreateOrUpdateRouteTable(string group, string table, string region, string subscription)
        {
            if (subscription == null)
                subscription = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

            if (subscription == null)
                throw new ArgumentException("Must provide Azure subscription ID.");

            AzureOperations azure = new AzureOperations(region, subscription);
            
            Console.WriteLine($"Getting Service Tags for geneva in {region} for subscription {subscription}.");
            string[] addresses = await azure.GetAzureMonitorIPs();

            Console.WriteLine($"Creating route table {table} in {group} with {addresses.Count()} addresses.");
            await azure.CreateRouteTableAsync(group, table, addresses);

            Console.WriteLine("Done");
        }
    }
}
