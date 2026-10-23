using System.Collections.Generic;

public class Area
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public char Symbol { get; set; }
    public string Color { get; set; } = "white";
    
    // Список ID областей, куда можно перейти из этой
    public List<string> Connections { get; set; } = new();

    // Для визуализации пройденных путей
    public bool IsVisited { get; set; } = false;
    
    // Удобное свойство для проверки, является ли это дорогой (без Id и Name)
    public bool IsRoad => string.IsNullOrEmpty(Id);
}