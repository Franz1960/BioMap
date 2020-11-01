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
    public int SeasonStartDay { get; set; } = 135;
    /// <summary>
    /// Length of growing season in days.
    /// </summary>
    public int SeasonLengthDays { get; set; } = 130;
    /// <summary>
    /// Size of a full-grown individual.
    /// </summary>
    public double FullSize { get; set; } = 60.0;
    /// <summary>
    /// Growth rate.
    /// </summary>
    public double GrowthRate { get; set; } = 1.03;
    /// <summary>
    /// Date of birth.
    /// </summary>
    public DateTime DateOfBirth { get; set; } = new DateTime(2020,1,1);
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
      if (dateTime<=this.DateOfBirth) {
        return 0;
      } else {
        var dtX = dateTime;
        var dtB = this.DateOfBirth;
        int nSeasonDayOfBirth = Math.Max(0,Math.Min(this.SeasonLengthDays,dtB.DayOfYear-this.SeasonStartDay));
        int nSeasonDayNow = Math.Max(0,Math.Min(this.SeasonLengthDays,dtX.DayOfYear-this.SeasonStartDay));
        int nGrowingTimeDays =
          (dtX.Year==dtB.Year)
          ?
          (nSeasonDayNow-nSeasonDayOfBirth)
          :
          (this.SeasonLengthDays-nSeasonDayOfBirth+(dtX.Year-dtB.Year-1)*this.SeasonLengthDays+nSeasonDayNow)
          ;
        double dGrowingTimeYears = ((double)nGrowingTimeDays)/this.SeasonLengthDays;
        double dSize = Math.Max(0.0,this.FullSize*(1-1/(1+this.GrowthRate*dGrowingTimeYears)));
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
