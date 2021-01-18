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
    public Filters(Func<User> getUserFunc) {
      this.GetUserFunc=getUserFunc;
    }
    private readonly Func<User> GetUserFunc;

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
    public string GenderFilter {
      get {
        return this._GenderFilter;
      }
      set {
        if (value!=this._GenderFilter) {
          this._GenderFilter=value;
          Utilities.FireEvent(this.FilterChanged,this,EventArgs.Empty);
        }
      }
    }
    private string _GenderFilter = "";
    public string HibernationsFilter {
      get {
        return this._HibernationsFilter;
      }
      set {
        if (value!=this._HibernationsFilter) {
          this._HibernationsFilter=value;
          Utilities.FireEvent(this.FilterChanged,this,EventArgs.Empty);
        }
      }
    }
    private string _HibernationsFilter = "";
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
    public DateTime? DateFromFilter {
      get {
        return this._DateFromFilter;
      }
      set {
        if (value!=this._DateFromFilter) {
          this._DateFromFilter=value;
          Utilities.FireEvent(this.FilterChanged,this,EventArgs.Empty);
        }
      }
    }
    private DateTime? _DateFromFilter = null;
    public DateTime? DateToFilter {
      get {
        return this._DateToFilter;
      }
      set {
        if (value!=this._DateToFilter) {
          this._DateToFilter=value;
          Utilities.FireEvent(this.FilterChanged,this,EventArgs.Empty);
        }
      }
    }
    private DateTime? _DateToFilter = null;
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
    public string ClassFilter {
      get {
        return this._ClassFilter;
      }
      set {
        if (value!=this._ClassFilter) {
          this._ClassFilter=value;
          Utilities.FireEvent(this.FilterChanged,this,EventArgs.Empty);
        }
      }
    }
    private string _ClassFilter = "";
    public string LogUserFilter {
      get {
        return this._LogUserFilter;
      }
      set {
        if (value!=this._LogUserFilter) {
          this._LogUserFilter=value;
          Utilities.FireEvent(this.FilterChanged,this,EventArgs.Empty);
        }
      }
    }
    private string _LogUserFilter = "";
    public string LogActionFilter {
      get {
        return this._LogActionFilter;
      }
      set {
        if (value!=this._LogActionFilter) {
          this._LogActionFilter=value;
          Utilities.FireEvent(this.FilterChanged,this,EventArgs.Empty);
        }
      }
    }
    private string _LogActionFilter = "";
    public string NotesAuthorFilter {
      get {
        return this._NotesAuthorFilter;
      }
      set {
        if (value!=this._NotesAuthorFilter) {
          this._NotesAuthorFilter=value;
          Utilities.FireEvent(this.FilterChanged,this,EventArgs.Empty);
        }
      }
    }
    private string _NotesAuthorFilter = "";
    public string NotesTextFilter {
      get {
        return this._NotesTextFilter;
      }
      set {
        if (value!=this._NotesTextFilter) {
          this._NotesTextFilter=value;
          Utilities.FireEvent(this.FilterChanged,this,EventArgs.Empty);
        }
      }
    }
    private string _NotesTextFilter = "";
    public static char[] NegateChars { get; } = new char[] { '-','!','^' };
    public static char[] SeparateChars { get; } = new char[] { ' ',',',';','|','+' };
    private static string GetFilterTermForWhereInClause(string sFilter) {
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
        foreach (var sPart in saParts) {
          sSqlIndis+=sDelim+"'"+sPart+"'";
          sDelim=",";
        }
        sSqlIndis+=")";
        sFilterTerm=(bNegate ? " NOT" : "")+" IN "+sSqlIndis;
      }
      return sFilterTerm;
    }
    private static string AddToWhereInClause(string sBasicWhereClause,string sTableRow,string sFilter) {
      string sWhereClause = sBasicWhereClause;
      var sFilterTerm = GetFilterTermForWhereInClause(sFilter);
      if (!string.IsNullOrEmpty(sFilterTerm)) {
        sWhereClause = Filters.AddToWhereClause(sBasicWhereClause,sTableRow+sFilterTerm);
      }
      return sWhereClause;
    }
    private static string AddToWhereCompareClause(string sBasicWhereClause,string sTableRow,string sFilter) {
      string sWhereClause = sBasicWhereClause;
      var sFilterTerm = sFilter?.Replace("{0}",sTableRow)?.Trim();
      if (!string.IsNullOrEmpty(sFilterTerm)) {
        sWhereClause = Filters.AddToWhereClause(sBasicWhereClause,sFilterTerm);
      }
      return sWhereClause;
    }
    public static string AddToWhereClause(string sBasicWhereClause,string sWhereClause) {
      System.Text.StringBuilder sb = new System.Text.StringBuilder(sBasicWhereClause);
      if (!string.IsNullOrEmpty(sWhereClause)) {
        if (!string.IsNullOrEmpty(sBasicWhereClause)) {
          sb.Append(" AND ");
        }
        sb.Append('(');
        sb.Append(sWhereClause);
        sb.Append(')');
      }
      return sb.ToString();
    }
    private string GetDateFilterWhereClause() {
      var sb = new System.Text.StringBuilder();
      if (this.DateFromFilter.HasValue) {
        sb.Append("elements.creationtime>='"+this.DateFromFilter.Value.ToString("yyyy-MM-dd")+"'");
      }
      if (this.DateToFilter.HasValue) {
        if (sb.Length>=1) {
          sb.Append(" AND ");
        }
        sb.Append("elements.creationtime<='"+this.DateToFilter.Value.ToString("yyyy-MM-dd")+"'");
      }
      return sb.ToString();
    }
    public string AddAllFiltersToWhereClause(string sBasicWhereClause) {
      string sResult = sBasicWhereClause;
      if (this.FilteringTarget==FilteringTargetEnum.Individuals) {
        sResult = Filters.AddToWhereClause(sResult,this.GetDateFilterWhereClause());
        sResult = AddToWhereInClause(sResult,"elements.place",ExpandPlaceFilter(this.PlaceFilter));
        sResult = AddToWhereInClause(sResult,"indivdata.iid",ExpandIndiFilter(this.IndiFilter));
        sResult = AddToWhereInClause(sResult,"indivdata.gender",ExpandGenderFilter(this.GenderFilter));
        sResult = AddToWhereCompareClause(sResult,"indivdata.winters",ExpandHibernationsFilter(this.HibernationsFilter));
        if (this.OnlyFirstIndiFilter) {
          sResult = Filters.AddToWhereClause(sResult,this.OnlyFirstIndiFilterExp);
        }
        if (this.OnlyLastIndiFilter) {
          sResult = Filters.AddToWhereClause(sResult,this.OnlyLastIndiFilterExp);
        }
      }
      if (this.FilteringTarget==FilteringTargetEnum.Elements) {
        sResult = Filters.AddToWhereClause(sResult,this.GetDateFilterWhereClause());
        sResult = AddToWhereInClause(sResult,"elements.place",ExpandPlaceFilter(this.PlaceFilter));
        sResult = AddToWhereInClause(sResult,"elements.category",ExpandCatFilter(this.CatFilter));
        if (!string.IsNullOrEmpty(this.ClassFilter)) {
          var classification=JsonConvert.DeserializeObject<ElementClassification>(this.ClassFilter);
          if (!string.IsNullOrEmpty(classification.ClassName)) {
            sResult = AddToWhereClause(sResult,"elements.classification LIKE '%\"ClassName\":\""+classification.ClassName+"\"%'");
            if (string.CompareOrdinal(classification.ClassName,"Living being")==0) {
              if (!string.IsNullOrEmpty(classification.LivingBeing?.Species?.SciName)) {
                sResult = AddToWhereClause(sResult,"elements.classification LIKE '%\"SciName\":\""+classification.LivingBeing.Species.SciName+"\"%'");
              }
              if (classification.LivingBeing!=null && classification.LivingBeing.Stadium!=ElementClassification.Stadium.None) {
                sResult = AddToWhereClause(sResult,"elements.classification LIKE '%\"Stadium\":"+((int)classification.LivingBeing.Stadium)+",%'");
              }
            }
            if (string.CompareOrdinal(classification.ClassName,"Habitat")==0) {
              if (classification.Habitat!=null && classification.Habitat.Quality!=0) {
                sResult = AddToWhereClause(sResult,"elements.classification LIKE '%\"Quality\":"+classification.Habitat.Quality+"%'");
              }
              if (classification.Habitat!=null && classification.Habitat.Monitoring) {
                sResult = AddToWhereClause(sResult,"elements.classification LIKE '%\"Monitoring\":true%'");
              }
            }
          }
        }
      }
      if (this.FilteringTarget==FilteringTargetEnum.Log) {
        var user = this.GetUserFunc();
        if (user==null || user.Level<400) {
          sResult = Filters.AddToWhereClause(sResult,"log.user='"+user.EMail+"'");
        }
        if (!string.IsNullOrEmpty(this.LogUserFilter)) {
          sResult = Filters.AddToWhereClause(sResult,"log.user LIKE '"+this.LogUserFilter+"'");
        }
        if (!string.IsNullOrEmpty(this.LogActionFilter)) {
          sResult = Filters.AddToWhereClause(sResult,"log.action LIKE '%"+this.LogActionFilter+"%'");
        }
      }
      if (this.FilteringTarget==FilteringTargetEnum.Notes) {
        var user = this.GetUserFunc();
        if (user==null || user.Level<400) {
          sResult = Filters.AddToWhereClause(sResult,"notes.author='"+user.EMail+"'");
        }
        if (!string.IsNullOrEmpty(this.NotesAuthorFilter)) {
          sResult = Filters.AddToWhereClause(sResult,"notes.author LIKE '"+this.NotesAuthorFilter+"'");
        }
        if (!string.IsNullOrEmpty(this.NotesTextFilter)) {
          sResult = Filters.AddToWhereClause(sResult,"notes.text LIKE '%"+this.NotesTextFilter+"%'");
        }
      }
      return sResult;
    }
    private static string ExpandIndiFilter(string sFilter) {
      return ExpandFilter(sFilter,(sIndiA,sIndiB) => {
        var sbExp = new System.Text.StringBuilder();
        int nIndiA = int.Parse(sIndiA);
        int nIndiB = int.Parse(sIndiB);
        for (int nIndi = nIndiA;nIndi<=nIndiB;nIndi++) {
          sbExp.Append(ConvInvar.ToString(nIndi));
          sbExp.Append(' ');
        }
        return sbExp.ToString();
      });
    }
    private static string ExpandGenderFilter(string sFilter) {
      if (sFilter=="-" || sFilter=="*") {
        return "";
      } else {
        var sResult = "";
        if (sFilter.Contains("j")) {
          sResult+="j0 j1 j2 j3 ";
        }
        if (sFilter.Contains("m")) {
          sResult+="ma ";
        }
        if (sFilter.Contains("f")) {
          sResult+="fa ";
        }
        return sResult.Trim();
      }
    }
    private static string ExpandHibernationsFilter(string sFilter) {
      string sResult = sFilter.Trim();
      if (sResult=="-" || sResult=="*") {
        return "";
      }
      return sResult;
    }
    private static string ExpandCatFilter(string sFilter) {
      return ExpandFilter(sFilter,(sCatA,sCatB) => {
        var sbExp = new System.Text.StringBuilder();
        int nCatA = int.Parse(sCatA);
        int nCatB = int.Parse(sCatB);
        for (int nCat = nCatA;nCat<=nCatB;nCat++) {
          sbExp.Append(ConvInvar.ToString(nCat));
          sbExp.Append(' ');
        }
        return sbExp.ToString();
      });
    }
    private static string ExpandPlaceFilter(string sFilter) {
      return sFilter.ToUpperInvariant();
    }
    private static string ExpandFilter(string sFilter,Func<string,string,string> funcExpand) {
      var sbExp = new System.Text.StringBuilder();
      if (sFilter.StartsWith("!") || sFilter.StartsWith("^") || sFilter.StartsWith("-")) {
        sbExp.Append('!');
        sFilter=sFilter.Substring(1);
      }
      var saParts = sFilter.Split(Filters.SeparateChars);
      for (int i=0;i<saParts.Length;i++) {
        var sPart = saParts[i];
        var sa1=System.Text.RegularExpressions.Regex.Split(sPart,@"(\d+)(-|\.\.)(\d+)");
        if (sa1.Length==1) {
          sbExp.Append(sa1[0]);
          sbExp.Append(' ');
        } else if (sa1.Length==5) {
          sbExp.Append(funcExpand(sa1[1],sa1[3]));
        }
      }
      return sbExp.ToString().Trim();
    }
  }
}
