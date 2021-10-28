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
  public partial class ClassFilter : ComponentBase
  {
    private ElementClassification Classification = new ElementClassification();
    protected override void OnInitialized() {
      if (!string.IsNullOrEmpty(SD.Filters.ClassFilter)) {
        this.Classification = JsonConvert.DeserializeObject<ElementClassification>(SD.Filters.ClassFilter);
      } else {
        this.Classification = new ElementClassification { ClassName = "" };
      }
    }
    private void Update() {
      SD.Filters.ClassFilter = JsonConvert.SerializeObject(this.Classification);
    }
    private RenderFragment CreateTreeNodeMenuItem(TreeNode treeNode, bool bRecursive) {
      return builder => {
        var taxon = treeNode.Data as Taxon;
        if (treeNode.HasChildren && bRecursive) {
          builder.OpenComponent<Blazorise.DropdownItem>();
          builder.Attribute("ChildContent", (RenderFragment)((builder2) => {
            builder2.OpenComponent<Blazorise.Dropdown>();
            builder2.Attribute("ChildContent", (RenderFragment)((builder3) => {
              builder3.OpenComponent<Blazorise.Button>();
              builder3.Attribute("Color", (string.CompareOrdinal(this.Classification.LivingBeing?.Taxon?.SciName, taxon.InvariantName) == 0) ? Color.Primary : Color.None);
              builder3.Attribute("Clicked", Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, () => {
                this.Classification.LivingBeing = new ElementClassification.LivingBeing_t {
                  Taxon = this.SD.CurrentProject.GetTaxon(taxon.InvariantName),
                  Stadium = ElementClassification.Stadium.None,
                };
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
                  builder4.Content(this.CreateTreeNodeMenuItem(childNode, bRecursive));
                }
              }));
              builder3.CloseComponent();
            }));
            builder2.CloseComponent();
          }));
          builder.CloseComponent();
        } else {
          builder.OpenComponent<Blazorise.DropdownItem>();
          builder.Attribute("Active", (string.CompareOrdinal(this.Classification.LivingBeing?.Taxon?.SciName, taxon.InvariantName) == 0));
          builder.Attribute("Clicked", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<object>(this, (ea) => {
            this.Classification.LivingBeing = new ElementClassification.LivingBeing_t {
              Taxon = this.SD.CurrentProject.GetTaxon(taxon.InvariantName),
              Stadium = ElementClassification.Stadium.None,
            };
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
