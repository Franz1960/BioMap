﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SQLite;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

namespace BioMap
{
  public class CsvHelper
  {
    public class CsvContent {
      public class Row {
        public DateTime DateTime;
        public double[] Columns;
      }
      public string[] Headers;
      public Row[] Rows;
    }
    //
    public CsvContent ReadCsv(SessionData sd,string sFileName) {
      var ds = DataService.Instance;
      var lHeaders=new List<string>();
      var laRows=new List<CsvContent.Row>();
      var sFilePath=ds.GetDataDir(sd) + "conf/"+sFileName;
      if (System.IO.File.Exists(sFilePath)) {
        var sr = new System.IO.StreamReader(sFilePath);
        try {
          var sLine = sr.ReadLine();
          lHeaders.AddRange(sLine.Split(',',StringSplitOptions.None));
          while (true) {
            sLine=sr.ReadLine();
            if (string.IsNullOrEmpty(sLine)) {
              break;
            } else {
              DateTime? dt=null;
              var lRowColumns=new List<double>();
              foreach (var sCol in sLine.Split(',')) {
                if (!dt.HasValue) {
                  dt=DateTime.Parse(sCol);
                } else {
                  lRowColumns.Add(ConvInvar.ToDouble(sCol));
                }
              }
              laRows.Add(new CsvContent.Row {
                DateTime=dt.Value,
                Columns=lRowColumns.ToArray(),
              });
            }
          }
        } finally {
          sr.Close();
        }
      }
      var csvContent=new CsvContent {
        Headers=lHeaders.ToArray(),
        Rows=laRows.ToArray(),
      };
      return csvContent;
    }
  }
}
