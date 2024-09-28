using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BioMap
{
  public static class Recatch
  {
    public class Result
    {
      public List<Element> Elements = new List<Element>();
    }
    public static Dictionary<string, int> GetResults(SessionData sd, int year) {
      var results = new Dictionary<string, int>();
      DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
      foreach (Place place in sd.DS.GetPlaces(sd)) {
        var dictResults = new Dictionary<int, Result>();
        foreach (Element el in sd.DS.GetElements(sd, sd.Filters, WhereClauses.Is_FromPlace(place.Name))) {
          int y = el.ElementProp.CreationTime.Year;
          int kw = dfi.Calendar.GetWeekOfYear(el.ElementProp.CreationTime, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
          if (y == year && kw >= Monitoring.kwMin && kw <= Monitoring.kwMax) {
            int? iid = el.GetIIdAsInt();
            if (iid.HasValue && iid.Value >= 1) {
              if (!dictResults.TryGetValue(kw, out Result result)) {
                result = new Result();
                dictResults[kw] = result;
              }
              result.Elements.Add(el);
            }
          }
        }
        var kwl = dictResults.Keys.ToList();
        kwl.Sort();
        int N_max = 0;
        if (kwl.Count < 1) {
          // No catch --> 0 population.
        } else if (kwl.Count < 2) {
          // One monitoring --> no recatch --> population size is no less than the caught individuals.
          int N = dictResults[kwl[0]].Elements.Count;
          N_max = Math.Max(N_max, N);
        } else {
          // More than 1 monitoring --> Petersen.
          for (int idx1 = 1; idx1 < kwl.Count; idx1++) {
            var elementsInThisKw = dictResults[kwl[idx1]].Elements;
            var elementsInPrevKw = dictResults[kwl[idx1 - 1]].Elements;
            //
            int M = elementsInPrevKw.Count;
            int n = elementsInThisKw.Count;
            int m = elementsInThisKw.Where(elThis => elementsInPrevKw.Any(elPrev => object.Equals(elPrev.GetIIdAsInt(), elThis.GetIIdAsInt()))).Count();
            int N;
            if (m == 0) {
              // No recatch --> population size is no less than the caught individuals.
              N = M + n;
            } else {
              // Formula by Petersen.
              N = (int)Math.Round(((double)(n * M)) / m);
            }
            N_max = Math.Max(N_max, N);
          }
        }
        results.Add(place.Name, N_max);
      }
      return results;
    }
  }
}
