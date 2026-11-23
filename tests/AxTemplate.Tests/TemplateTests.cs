using AxKit.Core;
using AxTemplate.Core;

namespace AxTemplate.Tests;

/// <summary>
/// axTemplate's pure code (docs/axTemplate/SPEC.md): the templates' folder as a list of the item format (TP-2), the
/// finding (TP-3.2), the Left keys after a paste (TP-2.3), and the settings file (TP-7.2).
/// </summary>
public sealed class TemplateTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "axTemplate-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private static Template T(string title, string text = "", params string[] showsIn) => new(title, text, showsIn, title + ".json");

    // ---- the folder (TP-2) ----

    [Fact]
    public void A_new_folder_gets_its_configuration_with_a_check_for_every_app()
    {
        var text = TemplateStore.ConfigurationToWrite(null, ["axDown", "axJSON"]);
        Assert.NotNull(text);
        var config = ListConfig.Parse(text);
        Assert.Equal(["Text", "Every axKit app", "axDown", "axJSON"], config.ItemFields.Select(field => field.Title));
        Assert.Equal([FieldTypes.LongText, FieldTypes.Check, FieldTypes.Check, FieldTypes.Check], config.ItemFields.Select(field => field.Type));
    }

    [Fact]
    public void A_newer_version_adds_a_field_and_takes_none_away()
    {
        var first = TemplateStore.ConfigurationToWrite(null, ["axDown"])!;
        Assert.Null(TemplateStore.ConfigurationToWrite(first, ["axDown"]));

        // The user's own field, added in axJSON, stays; the new app's check comes after everything there.
        var own = ListConfig.Parse(first);
        var withOwn = own.WithItemFields([.. own.ItemFields, new ItemField("Notes", FieldTypes.Text, FieldTypes.Empty(FieldTypes.Text), [])]).ToText();
        var grown = TemplateStore.ConfigurationToWrite(withOwn, ["axDown", "axClaude"]);
        Assert.NotNull(grown);
        Assert.Equal(["Text", "Every axKit app", "axDown", "Notes", "axClaude"], ListConfig.Parse(grown).ItemFields.Select(field => field.Title));
    }

    [Fact]
    public void A_configuration_that_does_not_read_is_left_alone()
    {
        Assert.Null(TemplateStore.ConfigurationToWrite("{ not json", ["axDown"]));
        Assert.Equal(["Text", "Every axKit app"], TemplateStore.Configuration("{ not json").ItemFields.Select(field => field.Title));
    }

    [Fact]
    public void A_template_is_an_item_with_its_text_and_its_checks()
    {
        var config = ListConfig.Parse(TemplateStore.ConfigurationToWrite(null, ["axDown", "axJSON"])!);
        var item = FlatItem.Parse(TemplateStore.NewItemText("Introduction", config))
            .With(TemplateStore.TextField, System.Text.Json.Nodes.JsonValue.Create("Dear {clipboard},\n{caret}"), config)
            .With("axDown", TemplateStore.Checked(true), config);

        var template = TemplateStore.Parse(Path.Combine("templates", "Introduction.json"), item.ToText());
        Assert.NotNull(template);
        Assert.Equal("Introduction", template.Title);
        Assert.Equal("Dear {clipboard},\n{caret}", template.Text);
        Assert.Equal(["axDown"], template.ShowsIn);
        Assert.True(template.ShowsInApp("axDown"));
        Assert.False(template.ShowsInApp("axJSON"));
        Assert.True(T("x", "", TemplateStore.EveryAppField).ShowsInApp("axJSON"));
    }

    [Fact]
    public void The_folder_is_read_in_the_order_of_the_names_and_says_what_is_no_template()
    {
        Directory.CreateDirectory(_folder);
        var config = ListConfig.Parse(TemplateStore.ConfigurationToWrite(null, ["axDown"])!);
        File.WriteAllText(TemplateStore.ConfigurationPath(_folder), config.ToText());
        File.WriteAllText(Path.Combine(_folder, "b.json"), TemplateStore.NewItemText("Thanks", config));
        File.WriteAllText(Path.Combine(_folder, "a.json"), TemplateStore.NewItemText("introduction", config));
        File.WriteAllText(Path.Combine(_folder, "broken.json"), "[1, 2]");
        File.WriteAllText(Path.Combine(_folder, "notes.txt"), "not looked at");

        var read = TemplateStore.Read(_folder);
        Assert.Equal(["introduction", "Thanks"], read.Templates.Select(template => template.Title));
        Assert.Equal(["broken"], read.NotTemplates);
        Assert.Empty(TemplateStore.Read(Path.Combine(_folder, "missing")).Templates);
    }

    [Fact]
    public void A_name_is_refused_when_empty_or_taken()
    {
        string[] names = ["Thanks", "Introduction"];
        Assert.Equal("Type the template's name", TemplateStore.NameRefused("  ", names));
        Assert.Equal("There is a template called thanks already", TemplateStore.NameRefused("thanks", names));
        Assert.Null(TemplateStore.NameRefused("THANKS", names, own: "Thanks"));
        Assert.Equal("There is a template called Introduction already", TemplateStore.NameRefused("Introduction", names, own: "Thanks"));
        Assert.Null(TemplateStore.NameRefused("Signature", names));
    }

    [Fact]
    public void Every_app_with_a_check_is_an_app_of_the_bundle()
    {
        var apps = Dispatch.Apps.Select(app => app.Name).ToList();
        Assert.All(TemplateStore.AppsWithField, name => Assert.Contains(name, apps));
        // The checks stand in the bundle's own order of apps.
        Assert.Equal(TemplateStore.AppsWithField.OrderBy(apps.IndexOf), TemplateStore.AppsWithField);
    }

    [Fact]
    public void Opening_the_folder_makes_it_and_its_configuration_once()
    {
        var folder = Path.Combine(_folder, "templates");
        var first = TemplateStore.Open(folder);
        Assert.Empty(first.Files);
        Assert.Equal(["Text", "Every axKit app", .. TemplateStore.AppsWithField], first.Config.ItemFields.Select(field => field.Title));

        var written = File.GetLastWriteTimeUtc(TemplateStore.ConfigurationPath(folder));
        File.WriteAllText(Path.Combine(folder, "Thanks.json"), TemplateStore.NewItemText("Thanks", first.Config));
        var second = TemplateStore.Open(folder);
        Assert.Equal(["Thanks"], second.Files.Select(file => file.Title));
        Assert.Equal(written, File.GetLastWriteTimeUtc(TemplateStore.ConfigurationPath(folder)));
    }

    [Fact]
    public void The_files_after_a_change_and_after_its_undo_without_reading_the_disk()
    {
        var config = TemplateStore.Configuration(null);
        var thanks = TemplateStore.NewItemText("Thanks", config);
        var intro = TemplateStore.NewItemText("Introduction", config);
        IReadOnlyList<TemplateFile> none = [];

        // Made, then renamed with its file, then deleted; each undone again.
        FileChange[] made = [FileChange.Write("t\\Thanks.json", null, thanks), FileChange.Write("t\\Introduction.json", null, intro)];
        var two = TemplateStore.After(none, made);
        Assert.Equal(["Introduction", "Thanks"], two.Select(file => file.Title));

        var renamedText = FlatItem.Parse(thanks).With(ItemDocument.TitleField, System.Text.Json.Nodes.JsonValue.Create("Zebra"), config).ToText();
        FileChange[] renamed = [FileChange.Write("t\\Introduction.json", intro, renamedText), FileChange.Move("t\\Introduction.json", "t\\Zebra.json")];
        var after = TemplateStore.After(two, renamed);
        Assert.Equal(["Thanks", "Zebra"], after.Select(file => file.Title));
        Assert.Equal("t\\Zebra.json", after[1].Path);
        Assert.Equal(["Introduction", "Thanks"], TemplateStore.After(after, renamed, undone: true).Select(file => file.Title));
        Assert.Equal("t\\Introduction.json", TemplateStore.After(after, renamed, undone: true)[0].Path);

        FileChange[] deleted = [new FileChange("t\\Thanks.json", thanks, null)];
        Assert.Equal(["Zebra"], TemplateStore.After(after, deleted).Select(file => file.Title));
        Assert.Equal(["Thanks", "Zebra"], TemplateStore.After(TemplateStore.After(after, deleted), deleted, undone: true).Select(file => file.Title));

        // A text that is no item is a file that is no template.
        var broken = TemplateStore.After(none, [FileChange.Write("t\\x.json", null, "[1]")]);
        Assert.Null(broken[0].Item);
        Assert.Equal("x", broken[0].Title);
        Assert.Empty(TemplateStore.After(two, made, undone: true));
    }

    // ---- finding (TP-3.2) ----

    [Fact]
    public void Every_word_must_be_found_in_the_name_or_the_text_and_names_come_first()
    {
        var templates = new[]
        {
            T("Introduction", "Dear colleague, the sprint starts"),
            T("Sprint notes", "What we did"),
            T("Thanks for the report", "Thank you, we will look at the sprint board"),
        };

        Assert.Equal(["Sprint notes", "Introduction", "Thanks for the report"], TemplateMatch.Find(templates, "sprint", []).Select(t => t.Title));
        Assert.Equal(["Thanks for the report"], TemplateMatch.Find(templates, "REPORT thanks", []).Select(t => t.Title));
        Assert.Equal(["Thanks for the report"], TemplateMatch.Find(templates, "report board", []).Select(t => t.Title));
        Assert.Empty(TemplateMatch.Find(templates, "sprint missing", []));
    }

    [Fact]
    public void An_empty_field_lists_all_with_the_ones_used_last_first()
    {
        var templates = new[] { T("Alpha"), T("Beta"), T("Gamma") };
        Assert.Equal(["Gamma", "Alpha", "Beta"], TemplateMatch.Find(templates, " ", ["gamma", "Gone"]).Select(t => t.Title));
        Assert.Equal(["Beta", "Gamma", "Alpha"], TemplateMatch.Used(["Gamma", "beta", "Alpha"], "Beta"));
        Assert.Equal(2, TemplateMatch.Used(["a", "b", "c"], "d", keep: 2).Count);
    }

    [Fact]
    public void What_is_said_of_a_match_and_of_none()
    {
        Assert.Equal("Alpha, 1 of 3", TemplateMatch.Spoken(T("Alpha"), 0, 3));
        Assert.Equal("No template matches sprint", TemplateMatch.Nothing(" sprint ", 4));
        Assert.Equal("No templates yet: Ctrl+B opens the browser, where one is made", TemplateMatch.Nothing("x", 0));
    }

    // ---- the caret after a paste (TP-2.3) ----

    [Theory]
    [InlineData("[{selection}]({caret})", 1)]
    [InlineData("no caret mark", 0)]
    [InlineData("{caret}two\r\nlines", 9)]
    public void The_left_keys_are_the_characters_after_the_caret_with_a_line_ending_as_one(string template, int keys)
    {
        Assert.Equal(keys, TemplateStore.LeftKeys(TextTemplate.Expand(template, string.Empty, string.Empty)));
    }

    // ---- the settings (TP-7.2) ----

    [Fact]
    public void A_missing_settings_file_gives_the_defaults()
    {
        var settings = TemplateSettings.Load(Path.Combine(_folder, "axTemplate.json"), out var error);
        Assert.Null(error);
        Assert.Equal("Ctrl+Shift+Semicolon", settings.HotkeyText);
        Assert.Empty(settings.RecentTemplates);
    }

    [Fact]
    public void The_first_default_now_axClips_is_taken_over_and_a_chosen_key_is_kept()
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, "axTemplate.json");
        File.WriteAllText(path, """{ "hotkey": "Ctrl+Shift+Apostrophe" }""");
        Assert.Equal("Ctrl+Shift+Semicolon", TemplateSettings.Load(path, out _).HotkeyText);
        File.WriteAllText(path, """{ "hotkey": "Ctrl+Alt+Shift+T" }""");
        Assert.Equal("Ctrl+Alt+Shift+T", TemplateSettings.Load(path, out _).HotkeyText);
    }
}
