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
      SD.Filters.FilterChanged += (sender, ev) =>
      {
        this.RefreshElementMarkers();
      };
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
        base.PhotoPopup = this.PhotoPopup1;
        this.RefreshElementMarkers();
      }
    }
    protected override void RefreshElementMarkers() {
      var lElementMarkers = new List<ElementMarker>();
      if (SD.CurrentUser.Level >= 0) {
        foreach (var el in DS.GetElements(SD, SD.Filters)) {
          var latLng = new LatLngLiteral(el.ElementProp.MarkerInfo.position.lng, el.ElementProp.MarkerInfo.position.lat);
          var symbolProps = el.GetSymbolProperties();
          string color = symbolProps.BgColor;
          if (el.Classification?.LivingBeing?.Taxon is Taxon taxon) {
            if (SD.CurrentProject.TaxaTree.RootNode.FindFirst(taxon.InvariantName) is TreeNode taxonNode) {
              var t = taxonNode.Ancestors.Append(taxonNode).Reverse().FirstOrDefault(n => !string.IsNullOrEmpty((n.Data as Taxon)?.Color))?.Data as Taxon;
              if (t != null) {
                color = t.Color;
              }
            }
          }
          var elm = base.GetMarkerForElement(el);
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
