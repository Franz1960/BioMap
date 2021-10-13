using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BioMap.Shared;
using GoogleMapsComponents;
using GoogleMapsComponents.Maps;
using GoogleMapsComponents.Maps.Extension;
using GoogleMapsComponents.Maps.Visualization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap.Pages.Maps
{
  public partial class HeatMapIndividuals : AreaMap
  {
    private HeatmapLayer heatmapLayer = null;
    //
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
      SD.Filters.FilterChanged += async (sender, ev) => {
        await this.RefreshHeatMap();
      };
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
        await CreateHeatmapLayer();
      }
    }
    private async Task CreateHeatmapLayer() {
      heatmapLayer = await HeatmapLayer.CreateAsync(googleMap.JsRuntime, new HeatmapLayerOptions {
        Map = googleMap.InteropObject,
        Dissipating = true,
        Radius = 50,
      });
      await RefreshHeatMap();
    }
    private async Task RefreshHeatMap() {
      if (heatmapLayer != null) {
        var lPoints = new List<LatLngLiteral>();
        if (SD.CurrentUser.Level >= 0) {
          foreach (var indi in DS.GetIndividuals(SD, SD.Filters).Values) {
            foreach (var el in indi) {
              var latLng = new LatLngLiteral(el.ElementProp.MarkerInfo.position.lng, el.ElementProp.MarkerInfo.position.lat);
              lPoints.Add(latLng);
            }
          }
        }
        await heatmapLayer.SetData(lPoints.ToArray());
      }
    }
  }
}
