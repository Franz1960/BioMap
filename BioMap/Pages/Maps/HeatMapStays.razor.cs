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
  public partial class HeatMapStays : AreaMap
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
        MaxIntensity = 3600,
      });
      await this.RefreshHeatMap();
    }
    private async Task RefreshHeatMap() {
      if (this.heatmapLayer != null) {
        var lPoints = new List<WeightedLocation>();
        if (this.SD.CurrentUser.Level >= 0) {
          int? nTimeSlotBegin = null;
          int? nTimeSlotEnd = null;
          if (this.TimeIntervalSlider1.TimeIntervalWeeks >= 1) {
            DateTime dtCenter = this.SD.CurrentProject.StartDate.Value + TimeSpan.FromDays(7 * this.TimeIntervalSlider1.Week);
            TimeSpan tsInterval = TimeSpan.FromDays(7 * this.TimeIntervalSlider1.TimeIntervalWeeks);
            DateTime dtA = dtCenter - tsInterval / 2;
            DateTime dtB = dtCenter + tsInterval / 2;
            nTimeSlotBegin = dtA.Year * 100 + dtA.Month;
            nTimeSlotEnd = dtB.Year * 100 + dtB.Month;
          }
          var project = this.SD.CurrentProject;
          foreach (LengthOfStayEvent lose in this.DS.GetLengthOfStayEvents(this.SD, Filters.ExpandUserFilter(this.SD.Filters.UserFilter), nTimeSlotBegin, nTimeSlotEnd)) {
            (double lat, double lon) = project.GetLatLonFromStayPoint(lose.StayLat, lose.StayLon);
            var latLng = new WeightedLocation { Location = new LatLngLiteral { Lng = lon, Lat = lat }, Weight = (float)lose.LengthOfStay };
            lPoints.Add(latLng);
          }
        }
        await this.heatmapLayer.SetData(lPoints.ToArray());
      }
    }
  }
}
