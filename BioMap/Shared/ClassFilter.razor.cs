using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazorise;
using GoogleMapsComponents;
using GoogleMapsComponents.Maps;
using Microsoft.AspNetCore.Components;
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
  }
}
