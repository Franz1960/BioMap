using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoogleMapsComponents;
using GoogleMapsComponents.Maps;
using GoogleMapsComponents.Maps.Coordinates;
using GoogleMapsComponents.Maps.Extension;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
      internal int ZIndex = 1000000;
    }
    [Parameter]
    public ElementMarker[] ElementMarkers {
      get => this._ElementMarkers;
      set {
        lock (this.ElementMarkersLock) {
          this._ElementMarkers = value;
        }
        this.DelayedStateHasChanged();
      }
    }
    private ElementMarker[] _ElementMarkers = null;
    public ElementMarker GetMarkerForElement(Element el) {
      if (this.ElementMarkers != null) {
        foreach (ElementMarker elm in this.ElementMarkers) {
          if (elm.Element?.ElementName == el?.ElementName) {
            return elm;
          }
        }
      }
      return null;
    }
    [Parameter]
    public PhotoPopup PhotoPopup { get; set; }
    [Parameter]
    public bool DynaZoomed {
      get => this.SD.CurrentUser.Prefs.DynaZoomed;
      set {
        this.SD.CurrentUser.Prefs.DynaZoomed = value;
        this.DS.WriteUser(this.SD, this.SD.CurrentUser);
        Task.Run(async () => await this.RefreshRadii());
      }
    }
    public void SetDynaZoomed(bool value) {
      this.DynaZoomed = value;
    }
    [Parameter]
    public int DisplayConnectors {
      get => this.SD.CurrentUser.Prefs.DisplayConnectors;
      set {
        this.SD.CurrentUser.Prefs.DisplayConnectors = value;
        this.DS.WriteUser(this.SD, this.SD.CurrentUser);
        this.DelayedStateHasChanged();
      }
    }
    //
    private CircleList circleList = null;
    private PolylineList connectorList = null;
    //
    protected LatLngBoundsLiteral elementBounds = null;
    //
    private readonly object ElementMarkersLock = new object();
    private int AfterRenderUpDownCnt = 0;
    private bool AfterRenderCancelReq = false;
    private double RadiusFactor = 1;
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
        await this.RefreshRadii();
        await this.googleMap.InteropObject.AddListener("zoom_changed", async () => await this.RefreshRadii());
      }
      if (this.ElementMarkers != null) {
        if (this.AfterRenderUpDownCnt >= 1) {
          this.AfterRenderCancelReq = true;
          while (this.AfterRenderCancelReq) {
            await Task.Delay(100);
          }
        }
        this.AfterRenderUpDownCnt++;
        LatLngBoundsLiteral bounds = null;
        try {
          var dictCircles = new Dictionary<string, CircleOptions>();
          var dictConnectors = new Dictionary<string, PolylineOptions>();
          lock (this.ElementMarkersLock) {
            foreach (ElementMarker elm in this.ElementMarkers.ToArray()) {
              var circleOptions = new CircleOptions {
                Map = this.googleMap.InteropObject,
                Center = elm.Position,
                Radius = elm.Radius * this.RadiusFactor,
                StrokeColor = elm.Color,
                StrokeOpacity = 0.60f,
                StrokeWeight = 2,
                FillColor = elm.Color,
                FillOpacity = 0.35f,
                ZIndex = elm.ZIndex,
              };
              dictCircles[elm.Element.ElementName] = circleOptions;
              LatLngBoundsLiteral.CreateOrExtend(ref bounds, elm.Position);
              if (elm.PrevMarker != null && this.DisplayConnectors >= 1) {
                var connectorOption = new PolylineOptions {
                  Map = this.googleMap.InteropObject,
                  Geodesic = true,
                  StrokeColor = "#50D020",
                  StrokeOpacity = 0.7f,
                  StrokeWeight = 2,
                  Icons = new[] {
                   new IconSequence { Icon=new Symbol { Path=SymbolPath.FORWARD_CLOSED_ARROW }, Offset="100%" },
                   new IconSequence { Icon=new Symbol { Path=SymbolPath.FORWARD_OPEN_ARROW }, Offset="66%" },
                   new IconSequence { Icon=new Symbol { Path=SymbolPath.FORWARD_OPEN_ARROW }, Offset="33%" },
                  },
                  Path = new[] {
                    elm.PrevMarker.Position,
                    elm.Position,
                  },
                };
                dictConnectors[elm.Element.ElementName] = connectorOption;
              }
            }
          }
          this.circleList = await CircleList.SyncAsync(this.circleList, this.googleMap.JsRuntime, dictCircles, (ev, sKey, entity) => {
            ElementMarker elm = this.GetElementMarker(sKey);
            if (elm != null && elm.Element != null) {
              if (this.PhotoPopup != null) {
                this.PhotoPopup.Show(elm.Element, () => {
                  this.RefreshElementMarkers();
                  // Set below lowest Z index.
                  int? minZIndex = null;
                  foreach (ElementMarker elm1 in this.ElementMarkers) {
                    int zIndex = elm1.ZIndex;
                    if (!minZIndex.HasValue || zIndex < minZIndex.Value) {
                      minZIndex = zIndex;
                    }
                  }
                  if (minZIndex.HasValue) {
                    elm.ZIndex = minZIndex.Value - 1;
                    ((Circle)entity).SetOptions(new CircleOptions {
                      ZIndex = elm.ZIndex,
                    });
                  }
                  this.StateHasChanged();
                });
              }
            }
          });
          this.connectorList = await PolylineList.SyncAsync(this.connectorList, this.googleMap.JsRuntime, dictConnectors);
        } finally {
          this.AfterRenderUpDownCnt--;
          this.elementBounds = bounds;
          if (this.AfterRenderCancelReq) {
            this.AfterRenderCancelReq = false;
          }
        }
      }
    }
    protected virtual void RefreshElementMarkers(bool bSkipDbQuery = false) {
    }
    private double? PrevRadiusFactor = null;
    private async Task RefreshRadii() {
      //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff")+": CallDelayed");
      await Task.Delay(200);
      {
        //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff")+": Elapsed");
        if (this.DynaZoomed) {
          try {
            LatLngBoundsLiteral bounds = await this.googleMap.InteropObject.GetBounds();
            double fHeight = Math.Abs(bounds.North - bounds.South) * 111000;
            this.RadiusFactor = fHeight * 0.004;
          } catch { }
        } else {
          this.RadiusFactor = 1;
        }
        if (this.circleList != null) {
          if (this.RadiusFactor != this.PrevRadiusFactor) {
            try {
              var dictRadii = new Dictionary<string, double>();
              lock (this.ElementMarkersLock) {
                int N = this.ElementMarkers.Count();
                for (int i = 0; i < N; i++) {
                  ElementMarker elm = this.ElementMarkers[i];
                  dictRadii[elm.Element.ElementName] = this.RadiusFactor * elm.Radius;
                }
              }
              await this.circleList.SetRadiuses(dictRadii);
            } catch { }
            this.PrevRadiusFactor = this.RadiusFactor;
          }
        }
      }
    }
    public override async Task FitBounds(bool bConsiderPlaces = true) {
      if (this.elementBounds == null || this.elementBounds.East == this.elementBounds.West || this.elementBounds.South == this.elementBounds.North) {
        await base.FitBounds(bConsiderPlaces);
      } else {
        await this.googleMap.InteropObject.FitBounds(this.elementBounds, OneOf.OneOf<int, Padding>.FromT0(5));
      }
    }
    public ElementMarker GetElementMarker(string sKey) {
      lock (this.ElementMarkersLock) {
        foreach (ElementMarker elm in this.ElementMarkers) {
          if (elm.Element.ElementName == sKey) {
            return elm;
          }
        }
      }
      return null;
    }
  }
}

