using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Blazorise.Extensions;
using Newtonsoft.Json;

namespace BioMap
{
  public class Filters
  {
    public Filters(Func<User> getUserFunc) {
      this.GetUserFunc = getUserFunc;
    }
    private readonly Func<User> GetUserFunc;

    public enum FilteringTargetEnum
    {
      None = 0,
      Elements,
      Catches,
      CatchesForIdentification,
      Individuals,
      Log,
      Notes,
      Users,
    }
    public event EventHandler FilterChanged;
    public FilteringTargetEnum FilteringTarget {
      get => this._FilteringTarget;
      set {
        if (value != this._FilteringTarget) {
          this._FilteringTarget = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private FilteringTargetEnum _FilteringTarget = FilteringTargetEnum.None;
    public string IndiFilter {
      get => this._IndiFilter;
      set {
        if (value != this._IndiFilter) {
          this._IndiFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private string _IndiFilter = "";

    /// <summary>
    /// Attention: This filter has to be applied by application code!
    /// </summary>
    public bool ExpandIfNoUnidentifiedFilter {
      get => this._ExpandIfNoUnidentifiedFilter;
      set {
        if (value != this._ExpandIfNoUnidentifiedFilter) {
          this._ExpandIfNoUnidentifiedFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private bool _ExpandIfNoUnidentifiedFilter = false;

    public string GenderFilter {
      get => this._GenderFilter;
      set {
        if (value != this._GenderFilter) {
          this._GenderFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private string _GenderFilter = "";
    public string PhenotypeFilter {
      get => this._PhenotypeFilter;
      set {
        if (value != this._PhenotypeFilter) {
          this._PhenotypeFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private string _PhenotypeFilter = "";
    public string GenotypeFilter {
      get => this._GenotypeFilter;
      set {
        if (value != this._GenotypeFilter) {
          this._GenotypeFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private string _GenotypeFilter = "";
    public string CaptivityBredFilter {
      get => this._CaptivityBredFilter;
      set {
        if (value != this._CaptivityBredFilter) {
          this._CaptivityBredFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private string _CaptivityBredFilter = "";
    public static string GetCaptivityBredFilterExp(string sFilter) {
      return 
      (sFilter == "1" ? "" : "NOT ") +
      "EXISTS (" +
      "SELECT * FROM elements e1" +
      " LEFT JOIN indivdata i1 ON (i1.name=e1.name)" +
      " WHERE (i1.iid=indivdata.iid AND " + WhereClauses.Is_ID_photo + " AND i1.captivitybred=1)" +
      ")";
    }
    public string HibernationsFilter {
      get => this._HibernationsFilter;
      set {
        if (value != this._HibernationsFilter) {
          this._HibernationsFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private string _HibernationsFilter = "";
    public float? MissingYearsFilter {
      get => this._MissingYearsFilter;
      set {
        if (value != this._MissingYearsFilter) {
          this._MissingYearsFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private float? _MissingYearsFilter = null;
    public float? BodyLengthMinFilter {
      get => this._BodyLengthMinFilter;
      set {
        if (value != this._BodyLengthMinFilter) {
          this._BodyLengthMinFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private float? _BodyLengthMinFilter = null;
    public float? BodyLengthMaxFilter {
      get => this._BodyLengthMaxFilter;
      set {
        if (value != this._BodyLengthMaxFilter) {
          this._BodyLengthMaxFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private float? _BodyLengthMaxFilter = null;
    public bool OnlyFirstIndiFilter {
      get => this._OnlyFirstIndiFilter;
      set {
        if (value != this._OnlyFirstIndiFilter) {
          this._OnlyFirstIndiFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private bool _OnlyFirstIndiFilter = false;
    private readonly string OnlyFirstIndiFilterExp =
      "NOT EXISTS (" +
      "SELECT * FROM elements e1" +
      " LEFT JOIN indivdata i1 ON (i1.name=e1.name)" +
      " WHERE (i1.iid=indivdata.iid AND " + WhereClauses.Is_ID_photo + " AND e1.creationtime<elements.creationtime)" +
      ")";
    public bool OnlyLastIndiFilter {
      get => this._OnlyLastIndiFilter;
      set {
        if (value != this._OnlyLastIndiFilter) {
          this._OnlyLastIndiFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private bool _OnlyLastIndiFilter = false;
    private readonly string OnlyLastIndiFilterExp =
      "NOT EXISTS (" +
      "SELECT * FROM elements e1" +
      " LEFT JOIN indivdata i1 ON (i1.name=e1.name)" +
      " WHERE (i1.iid=indivdata.iid AND " + WhereClauses.Is_ID_photo + " AND e1.creationtime>elements.creationtime)" +
      ")";
    public bool OnlyWithRecapturesFilter {
      get => this._OnlyWithRecapturesFilter;
      set {
        if (value != this._OnlyWithRecapturesFilter) {
          this._OnlyWithRecapturesFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private bool _OnlyWithRecapturesFilter = false;
    private readonly string OnlyWithRecapturesFilterExp =
      "EXISTS (" +
      "SELECT * FROM elements e1" +
      " LEFT JOIN indivdata i1 ON (i1.name=e1.name)" +
      " WHERE (i1.iid=indivdata.iid AND " + WhereClauses.Is_ID_photo + " AND e1.creationtime<>elements.creationtime)" +
      ")";
    public bool OnlyWithAdultRecapturesFilter {
      get => this._OnlyWithAdultRecapturesFilter;
      set {
        if (value != this._OnlyWithAdultRecapturesFilter) {
          this._OnlyWithAdultRecapturesFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private bool _OnlyWithAdultRecapturesFilter = false;
    public string UserFilter {
      get => this._UserFilter;
      set {
        if (value != this._UserFilter) {
          this._UserFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private string _UserFilter = "";
    public DateTime? DateFromFilter {
      get => this._DateFromFilter;
      set {
        if (value != this._DateFromFilter) {
          this._DateFromFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private DateTime? _DateFromFilter = null;
    public DateTime? DateToFilter {
      get => this._DateToFilter;
      set {
        if (value != this._DateToFilter) {
          this._DateToFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private DateTime? _DateToFilter = null;
    public string PlaceFilter {
      get => this._PlaceFilter;
      set {
        if (value != this._PlaceFilter) {
          this._PlaceFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private string _PlaceFilter = "";
    public string ClassFilter {
      get => this._ClassFilter;
      set {
        if (value != this._ClassFilter) {
          this._ClassFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private string _ClassFilter = "";
    public string LogUserFilter {
      get => this._LogUserFilter;
      set {
        if (value != this._LogUserFilter) {
          this._LogUserFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private string _LogUserFilter = "";
    public string LogActionFilter {
      get => this._LogActionFilter;
      set {
        if (value != this._LogActionFilter) {
          this._LogActionFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private string _LogActionFilter = "";
    public string NotesAuthorFilter {
      get => this._NotesAuthorFilter;
      set {
        if (value != this._NotesAuthorFilter) {
          this._NotesAuthorFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private string _NotesAuthorFilter = "";
    public string NotesTextFilter {
      get => this._NotesTextFilter;
      set {
        if (value != this._NotesTextFilter) {
          this._NotesTextFilter = value;
          Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
        }
      }
    }
    private string _NotesTextFilter = "";
    public static char[] NegateChars { get; } = new char[] { '!', '^' };
    public static char[] SeparateChars { get; } = new char[] { ' ', ',', ';', '|', '+' };
    private static string GetFilterTermForWhereInClause(string sFilter) {
      string sFilterTerm = null;
      if (!string.IsNullOrEmpty(sFilter)) {
        string sList = sFilter;
        string[] saParts;
        bool bNegate = (sList.IndexOfAny(Filters.NegateChars) == 0);
        if (bNegate) {
          sList = sList.Substring(1);
        }
        saParts = sList.Split(Filters.SeparateChars);
        string sSqlIndis = "";
        string sDelim = "(";
        foreach (string sPart in saParts) {
          string sPart1 = (sPart == "-") ? "" : sPart;
          sSqlIndis += sDelim + "'" + sPart1 + "'";
          sDelim = ",";
        }
        sSqlIndis += ")";
        sFilterTerm = (bNegate ? " NOT" : "") + " IN " + sSqlIndis;
      }
      return sFilterTerm;
    }
    private static string AddToWhereInClause(string sBasicWhereClause, string sTableRow, string sFilter) {
      string sWhereClause = sBasicWhereClause;
      string sFilterTerm = GetFilterTermForWhereInClause(sFilter);
      if (!string.IsNullOrEmpty(sFilterTerm)) {
        sWhereClause = Filters.AddToWhereClause(sBasicWhereClause, sTableRow + sFilterTerm);
      }
      return sWhereClause;
    }
    private static string AddToWhereCompareClause(string sBasicWhereClause, string sTableRow, string sFilter) {
      string sWhereClause = sBasicWhereClause;
      string sFilterTerm = sFilter?.Replace("{0}", sTableRow)?.Trim();
      if (!string.IsNullOrEmpty(sFilterTerm)) {
        sWhereClause = Filters.AddToWhereClause(sBasicWhereClause, sFilterTerm);
      }
      return sWhereClause;
    }
    public static string AddToWhereClause(string sBasicWhereClause, string sWhereClause) {
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
        sb.Append("elements.creationtime>='" + this.DateFromFilter.Value.ToString("yyyy-MM-dd") + "'");
      }
      if (this.DateToFilter.HasValue) {
        if (sb.Length >= 1) {
          sb.Append(" AND ");
        }
        sb.Append("elements.creationtime<='" + this.DateToFilter.Value.ToString("yyyy-MM-dd") + "'");
      }
      return sb.ToString();
    }
    private string GetPhenotypeFilterWhereClause() {
      var sb = new System.Text.StringBuilder();
      string sFilter = this.PhenotypeFilter;
      if (!sFilter.IsNullOrEmpty()) {
        bool bNegate = (sFilter.IndexOfAny(Filters.NegateChars) == 0);
        if (bNegate) {
          sFilter = sFilter.Substring(1);
        }
        if (bNegate) {
          sb.Append("indivdata.phenotypeidx<>" + sFilter);
        } else {
          sb.Append("indivdata.phenotypeidx=" + sFilter);
        }
      }
      return sb.ToString();
    }
    private string GetGenotypeFilterWhereClause() {
      var sb = new System.Text.StringBuilder();
      string sFilter = this.GenotypeFilter;
      if (!sFilter.IsNullOrEmpty()) {
        bool bNegate = (sFilter.IndexOfAny(Filters.NegateChars) == 0);
        if (bNegate) {
          sFilter = sFilter.Substring(1);
        }
        if (bNegate) {
          sb.Append("indivdata.genotypeidx<>" + sFilter);
        } else {
          sb.Append("indivdata.genotypeidx=" + sFilter);
        }
      }
      return sb.ToString();
    }
    public bool IsEmpty(Project project) {
      string sResult = this.AddAllFiltersToWhereClause("", project);
      return string.IsNullOrEmpty(sResult);
    }
    public string AddAllFiltersToWhereClause(string sBasicWhereClause, Project project) {
      string sResult = sBasicWhereClause;
      if (this.FilteringTarget == FilteringTargetEnum.Catches || this.FilteringTarget == FilteringTargetEnum.CatchesForIdentification) {
        sResult = Filters.AddToWhereClause(sResult, this.GetDateFilterWhereClause());
        sResult = AddToWhereInClause(sResult, "elements.place", ExpandPlaceFilter(this.PlaceFilter));
        sResult = AddToWhereInClause(sResult, "elements.uploader", ExpandUserFilter(this.UserFilter));
        sResult = AddToWhereInClause(sResult, "indivdata.iid", ExpandIndiFilter(this.IndiFilter));
        sResult = AddToWhereInClause(sResult, "indivdata.gender", ExpandGenderFilter(this.GenderFilter));
        sResult = AddToWhereClause(sResult, this.GetPhenotypeFilterWhereClause());
        sResult = AddToWhereClause(sResult, this.GetGenotypeFilterWhereClause());
        if (!string.IsNullOrEmpty(this.CaptivityBredFilter)) {
          sResult = Filters.AddToWhereClause(sResult, Filters.GetCaptivityBredFilterExp(this.CaptivityBredFilter));
        }
        sResult = AddToWhereCompareClause(sResult, "indivdata.winters", ExpandHibernationsFilter(this.HibernationsFilter));
        if (this.MissingYearsFilter.HasValue) {
          var dtMax = DateTime.Now - TimeSpan.FromDays(365 * this.MissingYearsFilter.Value);
          string sDtMax = dtMax.ToString("yyyy-MM-dd");
          sResult = AddToWhereClause(sResult, $"(SELECT COUNT(*) FROM elements AS elements1 LEFT JOIN indivdata AS indivdata1 ON (indivdata1.name=elements1.name) WHERE (elements1.creationtime>='{sDtMax}' AND indivdata1.iid=indivdata.iid))<1");
        }
        if (this.BodyLengthMinFilter.HasValue) {
          sResult = AddToWhereCompareClause(sResult, "indivdata.headbodylength", "{0}>=" + this.BodyLengthMinFilter.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo));
        }
        if (this.BodyLengthMaxFilter.HasValue) {
          sResult = AddToWhereCompareClause(sResult, "indivdata.headbodylength", "{0}<=" + this.BodyLengthMaxFilter.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo));
        }
        if (this.OnlyFirstIndiFilter) {
          sResult = Filters.AddToWhereClause(sResult, this.OnlyFirstIndiFilterExp);
        }
        if (this.OnlyLastIndiFilter) {
          sResult = Filters.AddToWhereClause(sResult, this.OnlyLastIndiFilterExp);
        }
        if (this.OnlyWithRecapturesFilter) {
          sResult = Filters.AddToWhereClause(sResult, this.OnlyWithRecapturesFilterExp);
        }
        if (this.OnlyWithAdultRecapturesFilter) {
          sResult = Filters.AddToWhereClause(sResult,
            "EXISTS (" +
            "SELECT * FROM elements e1" +
            " LEFT JOIN indivdata i1 ON (i1.name=e1.name)" +
            " WHERE (i1.iid=indivdata.iid AND " + WhereClauses.Is_ID_photo + " AND e1.creationtime<>elements.creationtime AND i1.headbodylength>=" + ConvInvar.ToString(project.AdultMinLength) + ")" +
            ")");
        }
      }
      if (this.FilteringTarget == FilteringTargetEnum.Individuals) {
        sResult = AddToWhereInClause(sResult, "indivdata.iid", ExpandIndiFilter(this.IndiFilter));
        if (!string.IsNullOrEmpty(this.CaptivityBredFilter)) {
          sResult = AddToWhereCompareClause(sResult, "indivdata.captivitybred", "{0}=" + this.CaptivityBredFilter);
        }
      }
      if (this.FilteringTarget == FilteringTargetEnum.Elements) {
        sResult = Filters.AddToWhereClause(sResult, this.GetDateFilterWhereClause());
        sResult = AddToWhereInClause(sResult, "elements.place", ExpandPlaceFilter(this.PlaceFilter));
        sResult = AddToWhereInClause(sResult, "elements.uploader", ExpandUserFilter(this.UserFilter));
        if (!string.IsNullOrEmpty(this.ClassFilter)) {
          ElementClassification classification = JsonConvert.DeserializeObject<ElementClassification>(this.ClassFilter);
          if (!string.IsNullOrEmpty(classification.ClassName)) {
            sResult = AddToWhereClause(sResult, "elements.classname='" + classification.ClassName + "'");
            if (classification.IsLivingBeing()) {
              if (!string.IsNullOrEmpty(classification.LivingBeing?.Taxon?.SciName)) {
                string[] aSciNames = project.TaxaTree.GetSciNamesOfSubTree(classification.LivingBeing.Taxon.SciName);
                if (aSciNames.Length >= 1) {
                  string sSciNameList = aSciNames
                    .Select(sSciName => ("'" + sSciName + "'"))
                    .Aggregate((sList, sSciName) => (sList + ((sList == "") ? "" : ",") + sSciName));
                  sResult = AddToWhereClause(sResult, "elements.lbsciname IN (" + sSciNameList + ")");
                }
              }
              if (classification.LivingBeing != null && classification.LivingBeing.Stadium != ElementClassification.Stadium.None) {
                sResult = AddToWhereClause(sResult, "elements.lbstadium='" + ConvInvar.ToString((int)classification.LivingBeing.Stadium) + "'");
              }
            }
            if (classification.IsHabitat()) {
              if (classification.Habitat != null && classification.Habitat.Quality != 0) {
                sResult = AddToWhereClause(sResult, "elements.habquality='" + ConvInvar.ToString(classification.Habitat.Quality) + "'");
              }
              if (classification.Habitat != null && classification.Habitat.Monitoring) {
                sResult = AddToWhereClause(sResult, "elements.habmonitoring='" + (classification.Habitat.Monitoring ? "1" : "0") + "'");
              }
            }
          }
        }
      }
      if (this.FilteringTarget == FilteringTargetEnum.Log) {
        User user = this.GetUserFunc();
        if (user == null || user.Level < 400) {
          sResult = Filters.AddToWhereClause(sResult, "log.user='" + user.EMail + "'");
        }
        if (!string.IsNullOrEmpty(this.LogUserFilter)) {
          sResult = Filters.AddToWhereClause(sResult, "log.user LIKE '" + this.LogUserFilter + "'");
        }
        if (!string.IsNullOrEmpty(this.LogActionFilter)) {
          sResult = Filters.AddToWhereClause(sResult, "log.action LIKE '%" + this.LogActionFilter + "%'");
        }
      }
      if (this.FilteringTarget == FilteringTargetEnum.Notes) {
        User user = this.GetUserFunc();
        if (user == null || user.Level < 400) {
          sResult = Filters.AddToWhereClause(sResult, "notes.author='" + user.EMail + "'");
        }
        if (!string.IsNullOrEmpty(this.NotesAuthorFilter)) {
          sResult = Filters.AddToWhereClause(sResult, "notes.author LIKE '" + this.NotesAuthorFilter + "'");
        }
        if (!string.IsNullOrEmpty(this.NotesTextFilter)) {
          sResult = Filters.AddToWhereClause(sResult, "notes.text LIKE '%" + this.NotesTextFilter + "%'");
        }
      }
      return sResult;
    }
    private static string ExpandIndiFilter(string sFilter) {
      return ExpandFilter(sFilter, (sIndiA, sIndiB) => {
        var sbExp = new System.Text.StringBuilder();
        int nIndiA = int.Parse(sIndiA);
        int nIndiB = int.Parse(sIndiB);
        for (int nIndi = nIndiA; nIndi <= nIndiB; nIndi++) {
          sbExp.Append(ConvInvar.ToString(nIndi));
          sbExp.Append(' ');
        }
        return sbExp.ToString();
      });
    }
    private static string ExpandGenderFilter(string sFilter) {
      if (sFilter == "-" || sFilter == "*") {
        return "";
      } else {
        bool bNegate = (sFilter.IndexOfAny(Filters.NegateChars) == 0);
        if (bNegate) {
          sFilter = sFilter.Substring(1);
        }
        string sResult = bNegate ? "^" : "";
        if (sFilter.Contains("j")) {
          sResult += "j ";
        }
        if (sFilter.Contains("m")) {
          sResult += "m ";
        }
        if (sFilter.Contains("f")) {
          sResult += "f ";
        }
        return sResult.Trim();
      }
    }
    private static string ExpandHibernationsFilter(string sFilter) {
      string sResult = sFilter.Trim();
      if (sResult == "-" || sResult == "*") {
        return "";
      }
      return sResult;
    }
    public static string ExpandUserFilter(string sFilter) {
      string sResult = sFilter.Trim();
      if (sResult == "-" || sResult == "*") {
        return "";
      }
      return sResult;
    }
    private static string ExpandPlaceFilter(string sFilter) {
      return sFilter;
    }
    private static string ExpandFilter(string sFilter, Func<string, string, string> funcExpand) {
      var sbExp = new System.Text.StringBuilder();
      if (sFilter.StartsWith("!") || sFilter.StartsWith("^") || sFilter.StartsWith("-")) {
        sbExp.Append('!');
        sFilter = sFilter.Substring(1);
      }
      string[] saParts = sFilter.Split(Filters.SeparateChars);
      for (int i = 0; i < saParts.Length; i++) {
        string sPart = saParts[i];
        string[] sa1 = System.Text.RegularExpressions.Regex.Split(sPart, @"(\d+)(-|\.\.)(\d+)");
        if (sa1.Length == 1) {
          sbExp.Append(sa1[0]);
          sbExp.Append(' ');
        } else if (sa1.Length == 5) {
          sbExp.Append(funcExpand(sa1[1], sa1[3]));
        }
      }
      return sbExp.ToString().Trim();
    }

    public bool IsAnyActive(Project project) {
      string sResult = this.AddAllFiltersToWhereClause("", project);
      return !string.IsNullOrEmpty(sResult.Trim());
    }

    public void ClearAllFilters() {
      this.BodyLengthMaxFilter = null;
      this.BodyLengthMinFilter = null;
      this.CaptivityBredFilter = "";
      this.ClassFilter = "";
      this.DateFromFilter = null;
      this.DateToFilter = null;
      this.GenderFilter = "";
      this.GenotypeFilter = "";
      this.HibernationsFilter = "";
      this.IndiFilter = "";
      this.LogActionFilter = "";
      this.LogUserFilter = "";
      this.MissingYearsFilter = null;
      this.NotesAuthorFilter = "";
      this.NotesTextFilter = "";
      this.OnlyFirstIndiFilter = false;
      this.OnlyLastIndiFilter = false;
      this.OnlyWithAdultRecapturesFilter = false;
      this.OnlyWithRecapturesFilter = false;
      this.PhenotypeFilter = "";
      this.PlaceFilter = "";
      this.UserFilter = "";
      //
      Utilities.FireEvent(this.FilterChanged, this, EventArgs.Empty);
    }
  }
}
