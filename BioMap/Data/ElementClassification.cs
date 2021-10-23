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
    public class LivingBeing_t
    {
      public Species Species;
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
  [JsonObject(MemberSerialization.Fields)]
  public class Species
  {
    public string ParentSciName;
    public string SciName;
    public string Name_de;
    public string Name_en;
    public string GetLocalizedName(string sCultureName) {
      if (sCultureName.StartsWith("de") && !string.IsNullOrEmpty(this.Name_de)) {
        return this.Name_de;
      } else if (sCultureName.StartsWith("en") && !string.IsNullOrEmpty(this.Name_en)) {
        return this.Name_en;
      } else {
        return this.SciName;
      }
    }
  }
  public class SpeciesNode
  {
    public Species Species;
    public SpeciesNode Parent;
    public SpeciesNode[] Children = new SpeciesNode[0];
    public bool HasChildren => this.Children.Count()>=1;
    public SpeciesNode[] Ancestors {
      get {
        var lNodes = new List<SpeciesNode>();
        var node = this;
        while (true) {
          node = node.Parent;
          if (node != null) {
            lNodes.Insert(0,node);
          } else {
            break;
          }
        }
        return lNodes.ToArray();
      }
    }
    public SpeciesNode Find(string sSciName) {
      if (this.Species.SciName==sSciName) {
        return this;
      }
      foreach (var child in this.Children) {
        var result = child.Find(sSciName);
        if (result != null) {
          return result;
        }
      }
      return null;
    }
    public IEnumerable<SpeciesNode> GetFlatList() {
      var speciesList = new List<SpeciesNode>();
      speciesList.Add(this);
      foreach (var childNode in this.Children) {
        speciesList.AddRange(childNode.GetFlatList());
      }
      return speciesList;
    }
  }
  public class SpeciesTree
  {
    public SpeciesNode[] RootNodes;
    public void Init(IEnumerable<Species> speciesList) {
      var flatNodes = speciesList.Select(sp => new SpeciesNode { Species = sp, Children = new SpeciesNode[0], Parent = null }).ToArray();
      var rootNodes = new List<SpeciesNode>();
      foreach (var speciesNode in flatNodes.ToArray()) {
        if (string.IsNullOrEmpty(speciesNode.Species.ParentSciName)) {
          rootNodes.Add(speciesNode);
        } else {
          foreach (var parentNode in flatNodes.ToArray()) {
            if (string.CompareOrdinal(speciesNode.Species.ParentSciName, parentNode.Species.SciName) == 0) {
              speciesNode.Parent = parentNode;
              parentNode.Children = parentNode.Children.Append(speciesNode).ToArray();
              break;
            }
          }
        }
      }
      // Leeren Knoten zur Eingabe eines neuen anfügen.
      rootNodes.Add(new SpeciesNode { Species = new Species { SciName = "" }});
      this.RootNodes = rootNodes.ToArray();
    }
    public IEnumerable<SpeciesNode> ToSpeciesList() {
      var speciesList = new List<SpeciesNode>();
      foreach (var childNode in this.RootNodes) {
        speciesList.AddRange(childNode.GetFlatList());
      }
      return speciesList.Where(sn => !string.IsNullOrEmpty(sn.Species.SciName));
    }
    public SpeciesNode Find(string sSciName) {
      foreach (var child in this.RootNodes) {
        var result = child.Find(sSciName);
        if (result != null) {
          return result;
        }
      }
      return null;
    }
    private void AddSpecies(List<SpeciesNode> rootNodes, Species species) {
      if (string.IsNullOrEmpty(species.ParentSciName)) {
        rootNodes.Add(new SpeciesNode { Species = species, Parent = null, Children = new SpeciesNode[0] });
      } else {
        var parentNode = this.RootNodes.FirstOrDefault(sn => string.CompareOrdinal(sn.Species.SciName, species.ParentSciName) == 0);
        if (parentNode == null) {
          throw new ArgumentException("Cannot add species to tree: the parent species is not null but also not known.");
        } else {
          var speciesNode = new SpeciesNode { Species = species, Parent = parentNode, Children = new SpeciesNode[0] };
          parentNode.Children = parentNode.Children.Append(speciesNode).ToArray();
        }
      }
    }
  }
}
