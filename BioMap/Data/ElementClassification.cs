using System;
using System.Collections.Generic;
using System.Linq;
//using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.Jpeg;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap
{
  public class ElementClassification
  {
    public static readonly string[] ClassNames = new string[] {
      "New",
      "ID photo",
      "Normalized non-ID photo",
      "Living being",
      "Habitat",
      "Other",
    };
    public static readonly Dictionary<string, string> ClassColors = new Dictionary<string, string>(new KeyValuePair<string, string>[] {
      new KeyValuePair<string,string>("New","#FFFFFF"),
      new KeyValuePair<string,string>("ID photo","#FFD800"),
      new KeyValuePair<string,string>("Normalized non-ID photo","#7F6420"),
      new KeyValuePair<string,string>("Living being","#C96A00"),
      new KeyValuePair<string,string>("Habitat","#AAC643"),
      new KeyValuePair<string,string>("Other","#9132D1"),
    });
    public enum Stadium
    {
      None,
      Eggs = 1,
      Larvae,
      Juveniles,
      Adults,
      Deads,
    }
    [JsonObject(MemberSerialization.Fields)]
    public class Species
    {
      public string SciName;
      public string Name_de;
      public string Name_en;
    }
    public class LivingBeing_t
    {
      public Species Species;
      public Taxon Taxon;
      public Stadium Stadium = Stadium.Adults;
      public int Count = 1;
    }
    public class Habitat_t
    {
      public bool Monitoring;
      public int Quality = 3;
      public string GetQualityAsSymbols() {
        return GetQualityAsSymbols(this.Quality);
      }
      public static string GetQualityAsSymbols(int nQuality) {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 5; i++) {
          sb.Append(i < nQuality ? '\u2605' : '\u2606');
        }
        return sb.ToString();
      }
    }
    public static bool IsNormed(string sClassName) {
      return (string.CompareOrdinal(sClassName, "ID photo") == 0 || string.CompareOrdinal(sClassName, "Normalized non-ID photo") == 0);
    }
    public bool IsIdPhoto() {
      return (string.CompareOrdinal(this.ClassName, "ID photo") == 0);
    }
    public bool IsMonitoring() {
      return (this.IsIdPhoto() || (this.Habitat != null && this.Habitat.Monitoring));
    }
    public string ClassName = "New";
    public LivingBeing_t LivingBeing;
    public Habitat_t Habitat;
  }
}
