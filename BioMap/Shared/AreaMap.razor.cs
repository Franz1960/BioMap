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
  public partial class AreaMap : ComponentBase
  {
    [Inject]
    protected DataService DS { get; set; }
    [Inject]
    protected SessionData SD { get; set; }
    [Inject]
    protected NavigationManager NM { get; set; }
    [Inject]
    protected IJSRuntime JSRuntime { get; set; }
    [Parameter]
    public int ShowPlaces {
      get => this.SD.CurrentUser.Prefs.ShowPlaces;
      set {
        this.SD.CurrentUser.Prefs.ShowPlaces = value;
        this.DS.WriteUser(this.SD, this.SD.CurrentUser);
        this.DelayedStateHasChanged();
      }
    }
    private int? PrevShowPlaces = null;
    public void SetShowPlaces(int value) {
      this.ShowPlaces = value;
    }
    [Parameter]
    public bool ShowCustomMap {
      get => this.SD.CurrentUser.Prefs.ShowCustomMap;
      set {
        this.SD.CurrentUser.Prefs.ShowCustomMap = value;
        this.DelayedStateHasChanged();
      }
    }
    public void SetShowCustomMap(bool value) {
      this.ShowCustomMap = value;
    }
    //
    [Parameter]
    public bool AoiEditable {
      get => this._AoiEditable;
      set {
        this._AoiEditable = value;
        try {
          if (this.AoiPolygon != null) {
            this.AoiPolygonOptions.Editable = this.AoiEditable;
            this.AoiPolygon.SetOptions(this.AoiPolygonOptions);
          }
        } catch { }
        this.DelayedStateHasChanged();
      }
    }
    private bool _AoiEditable = false;
    public async Task<IEnumerable<LatLngLiteral>> GetAoiPath() {
      IEnumerable<LatLngLiteral> latLngLiterals = await this.AoiPolygon.GetPath();
      return latLngLiterals;
    }
    public async Task ClearAoiPath() {
      LatLngBoundsLiteral b = await this.googleMap.InteropObject.GetBounds();
      var path = new List<LatLngLiteral>(new[] {
        new LatLngLiteral { Lng = (b.West+b.East)/2, Lat = (2*b.North+b.South)/3 },
        new LatLngLiteral { Lng = (2*b.East+b.West)/3, Lat = (b.North+2*b.South)/3 },
        new LatLngLiteral { Lng = (b.East+2*b.West)/3, Lat = (b.North+2*b.South)/3 },
      });
      this.AoiPolygonOptions.Paths = new List<List<LatLngLiteral>>(new[] { path });
      await this.AoiPolygon.SetOptions(this.AoiPolygonOptions);
    }
    protected Polygon AoiPolygon = null;
    private PolygonOptions AoiPolygonOptions = null;
    //
    protected GoogleMap googleMap;
    protected MapOptions mapOptions;
    protected LatLngBoundsLiteral aoiBounds;
    protected LatLngBoundsLiteral placesBounds;
    //
    private CircleList placeCircleList = null;
    private MarkerList placeMarkerList = null;
    private GroundOverlay customMapOverlay = null;
    //
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
      this.mt = new Monitoring(this.SD);
      this.mt.RefreshData();
      this.mapOptions = new MapOptions() {
        Zoom = 12,
        Center = new LatLngLiteral {
          Lat = this.SD.CurrentProject.AoiCenterLat,
          Lng = this.SD.CurrentProject.AoiCenterLng
        },
        MapTypeId = (string.IsNullOrEmpty(this.SD.CurrentUser.Prefs.MaptypeId) ? MapTypeId.Roadmap : Enum.Parse<MapTypeId>(this.SD.CurrentUser.Prefs.MaptypeId)),
        StreetViewControl = false,
        MapTypeControlOptions = new MapTypeControlOptions {
          position = ControlPosition.TopLeft,
          mapTypeIds = new[] { MapTypeId.Roadmap, MapTypeId.Terrain, MapTypeId.Satellite, MapTypeId.Hybrid },
        },
      };
      if (this.DS.GetAoi(this.SD) == null) {
        this.mapOptions.Center = new LatLngLiteral {
          Lat = 49.1433,
          Lng = 12.3847
        };
        this.mapOptions.Zoom = 8;
      }
    }
    //
    private Monitoring mt { get; set; }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
        while (this.googleMap.InteropObject == null) {
          await Task.Delay(100);
        }
        await this.googleMap.InteropObject.AddListener("maptypeid_changed", async () => {
          MapTypeId mt = await this.googleMap.InteropObject.GetMapTypeId();
          this.SD.CurrentUser.Prefs.MaptypeId = mt.ToString();
          this.DS.WriteUser(this.SD, this.SD.CurrentUser);
        });
        #region Add area of interest.
        try {
          var path = new List<LatLngLiteral>();
          IEnumerable<LatLngLiteral> vertices = this.DS.GetAoi(this.SD);
          if (vertices != null) {
            path.AddRange(vertices);
          }
          this.AoiPolygonOptions = new PolygonOptions {
            Map = this.googleMap.InteropObject,
            Editable = this.AoiEditable,
            StrokeColor = "#FF0000",
            StrokeOpacity = 0.8f,
            StrokeWeight = 3,
            FillColor = "#FF0000",
            FillOpacity = 0.02f,
            ZIndex = -1000000,
            Paths = new List<List<LatLngLiteral>>(new[] { path }),
          };
          this.AoiPolygon = await Polygon.CreateAsync(this.googleMap.JsRuntime, this.AoiPolygonOptions);
          LatLngBoundsLiteral bounds = null;
          foreach (LatLngLiteral latLng in path) {
            LatLngBoundsLiteral.CreateOrExtend(ref bounds, latLng);
          }
          this.aoiBounds = bounds;
          if (this.aoiBounds != null && !this.aoiBounds.IsEmpty()) {
            await this.googleMap.InteropObject.FitBounds(this.aoiBounds, OneOf.OneOf<int, Padding>.FromT0(3));
          }
        } catch { }
        #endregion
        #region Add custom map.
        try {
          string sFilePathJson = this.DS.GetDataDir(this.SD) + "conf/MapImageBounds.json";
          string sFilePathMapImage = this.DS.GetDataDir(this.SD) + "conf/MapImage.jpg";
          if (System.IO.File.Exists(sFilePathJson) && System.IO.File.Exists(sFilePathMapImage)) {
            string sJson = System.IO.File.ReadAllText(sFilePathJson);
            LatLngLiteral[] bounds = JsonConvert.DeserializeObject<LatLngLiteral[]>(sJson);
            this.customMapOverlay = await GroundOverlay.CreateAsync(
              this.googleMap.JsRuntime,
              "/api/conf/MapImage.jpg?Project=" + this.SD.CurrentUser.Project,
              new LatLngBoundsLiteral(bounds[0], bounds[1]),
              new GroundOverlayOptions {
                Opacity = 01,
              });
          }
        } catch { }
        #endregion
      }
      if (this.customMapOverlay != null) {
        await this.customMapOverlay.SetMap(this.ShowCustomMap ? this.googleMap.InteropObject : null);
      }
      #region Add places.
      {
        if (!this.PrevShowPlaces.HasValue || this.ShowPlaces != this.PrevShowPlaces || this.refreshPlacesReq) {
          if (this.refreshPlacesReq) {
            await this.RefreshPlaces();
            this.refreshPlacesReq = false;
          }
          LatLngBoundsLiteral bounds = null;
          this.PrevShowPlaces = this.ShowPlaces;
          var dictPlaceCircles = new Dictionary<string, CircleOptions>();
          var dictPlaceMarkers = new Dictionary<string, MarkerOptions>();
          if (this.ShowPlaces >= 1) {
            foreach (Place place in this.DS.GetPlaces(this.SD)) {
              string mtColor = null;
              if (this.ShowPlaces == 1) {
                mtColor = "Orange";
              } else if (this.mt.Results.TryGetValue(place.Name, out Monitoring.ResultOfPlace rop)) {
                mtColor = rop?.PlannedMonitoring?.Color;
              }
              if (string.IsNullOrEmpty(mtColor)) {
                mtColor = "Grey";
              }
              var circleOptions = new CircleOptions {
                Map = this.googleMap.InteropObject,
                Center = new LatLngLiteral { Lng = place.LatLng.lng, Lat = place.LatLng.lat },
                Radius = place.Radius,
                StrokeColor = mtColor,
                StrokeOpacity = 0.8f,
                StrokeWeight = 3,
                FillColor = mtColor,
                FillOpacity = 0.02f,
              };
              dictPlaceCircles[place.Name] = circleOptions;
              var markerOptions = new MarkerOptions {
                Map = this.googleMap.InteropObject,
                Position = new LatLngLiteral { Lng = place.LatLng.lng, Lat = place.LatLng.lat - 0.0000096 * place.Radius },
                Label = new MarkerLabel {
                  Text = place.Name,
                  FontSize = "18px",
                  FontWeight = "bold",
                  Color = mtColor,
                },
                Icon = new Symbol { Path = "M -15,10 L 15,10 z", StrokeColor = mtColor, },
              };
              dictPlaceMarkers[place.Name] = markerOptions;
              LatLngBoundsLiteral.CreateOrExtend(ref bounds, new LatLngLiteral { Lng = place.LatLng.lng, Lat = place.LatLng.lat });
            }
          }
          this.placeCircleList = await CircleList.SyncAsync(this.placeCircleList, this.googleMap.JsRuntime, dictPlaceCircles);
          this.placeMarkerList = await MarkerList.SyncAsync(this.placeMarkerList, this.googleMap.JsRuntime, dictPlaceMarkers);
          this.placesBounds = bounds;
        }
      }
      #endregion
    }
    public void Invalidate() {
      this.DelayedStateHasChanged();
    }
    public async Task RefreshPlaces() {
      this.refreshPlacesReq = true;
      await this.InvokeAsync(() => this.StateHasChanged());
    }
    private bool refreshPlacesReq = true;
    public virtual async Task FitBounds(bool bConsiderPlaces = true) {
      await this.googleMap.InteropObject.SetZoom(8);
      if (!bConsiderPlaces || this.placesBounds == null || this.placesBounds.IsEmpty()) {
        if (this.aoiBounds == null || this.aoiBounds.IsEmpty()) {
          return;
        } else {
          await this.googleMap.InteropObject.FitBounds(this.aoiBounds, OneOf.OneOf<int, Padding>.FromT0(3));
        }
      } else {
        await this.googleMap.InteropObject.FitBounds(this.placesBounds, OneOf.OneOf<int, Padding>.FromT0(3));
      }
    }
    public void DelayedStateHasChanged() {
      Utilities.CallDelayed(100, (oaArgs) => {
        this.DS.WriteUser(this.SD, this.SD.CurrentUser);
        base.InvokeAsync(this.StateHasChanged).Wait();
      });
    }
  }
}
