using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BioMap.Shared;
using GoogleMapsComponents;
using GoogleMapsComponents.Maps;
using GoogleMapsComponents.Maps.Extension;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap.Pages.Maps
{
  public partial class Individuals : ElementMap
  {
    private PhotoPopup PhotoPopup1;
    private TimeIntervalSlider TimeIntervalSlider1 = null;
    private Dictionary<int, List<Element>> DictIndividuals = new Dictionary<int, List<Element>>();
    //
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
      this.SD.Filters.FilterChanged += (sender, ev) => this.RefreshElementMarkers();
      this.NM.LocationChanged += this.NM_LocationChanged;
    }
    private void NM_LocationChanged(object sender, LocationChangedEventArgs e) {
      this.NM.LocationChanged -= this.NM_LocationChanged;
      this.TimeIntervalSlider1.StopPlaying();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
        base.PhotoPopup = this.PhotoPopup1;
        this.TimeIntervalSlider1.Week = this.TimeIntervalSlider1.MaxWeek;
        this.RefreshElementMarkers();
      }
    }
    private async Task TimeIntervalSlider1_AnyChanged(EventArgs e) {
      await Task.Run(() => this.RefreshElementMarkers(true));
    }
    protected override void RefreshElementMarkers(bool bSkipDbQuery = false) {
      var lElementMarkers = new List<ElementMarker>();
      if (this.SD.CurrentUser.Level >= 0 && this.TimeIntervalSlider1 != null) {
        DateTime dtSelected = this.SD.CurrentProject.StartDate.Value + TimeSpan.FromDays(7 * this.TimeIntervalSlider1.Week);
        if (!bSkipDbQuery) {
          this.DictIndividuals = this.DS.GetIndividuals(this.SD, this.SD.Filters, null, true);
        }
        //var sw = System.Diagnostics.Stopwatch.StartNew();
        foreach (int iid in this.DictIndividuals.Keys) {
          List<Element> aIndis = this.DictIndividuals[iid];
          LatLngLiteral latLng = null;
          Element el = aIndis.Where(el1 => el1.ElementProp.CreationTime <= dtSelected).LastOrDefault();
          Element elNext = aIndis.Where(el1 => el1.ElementProp.CreationTime > dtSelected).FirstOrDefault();
          if (el != null) {
            if (elNext != null) {
              double a = (elNext.ElementProp.CreationTime - el.ElementProp.CreationTime).TotalDays;
              double b = (elNext.ElementProp.CreationTime - dtSelected).TotalDays;
              float c = (float)(1 - b / a);
              latLng = new LatLngLiteral() {
                Lat = (el.ElementProp.MarkerInfo.position.lat + c * (elNext.ElementProp.MarkerInfo.position.lat - el.ElementProp.MarkerInfo.position.lat)),
                Lng = (el.ElementProp.MarkerInfo.position.lng + c * (elNext.ElementProp.MarkerInfo.position.lng - el.ElementProp.MarkerInfo.position.lng)),
              };
            } else {
              if ((dtSelected - el.ElementProp.CreationTime) < TimeSpan.FromDays(548)) {
                latLng = new LatLngLiteral() { Lat = el.ElementProp.MarkerInfo.position.lat, Lng = el.ElementProp.MarkerInfo.position.lng };
              }
            }
          }
          if (latLng != null) {
            Element.SymbolProperties symbolProps = el.GetSymbolProperties(this.SD);
            var elm = new ElementMarker {
              Position = latLng,
              Radius = symbolProps.Radius,
              Color = symbolProps.BgColor,
              Element = el,
            };
            lElementMarkers.Add(elm);
          }
        }
        // Die Laufzeit war 0..3ms auf dem Entwicklungs-Notebook.
        //System.Diagnostics.Debug.WriteLine($"RefreshElementMarkers() {sw.ElapsedMilliseconds} ms");
      }
      base.ElementMarkers = lElementMarkers.ToArray();
    }
  }
}
