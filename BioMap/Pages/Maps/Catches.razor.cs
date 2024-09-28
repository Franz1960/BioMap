using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BioMap.Shared;
using GoogleMapsComponents;
using GoogleMapsComponents.Maps;
using GoogleMapsComponents.Maps.Extension;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap.Pages.Maps
{
  public partial class Catches : ElementMap
  {
    private PhotoPopup PhotoPopup1;
    //
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
      this.SD.Filters.FilterChanged += (sender, ev) => this.RefreshElementMarkers();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
        base.PhotoPopup = this.PhotoPopup1;
        this.RefreshElementMarkers();
      }
    }
    protected override void RefreshElementMarkers(bool bSkipDbQuery = false) {
      var lElementMarkers = new List<ElementMarker>();
      if (this.SD.CurrentUser.Level >= 0) {
        Dictionary<int, List<Element>> dictIndividuals = this.DS.GetIndividuals(this.SD, this.SD.Filters, null, this.DisplayConnectors >= 1);
        foreach (int iid in dictIndividuals.Keys) {
          ElementMarker prevMarker = null;
          List<Element> lIndividuals = dictIndividuals[iid];
          if (this.DisplayConnectors == 1) {
            if (lIndividuals.Count > 2) {
              lIndividuals.RemoveRange(0, lIndividuals.Count - 2);
            }
          }
          foreach (Element el in lIndividuals) {
            var latLng = new LatLngLiteral { Lng = el.ElementProp.MarkerInfo.position.lng, Lat = el.ElementProp.MarkerInfo.position.lat };
            Element.SymbolProperties symbolProps = el.GetSymbolProperties(this.SD);
            var elm = new ElementMarker {
              Position = latLng,
              Radius = symbolProps.Radius,
              Color = symbolProps.BgColor,
              Element = el,
              PrevMarker = prevMarker,
            };
            lElementMarkers.Add(elm);
            prevMarker = elm;
          }
        }
      }
      base.ElementMarkers = lElementMarkers.ToArray();
    }
  }
}
