using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SQLite;
using Newtonsoft.Json;

namespace BioMap
{
  public class Filters
  {
    public enum FilteringTargetEnum
    {
      None=0,
      Elements,
      Individuals,
      Log,
      Notes,
    }
    public event EventHandler FilterChanged;
    public FilteringTargetEnum FilteringTarget {
      get {
        return this._FilteringTarget;
      }
      set {
        if (value!=this._FilteringTarget) {
          this._FilteringTarget=value;
          Utilities.FireEvent(this.FilterChanged,this,EventArgs.Empty);
        }
      }
    }
    private FilteringTargetEnum _FilteringTarget = FilteringTargetEnum.None;
    public string IndiFilter {
      get {
        return this._IndiFilter;
      }
      set {
        if (value!=this._IndiFilter) {
          this._IndiFilter=value;
          Utilities.FireEvent(this.FilterChanged,this,EventArgs.Empty);
        }
      }
    }
    private string _IndiFilter = "";
    public bool OnlyFirstIndiFilter {
      get {
        return this._OnlyFirstIndiFilter;
      }
      set {
        if (value!=this._OnlyFirstIndiFilter) {
          this._OnlyFirstIndiFilter=value;
          Utilities.FireEvent(this.FilterChanged,this,EventArgs.Empty);
        }
      }
    }
    private bool _OnlyFirstIndiFilter = false;
    private readonly string OnlyFirstIndiFilterExp =
      "NOT EXISTS (" +
      "SELECT * FROM elements e1" +
      " LEFT JOIN indivdata i1 ON (i1.name=e1.name)" +
      " WHERE (i1.iid=indivdata.iid AND e1.category=350 AND e1.creationtime<elements.creationtime)" +
      ")";
    public bool OnlyLastIndiFilter {
      get {
        return this._OnlyLastIndiFilter;
      }
      set {
        if (value!=this._OnlyLastIndiFilter) {
          this._OnlyLastIndiFilter=value;
          Utilities.FireEvent(this.FilterChanged,this,EventArgs.Empty);
        }
      }
    }
    private bool _OnlyLastIndiFilter = false;
    private readonly string OnlyLastIndiFilterExp =
      "NOT EXISTS (" +
      "SELECT * FROM elements e1" +
      " LEFT JOIN indivdata i1 ON (i1.name=e1.name)" +
      " WHERE (i1.iid=indivdata.iid AND e1.category=350 AND e1.creationtime>elements.creationtime)" +
      ")";
    public bool ExcludeFreshBornFilter {
      get {
        return this._ExcludeFreshBornFilter;
      }
      set {
        if (value!=this._ExcludeFreshBornFilter) {
          this._ExcludeFreshBornFilter=value;
          Utilities.FireEvent(this.FilterChanged,this,EventArgs.Empty);
        }
      }
    }
    private bool _ExcludeFreshBornFilter = false;
    private readonly string ExcludeFreshBornFilterExp = "indivdata.winters>=1";
    public bool OnlyFreshBornFilter {
      get {
        return this._OnlyFreshBornFilter;
      }
      set {
        if (value!=this._OnlyFreshBornFilter) {
          this._OnlyFreshBornFilter=value;
          Utilities.FireEvent(this.FilterChanged,this,EventArgs.Empty);
        }
      }
    }
    private bool _OnlyFreshBornFilter = false;
    private readonly string OnlyFreshBornFilterExp = "indivdata.winters<1";
    public string PlaceFilter {
      get {
        return this._PlaceFilter;
      }
      set {
        if (value!=this._PlaceFilter) {
          this._PlaceFilter=value;
          Utilities.FireEvent(this.FilterChanged,this,EventArgs.Empty);
        }
      }
    }
    private string _PlaceFilter = "";
    public string CatFilter {
      get {
        return this._CatFilter;
      }
      set {
        if (value!=this._CatFilter) {
          this._CatFilter=value;
          Utilities.FireEvent(this.FilterChanged,this,EventArgs.Empty);
        }
      }
    }
    private string _CatFilter = "";
    public static char[] NegateChars { get; } = new char[] { '-','!','^' };
    public static char[] SeparateChars { get; } = new char[] { ' ',',',';','|','+' };
    private string GetFilterTermForWhereClause(string sFilter) {
      string sFilterTerm = null;
      if (!string.IsNullOrEmpty(sFilter)) {
        string sList = sFilter;
        string[] saParts;
        bool bNegate = (sList.IndexOfAny(Filters.NegateChars)==0);
        if (bNegate) {
          sList=sList.Substring(1);
        }
        saParts=sList.Split(Filters.SeparateChars);
        string sSqlIndis = "";
        string sDelim = "(";
        foreach (var sPlace in saParts) {
          sSqlIndis+=sDelim+"'"+sPlace.ToUpperInvariant()+"'";
          sDelim=",";
        }
        sSqlIndis+=")";
        sFilterTerm=(bNegate ? " NOT" : "")+" IN "+sSqlIndis;
      }
      return sFilterTerm;
    }
    private string AddToWhereClause(string sBasicWhereClause,string sTableRow,string sFilter) {
      string sWhereClause = sBasicWhereClause;
      var sFilterTerm = this.GetFilterTermForWhereClause(sFilter);
      if (!string.IsNullOrEmpty(sFilterTerm)) {
        string sList = this.IndiFilter;
        string[] saIndis;
        bool bNegate = (sList.IndexOfAny(Filters.NegateChars)==0);
        if (bNegate) {
          sList=sList.Substring(1);
        }
        saIndis=sList.Split(Filters.SeparateChars);
        string sSqlIndis = "";
        string sDelim = "(";
        foreach (var sPlace in saIndis) {
          sSqlIndis+=sDelim+"'"+sPlace.ToUpperInvariant()+"'";
          sDelim=",";
        }
        sSqlIndis+=")";
        sWhereClause = Filters.AddToWhereClause(sBasicWhereClause,sTableRow+sFilterTerm);
      }
      return sWhereClause;
    }
    public static string AddToWhereClause(string sBasicWhereClause,string sWhereClause) {
      System.Text.StringBuilder sb = new System.Text.StringBuilder(sBasicWhereClause);
      if (!string.IsNullOrEmpty(sWhereClause)) {
        if (!string.IsNullOrEmpty(sBasicWhereClause)) {
          sb.Append(" AND ");
        }
        sb.Append("(");
        sb.Append(sWhereClause);
        sb.Append(")");
      }
      return sb.ToString();
    }
    public string AddAllFiltersToWhereClause(string sBasicWhereClause) {
      string sResult = sBasicWhereClause;
      if (this.FilteringTarget==FilteringTargetEnum.Individuals) {
        sResult = this.AddToWhereClause(sResult,"elements.place",ExpandPlaceFilter(this.PlaceFilter));
        sResult = this.AddToWhereClause(sResult,"indivdata.iid",ExpandIndiFilter(this.IndiFilter));
        if (this.OnlyFirstIndiFilter) {
          sResult = Filters.AddToWhereClause(sResult,this.OnlyFirstIndiFilterExp);
        }
        if (this.OnlyLastIndiFilter) {
          sResult = Filters.AddToWhereClause(sResult,this.OnlyLastIndiFilterExp);
        }
        if (this.ExcludeFreshBornFilter) {
          sResult = Filters.AddToWhereClause(sResult,this.ExcludeFreshBornFilterExp);
        }
        if (this.OnlyFreshBornFilter) {
          sResult = Filters.AddToWhereClause(sResult,this.OnlyFreshBornFilterExp);
        }
      }
      if (this.FilteringTarget==FilteringTargetEnum.Elements) {
        sResult = this.AddToWhereClause(sResult,"elements.place",ExpandPlaceFilter(this.PlaceFilter));
        sResult = this.AddToWhereClause(sResult,"elements.category",ExpandCatFilter(this.CatFilter));
      }
      return sResult;
    }
    private string ExpandIndiFilter(string sFilter) {
      return ExpandFilter(sFilter,(sIndiA,sIndiB) => {
        var sbExp = new System.Text.StringBuilder();
        int nIndiA = int.Parse(sIndiA);
        int nIndiB = int.Parse(sIndiB);
        for (int nIndi = nIndiA;nIndi<=nIndiB;nIndi++) {
          sbExp.Append(ConvInvar.ToString(nIndi));
          sbExp.Append(" ");
        }
        return sbExp.ToString();
      });
    }
    private string ExpandCatFilter(string sFilter) {
      return ExpandFilter(sFilter,(sCatA,sCatB) => {
        var sbExp = new System.Text.StringBuilder();
        int nCatA = int.Parse(sCatA);
        int nCatB = int.Parse(sCatB);
        for (int nCat = nCatA;nCat<=nCatB;nCat++) {
          sbExp.Append(ConvInvar.ToString(nCat));
          sbExp.Append(" ");
        }
        return sbExp.ToString();
      });
    }
    private string ExpandPlaceFilter(string sFilter) {
      return sFilter;
    }
    private string ExpandFilter(string sFilter,Func<string,string,string> funcExpand) {
      var sbExp = new System.Text.StringBuilder();
      if (sFilter.StartsWith("!") || sFilter.StartsWith("^") || sFilter.StartsWith("-")) {
        sbExp.Append("!");
        sFilter=sFilter.Substring(1);
      }
      var saParts = sFilter.Split(Filters.SeparateChars);
      for (int i=0;i<saParts.Length;i++) {
        var sPart = saParts[i];
        var sa1=System.Text.RegularExpressions.Regex.Split(sPart,@"(\d+)(-|\.\.)(\d+)");
        if (sa1.Length==1) {
          sbExp.Append(sa1[0]);
          sbExp.Append(" ");
        } else if (sa1.Length==5) {
          sbExp.Append(funcExpand(sa1[1],sa1[3]));
        }
      }
      return sbExp.ToString().Trim();
    }
  }
}
