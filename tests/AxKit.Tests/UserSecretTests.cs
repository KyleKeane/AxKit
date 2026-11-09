using AxKit.Core;

namespace AxKit.Tests;

/// <summary>The user's secret store (DPAPI; axClip's kept history, the GitHub token): a text comes back for the same account, and nonsense is null, never an exception.</summary>
public sealed class UserSecretTests
{
    [Fact]
    public void A_protected_text_comes_back_and_nonsense_is_null()
    {
        var kept = UserSecret.Protect("a token of the user's");
        Assert.NotEqual("a token of the user's", kept);
        Assert.Equal("a token of the user's", UserSecret.Unprotect(kept));
        Assert.Null(UserSecret.Unprotect("not base64!"));
        Assert.Null(UserSecret.Unprotect(Convert.ToBase64String([1, 2, 3])));
    }
}