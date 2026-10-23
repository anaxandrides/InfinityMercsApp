using InfinityMercsApp.ApiClient;
using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Web;
using InfinityMercsApp.Web.Files;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Signing in: the Entra External ID tenant, through the same app registration as the phone app (wwwroot/appsettings.json).
var api = builder.Configuration.GetSection("Api").Get<ApiSettings>() ?? new ApiSettings();
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("Entra", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add(api.Scope);
    options.ProviderOptions.LoginMode = "redirect";
});

// The user's companies and seasons, from the app's own API.
builder.Services.AddScoped(_ => new HttpClient())
    .AddScoped(_ => new UserDocumentsEndpoint(string.IsNullOrWhiteSpace(api.BaseAddress) ? ApiAddress.Production : new Uri(api.BaseAddress)))
    .AddScoped<IUserDocumentsApi, UserDocumentsClient>()
    .AddScoped<IAccessTokens, MsalAccessTokens>()
    .AddScoped<UserFiles>();

await builder.Build().RunAsync();
