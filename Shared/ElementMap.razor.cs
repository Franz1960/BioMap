using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GoogleMapsComponents;
using GoogleMapsComponents.Maps;
using GoogleMapsComponents.Maps.Coordinates;
using GoogleMapsComponents.Maps.Extension;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BioMap.Shared
{
  public partial class ElementMap : AreaMap
  {
    public class ElementMarker
    {
      public double Radius;
      public string Color;
      public LatLngLiteral Position;
      public Element Element;
      internal Circle Circle;
    }
    [Parameter]
    public ElementMarker[] ElementMarkers {
      get {
        return this._ElementMarkers;
      }
      set {
        this._ElementMarkers=value;
        this.StateHasChanged();
      }
    }
    private ElementMarker[] _ElementMarkers = null;
    private Blazorise.Utils.ValueDelayer zoomValueDelayer;
    private double RadiusFactor = 1;
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
      this.zoomValueDelayer = new Blazorise.Utils.ValueDelayer(800);
      this.zoomValueDelayer.Delayed += this.OnZoomValueDelayed;
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
        await this.googleMap.InteropObject.AddListener("zoom_changed",async () => {
          this.zoomValueDelayer.Update(ConvInvar.ToString(await this.googleMap.InteropObject.GetZoom()));
        });
      }
      if (this.ElementMarkers!=null) {
        foreach (var elm in this.ElementMarkers) {
          var circle = await Circle.CreateAsync(googleMap.JsRuntime,new CircleOptions {
            Map=googleMap.InteropObject,
            Center=elm.Position,
            Radius=elm.Radius,
            StrokeColor=elm.Color,
            StrokeOpacity=0.60f,
            StrokeWeight=2,
            FillColor=elm.Color,
            FillOpacity=0.35f,
            ZIndex=1000000,
          });
          elm.Circle=circle;
          await circle.AddListener("click",async () => {
            var s = elm.Element.ElementName;
          });
        }
      }
    }
    private async void OnZoomValueDelayed(object sender,string sValue) {
      try {
        var bounds = await this.googleMap.InteropObject.GetBounds();
        var fHeight = Math.Abs(bounds.North-bounds.South)*111000;
        this.RadiusFactor=fHeight*0.004;
        {
          var N = this.ElementMarkers.Count();
          var aOffsets = new LatLngLiteral[N];
          for (int i = 0;i<N;i++) {
            aOffsets[i]=new LatLngLiteral();
          }
          for (int i = 0;i<N-1;i++) {
            var elm1 = this.ElementMarkers[i];
            for (int j = i+1;j<N;j++) {
              var elm2 = this.ElementMarkers[j];
              var geoDistance = Geo.Geodesy.GeodeticCalculations.CalculateGreatCircleLine(
                new Geo.Coordinate(elm1.Position.Lat,elm1.Position.Lng),
                new Geo.Coordinate(elm2.Position.Lat,elm2.Position.Lng))?.Distance;
              var distance = geoDistance.HasValue ? geoDistance.Value.Value : 0;
              double minDistance = this.RadiusFactor*(elm1.Radius+elm2.Radius);
              if (distance>0 && distance<minDistance) {
                double force =
                  0.005
                  *
                  minDistance
                  /
                  (distance)
                  ;
                var delta = new LatLngLiteral {
                  Lat = elm2.Position.Lat-elm1.Position.Lat,
                  Lng = elm2.Position.Lng-elm1.Position.Lng,
                };
                aOffsets[i]=new LatLngLiteral {
                  Lat = aOffsets[i].Lat-force*delta.Lat,
                  Lng = aOffsets[i].Lng-force*delta.Lng,
                };
                aOffsets[j]=new LatLngLiteral {
                  Lat = aOffsets[j].Lat+force*delta.Lat,
                  Lng = aOffsets[j].Lng+force*delta.Lng,
                };
              }
            }
          }
          for (int i = 0;i<N-1;i++) {
            var elm = this.ElementMarkers[i];
            if (elm.Circle!=null) {
              await elm.Circle.SetCenter(new LatLngLiteral {
                Lat=elm.Position.Lat+aOffsets[i].Lat,
                Lng=elm.Position.Lng+aOffsets[i].Lng,
              });
              await elm.Circle.SetRadius(this.RadiusFactor*elm.Radius);
            }
          }
        }
      } catch { }
    }
  }
}
