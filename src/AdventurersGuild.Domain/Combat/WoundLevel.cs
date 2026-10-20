namespace AdventurersGuild.Domain.Combat;

public enum WoundLevel
{
    None,   // Здоров
    Light,  // Легко ранен (-10% к атаке)
    Medium, // Средне ранен (-10% к атаке и защите)
    Heavy,  // Тяжело ранен (-25% к атаке и защите)
    Dead    // Погиб
}