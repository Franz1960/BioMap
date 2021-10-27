using System;
using System.Collections.Generic;
using System.Linq;

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
      foreach (var node in flatNodeList.ToArray()) {
        var taxon = node.Data as Taxon;
        if (taxon.ParentSciNameArray.Length == 0) {
          this.RootNode.Add(node);
        } else {
          foreach (var sParentSciName in taxon.ParentSciNameArray) {
            foreach (var parentNode in flatNodeList.ToArray()) {
              if (string.CompareOrdinal(sParentSciName, parentNode.Data.InvariantName) == 0) {
                node.Parent = parentNode;
                parentNode.Add(node);
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
      var result = taxaList.Where(taxon => !string.IsNullOrEmpty(taxon.SciName));
      result = result.Distinct();
      result = result.ToArray();
      return result;
    }
    private void AddTaxon(Taxon taxon) {
      if (taxon.ParentSciNameArray.Length == 0) {
        this.RootNode.Add(new TreeNode(taxon));
      } else {
        foreach (var sParentSciName in taxon.ParentSciNameArray) {
          var parentNode = this.RootNode.Find(sParentSciName);
          if (parentNode == null) {
            throw new ArgumentException("Cannot add species to tree: the parent species is not null but also not known.");
          } else {
            var node = new TreeNode(taxon) { Parent = parentNode };
            parentNode.Add(node);
          }
        }
      }
    }
  }
}
