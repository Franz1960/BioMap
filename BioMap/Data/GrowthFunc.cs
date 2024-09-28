using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
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
    /// Calculate the number of days in the growing season in a given time span.
    /// </summary>
    /// <param name="dtStart">
    /// Start of time span.
    /// </param>
    /// <param name="dtEnd">
    /// End of time span.
    /// </param>
    /// <returns>
    /// The number of days.
    /// </returns>
    public int GetGrowingTimeDays(DateTime dtStart, DateTime dtEnd) {
      if (dtEnd < dtStart) {
        throw new ArgumentException($"End time is before start time.");
      } else {
        DateTime dtX = dtEnd;
        DateTime dtB = dtStart;
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
        return nGrowingTimeDays;
      }
    }
    /// <summary>
    /// Calculate the size of a specimen with given parameters at a given moment in time.
    /// </summary>
    /// <param name="dateTime">
    /// The moment in time.
    /// </param>
    /// <returns>
    /// The calculated size. Negative if before date of birth.
    /// </returns>
    public double GetSize(DateTime dateTime) {
      if (dateTime <= this.DateOfBirth) {
        int nGrowingTimeDays = this.GetGrowingTimeDays(dateTime, this.DateOfBirth);
        return -0.01 * nGrowingTimeDays * GrowthFunc.HatchSize;
      } else {
        int nGrowingTimeDays = this.GetGrowingTimeDays(this.DateOfBirth, dateTime);
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
    public static GrowthFunc FromCatches(IEnumerable<Element> catches) {
      var ldaPoints = new List<double[]>();
      foreach (Element el in catches) {
        try {
          double l = el.ElementProp.IndivData.MeasuredData.HeadBodyLength;
          if (l != 0) {
            double t = Utilities.Years_from_DateTime(el.ElementProp.CreationTime);
            ldaPoints.Add(new double[] { t, l });
          }
        } catch { }
      }
      if (ldaPoints.Count >= 1) {
        DateTime dtFirstPoint = Utilities.DateTime_from_Years(ldaPoints[0][0]);
        double? dMinYear = null;
        double dFirstLength = ldaPoints[0][1];
        DateTime dtEarliestHatchTimeInFirstYear = new DateTime(dtFirstPoint.Year, 1, 1) + TimeSpan.FromDays(GrowthFunc.SeasonStartDay - GrowthFunc.MaxAddDaysInFirstSeason);
        double dMaxLengthFirstYear = GrowthFunc.GetSizeForNetGrowingTime(((double)(dtFirstPoint - dtEarliestHatchTimeInFirstYear).Days) / GrowthFunc.SeasonLengthDays);
        if (dFirstLength < dMaxLengthFirstYear) {
          dMinYear = Utilities.Years_from_DateTime(new DateTime(dtFirstPoint.Year, 1, 1) + TimeSpan.FromDays(GrowthFunc.SeasonStartDay - GrowthFunc.MaxAddDaysInFirstSeason));
        }
        if (!dMinYear.HasValue) {
          dMinYear = dtFirstPoint.Year - 9;
        }
        double dMaxYear = Utilities.Years_from_DateTime(dtFirstPoint);
        var lsf = new LeastSquareFit();
        lsf.Optimize(
          new double[][] { new double[] { dMinYear.Value, dMaxYear } },
          ldaPoints.ToArray(),
          (daParams, daaPoints) => {
            double dyTimeOfBirth = daParams[0];
            var fg = new GrowthFunc() {
              DateOfBirth = Utilities.DateTime_from_Years(dyTimeOfBirth),
            };
            //System.Diagnostics.Debug.Write(fg.DateOfBirth.ToString()+": ");
            double dDevSum = 0;
            for (int i = 0; i < daaPoints.Length; i++) {
              double dyTime = daaPoints[i][0];
              double lReal = daaPoints[i][1];
              double lCalc = fg.GetSize(dyTime);
              double dDev = lReal - lCalc;
              dDevSum += (dDev * dDev);
              //System.Diagnostics.Debug.Write(" Dev="+ConvInvar.ToDecimalString(dDev,5));
            }
            //System.Diagnostics.Debug.WriteLine(" DevSum="+ConvInvar.ToDecimalString(dDevSum,5));
            return dDevSum;
          },
          -0.02,
          0.0001,
          out double[] daBestParams,
          LeastSquareFit.Method.Directed);
        return new GrowthFunc() {
          DateOfBirth = Utilities.DateTime_from_Years(daBestParams[0]),
        };
      }
      return null;
    }
  }
}
