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

        // Собираем текст в StringBuilder (быстро и без лишних перерисовок)
        var sb = new StringBuilder();

        foreach (var quest in day.Quests)
        {
            foreach (var line in quest.Lines)
            {
                sb.Append(line);

                // Подстраховка: если вдруг в конце строки нет переноса, добавляем
                if (!line.EndsWith("\n"))
                    sb.Append('\n');
            }

            if (quest.IsFinal)
                sb.Append("\n[color=#aaa]— квест завершён —[/color]\n");

            sb.Append('\n'); // Пустая строка между квестами
        }

        // Отправляем всё в UI одним куском
        body.Text = sb.ToString();
    }
}