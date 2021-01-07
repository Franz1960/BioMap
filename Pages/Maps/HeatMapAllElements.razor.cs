using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GoogleMapsComponents;
using GoogleMapsComponents.Maps;
using GoogleMapsComponents.Maps.Visualization;
using GoogleMapsComponents.Maps.Extension;
using BioMap.Shared;

namespace BioMap.Pages.Maps
{
  public partial class HeatMapAllElements : AreaMap
  {
    [Inject]
    protected DataService DS { get; set; }
    [Inject]
    protected SessionData SD { get; set; }
    //
    private HeatmapLayer heatmapLayer=null;
    //
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
      SD.Filters.FilterChanged+=async (sender,ev) => {
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
      heatmapLayer = await HeatmapLayer.CreateAsync(googleMap.JsRuntime,new HeatmapLayerOptions {
        Map = googleMap.InteropObject,
        Dissipating = true,
        Radius = 50,
      });
      await RefreshHeatMap();
    }
    private async Task RefreshHeatMap() {
      if (heatmapLayer!=null) {
        var lPoints=new List<LatLngLiteral>();
        if (SD.CurrentUser.Level>=0) {
          foreach (var el in DS.GetElements(SD,SD.Filters)) {
            var latLng = new LatLngLiteral(el.ElementProp.MarkerInfo.position.lng,el.ElementProp.MarkerInfo.position.lat);
            lPoints.Add(latLng);
          }
        }
        await heatmapLayer.SetData(lPoints.ToArray());
      }
    }
  }
}