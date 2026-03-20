using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;

namespace TodoPalExtension;

public sealed class GraphAuthService
{
    private const string ClientId = "14d82eec-204b-4c2f-b7e8-296a70dab67e";
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
                .WithParentActivityOrWindow(GetConsoleWindowHandle())
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

    private static nint GetConsoleWindowHandle()
    {
        return System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
    }
}
