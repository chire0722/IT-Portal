// Project comment: SlaHelper.cs is part of the IT Form application source code.

using System;

namespace IT_Form.Helpers
{
    /// <summary>
    /// Defines the SlaHelper class used by the IT Form application.
    /// </summary>
    public static class SlaHelper
    {
        public static readonly TimeSpan DefaultBusinessStartTime = new(8, 30, 0);
        public static readonly TimeSpan DefaultBusinessEndTime = new(17, 30, 0);
        private static readonly DayOfWeek[] DefaultWorkingDaysValues =
        [
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday
        ];

        /// <summary>
        /// Gets the data required by the GetDefaultWorkingDays operation.
        /// </summary>
        public static HashSet<DayOfWeek> GetDefaultWorkingDays()
            => [.. DefaultWorkingDaysValues];

        /// <summary>
        /// Gets the data required by the GetMalaysiaTime operation.
        /// </summary>
        public static DateTime GetMalaysiaTime(DateTime utcDate)
        {
            return CompanyTimeZoneHelper.ConvertFromUtc(utcDate, CompanyTimeZoneHelper.DefaultTimeZoneId);
        }

        /// <summary>
        /// Runs the CalculateSlaDeadline operation.
        /// </summary>
        public static DateTime CalculateSlaDeadline(DateTime startUtc, int targetDays)
            => CalculateSlaDeadline(startUtc, targetDays, DefaultBusinessStartTime, DefaultBusinessEndTime, GetDefaultWorkingDays());

        /// <summary>
        /// Runs the CalculateSlaDeadline operation.
        /// </summary>
        public static DateTime CalculateSlaDeadline(
            DateTime startUtc,
            int targetDays,
            TimeSpan businessStartTime,
            TimeSpan businessEndTime)
            => CalculateSlaDeadline(startUtc, targetDays, businessStartTime, businessEndTime, GetDefaultWorkingDays());

        /// <summary>
        /// Runs the CalculateSlaDeadline operation.
        /// </summary>
        public static DateTime CalculateSlaDeadline(
            DateTime startUtc,
            int targetDays,
            TimeSpan businessStartTime,
            TimeSpan businessEndTime,
            IEnumerable<DayOfWeek>? workingDays,
            string? timeZoneId = null)
        {
            var businessSchedule = NormalizeBusinessSchedule(businessStartTime, businessEndTime, workingDays, timeZoneId);
            double remainingHours = targetDays * businessSchedule.HoursPerDay;
            DateTime currentLocal = CompanyTimeZoneHelper.ConvertFromUtc(startUtc, businessSchedule.TimeZoneId);

            if (currentLocal.TimeOfDay >= businessSchedule.EndTime)
            {
                currentLocal = currentLocal.Date.AddDays(1).Add(businessSchedule.StartTime);
            }
            else if (currentLocal.TimeOfDay < businessSchedule.StartTime)
            {
                currentLocal = currentLocal.Date.Add(businessSchedule.StartTime);
            }

            currentLocal = MoveToNextWorkingDate(currentLocal, businessSchedule);

            while (remainingHours > 0)
            {
                DateTime endOfToday = currentLocal.Date.Add(businessSchedule.EndTime);
                double hoursLeftToday = (endOfToday - currentLocal).TotalHours;

                if (hoursLeftToday >= remainingHours)
                {
                    currentLocal = currentLocal.AddHours(remainingHours);
                    remainingHours = 0;
                }
                else
                {
                    remainingHours -= hoursLeftToday;
                    currentLocal = MoveToNextWorkingDate(
                        currentLocal.Date.AddDays(1).Add(businessSchedule.StartTime),
                        businessSchedule);
                }
            }

            return CompanyTimeZoneHelper.ConvertToUtc(currentLocal, businessSchedule.TimeZoneId);
        }

        /// <summary>
        /// Runs the CalculateBusinessHoursSpent operation.
        /// </summary>
        public static double CalculateBusinessHoursSpent(DateTime startUtc, DateTime endUtc)
            => CalculateBusinessHoursSpent(startUtc, endUtc, DefaultBusinessStartTime, DefaultBusinessEndTime, GetDefaultWorkingDays());

        /// <summary>
        /// Runs the CalculateBusinessHoursSpent operation.
        /// </summary>
        public static double CalculateBusinessHoursSpent(
            DateTime startUtc,
            DateTime endUtc,
            TimeSpan businessStartTime,
            TimeSpan businessEndTime)
            => CalculateBusinessHoursSpent(startUtc, endUtc, businessStartTime, businessEndTime, GetDefaultWorkingDays());

