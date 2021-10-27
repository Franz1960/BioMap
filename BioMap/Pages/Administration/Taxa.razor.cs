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
    private Blazorise.TreeView.TreeView<TreeNode> treeView;
    private TreeNode SelectedNode {
      get => this._SelectedNode;
      set {
        if (value != this._SelectedNode) {
          this._SelectedNode = value;

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
          this.treeView.SelectNode(SD.CurrentProject.TaxaTree.RootNode.Find(SD.CurrentProject.SpeciesSciName));
          if (this.treeView.ExpandedNodes.Count == 0) {
            foreach (var node in this.SelectedNode.Ancestors) {
              this.treeView.ExpandedNodes.Add(node);
            }
            this.StateHasChanged();
          }
        }
      }
    }
    private void NM_LocationChanged(object sender, LocationChangedEventArgs e) {
      NM.LocationChanged -= NM_LocationChanged;
      DS.WriteProject(SD, SD.CurrentProject);
    }
    private void RefreshData() {
    }
    private async Task Update_Clicked() {
      string sSelectedSciName = this.SelectedNode?.Data?.InvariantName;
      SD.CurrentProject.TaxaTree.FromTaxaList(SD.CurrentProject.TaxaTree.ToTaxaList());
      DS.WriteProject(SD,SD.CurrentProject);
      this.SelectedNode = SD.CurrentProject.TaxaTree.RootNode.Find(sSelectedSciName);
      if (this.SelectedNode != null) {
        this.treeView.ExpandedNodes.Clear();
        foreach (var node in this.SelectedNode.Ancestors) {
          this.treeView.ExpandedNodes.Add(node);
        }
      }
      this.StateHasChanged();
    }
  }
}
