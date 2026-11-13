using System.Diagnostics;

namespace AxKit.Core;

/// <summary>
/// The two variables a test run gives the windows it starts (docs/axKit/automation.md; security audit S-11):
/// <c>AXKIT_AUTOMATION</c>, the run's secret, which opens the window's automation channel to the runner alone, and
/// <c>AXKIT_HOME</c>, the run's folder for the settings and logs. They are read once, as the process starts, and taken
/// out of its environment at once, so that no program the window starts inherits them (a shell, Claude, git, the user's
/// own programs): only a new axKit window of the same run gets them, from <see cref="PassTo"/>. A secret shorter than 32
/// characters opens nothing; the secret and the folder count only together, and never from the lasting environment
/// (<see cref="Accepted"/>).
/// </summary>
public static class AutomationEnvironment
{
    public const string SecretVariable = "AXKIT_AUTOMATION";
    public const string HomeVariable = "AXKIT_HOME";

    /// <summary>The shortest secret the channel accepts: 128 bits as hexadecimal.</summary>
    public const int MinimumSecretLength = 32;

    private static bool _captured;
    private static string? _secret;
    private static string? _home;

    /// <summary>The run's secret; null outside a test run.</summary>
    public static string? Secret
    {
        get
        {
            Capture();
            return _secret;
        }
    }

    /// <summary>The run's folder for settings and logs; null outside a test run.</summary>
    public static string? Home
    {
        get
        {
            Capture();
            return _home;
        }
    }

    /// <summary>Whether this process is a window of a test run.</summary>
    public static bool Requested => Secret is not null;

    /// <summary>Reads the two variables and takes them out of the process's environment; the first call does it, the program's <c>Main</c> makes that call before anything else.</summary>
    public static void Capture()
    {
        if (_captured)
        {
            return;
        }

        _captured = true;
        var secret = Environment.GetEnvironmentVariable(SecretVariable);
        var home = Environment.GetEnvironmentVariable(HomeVariable);
        Environment.SetEnvironmentVariable(SecretVariable, null);
        Environment.SetEnvironmentVariable(HomeVariable, null);
        // The registry is asked only when a secret is there at all: never in a start of the user's own.
        (_secret, _home) = Accepted(secret, home, secret is not null && (Persisted(SecretVariable) || Persisted(HomeVariable)));
    }

    /// <summary>
    /// What a start takes of the two variables (docs/axKit/repairs.json R-02): the secret and the folder together, or
    /// neither. A run's windows get both, in their own environment block, from the runner that starts them. A secret
    /// without a folder would open the channel on the user's real settings and files, and one set in the user's or
    /// the computer's lasting environment (<paramref name="persisted"/>) was put there by something other than a
    /// run, to reach every window started later: neither opens anything.
    /// </summary>
    public static (string? Secret, string? Home) Accepted(string? secret, string? home, bool persisted) =>
        !persisted && secret is { Length: >= MinimumSecretLength } && home is { Length: > 0 } ? (secret, home) : (null, null);

    /// <summary>Whether a variable is set in the user's or the computer's environment as Windows keeps it, beyond this process.</summary>
    private static bool Persisted(string variable) =>
        Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User) is not null
        || Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.Machine) is not null;

    /// <summary>A new axKit window of the same run gets the run's variables; outside a run, nothing.</summary>
    public static void PassTo(ProcessStartInfo info)
    {
        if (Secret is { } secret)
        {
            info.Environment[SecretVariable] = secret;
            if (Home is { } home)
            {
                info.Environment[HomeVariable] = home;
            }
        }
    }
}