        /// <summary>
        /// Runs the CalculateBusinessHoursSpent operation.
        /// </summary>
        public static double CalculateBusinessHoursSpent(
            DateTime startUtc,
            DateTime endUtc,
            TimeSpan businessStartTime,
            TimeSpan businessEndTime,
            IEnumerable<DayOfWeek>? workingDays,
            string? timeZoneId = null)
        {
            if (endUtc <= startUtc) return 0;

            var businessSchedule = NormalizeBusinessSchedule(businessStartTime, businessEndTime, workingDays, timeZoneId);
            DateTime currentLocal = CompanyTimeZoneHelper.ConvertFromUtc(startUtc, businessSchedule.TimeZoneId);
            DateTime endLocal = CompanyTimeZoneHelper.ConvertFromUtc(endUtc, businessSchedule.TimeZoneId);
            double totalHours = 0;

            if (currentLocal.TimeOfDay >= businessSchedule.EndTime) currentLocal = currentLocal.Date.AddDays(1).Add(businessSchedule.StartTime);
            else if (currentLocal.TimeOfDay < businessSchedule.StartTime) currentLocal = currentLocal.Date.Add(businessSchedule.StartTime);
            currentLocal = MoveToNextWorkingDate(currentLocal, businessSchedule);

            if (currentLocal >= endLocal) return 0;

            while (currentLocal.Date < endLocal.Date)
            {
                DateTime endOfToday = currentLocal.Date.Add(businessSchedule.EndTime);
                if (currentLocal < endOfToday) totalHours += (endOfToday - currentLocal).TotalHours;

                currentLocal = MoveToNextWorkingDate(
                    currentLocal.Date.AddDays(1).Add(businessSchedule.StartTime),
                    businessSchedule);
            }

            DateTime startOfLastDay = currentLocal.Date.Add(businessSchedule.StartTime);
            DateTime endOfLastDay = currentLocal.Date.Add(businessSchedule.EndTime);
            DateTime actualStart = currentLocal > startOfLastDay ? currentLocal : startOfLastDay;
            DateTime actualEnd = endLocal < endOfLastDay ? endLocal : endOfLastDay;

            if (actualEnd > actualStart && businessSchedule.IsWorkingDay(currentLocal.DayOfWeek))
            {
                totalHours += (actualEnd - actualStart).TotalHours;
            }

            return totalHours;
        }

        /// <summary>
        /// Runs the IsSlaOverdue operation.
        /// </summary>
        public static bool IsSlaOverdue(DateTime startUtc, int targetDays)
        {
            return IsSlaOverdue(startUtc, targetDays, DefaultBusinessStartTime, DefaultBusinessEndTime, GetDefaultWorkingDays());
        }

        /// <summary>
        /// Runs the IsSlaOverdue operation.
        /// </summary>
        public static bool IsSlaOverdue(
            DateTime startUtc,
            int targetDays,
            TimeSpan businessStartTime,
            TimeSpan businessEndTime)
            => IsSlaOverdue(startUtc, targetDays, businessStartTime, businessEndTime, GetDefaultWorkingDays());

        /// <summary>
        /// Runs the IsSlaOverdue operation.
        /// </summary>
        public static bool IsSlaOverdue(
            DateTime startUtc,
            int targetDays,
            TimeSpan businessStartTime,
            TimeSpan businessEndTime,
            IEnumerable<DayOfWeek>? workingDays,
            string? timeZoneId = null)
        {
            return DateTime.UtcNow > CalculateSlaDeadline(startUtc, targetDays, businessStartTime, businessEndTime, workingDays, timeZoneId);
        }

        /// <summary>
        /// Runs the NormalizeBusinessSchedule operation.
        /// </summary>
        private static CompanyBusinessSchedule NormalizeBusinessSchedule(
            TimeSpan businessStartTime,
            TimeSpan businessEndTime,
            IEnumerable<DayOfWeek>? workingDays,
            string? timeZoneId = null)
        {
            var normalizedWorkingDays = workingDays?
                .Distinct()
                .ToHashSet()
                ?? GetDefaultWorkingDays();

            if (normalizedWorkingDays.Count == 0)
            {
                normalizedWorkingDays = GetDefaultWorkingDays();
            }

            if (businessEndTime <= businessStartTime)
            {
                return new CompanyBusinessSchedule(
                    DefaultBusinessStartTime,
                    DefaultBusinessEndTime,
                    normalizedWorkingDays,
                    CompanyTimeZoneHelper.NormalizeTimeZoneId(timeZoneId));
            }

            return new CompanyBusinessSchedule(
                businessStartTime,
                businessEndTime,
                normalizedWorkingDays,
                CompanyTimeZoneHelper.NormalizeTimeZoneId(timeZoneId));
        }

        /// <summary>
        /// Runs the MoveToNextWorkingDate operation.
        /// </summary>
        private static DateTime MoveToNextWorkingDate(DateTime currentLocal, CompanyBusinessSchedule businessSchedule)
        {
            var next = currentLocal;

            while (!businessSchedule.IsWorkingDay(next.DayOfWeek))
            {
                next = next.Date.AddDays(1).Add(businessSchedule.StartTime);
            }

            return next;
        }
    }
}
