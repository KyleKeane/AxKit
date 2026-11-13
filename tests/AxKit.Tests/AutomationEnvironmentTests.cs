using AxKit.Core;

namespace AxKit.Tests;

/// <summary>What a start takes of a test run's two variables (AutomationEnvironment.Accepted; security audit S-11, docs/axKit/repairs.json R-02).</summary>
public sealed class AutomationEnvironmentTests
{
    private static readonly string Secret = new('a', AutomationEnvironment.MinimumSecretLength);

    private const string Home = @"C:\Temp\axkit-automate\1234\home";

    [Fact]
    public void A_run_s_secret_and_folder_together_are_taken()
    {
        Assert.Equal((Secret, Home), AutomationEnvironment.Accepted(Secret, Home, persisted: false));
    }

    [Fact]
    public void A_secret_without_the_run_s_folder_opens_nothing_since_it_would_act_on_the_user_s_own_files()
    {
        Assert.Equal((null, null), AutomationEnvironment.Accepted(Secret, null, persisted: false));
        Assert.Equal((null, null), AutomationEnvironment.Accepted(Secret, string.Empty, persisted: false));
    }

    [Fact]
    public void A_short_secret_or_none_opens_nothing_and_the_folder_alone_counts_for_nothing()
    {
        Assert.Equal((null, null), AutomationEnvironment.Accepted(Secret[1..], Home, persisted: false));
        Assert.Equal((null, null), AutomationEnvironment.Accepted(null, Home, persisted: false));
    }

    [Fact]
    public void Variables_set_in_the_lasting_environment_open_nothing_since_no_run_puts_them_there()
    {
        Assert.Equal((null, null), AutomationEnvironment.Accepted(Secret, Home, persisted: true));
    }
}
