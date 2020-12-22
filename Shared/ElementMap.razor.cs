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
      public ElementMarker PrevMarker;
      internal Circle Circle;
      internal Polyline Connector;
      internal int ZIndex = 1000000;
    }
    [Parameter]
    public ElementMarker[] ElementMarkers {
      get {
        return this._ElementMarkers;
      }
      set {
        lock (this.ElementMarkersLock) {
          if (this._ElementMarkers!=null) {
            foreach (var elm in this._ElementMarkers) {
              elm?.Circle?.SetMap(null);
              elm?.Connector?.SetMap(null);
            }
          }
          this._ElementMarkers=value;
        }
        this.DelayedStateHasChanged();
      }
    }
    private ElementMarker[] _ElementMarkers = null;
    [Parameter]
    public PhotoPopup PhotoPopup { get; set; }
    [Parameter]
    public bool DynaZoomed {
      get {
        return this._DynaZoomed;
      }
      set {
        this._DynaZoomed=value;
        this.DelayedStateHasChanged();
      }
    }
    private bool _DynaZoomed = false;
    [Parameter]
    public bool DisplayConnectors {
      get {
        return this._DisplayConnectors;
      }
      set {
        this._DisplayConnectors=value;
        this.DelayedStateHasChanged();
      }
    }
    private bool _DisplayConnectors = false;
    //
    private CircleList circleList=null;
    //
    protected LatLngBoundsLiteral elementBounds=null;
    //
    private readonly object ElementMarkersLock = new object();
    private int AfterRenderUpDownCnt = 0;
    private bool AfterRenderCancelReq = false;
    private Blazorise.Utils.ValueDelayer zoomValueDelayer;
    private double RadiusFactor = 1;
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
      this.zoomValueDelayer = new Blazorise.Utils.ValueDelayer(800);
      this.zoomValueDelayer.Delayed += async (sender,sValue) =>await this.OnZoomValueDelayed(sValue);
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
        await this.googleMap.InteropObject.AddListener("zoom_changed",async () => {
          this.zoomValueDelayer.Update(ConvInvar.ToString(await this.googleMap.InteropObject.GetZoom()));
        });
      }
      if (this.ElementMarkers!=null) {
        if (this.AfterRenderUpDownCnt>=1) {
          this.AfterRenderCancelReq=true;
          while (this.AfterRenderCancelReq) {
            await Task.Delay(100);
          }
        }
        this.AfterRenderUpDownCnt++;
        LatLngBoundsLiteral bounds=null;
        var lCircles = new List<Circle>();
        var lConnectors = new List<Polyline>();
        try {
          bool bCancelled = false;
          var dictCurrent = new Dictionary<string,CircleOptions>();
          foreach (var elm in this.ElementMarkers.ToArray()) {
            var circleOptions = new CircleOptions {
              Map=googleMap.InteropObject,
              Center=elm.Position,
              Radius=elm.Radius,
              StrokeColor=elm.Color,
              StrokeOpacity=0.60f,
              StrokeWeight=2,
              FillColor=elm.Color,
              FillOpacity=0.35f,
              ZIndex=1000000,
            };
            dictCurrent[elm.Element.ElementName]=circleOptions;
            LatLngBoundsLiteral.CreateOrExtend(ref bounds,elm.Position);
          }
          if (this.circleList==null) {
            this.circleList = await CircleList.CreateAsync(this.googleMap.JsRuntime,dictCurrent);
          } else {
            await this.circleList.SetMultipleAsync(dictCurrent);
          }
          if (!bCancelled) {
            await this.OnZoomValueDelayed(null);
          }
        } finally {
          this.AfterRenderUpDownCnt--;
          this.elementBounds=bounds;
          if (this.AfterRenderCancelReq) {
            foreach (var circle in lCircles) {
              await circle.SetMap(null);
            }
            foreach (var connector in lConnectors) {
              await connector.SetMap(null);
            }
            this.AfterRenderCancelReq=false;
          }
        }
      }
    }
    private async Task OnZoomValueDelayed(string sValue) {
      if (!this.DynaZoomed) {
        return;
      }
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
          var dictCenters = new Dictionary<string,LatLngLiteral>();
          var dictRadii = new Dictionary<string,double>();
          for (int i = 0;i<N-1;i++) {
            var elm = this.ElementMarkers[i];
            dictCenters[elm.Element.ElementName]=new LatLngLiteral {
              Lat=elm.Position.Lat+aOffsets[i].Lat,
              Lng=elm.Position.Lng+aOffsets[i].Lng,
            };
            dictRadii[elm.Element.ElementName]=this.RadiusFactor*elm.Radius;
          }
          await this.circleList.SetCenters(dictCenters);
          await this.circleList.SetRadiuses(dictRadii);
        }
      } catch { }
    }
    protected override async Task FitBounds() {
      if (this.elementBounds==null || this.elementBounds.East==this.elementBounds.West || this.elementBounds.South==this.elementBounds.North) {
        await base.FitBounds();
      } else {
        await this.googleMap.InteropObject.FitBounds(this.elementBounds,OneOf.OneOf<int,Padding>.FromT0(5));
      }
    }
  }
}
