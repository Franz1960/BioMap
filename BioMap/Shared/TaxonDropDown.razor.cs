using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazorise;
using Blazorise.Extensions;
using GoogleMapsComponents;
using GoogleMapsComponents.Maps;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap.Shared
{
  public partial class TaxonDropDown : ComponentBase
  {
    [Parameter]
    public bool Disabled { get; set; }
    [Parameter]
    public bool IncludeCollections { get; set; }
    [Parameter]
    public EventCallback SelectedTaxonChanged { get; set; }
    //
    [Parameter]
    public Taxon SelectedTaxon {
      get => this._SelectedTaxon;
      set {
        if (value != this._SelectedTaxon) {
          this._SelectedTaxon = value;
          this.SelectedTaxonChanged.InvokeAsync();
        }
      }
    }
    private Taxon _SelectedTaxon = new Taxon();
    protected override void OnInitialized() {
    }
    private void Update() {
    }
    private RenderFragment CreateTaxonDropDownItem(TreeNode treeNode, bool bRecursive) {
      return builder => {
        var taxon = treeNode.Data as Taxon;
        if (treeNode.HasChildren && bRecursive) {
          builder.OpenComponent<Blazorise.DropdownItem>();
          builder.Attribute("ChildContent", (RenderFragment)((builder2) => {
            builder2.OpenComponent<Blazorise.Dropdown>();
            builder2.Attribute("ChildContent", (RenderFragment)((builder3) => {
              builder3.OpenComponent<Blazorise.Button>();
              builder3.Attribute("Color", (string.CompareOrdinal(this.SelectedTaxon?.SciName, taxon.InvariantName) == 0) ? Color.Primary : Color.None);
              builder3.Attribute("Clicked", Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, () => {
                this.SelectedTaxon = this.SD.CurrentProject.GetTaxon(taxon.InvariantName);
                this.Update();
              }));
              builder3.Attribute("ChildContent", (RenderFragment)((builder4) => {
                builder4.AddContent(45, taxon.GetLocalizedName(this.SD.CurrentCultureName));
              }));
              builder3.CloseComponent();
              builder3.OpenComponent<Blazorise.DropdownToggle>();
              builder3.Attribute("Split", true);
              builder3.CloseComponent();
              builder3.OpenComponent<Blazorise.DropdownMenu>();
              builder3.Attribute("ChildContent", (RenderFragment)((builder4) => {
                foreach (var childNode in treeNode.Children) {
                  builder4.Content(this.CreateTaxonDropDownItem(childNode, bRecursive));
                }
              }));
              builder3.CloseComponent();
            }));
            builder2.CloseComponent();
          }));
          builder.CloseComponent();
        } else {
          builder.OpenComponent<Blazorise.DropdownItem>();
          builder.Attribute("Active", (string.CompareOrdinal(this.SelectedTaxon?.SciName, taxon.InvariantName) == 0));
          builder.Attribute("Clicked", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<object>(this, (ea) => {
            this.SelectedTaxon = this.SD.CurrentProject.GetTaxon(taxon.InvariantName);
            this.Update();
          }));
          builder.Attribute("ChildContent", (RenderFragment)((builder2) => {
            builder2.AddContent(8, taxon.GetLocalizedName(this.SD.CurrentCultureName));
          }));
          builder.CloseComponent();
        }
      };
    }
  }
}
