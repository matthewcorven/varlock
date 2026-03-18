using Varlock.DotNet; // 👈 Varlock

namespace dotnet_winforms;

public partial class Form1 : Form
{
    public Form1(VarlockResolvedGraph graph)
    {
        InitializeComponent();

        var appName = graph.Items.TryGetValue("APP_NAME", out var nameItem)
            ? nameItem.Value?.ToString() ?? "(null)"
            : "(null)";
        var windowTitle = graph.Items.TryGetValue("WINDOW_TITLE", out var titleItem)
            ? titleItem.Value?.ToString() ?? "(null)"
            : "(null)";

        Text = windowTitle;
        var listBox = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 11),
        };

        listBox.Items.Add($"APP_NAME = {appName}");
        listBox.Items.Add($"WINDOW_TITLE = {windowTitle}");
        listBox.Items.Add($"SCHEMA_SOURCE_PRESENT = {graph.Sources.Any((source) => source.Label.Contains(".env.schema", StringComparison.OrdinalIgnoreCase))}");

        Controls.Add(listBox);
    }
}
