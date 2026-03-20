using System.Runtime.InteropServices;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;

namespace TodoPalExtension;

public sealed class GraphAuthService
{
    // Microsoft Office first-party client ID. Pre-approved in most M365 tenants,
    // avoiding the "admin approval required" prompt that less common client IDs trigger.
    private const string ClientId = "d3590ed6-52b3-4102-aeff-aad2292ab01c";
    private static readonly string[] s_scopes = ["Tasks.ReadWrite"];

    private readonly IPublicClientApplication _app;

    public GraphAuthService()
    {
        _app = PublicClientApplicationBuilder
            .Create(ClientId)
            .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
            .WithDefaultRedirectUri()
            .Build();
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try silent auth first using cached token or OS account
            var accounts = await _app.GetAccountsAsync();
            var result = await _app.AcquireTokenSilent(s_scopes, accounts.FirstOrDefault())
                .ExecuteAsync(cancellationToken);
            return result.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            // No cached token - need interactive auth via WAM
            var result = await _app.AcquireTokenInteractive(s_scopes)
                .WithParentActivityOrWindow(GetForegroundWindow())
                .ExecuteAsync(cancellationToken);
            return result.AccessToken;
        }
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _app.GetAccountsAsync();
        foreach (var account in accounts)
        {
            await _app.RemoveAsync(account);
        }
    }

    public async Task<bool> IsSignedInAsync()
    {
        var accounts = await _app.GetAccountsAsync();
        return accounts.Any();
    }

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
}
