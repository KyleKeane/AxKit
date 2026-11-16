using System.Text.Json.Nodes;

namespace AxKit.TestShared;

/// <summary>
/// A settings file changed as another program or a hand would change it, for the tests of the settings' seal
/// (docs/axKit/SPEC.md AX-5.6): the JSON read, changed and written back, with no seal made.
/// </summary>
internal static class SettingsText
{
    /// <summary>Changes the file's JSON object in place.</summary>
    public static void Change(string path, Action<JsonObject> change)
    {
        var whole = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        change(whole);
        File.WriteAllText(path, whole.ToJsonString());
    }

    /// <summary>Takes the seal out of the file, as someone would who hoped an unsealed file counts.</summary>
    public static void RemoveSeal(string path) => Change(path, whole => whole.Remove("seal"));

    /// <summary>Whether the file carries a seal.</summary>
    public static bool HasSeal(string path) =>
        JsonNode.Parse(File.ReadAllText(path))!.AsObject()["seal"]?.GetValue<string>() is { Length: > 0 };
}
