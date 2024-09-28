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
  public partial class HeatMapAllElements : AreaMap
  {
    private HeatmapLayer heatmapLayer = null;
    private TimeIntervalSlider TimeIntervalSlider1 = null;
    //
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
      this.SD.Filters.FilterChanged += async (sender, ev) => await this.RefreshHeatMap();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
        await this.CreateHeatmapLayer();
      }
    }
    private async Task TimeIntervalSlider1_AnyChanged(EventArgs e) {
      await this.RefreshHeatMap();
    }
    private async Task CreateHeatmapLayer() {
      this.heatmapLayer = await HeatmapLayer.CreateAsync(this.googleMap.JsRuntime, new HeatmapLayerOptions {
        Map = this.googleMap.InteropObject,
        Dissipating = true,
        Radius = 50,
        MaxIntensity = 60,
      });
      await this.RefreshHeatMap();
    }
    private async Task RefreshHeatMap() {
      if (this.heatmapLayer != null) {
        var lPoints = new List<LatLngLiteral>();
        if (this.SD.CurrentUser.Level >= 0) {
          string sAddWhereClause = null;
          if (this.TimeIntervalSlider1.TimeIntervalWeeks >= 1) {
            DateTime dtCenter = this.SD.CurrentProject.StartDate.Value + TimeSpan.FromDays(7 * this.TimeIntervalSlider1.Week);
            TimeSpan tsInterval = TimeSpan.FromDays(7 * this.TimeIntervalSlider1.TimeIntervalWeeks);
            string sIntervalA = ConvInvar.ToIsoDateTime(dtCenter - tsInterval / 2);
            string sIntervalB = ConvInvar.ToIsoDateTime(dtCenter + tsInterval / 2);
            sAddWhereClause = $"elements.creationtime>='{sIntervalA}' AND elements.creationtime<'{sIntervalB}'";
          }
          foreach (Element el in this.DS.GetElements(this.SD, this.SD.Filters, sAddWhereClause)) {
            var latLng = new LatLngLiteral { Lng = el.ElementProp.MarkerInfo.position.lng, Lat = el.ElementProp.MarkerInfo.position.lat };
            lPoints.Add(latLng);
          }
        }
        await this.heatmapLayer.SetData(lPoints.ToArray());
      }
    }
  }
}
