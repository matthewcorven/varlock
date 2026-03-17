using Varlock.DotNet; // 👈 Varlock

namespace dotnet_winforms;

public partial class Form1 : Form
{
    public Form1(VarlockResolvedGraph graph) // 👈 Varlock: accept resolved graph
    {
        InitializeComponent();

        // 👈 Varlock: display configuration values from .env.schema
        Text = "Varlock Configuration";
        var listBox = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 11),
        };

        foreach (var item in graph.Items)
        {
            var display = item.Value.IsSensitive ? "***" : item.Value.Value?.ToString() ?? "(null)";
            listBox.Items.Add($"  {item.Key} = {display}");
        }

        Controls.Add(listBox);
    }
}
