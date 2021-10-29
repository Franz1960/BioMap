using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace BioMap
{
  /// <summary>
  /// The tree of life for the purpose of BioMap.
  /// Taxons may be from the biological taxonomic system; for the purpose of BioMap
  /// they may also be pragmatic groups like predators. A taxon may belong to more than
  /// one parent taxa.
  /// </summary>
  public class TaxaTree
  {
    public readonly TreeNode RootNode = new TreeNode(null);
    public void FromTaxaList(IEnumerable<Taxon> taxaList) {
      this.RootNode.Clear();
      var flatNodeList = taxaList.Select(taxon => new TreeNode(taxon)).ToArray();
      var lSciNamesInUse = new List<string>();
      foreach (var node in flatNodeList.ToArray()) {
        var taxon = node.Data as Taxon;
        if (taxon.ParentSciNameArray.Length == 0) {
          this.RootNode.Add(node);
        } else {
          foreach (var sParentSciName in taxon.ParentSciNameArray) {
            foreach (var parentNode in flatNodeList.ToArray()) {
              if (string.CompareOrdinal(sParentSciName, parentNode.Data.InvariantName) == 0) {
                if (!lSciNamesInUse.Contains(taxon.SciName)) {
                  node.Parent = parentNode;
                  parentNode.Add(node);
                  lSciNamesInUse.Add(taxon.SciName);
                } else {
                  var newNode = new TreeNode(node.Data);
                  newNode.Parent = parentNode;
                  parentNode.Add(newNode);
                }
                break;
              }
            }
          }
        }
      }
      // Leeren Knoten zur Eingabe eines neuen anfügen.
      this.RootNode.Add(new TreeNode(new Taxon { }));
    }
    public IEnumerable<Taxon> ToTaxaList() {
      var taxaList = new List<Taxon>();
      taxaList.AddRange(this.RootNode.GetChildrenFlatList().Select(node => node.Data as Taxon));
      var result = taxaList.Where(taxon => !string.IsNullOrEmpty(taxon.SciName))
        .Distinct()
        .ToArray();
      return result;
    }
    public string ToJSON() {
      var sJson = JsonConvert.SerializeObject(this.ToTaxaList());
      return sJson;
    }
    public string[] GetSciNamesOfSubTree(string sSubTreeSciName) {
      TreeNode subTreeRootNode = this.RootNode.FindFirst(sSubTreeSciName);
      var lSciNames = new List<string>();
      if (subTreeRootNode != null) {
        if (!string.IsNullOrEmpty(subTreeRootNode.Parent?.Data?.InvariantName) && subTreeRootNode.Parent.Data.InvariantName.StartsWith("(")) {
          // Sammelgruppe.
          string[][] aaLists = this.RootNode.GetChildrenFlatList()
            .Where(node => {
              if (node?.Data is Taxon taxon) {
                if (taxon.ParentSciNameArray.Any(s => string.CompareOrdinal(s, sSubTreeSciName) == 0)) {
                  return true;
                }
              }
              return false;
            })
            .Select(node => this.GetSciNamesOfSubTree(node.Data?.InvariantName))
            .ToArray();
          var l1 = new List<string>();
          foreach (string[] aList in aaLists) {
            l1.AddRange(aList);
          }
          lSciNames.Add(subTreeRootNode.Data?.InvariantName);
          lSciNames.AddRange(l1.Distinct());
        } else {
          // Systematisches Taxon.
          lSciNames.Add(subTreeRootNode.Data?.InvariantName);
          lSciNames.AddRange(
            subTreeRootNode.GetChildrenFlatList()
            .Select(node => node.Data?.InvariantName)
            .Distinct()
            .Where(s => !string.IsNullOrEmpty(s))
            );
        }
      }
      return lSciNames.ToArray();
    }
  }
}
