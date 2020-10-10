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
    public double GrowthRate { get; set; } = 1.7;
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
      var dtX = dateTime;
      var dtYoB = new DateTime(this.DateOfBirth.Year,1,1);
      var tsAge = dtX-dtYoB;
      int nFullYears = Math.Max(0,dtX.Year-dtYoB.Year);
      int nDayOfYear = dtX.DayOfYear;
      double dSeasonDayOfBirth = Math.Max(0,Math.Min(this.SeasonLengthDays,(this.DateOfBirth-dtYoB).TotalDays-this.SeasonStartDay));
      double dElapsedInCurrentYear =
        (dtX.Year<dtYoB.Year) ? 0 :
        (nDayOfYear<this.SeasonStartDay) ? 0 :
        (nDayOfYear>this.SeasonStartDay+this.SeasonLengthDays) ? 1 :
        (((double)(nDayOfYear-this.SeasonStartDay))/this.SeasonLengthDays);
      double dYearsToGrow = Math.Max(0.001,nFullYears+dElapsedInCurrentYear+1.0-(dSeasonDayOfBirth/this.SeasonLengthDays));
      double dSize = Math.Max(0.0,this.FullSize-(100/(this.GrowthRate*dYearsToGrow)));
      return dSize;
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
