using System;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Smart.Blazor;


namespace BlazorChatApp.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");

        var serverHost = string.IsNullOrEmpty(builder.Configuration["SERVER_HOST"]) ? builder.HostEnvironment.BaseAddress : builder.Configuration["SERVER_HOST"];

        //builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        builder.Services.AddSmart();

        builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(serverHost) });
                    

        await builder.Build().RunAsync();
        }
    }
}
