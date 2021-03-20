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
using GoogleMapsComponents.Maps.Extension;
using BioMap.Shared;

namespace BioMap.Pages.Maps
{
    public partial class Individuals : ElementMap
    {
        private PhotoPopup PhotoPopup1;
        //
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            SD.Filters.FilterChanged += (sender, ev) =>
            {
                this.RefreshElementMarkers();
            };
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                base.PhotoPopup = this.PhotoPopup1;
                this.RefreshElementMarkers();
            }
        }
        protected override void RefreshElementMarkers()
        {
            var lElementMarkers = new List<ElementMarker>();
            if (SD.CurrentUser.Level >= 0)
            {
                var dictIndividuals = DataService.Instance.GetIndividuals(SD, SD.Filters);
                foreach (var iid in dictIndividuals.Keys)
                {
                    ElementMarker prevMarker = null;
                    foreach (var el in dictIndividuals[iid])
                    {
                        var latLng = new LatLngLiteral(el.ElementProp.MarkerInfo.position.lng, el.ElementProp.MarkerInfo.position.lat);
                        var symbolProps = el.GetSymbolProperties();
                        var elm = new ElementMarker
                        {
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
