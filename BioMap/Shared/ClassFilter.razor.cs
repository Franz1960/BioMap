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
    private TaxonDropDown taxonDropDown;
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
    private void TaxonDropDown_SelectedTaxonChanged() {
      this.Classification.LivingBeing = new ElementClassification.LivingBeing_t {
        Taxon = this.taxonDropDown.SelectedTaxon,
        Stadium = ElementClassification.Stadium.None,
      };
    }
  }
}
