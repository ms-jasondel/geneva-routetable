using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using Azure.Core;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;

namespace GenevaServiceTag
{
    public class RouteTableFactory
    {
        private string region;
        private string routeTableName;
        private string virtualFirewallIp;
        private IEnumerable<string> addresses;

        public RouteTableFactory(string region, string routeTableName, string virtualFirewallIp, IEnumerable<string> addresses)
        {
            this.region = region;
            this.routeTableName = routeTableName;
            this.virtualFirewallIp = virtualFirewallIp;
            this.addresses = addresses;
        }

        public RouteTable Create()
        {
            RouteTable table = CreateRouteTable();
            table.Routes.Add(CreateDefaultRoute());            
            IEnumerable<Route> newRoutes = CreateRoutes();
            
            // Add route for each of the IPs represented by the ServiceTag.
            foreach (Route route in newRoutes)
                table.Routes.Add(route);
            
            return table;
        }

         private RouteTable CreateRouteTable()
        {
            RouteTable table = new Azure.ResourceManager.Network.Models.RouteTable();
            table.Location = region.ToLower();
            table.Routes = new List<Route>();
            return table;
        }

        private Route CreateDefaultRoute()
        {
            Route defaultRoute = new Route();
            defaultRoute.Id = "default";
            defaultRoute.Name = "default";
            defaultRoute.AddressPrefix = "0.0.0.0/0";
            defaultRoute.NextHopIpAddress = virtualFirewallIp;
            defaultRoute.NextHopType = RouteNextHopType.VirtualAppliance;
            return defaultRoute;
        }

        private IEnumerable<Route> CreateRoutes()
        {
            IEqualityComparer<Route> routeComparer = new RouteComparer();

            List<Route> routes = new List<Route>();
            foreach (string address in addresses)
            {
                Route route = new Route();
                route.Id = FormatName(routeTableName, address);
                route.Name = FormatName(routeTableName, address);
                route.AddressPrefix = address;
                route.NextHopType = RouteNextHopType.Internet;

                if (!routes.Contains(route, routeComparer))
                    routes.Add(route);
            }            
            
            return routes;
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
    }
    
    internal class RouteComparer : IEqualityComparer<Route>
    {
        public bool Equals(Route lhs, Route rhs)
        {
            return (lhs.AddressPrefix == rhs.AddressPrefix &&
                lhs.NextHopIpAddress == rhs.NextHopIpAddress);
        }

        public int GetHashCode(Route route)
        {
            return route.Name.GetHashCode();
        }
    }    
}
