using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap
{
  /// <summary>
  /// A taxon in the tree of life.
  /// Taxons may be from the biological taxonomic system; for the purpose of BioMap
  /// they may also be pragmatic groups like predators. A taxon may be child to zero, one
  /// or more parent taxons.
  /// </summary>
  [JsonObject(MemberSerialization.Fields)]
  [System.Diagnostics.DebuggerDisplay("{ToString()}")]
  public class Taxon : ITreeNodeData
  {
    public override string ToString() {
      return "Taxon(" + this.SciName + ") <-- " + this.ParentSciNames;
    }
    public override bool Equals(object obj) {
      var other = obj as Taxon;
      return (string.CompareOrdinal(this.SciName,other.SciName) == 0);
    }
    public override int GetHashCode() {
      return this.SciName.GetHashCode();
    }
    /// <summary>
    /// Semicolon separated list of parent taxa's scientific names.
    /// </summary>
    public string ParentSciNames;
    /// <summary>
    /// Scientific name.
    /// </summary>
    public string SciName;
    /// <summary>
    /// German common name.
    /// </summary>
    public string Name_de;
    /// <summary>
    /// English common name.
    /// </summary>
    public string Name_en;
    /// <summary>
    /// Associated color, e.g. in map views.
    /// </summary>
    public string Color;
    public string[] ParentSciNameArray {
      get {
        if (this.ParentSciNames == null) {
          return new string[0];
        } else {
          return this.ParentSciNames.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToArray();
        }
      }
    }
    public static Taxon Clone(Taxon other) {
      var taxon = new Taxon();
      taxon.CopyFrom(other);
      return taxon;
    }
    public void CopyFrom(Taxon other) {
      if (other == null) {
        this.ParentSciNames = "";
        this.SciName = "";
        this.Name_en = "";
        this.Name_de = "";
        this.Color = "";
      } else {
        this.ParentSciNames = other.ParentSciNames;
        this.SciName = other.SciName;
        this.Name_en = other.Name_en;
        this.Name_de = other.Name_de;
        this.Color = other.Color;
      }
    }
    /// <summary>
    /// ITreeNodeData interface implementation.
    /// </summary>
    public string InvariantName => this.SciName;
    /// <summary>
    /// Common name in given language.
    /// </summary>
    /// <param name="sLanguageName">
    /// The language name, 'de' or 'en'.
    /// </param>
    /// <returns>
    /// The taxon's common name in the given language; the scientific name if no common name is found.
    /// </returns>
    public string GetLocalizedName(string sLanguageName) {
      if (sLanguageName.StartsWith("de") && !string.IsNullOrEmpty(this.Name_de)) {
        return this.Name_de;
      } else if (sLanguageName.StartsWith("en") && !string.IsNullOrEmpty(this.Name_en)) {
        return this.Name_en;
      } else {
        return this.SciName;
      }
    }
  }
}
