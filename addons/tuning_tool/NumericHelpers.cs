#if TOOLS
using System;

/// <summary>
/// Числовые утилиты для Tuning Tool.
/// Округляют double-значения из SpinBox перед записью в модели,
/// убирая шум вроде 0.15000000000000036, который появляется
/// при сложении/умножении double с шагом 0.05 / 0.01.
/// </summary>
public static class NumericHelpers
{
    /// <summary>
    /// Округление до 4 знаков после запятой (half away from zero).
    /// 4 знаков достаточно для DC и множителей; в JSON'ах проекта
    /// значений с большей точностью нет.
    /// </summary>
    public static double Round4(double v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Округление до N знаков (half away from zero).
    /// На случай, если в будущем где-то понадобится другая точность.
    /// </summary>
    public static double RoundTo(double v, int digits)
        => Math.Round(v, digits, MidpointRounding.AwayFromZero);
}
#endif