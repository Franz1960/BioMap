﻿using System;
using Newtonsoft.Json;

namespace BioMap
{
  public class GrowthFunc
  {
    /// <summary>
    /// Start of growing season in days since beginning of year.
    /// </summary>
    public int SeasonStartDay { get; set; } = 90;
    /// <summary>
    /// Length of growing season in days.
    /// </summary>
    public int SeasonLengthDays { get; set; } = 182;
    /// <summary>
    /// Size of a full-grown individual.
    /// </summary>
    public double FullSize { get; set; } = 50.0;
    /// <summary>
    /// Growth in the first year.
    /// </summary>
    public double GrowthInFirstYear { get; set; } = 25.0;
    /// <summary>
    /// Year of birth in Gregorian calendar.
    /// </summary>
    public int YearOfBirth { get; set; } = 2020;
    /// <summary>
    /// Calculate the size of a specimen with given parameters at a given moment in time.
    /// </summary>
    /// <param name="ticks">
    /// The moment in time in ticks, i.e. the number of 100 nanosecond intervals 
    /// since 0001-01-01 00:00:00. It is the 'Ticks' in the DateTime class of .NET.
    /// </param>
    /// <returns>
    /// The calculated size.
    /// </returns>
    public double GetSize(double ticks) {
      var dtX = new DateTime((long)ticks);
      var dtYoB = new DateTime(this.YearOfBirth,1,1);
      var tsAge = dtX-dtYoB;
      int nFullYears = dtX.Year-dtYoB.Year;
      int nDayOfYear = dtX.DayOfYear;
      double dElapsedInCurrentYear =
        (nDayOfYear<this.SeasonStartDay) ? 0 :
        (nDayOfYear>this.SeasonStartDay+this.SeasonLengthDays) ? 1 :
        (((double)(nDayOfYear-this.SeasonStartDay))/this.SeasonLengthDays);
      double dYearsToGrow = nFullYears+dElapsedInCurrentYear;
      double dSize = Math.Max(0.0,this.FullSize-(this.GrowthInFirstYear/Math.Max(0.01,dYearsToGrow)));
      return dSize;
    }
  }
}
