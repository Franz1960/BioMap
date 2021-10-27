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
      NM.LocationChanged += NM_LocationChanged;
      RefreshData();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
        if (this.SelectedNode == null) {
          var node = SD.CurrentProject.TaxaTree.RootNode.FindFirst(SD.CurrentProject.SpeciesSciName);
          this.treeView.SelectNode(node);
          this.EnsureSelectedVisible();
          this.StateHasChanged();
        }
      }
    }
    private void NM_LocationChanged(object sender, LocationChangedEventArgs e) {
      NM.LocationChanged -= NM_LocationChanged;
      DS.WriteProject(SD, SD.CurrentProject);
    }
    private void RefreshData() {
    }
    private void EnsureSelectedVisible() {
      this.treeView.ExpandedNodes.Clear();
      var nodes = SD.CurrentProject.TaxaTree.RootNode.Find(this.SelectedNode?.Data?.InvariantName);
      foreach (var node in SD.CurrentProject.TaxaTree.RootNode.Find(this.SelectedNode?.Data?.InvariantName)) {
        foreach (var ancestor in node.Ancestors) {
          if (!this.treeView.ExpandedNodes.Contains(ancestor)) {
            this.treeView.ExpandedNodes.Add(ancestor);
          }
        }
      }
    }
    private async Task Save_Clicked() {
      var taxaTree = SD.CurrentProject.TaxaTree;
      TreeNode node = taxaTree.RootNode.FindFirst(this.EditedTaxon.SciName);
      if (node?.Data is Taxon nodeTaxon) {
        nodeTaxon.CopyFrom(this.EditedTaxon);
      } else {
        nodeTaxon = Taxon.Clone(this.EditedTaxon);
        taxaTree.RootNode.Add(new TreeNode(nodeTaxon));
      }
      string sSelectedSciName = nodeTaxon.SciName;
      taxaTree.FromTaxaList(taxaTree.ToTaxaList());
      DS.WriteProject(SD,SD.CurrentProject);
      this.SelectedNode = taxaTree.RootNode.FindFirst(sSelectedSciName);
      this.EnsureSelectedVisible();
      this.StateHasChanged();
    }
    private async Task NewTaxon_Clicked() {
      this.EditedTaxon.CopyFrom(null);
      if (this.SelectedNode?.Data is Taxon selectedTaxon) {
        this.EditedTaxon.ParentSciNames = selectedTaxon.SciName;
      }
      this.StateHasChanged();
    }
  }
}
