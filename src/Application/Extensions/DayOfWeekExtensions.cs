using System;
using Domain.Enums;

namespace Application.Extensions;

public static class DayOfWeekExtensions
{
    public static Domain.Enums.DayOfWeek ToDomain(this System.DayOfWeek systemDay)
    {
        return (Domain.Enums.DayOfWeek)((int)systemDay + 1);
    }

    public static System.DayOfWeek ToSystem(this Domain.Enums.DayOfWeek domainDay)
    {
        return (System.DayOfWeek)((int)domainDay - 1);
    }
}
