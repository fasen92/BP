using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WifiLocatorWeb;
using System.Globalization;
using WifiLocatorWeb.Api;



var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var backendUrl = builder.Configuration["BackendUrl"] ?? "http://localhost:7147/api/Wifi/"; 

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(backendUrl)
});
builder.Services.AddScoped<ApiClient>();

await builder.Build().RunAsync();
