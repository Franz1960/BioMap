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
  public partial class AllElements : ElementMap
  {
    private PhotoPopup PhotoPopup1;
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
        foreach (Element el in this.DS.GetElements(this.SD, this.SD.Filters)) {
          var latLng = new LatLngLiteral { Lng = el.ElementProp.MarkerInfo.position.lng, Lat = el.ElementProp.MarkerInfo.position.lat };
          Element.SymbolProperties symbolProps = el.GetSymbolProperties(this.SD);
          string color = symbolProps.BgColor;
          if (el.Classification?.LivingBeing?.Taxon is Taxon taxon) {
            if (this.SD.CurrentProject.TaxaTree.RootNode.FindFirst(taxon.InvariantName) is TreeNode taxonNode) {
              var t = taxonNode.Ancestors.Append(taxonNode).Reverse().FirstOrDefault(n => !string.IsNullOrEmpty((n.Data as Taxon)?.Color))?.Data as Taxon;
              if (t != null) {
                color = t.Color;
              }
            }
          }
          ElementMarker elm = base.GetMarkerForElement(el);
          if (elm == null) {
            elm = new ElementMarker {
              Position = latLng,
              Radius = symbolProps.Radius,
              Color = color,
              Element = el,
            };
          } else {
            elm.Position = latLng;
            elm.Radius = symbolProps.Radius;
            elm.Color = color;
          }
          lElementMarkers.Add(elm);
        }
      }
      base.ElementMarkers = lElementMarkers.ToArray();
    }
  }
}
