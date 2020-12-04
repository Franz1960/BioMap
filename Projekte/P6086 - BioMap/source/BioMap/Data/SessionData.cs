using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SQLite;
using Newtonsoft.Json;

namespace BioMap
{
  public class SessionData
  {
    public Filters Filters { get; } = new Filters();
    public bool SizeTimeChartShowGrowingCurves { get; set; } = true;
    public bool SizeTimeChartIncludeSinglePoints { get; set; } = true;
    public bool SizeTimeChartFit { get; set; } = false;
  }
}
