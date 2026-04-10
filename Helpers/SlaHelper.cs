using System;

namespace IT_Form.Helpers
{
    public static class SlaHelper
    {
        public static DateTime GetMalaysiaTime(DateTime utcDate)
        {
            return DateTime.SpecifyKind(utcDate, DateTimeKind.Utc).AddHours(8);
        }

        public static DateTime CalculateSlaDeadline(DateTime startUtc, int targetDays)
        {
            double remainingHours = targetDays * 9.0;
            DateTime currentMyt = GetMalaysiaTime(startUtc);

            // Adjust start to within working hours (08:30 - 17:30 MYT)
            if (currentMyt.TimeOfDay > new TimeSpan(17, 30, 0))
            {
                currentMyt = currentMyt.Date.AddDays(1).Add(new TimeSpan(8, 30, 0));
            }
            else if (currentMyt.TimeOfDay < new TimeSpan(8, 30, 0))
            {
                currentMyt = currentMyt.Date.Add(new TimeSpan(8, 30, 0));
            }

            // Jump over Sundays for the adjusted start in MYT
            while (currentMyt.DayOfWeek == DayOfWeek.Sunday)
            {
                currentMyt = currentMyt.AddDays(1);
            }

            while (remainingHours > 0)
            {
                DateTime endOfToday = currentMyt.Date.Add(new TimeSpan(17, 30, 0));
                double hoursLeftToday = (endOfToday - currentMyt).TotalHours;

                if (hoursLeftToday >= remainingHours)
                {
                    currentMyt = currentMyt.AddHours(remainingHours);
                    remainingHours = 0;
                }
                else
                {
                    remainingHours -= hoursLeftToday;
                    currentMyt = currentMyt.Date.AddDays(1).Add(new TimeSpan(8, 30, 0));
                    while (currentMyt.DayOfWeek == DayOfWeek.Sunday) currentMyt = currentMyt.AddDays(1);
                }
            }
            // Return UTC to maintain consistency in transmission
            return currentMyt.AddHours(-8);
        }

        public static double CalculateBusinessHoursSpent(DateTime startUtc, DateTime endUtc)
        {
            if (endUtc <= startUtc) return 0;

            DateTime currentMyt = GetMalaysiaTime(startUtc);
            DateTime endMyt = GetMalaysiaTime(endUtc);
            double totalHours = 0;

            // Adjust start to within working hours (08:30 - 17:30 MYT)
            if (currentMyt.TimeOfDay > new TimeSpan(17, 30, 0)) currentMyt = currentMyt.Date.AddDays(1).Add(new TimeSpan(8, 30, 0));
            else if (currentMyt.TimeOfDay < new TimeSpan(8, 30, 0)) currentMyt = currentMyt.Date.Add(new TimeSpan(8, 30, 0));
            while (currentMyt.DayOfWeek == DayOfWeek.Sunday) currentMyt = currentMyt.AddDays(1);

            if (currentMyt >= endMyt) return 0;

            while (currentMyt.Date < endMyt.Date)
            {
                DateTime endOfToday = currentMyt.Date.Add(new TimeSpan(17, 30, 0));
                if (currentMyt < endOfToday) totalHours += (endOfToday - currentMyt).TotalHours;

                currentMyt = currentMyt.Date.AddDays(1).Add(new TimeSpan(8, 30, 0));
                while (currentMyt.DayOfWeek == DayOfWeek.Sunday) currentMyt = currentMyt.AddDays(1);
            }

            // Remainder of the last day
            DateTime startOfLastDay = currentMyt.Date.Add(new TimeSpan(8, 30, 0));
            DateTime endOfLastDay = currentMyt.Date.Add(new TimeSpan(17, 30, 0));
            DateTime actualStart = currentMyt > startOfLastDay ? currentMyt : startOfLastDay;
            DateTime actualEnd = endMyt < endOfLastDay ? endMyt : endOfLastDay;

            if (actualEnd > actualStart && currentMyt.DayOfWeek != DayOfWeek.Sunday)
            {
                totalHours += (actualEnd - actualStart).TotalHours;
            }

            return totalHours;
        }

        public static bool IsSlaOverdue(DateTime startUtc, int targetDays)
        {
            return DateTime.UtcNow > CalculateSlaDeadline(startUtc, targetDays);
        }
    }
}
