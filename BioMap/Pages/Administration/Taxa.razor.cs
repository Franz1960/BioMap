using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BioMap.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap.Pages.Administration
{
  public partial class Taxa : ComponentBase
  {
    [Inject]
    protected NavigationManager NM { get; set; }
    [Inject]
    protected DataService DS { get; set; }
    [Inject]
    protected SessionData SD { get; set; }
    //
    private string selectedTab = "Tree";
    //
    private string TaxaJSON { get; set; } = "";
    private readonly Taxon EditedTaxon = new Taxon();
    private Blazorise.TreeView.TreeView<TreeNode> treeView;
    private TreeNode SelectedNode {
      get => this._SelectedNode;
      set {
        if (value != this._SelectedNode) {
          this._SelectedNode = value;
          this.EditedTaxon.CopyFrom(value?.Data as Taxon);
        }
      }
    }
    private TreeNode _SelectedNode = null;
    private IList<TreeNode> ExpandedNodes { get; set; } = new List<TreeNode>();
    //
    private void OnSelectedTabChanged(string name) {
      this.selectedTab = name;
    }
    //
    protected override void OnInitialized() {
      base.OnInitialized();
      this.NM.LocationChanged += this.NM_LocationChanged;
      this.RefreshData();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
        if (this.SelectedNode == null) {
          TreeNode node = this.SD.CurrentProject.TaxaTree.RootNode.FindFirst(this.SD.CurrentProject.SpeciesSciName);
          this.treeView.SelectNode(node);
          this.EnsureSelectedVisible();
          this.StateHasChanged();
        }
      }
    }
    private void NM_LocationChanged(object sender, LocationChangedEventArgs e) {
      this.NM.LocationChanged -= this.NM_LocationChanged;
      this.DS.WriteProject(this.SD, this.SD.CurrentProject);
    }
    private void RefreshData() {
    }
    private string GetNiceNodeName(TreeNode node) {
      var taxon = node?.Data as Taxon;
      if (taxon == null) {
        return "(null)";
      } else {
        string sLocalName = taxon.GetLocalizedName(this.SD.CurrentCultureName);
        if (sLocalName == taxon.InvariantName || string.IsNullOrEmpty(sLocalName)) {
          return taxon.InvariantName;
        } else {
          return taxon.InvariantName + " (" + sLocalName + ")";
        }
      }
    }
    private void EnsureSelectedVisible() {
      this.treeView.ExpandedNodes.Clear();
      TreeNode[] nodes = this.SD.CurrentProject.TaxaTree.RootNode.Find(this.SelectedNode?.Data?.InvariantName);
      foreach (TreeNode node in this.SD.CurrentProject.TaxaTree.RootNode.Find(this.SelectedNode?.Data?.InvariantName)) {
        foreach (TreeNode ancestor in node.Ancestors) {
          if (!this.treeView.ExpandedNodes.Contains(ancestor)) {
            this.treeView.ExpandedNodes.Add(ancestor);
          }
        }
      }
    }
    private async Task Save_Clicked() {
      await Task.Run(() => {
        TaxaTree taxaTree = this.SD.CurrentProject.TaxaTree;
        TreeNode node = taxaTree.RootNode.FindFirst(this.EditedTaxon.SciName);
        if (node?.Data is Taxon nodeTaxon) {
          nodeTaxon.CopyFrom(this.EditedTaxon);
        } else {
          nodeTaxon = Taxon.Clone(this.EditedTaxon);
          taxaTree.RootNode.Add(new TreeNode(nodeTaxon));
        }
        string sSelectedSciName = nodeTaxon.SciName;
        taxaTree.FromTaxaList(taxaTree.ToTaxaList());
        this.DS.WriteProject(this.SD, this.SD.CurrentProject);
        this.SelectedNode = taxaTree.RootNode.FindFirst(sSelectedSciName);
        this.EnsureSelectedVisible();
        this.StateHasChanged();
      });
    }
    private async Task NewTaxon_Clicked() {
      await Task.Run(() => {
        this.EditedTaxon.CopyFrom(null);
        if (this.SelectedNode?.Data is Taxon selectedTaxon) {
          this.EditedTaxon.ParentSciNames = selectedTaxon.SciName;
        }
        this.StateHasChanged();
      });
    }
    private async Task Delete_Clicked() {
      await Task.Run(() => {
        TaxaTree taxaTree = this.SD.CurrentProject.TaxaTree;
        string sSelectedSciName = this.SelectedNode?.Parent?.Data?.InvariantName;
        taxaTree.RootNode.Remove(this.SelectedNode);
        taxaTree.FromTaxaList(taxaTree.ToTaxaList());
        this.DS.WriteProject(this.SD, this.SD.CurrentProject);
        this.SelectedNode = taxaTree.RootNode.FindFirst(sSelectedSciName);
        this.EnsureSelectedVisible();
        this.StateHasChanged();
      });
    }
    private async Task Expand_Clicked() {
      await Task.Run(() => {
        this.treeView.ExpandedNodes.Clear();
        foreach (TreeNode node in this.SD.CurrentProject.TaxaTree.RootNode.GetChildrenFlatList()) {
          if (node.HasChildren) {
            this.treeView.ExpandedNodes.Add(node);
          }
        }
        this.StateHasChanged();
      });
    }
    private async Task SaveJSON_Clicked() {
      await Task.Run(() => {
        TaxaTree taxaTree = this.SD.CurrentProject.TaxaTree;
        try {
          string sJson = this.TaxaJSON;
          IEnumerable<Taxon> taxaList = JsonConvert.DeserializeObject<IEnumerable<Taxon>>(sJson);
          taxaTree.FromTaxaList(taxaList);
          this.DS.WriteProject(this.SD, this.SD.CurrentProject);
        } catch {
        }
      });
    }
  }
}
