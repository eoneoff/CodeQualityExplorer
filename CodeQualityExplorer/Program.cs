using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Mysoft.JenkinsNET;

namespace CodeQualityExplorer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddJenkinsNET("http://ec2-52-208-225-245.eu-west-1.compute.amazonaws.com:8080", "giraffic", "11ac68c90f7b88cc2ebacb61a943239a37");
            builder.Services.AddSingleton<Models.CrumbFactory>();

            await builder.Build().RunAsync();
        }
    }
}
