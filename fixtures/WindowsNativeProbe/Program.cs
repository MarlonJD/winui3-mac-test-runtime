using System.Drawing;
using System.Windows.Forms;

namespace WindowsNativeProbe;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ProbeForm());
    }
}

internal sealed class ProbeForm : Form
{
    public ProbeForm()
    {
        Text = "WinUI3 Mac Test Runtime Native Probe";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1280, 800);
        MinimumSize = new Size(960, 640);
        BackColor = Color.FromArgb(247, 248, 250);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.FromArgb(247, 248, 250)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 248));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(CreateNavigationPane(), 0, 0);
        root.Controls.Add(CreateContentPane(), 1, 0);
    }

    private static Control CreateNavigationPane()
    {
        var pane = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(242, 243, 245),
            Padding = new Padding(14, 18, 14, 18)
        };

        var title = new Label
        {
            Text = "Navigation",
            AutoSize = false,
            Height = 32,
            Dock = DockStyle.Top,
            ForeColor = Color.FromArgb(80, 88, 101)
        };
        pane.Controls.Add(title);

        var menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Height = 330,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = pane.BackColor
        };
        pane.Controls.Add(menu);
        menu.BringToFront();

        foreach (var item in new[] { "Home", "Channels", "Events", "Messages", "Notifications", "Settings", "Admin" })
        {
            menu.Controls.Add(CreateNavigationItem(item, selected: item == "Home"));
        }

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 150,
            BackColor = pane.BackColor
        };
        pane.Controls.Add(footer);

        var account = new Label
        {
            Text = "  Demo Admin\r\n  @demo",
            AutoSize = false,
            Height = 74,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(31, 35, 42)
        };
        footer.Controls.Add(account);

        var signOut = new Button
        {
            Text = "Sign out",
            Dock = DockStyle.Bottom,
            Height = 40,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(31, 35, 42)
        };
        footer.Controls.Add(signOut);
        return pane;
    }

    private static Control CreateNavigationItem(string text, bool selected)
    {
        var label = new Label
        {
            Text = "  ●  " + text,
            AutoSize = false,
            Width = 220,
            Height = 40,
            Margin = new Padding(0, 0, 0, 4),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = selected ? Color.FromArgb(232, 240, 255) : Color.FromArgb(242, 243, 245),
            ForeColor = selected ? Color.FromArgb(37, 98, 217) : Color.FromArgb(31, 35, 42)
        };
        return label;
    }

    private static Control CreateContentPane()
    {
        var pane = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(32, 28, 32, 32),
            BackColor = Color.FromArgb(247, 248, 250)
        };

        var heading = new Label
        {
            Text = "Native Windows Screenshot Probe",
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 34,
            Font = new Font(SystemFonts.MessageBoxFont?.FontFamily ?? FontFamily.GenericSansSerif, 17, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 35, 42)
        };
        pane.Controls.Add(heading);

        var subtitle = new Label
        {
            Text = "Captured on a GitHub-hosted windows-latest runner",
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 30,
            ForeColor = Color.FromArgb(93, 102, 115)
        };
        pane.Controls.Add(subtitle);
        subtitle.BringToFront();

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(30),
            BackColor = Color.White
        };
        pane.Controls.Add(card);
        card.BringToFront();

        var body = new Label
        {
            Text = "This generic public fixture proves the workflow can launch a real Windows desktop window and upload a PNG screenshot artifact.",
            Dock = DockStyle.Top,
            Height = 80,
            ForeColor = Color.FromArgb(31, 35, 42)
        };
        card.Controls.Add(body);
        return pane;
    }
}
