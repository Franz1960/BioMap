using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace BioMap
{
  public class GrowthFunc
  {
    /// <summary>
    /// Start of growing season in days since beginning of year.
    /// </summary>
    public static int SeasonStartDay { get; set; } = 135;
    /// <summary>
    /// Length of growing season in days.
    /// </summary>
    public static int SeasonLengthDays { get; set; } = 130;
    public static double HatchSize { get; set; } = 12.0;
    /// <summary>
    /// Size of a full-grown individual.
    /// </summary>
    public static double FullSize { get; set; } = 60.0;
    /// <summary>
    /// Growth rate.
    /// </summary>
    public static double GrowthRate { get; set; } = 1.03;
    /// <summary>
    /// Maximum additional days in first season.
    /// </summary>
    public static int MaxAddDaysInFirstSeason { get; set; } = 15;
    /// <summary>
    /// Date of birth.
    /// </summary>
    public DateTime DateOfBirth { get; set; } = new DateTime(2020, 1, 1);
    /// <summary>
    /// Calculate the size of a specimen with given parameters for a given net growth time.
    /// </summary>
    /// <param name="dateTime">
    /// The net growth time, unit years.
    /// </param>
    /// <returns>
    /// The calculated size.
    /// </returns>
    public static double GetSizeForNetGrowingTime(double dGrowingTimeYears) {
      double dX = dGrowingTimeYears;
      double dSize = GrowthFunc.HatchSize + Math.Max(0.0, (GrowthFunc.FullSize - GrowthFunc.HatchSize) * (1 - 1 / (1 + GrowthFunc.GrowthRate * dX)));
      return dSize;
    }
    /// <summary>
    /// Calculate the size of a specimen with given parameters at a given moment in time.
    /// </summary>
    /// <param name="dateTime">
    /// The moment in time.
    /// </param>
    /// <returns>
    /// The calculated size.
    /// </returns>
    public double GetSize(DateTime dateTime) {
      if (dateTime <= this.DateOfBirth) {
        return 0;
      } else {
        var dtX = dateTime;
        var dtB = this.DateOfBirth;
        int nDaysInFirstSeason;
        int nDaysInLaterSeasons;
        if (dtB.Month >= 13) {
          // So spät überlebt kein Hüpferling mehr.
          nDaysInFirstSeason = 0;
          nDaysInLaterSeasons = 0;
        } else if (dtX.Year == dtB.Year) {
          int nStartDayInFirstSeason = Math.Max(dtB.DayOfYear, GrowthFunc.SeasonStartDay - GrowthFunc.MaxAddDaysInFirstSeason);
          int nEndDayInFirstSeason = Math.Min(dtX.DayOfYear, GrowthFunc.SeasonStartDay + GrowthFunc.SeasonLengthDays);
          nDaysInFirstSeason = Math.Max(0, nEndDayInFirstSeason - nStartDayInFirstSeason);
          nDaysInLaterSeasons = 0;
        } else {
          int nStartDayInFirstSeason = Math.Max(dtB.DayOfYear, GrowthFunc.SeasonStartDay - GrowthFunc.MaxAddDaysInFirstSeason);
          int nEndDayInFirstSeason = GrowthFunc.SeasonStartDay + GrowthFunc.SeasonLengthDays;
          nDaysInFirstSeason = Math.Max(0, nEndDayInFirstSeason - nStartDayInFirstSeason);
          int nStartDayInLastSeason = GrowthFunc.SeasonStartDay;
          int nEndDayInLastSeason = Math.Min(dtX.DayOfYear, GrowthFunc.SeasonStartDay + GrowthFunc.SeasonLengthDays);
          nDaysInLaterSeasons = Math.Max(0, nEndDayInLastSeason - nStartDayInLastSeason) + (dtX.Year - dtB.Year - 1) * GrowthFunc.SeasonLengthDays;
        }
        int nGrowingTimeDays = nDaysInFirstSeason + nDaysInLaterSeasons;
        double dGrowingTimeYears = ((double)nGrowingTimeDays) / GrowthFunc.SeasonLengthDays;
        double dSize = GrowthFunc.GetSizeForNetGrowingTime(dGrowingTimeYears);
        return dSize;
      }
    }
    /// <summary>
    /// Calculate the size of a specimen with given parameters at a given moment in time.
    /// </summary>
    /// <param name="dYears">
    /// The moment in time in years. The fraction contains the elapsed time since start of the year.
    /// </param>
    /// <returns>
    /// The calculated size.
    /// </returns>
    public double GetSize(double dYears) {
      return this.GetSize(Utilities.DateTime_from_Years(dYears));
    }
  }
}
