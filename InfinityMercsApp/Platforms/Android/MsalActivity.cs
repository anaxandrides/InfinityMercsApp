using Android.App;
using Android.Content;
using Microsoft.Identity.Client;

namespace InfinityMercsApp;

/// <summary>
/// Takes the browser's sign-in result back into the app: it catches the redirect URI (msal{client id}://auth, see
/// SignInSettings) and hands it to MSAL.
/// </summary>
[Activity(Exported = true)]
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
    DataScheme = "msal0bbba93d-840e-48ce-b2d2-8007f227ba8d",
    DataHost = "auth")]
public class MsalActivity : BrowserTabActivity
{
}
