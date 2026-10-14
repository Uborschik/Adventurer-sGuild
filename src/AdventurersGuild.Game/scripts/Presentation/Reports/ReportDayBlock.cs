namespace AdventurersGuild.Presentation.Reports;

using System.Text;
using Godot;
using AdventurersGuild.Domain.Reports;

public partial class ReportDayBlock : Control
{
    private Label header;
    private RichTextLabel body;

    public override void _Ready()
    {
        header = GetNode<Label>("Header");
        body = GetNode<RichTextLabel>("Body");

        body.FitContent = true;
        body.BbcodeEnabled = true;
        body.AddThemeConstantOverride("line_separation", 6);
    }

    public void Bind(DayReport day)
    {
        header.Text = $"День {day.GlobalDay}";

        body.Clear();
        body.BbcodeEnabled = true;

        foreach (var quest in day.Quests)
        {
            foreach (var line in quest.Lines)
                body.AppendText(line);

            if (quest.IsFinal)
                body.AppendText("[color=#aaa]— квест завершён —[/color]\n");

            body.AppendText("\n");
        }
    }
}