using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazorise;
using ChartJs.Blazor;
using ChartJs.Blazor.BarChart;
using ChartJs.Blazor.BarChart.Axes;
using ChartJs.Blazor.Common;
using ChartJs.Blazor.Common.Axes;
using ChartJs.Blazor.Common.Axes.Ticks;
using ChartJs.Blazor.Common.Enums;
using ChartJs.Blazor.Common.Handlers;
using ChartJs.Blazor.Common.Time;
using ChartJs.Blazor.LineChart;
using ChartJs.Blazor.Util;
using GoogleMapsComponents;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap.Pages.Statistics
{
  public partial class Summary : ComponentBase
  {
    //
    private int ProjectYearBegin;
    private int ProjectYearEnd;
    private class DataByYears_t
    {
      public string Name;
      public string AllYears;
      public string[] ByYear;
    }
    private bool ShowDetails = false;
    private readonly List<DataByYears_t> IndividualData = new List<DataByYears_t>();
    private readonly List<DataByYears_t> AnonymeData = new List<DataByYears_t>();
    private readonly List<DataByYears_t> OtherData = new List<DataByYears_t>();
    private readonly List<DataByYears_t> CatData = new List<DataByYears_t>();
    protected override void OnInitialized() {
      base.OnInitialized();
      this.ProjectYearBegin = this.SD.CurrentProject.StartDate.Value.Year;
      this.ProjectYearEnd = DateTime.Now.Year;
      this.SD.Filters.FilterChanged += (sender, ev) => {
        this.RefreshData();
        base.InvokeAsync(this.StateHasChanged);
      };
      this.RefreshData();
    }
    private void RefreshData() {
      //
      this.IndividualData.Clear();
      {
        Tuple<string, string>[] aIndiSpecs;
        if (this.ShowDetails) {
          aIndiSpecs = new[] {
            new Tuple<string,string>(this.Localize["Totals"],""),
            new Tuple<string,string>(">= 38 mm","indivdata.headbodylength>=38"),
            new Tuple<string,string>(">= 33 mm","indivdata.headbodylength>=33"),
            new Tuple<string,string>("< 33 mm","indivdata.headbodylength<33"),
            new Tuple<string,string>("0 "+this.Localize["Hibernations"],"indivdata.winters=0"),
            new Tuple<string,string>("1 "+this.Localize["Hibernations"],"indivdata.winters=1"),
            new Tuple<string,string>("1+ "+this.Localize["Hibernations"],"indivdata.winters>=1"),
            new Tuple<string,string>("2 "+this.Localize["Hibernations"],"indivdata.winters=2"),
            new Tuple<string,string>("2+ "+this.Localize["Hibernations"],"indivdata.winters>=2"),
            new Tuple<string,string>("3 "+this.Localize["Hibernations"],"indivdata.winters=3"),
            new Tuple<string,string>("3+ "+this.Localize["Hibernations"],"indivdata.winters>=3"),
            new Tuple<string,string>(this.Localize["Female"],"indivdata.gender='f'"),
            new Tuple<string,string>(this.Localize["Male"],"indivdata.gender='m'"),
            new Tuple<string,string>(this.Localize["Female"]+" 1+ "+this.Localize["Hibernations"],"indivdata.gender='f' AND indivdata.winters>=1"),
            new Tuple<string,string>(this.Localize["Male"]+" 1+ "+this.Localize["Hibernations"],"indivdata.gender='m' AND indivdata.winters>=1"),
            new Tuple<string,string>(this.Localize["Female"]+" 2+ "+this.Localize["Hibernations"],"indivdata.gender='f' AND indivdata.winters>=2"),
            new Tuple<string,string>(this.Localize["Male"]+" 2+ "+this.Localize["Hibernations"],"indivdata.gender='m' AND indivdata.winters>=2"),
          };
        } else {
          aIndiSpecs = new[] {
            new Tuple<string,string>(this.Localize["Totals"],""),
            new Tuple<string,string>("0 "+this.Localize["Hibernations"],"indivdata.winters=0"),
            new Tuple<string,string>("1 "+this.Localize["Hibernations"],"indivdata.winters=1"),
            new Tuple<string,string>("2+ "+this.Localize["Hibernations"],"indivdata.winters>=2"),
            new Tuple<string,string>(this.Localize["Female"]+" 2+ "+this.Localize["Hibernations"],"indivdata.gender='f' AND indivdata.winters>=2"),
            new Tuple<string,string>(this.Localize["Male"]+" 2+ "+this.Localize["Hibernations"],"indivdata.gender='m' AND indivdata.winters>=2"),
          };
        }
        if (this.SD.CurrentProject.DisplayCaptivityBred) {
          aIndiSpecs = aIndiSpecs.Append(new Tuple<string, string>(this.Localize["Bred in captivity"], Filters.GetCaptivityBredFilterExp("1") + "")).ToArray();
          aIndiSpecs = aIndiSpecs.Append(new Tuple<string, string>(this.Localize["Bred in captivity"] + ", 0 " + this.Localize["Hibernations"], Filters.GetCaptivityBredFilterExp("1") + " AND indivdata.winters=0")).ToArray();
          aIndiSpecs = aIndiSpecs.Append(new Tuple<string, string>(this.Localize["Bred in captivity"] + ", 1 " + this.Localize["Hibernations"], Filters.GetCaptivityBredFilterExp("1") + " AND indivdata.winters=1")).ToArray();
          aIndiSpecs = aIndiSpecs.Append(new Tuple<string, string>(this.Localize["Bred in captivity"] + ", 2+ " + this.Localize["Hibernations"], Filters.GetCaptivityBredFilterExp("1") + " AND indivdata.winters>=2")).ToArray();
          aIndiSpecs = aIndiSpecs.Append(new Tuple<string, string>(this.Localize["Bred in captivity"] + ", " + this.Localize["Female"] + ", 2+" + this.Localize["Hibernations"], Filters.GetCaptivityBredFilterExp("1") + " AND indivdata.gender='f' AND indivdata.winters>=2")).ToArray();
          aIndiSpecs = aIndiSpecs.Append(new Tuple<string, string>(this.Localize["Bred in captivity"] + ", " + this.Localize["Male"] + ", 2+" + this.Localize["Hibernations"], Filters.GetCaptivityBredFilterExp("1") + " AND indivdata.gender='m' AND indivdata.winters>=2")).ToArray();
        }
        if (this.SD.CurrentProject.PhenotypeArray.Length >= 2) {
          foreach (int idx in Enumerable.Range(0, this.SD.CurrentProject.PhenotypeArray.Length)) {
            string sVariant = this.Localize["Phenotype"] + " " + this.SD.CurrentProject.PhenotypeArray[idx];
            aIndiSpecs = aIndiSpecs.Append(new Tuple<string, string>(sVariant, $"indivdata.phenotypeidx={idx}")).ToArray();
            aIndiSpecs = aIndiSpecs.Append(new Tuple<string, string>(sVariant + ", 0 " + this.Localize["Hibernations"], $"indivdata.phenotypeidx={idx} AND indivdata.winters=0")).ToArray();
            aIndiSpecs = aIndiSpecs.Append(new Tuple<string, string>(sVariant + ", 1 " + this.Localize["Hibernations"], $"indivdata.phenotypeidx={idx} AND indivdata.winters=1")).ToArray();
            aIndiSpecs = aIndiSpecs.Append(new Tuple<string, string>(sVariant + ", 2+ " + this.Localize["Hibernations"], $"indivdata.phenotypeidx={idx} AND indivdata.winters>=2")).ToArray();
            aIndiSpecs = aIndiSpecs.Append(new Tuple<string, string>(sVariant + ", " + this.Localize["Female"] + ", 2+" + this.Localize["Hibernations"], $"indivdata.phenotypeidx={idx} AND indivdata.gender='f' AND indivdata.winters>=2")).ToArray();
            aIndiSpecs = aIndiSpecs.Append(new Tuple<string, string>(sVariant + ", " + this.Localize["Male"] + ", 2+" + this.Localize["Hibernations"], $"indivdata.phenotypeidx={idx} AND indivdata.gender='m' AND indivdata.winters>=2")).ToArray();
          }
        }
        if (this.SD.CurrentProject.GenotypeArray.Length >= 2) {
          foreach (int idx in Enumerable.Range(0, this.SD.CurrentProject.GenotypeArray.Length)) {
            string sVariant = this.Localize["Genotype"] + " " + this.SD.CurrentProject.GenotypeArray[idx];
            aIndiSpecs = aIndiSpecs.Append(new Tuple<string, string>(sVariant, $"indivdata.genotypeidx={idx}")).ToArray();
            aIndiSpecs = aIndiSpecs.Append(new Tuple<string, string>(sVariant + ", 0 " + this.Localize["Hibernations"], $"indivdata.genotypeidx={idx} AND indivdata.winters=0")).ToArray();
            aIndiSpecs = aIndiSpecs.Append(new Tuple<string, string>(sVariant + ", 1 " + this.Localize["Hibernations"], $"indivdata.genotypeidx={idx} AND indivdata.winters=1")).ToArray();
            aIndiSpecs = aIndiSpecs.Append(new Tuple<string, string>(sVariant + ", 2+ " + this.Localize["Hibernations"], $"indivdata.genotypeidx={idx} AND indivdata.winters>=2")).ToArray();
            aIndiSpecs = aIndiSpecs.Append(new Tuple<string, string>(sVariant + ", " + this.Localize["Female"] + ", 2+" + this.Localize["Hibernations"], $"indivdata.genotypeidx={idx} AND indivdata.gender='f' AND indivdata.winters>=2")).ToArray();
            aIndiSpecs = aIndiSpecs.Append(new Tuple<string, string>(sVariant + ", " + this.Localize["Male"] + ", 2+" + this.Localize["Hibernations"], $"indivdata.genotypeidx={idx} AND indivdata.gender='m' AND indivdata.winters>=2")).ToArray();
          }
        }
        foreach (Tuple<string, string> indiSpec in aIndiSpecs) {
          Dictionary<int, List<Element>> aaIndisByIId = this.DS.GetIndividuals(this.SD, null, indiSpec.Item2);
          int nAllYears = 0;
          int[] aByYears = new int[this.ProjectYearEnd + 1 - this.ProjectYearBegin];
          foreach (int iid in aaIndisByIId.Keys) {
            foreach (Element el in aaIndisByIId[iid]) {
              int year = el.ElementProp.CreationTime.Year;
              if (year >= this.ProjectYearBegin && year <= this.ProjectYearEnd) {
                nAllYears++;
                break;
              }
            }
            for (int year = this.ProjectYearBegin; year <= this.ProjectYearEnd; year++) {
              foreach (Element el in aaIndisByIId[iid]) {
                if (el.ElementProp.CreationTime.Year == year) {
                  aByYears[year - this.ProjectYearBegin]++;
                  break;
                }
              }
            }
          }
          this.IndividualData.Add(
            new DataByYears_t {
              Name = indiSpec.Item1,
              AllYears = nAllYears.ToString(),
              ByYear = Array.ConvertAll<int, string>(aByYears, (n) => n.ToString()),
            }
          );
        }
      }
      //
      {
        Dictionary<int, List<Element>> aaIndisByIId = this.DS.GetIndividuals(this.SD);
        Tuple<string, Func<List<Element>, int, bool>>[] aIndiSpecs;
        if (this.ShowDetails) {
          aIndiSpecs = new Tuple<string, Func<List<Element>, int, bool>>[] {
        new Tuple<string,Func<List<Element>,int,bool>>(
        this.Localize["Missed individuals"],
        (ea,year)=>ea[^1].ElementProp.CreationTime.Year==year-1),
        new Tuple<string,Func<List<Element>,int,bool>>(
        this.Localize["Missed individuals"]+" 0 "+this.Localize["Hibernations"],
        (ea,year)=>ea[^1].ElementProp.CreationTime.Year==year-1 && ea[^1].GetWinters()<1),
        new Tuple<string,Func<List<Element>,int,bool>>(
        this.Localize["Missed individuals"]+" 1 "+this.Localize["Hibernations"],
        (ea,year)=>ea[^1].ElementProp.CreationTime.Year==year-1 && ea[^1].GetWinters()==1),
        new Tuple<string,Func<List<Element>,int,bool>>(
        this.Localize["Missed individuals"]+" 1+ "+this.Localize["Hibernations"],
        (ea,year)=>ea[^1].ElementProp.CreationTime.Year==year-1 && ea[^1].GetWinters()>=1),
        new Tuple<string,Func<List<Element>,int,bool>>(
        this.Localize["Missed individuals"]+" 2 "+this.Localize["Hibernations"],
        (ea,year)=>ea[^1].ElementProp.CreationTime.Year==year-1 && ea[^1].GetWinters()==2),
        new Tuple<string,Func<List<Element>,int,bool>>(
        this.Localize["Missed individuals"]+" 2+ "+this.Localize["Hibernations"],
        (ea,year)=>ea[^1].ElementProp.CreationTime.Year==year-1 && ea[^1].GetWinters()>=2),
        new Tuple<string,Func<List<Element>,int,bool>>(
        this.Localize["New individuals"],
        (ea,year)=>ea[0].ElementProp.CreationTime.Year==year),
        new Tuple<string,Func<List<Element>,int,bool>>(
        this.Localize["New individuals"]+" 0 "+this.Localize["Hibernations"],
        (ea,year)=>ea[0].ElementProp.CreationTime.Year==year && ea[0].GetWinters()<1),
        new Tuple<string,Func<List<Element>,int,bool>>(
        this.Localize["New individuals"]+" 1 "+this.Localize["Hibernations"],
        (ea,year)=>ea[0].ElementProp.CreationTime.Year==year && ea[0].GetWinters()==1),
        new Tuple<string,Func<List<Element>,int,bool>>(
        this.Localize["New individuals"]+" 1+ "+this.Localize["Hibernations"],
        (ea,year)=>ea[0].ElementProp.CreationTime.Year==year && ea[0].GetWinters()>=1),
        new Tuple<string,Func<List<Element>,int,bool>>(
        this.Localize["New individuals"]+" 2 "+this.Localize["Hibernations"],
        (ea,year)=>ea[0].ElementProp.CreationTime.Year==year && ea[0].GetWinters()==2),
        new Tuple<string,Func<List<Element>,int,bool>>(
        this.Localize["New individuals"]+" 2+ "+this.Localize["Hibernations"],
        (ea,year)=>ea[0].ElementProp.CreationTime.Year==year && ea[0].GetWinters()>=2),
      };
        } else {
          aIndiSpecs = new Tuple<string, Func<List<Element>, int, bool>>[] {
        new Tuple<string,Func<List<Element>,int,bool>>(
        this.Localize["Missed individuals"],
        (ea,year)=>ea[^1].ElementProp.CreationTime.Year==year-1),
        new Tuple<string,Func<List<Element>,int,bool>>(
        this.Localize["New individuals"],
        (ea,year)=>ea[0].ElementProp.CreationTime.Year==year),
      };
        }
        foreach (Tuple<string, Func<List<Element>, int, bool>> indiSpec in aIndiSpecs) {
          var lCounts = new List<int>();
          for (int year = this.ProjectYearBegin; year <= this.ProjectYearEnd; year++) {
            int nCount = 0;
            foreach (List<Element> ea in aaIndisByIId.Values) {
              if (indiSpec.Item2(ea, year)) {
                nCount++;
              }
            }
            lCounts.Add(nCount);
          }
          this.IndividualData.Add(
            new DataByYears_t {
              Name = indiSpec.Item1,
              AllYears = "",
              ByYear = Array.ConvertAll<int, string>(lCounts.ToArray(), (n) => n.ToString()),
            }
          );
        }
      }
      //
      this.AnonymeData.Clear();
      {
        string sClassName = "Living being";
        Element[] aEls = this.DS.GetElements(this.SD, null, "elements.classname='" + sClassName + "'")
          .Where(el => el.Classification?.LivingBeing?.Taxon?.SciName == this.SD.CurrentProject.SpeciesSciName).ToArray();
        foreach (ElementClassification.Stadium stadium in new[] {
          ElementClassification.Stadium.Eggs,
          ElementClassification.Stadium.Larvae,
          ElementClassification.Stadium.Juveniles,
          ElementClassification.Stadium.Adults,
          ElementClassification.Stadium.Deads,
        }) {
          Element[] aElementsInStadium = aEls.Where(el => el.Classification.LivingBeing.Stadium == stadium).ToArray();
          int nAllYears = 0;
          int[] aByYears = new int[this.ProjectYearEnd + 1 - this.ProjectYearBegin];
          foreach (Element el in aElementsInStadium) {
            int year = el.ElementProp.CreationTime.Year;
            if (year >= this.ProjectYearBegin && year <= this.ProjectYearEnd) {
              nAllYears += el.Classification.LivingBeing.Count;
            }
          }
          for (int year = this.ProjectYearBegin; year <= this.ProjectYearEnd; year++) {
            foreach (Element el in aElementsInStadium) {
              if (el.ElementProp.CreationTime.Year == year) {
                aByYears[year - this.ProjectYearBegin] += el.Classification.LivingBeing.Count;
              }
            }
          }
          this.AnonymeData.Add(
            new DataByYears_t {
              Name = this.Localize[Enum.GetName(stadium)],
              AllYears = nAllYears.ToString(),
              ByYear = Array.ConvertAll<int, string>(aByYears, (n) => n.ToString()),
            }
          );
        }
      }
      //
      this.OtherData.Clear();
      foreach (Tuple<string, string> indiSpec in new[] {
        new Tuple<string,string>(this.Localize["Totals"],""),
        new Tuple<string,string>(this.Localize["ID Cards"],WhereClauses.Is_ID_photo),
        new Tuple<string,string>(this.Localize["Other photos and local news"],"NOT "+WhereClauses.Is_ID_photo),
      }) {
        Element[] aEls = this.DS.GetElements(this.SD, null, indiSpec.Item2);
        int nAllYears = 0;
        int[] aByYears = new int[this.ProjectYearEnd + 1 - this.ProjectYearBegin];
        foreach (Element el in aEls) {
          int year = el.ElementProp.CreationTime.Year;
          if (year >= this.ProjectYearBegin && year <= this.ProjectYearEnd) {
            nAllYears++;
          }
        }
        for (int year = this.ProjectYearBegin; year <= this.ProjectYearEnd; year++) {
          foreach (Element el in aEls) {
            if (el.ElementProp.CreationTime.Year == year) {
              aByYears[year - this.ProjectYearBegin]++;
            }
          }
        }
        this.OtherData.Add(
          new DataByYears_t {
            Name = indiSpec.Item1,
            AllYears = nAllYears.ToString(),
            ByYear = Array.ConvertAll<int, string>(aByYears, (n) => n.ToString()),
          }
        );
      }
      //
      this.CatData.Clear();
      foreach (string sClassName in ElementClassification.ClassNames) {
        Element[] aEls = this.DS.GetElements(this.SD, null, "elements.classname='" + sClassName + "'");
        int nAllYears = 0;
        int[] aByYears = new int[this.ProjectYearEnd + 1 - this.ProjectYearBegin];
        foreach (Element el in aEls) {
          int year = el.ElementProp.CreationTime.Year;
          if (year >= this.ProjectYearBegin && year <= this.ProjectYearEnd) {
            nAllYears++;
          }
        }
        for (int year = this.ProjectYearBegin; year <= this.ProjectYearEnd; year++) {
          foreach (Element el in aEls) {
            if (el.ElementProp.CreationTime.Year == year) {
              aByYears[year - this.ProjectYearBegin]++;
            }
          }
        }
        this.CatData.Add(
          new DataByYears_t {
            Name = this.Localize[sClassName],
            AllYears = nAllYears.ToString(),
            ByYear = Array.ConvertAll<int, string>(aByYears, (n) => n.ToString()),
          }
        );
      }
    }
  }
}
