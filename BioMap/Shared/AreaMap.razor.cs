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
  public partial class AreaMap : ComponentBase
  {
    [Inject]
    protected DataService DS { get; set; }
    [Inject]
    protected SessionData SD { get; set; }
    [Parameter]
    public bool ShowPlaces {
      get {
        return this._ShowPlaces;
      }
      set {
        this._ShowPlaces=value;
        this.DelayedStateHasChanged();
      }
    }
    private bool _ShowPlaces = true;
    private bool? PrevShowPlaces = null;
    [Parameter]
    public bool ShowCustomMap {
      get {
        return SD.ShowCustomMap;
      }
      set {
        SD.ShowCustomMap=value;
        this.DelayedStateHasChanged();
      }
    }
    //
    [Parameter]
    public bool AoiEditable {
      get {
        return this._AoiEditable;
      }
      set {
        this._AoiEditable=value;
        try {
          if (this.AoiPolygon!=null) {
            this.AoiPolygonOptions.Editable=this.AoiEditable;
            this.AoiPolygon.SetOptions(this.AoiPolygonOptions);
          }
        } catch { }
        this.DelayedStateHasChanged();
      }
    }
    private bool _AoiEditable = false;
    public async Task<IEnumerable<LatLngLiteral>> GetAoiPath() {
      var latLngLiterals=await this.AoiPolygon.GetPath();
      return latLngLiterals;
    }
    public async Task ClearAoiPath() {
      var b=await this.googleMap.InteropObject.GetBounds();
      var path=new List<LatLngLiteral>(new [] {
        new LatLngLiteral((b.West+b.East)/2,b.North),
        new LatLngLiteral(b.East,b.South),
        new LatLngLiteral(b.West,b.South),
      });
      this.AoiPolygonOptions.Paths=new List<List<LatLngLiteral>>(new[] { path });
      await this.AoiPolygon.SetOptions(this.AoiPolygonOptions);
    }
    private Polygon AoiPolygon = null;
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
      mapOptions = new MapOptions() {
        Zoom = 12,
        Center = new LatLngLiteral() {
          Lat = 48.994249,
          Lng = 12.190451
        },
        MapTypeId = MapTypeId.Roadmap,
        StreetViewControl=false,
      };
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
        while (googleMap.InteropObject==null) {
          await Task.Delay(100);
        }
        #region Add area of interest.
        try {
          var vertices = DS.GetAoi(SD);
          var path = new List<LatLngLiteral>(vertices);
          this.AoiPolygonOptions=new PolygonOptions {
            Map=googleMap.InteropObject,
            Editable=this.AoiEditable,
            StrokeColor="#FF0000",
            StrokeOpacity=0.8f,
            StrokeWeight=3,
            FillColor="#FF0000",
            FillOpacity=0.02f,
            ZIndex=-1000000,
            Paths = new List<List<LatLngLiteral>>(new[] { path }),
          };
          this.AoiPolygon = await Polygon.CreateAsync(googleMap.JsRuntime,this.AoiPolygonOptions);
          LatLngBoundsLiteral bounds=null;
          foreach (var latLng in path) {
            LatLngBoundsLiteral.CreateOrExtend(ref bounds,latLng);
          }
          this.aoiBounds = bounds;
          if (!this.aoiBounds.IsEmpty()) {
            await this.googleMap.InteropObject.FitBounds(this.aoiBounds,OneOf.OneOf<int,Padding>.FromT0(5));
          }
        } catch { }
        #endregion
        #region Add custom map.
        try {
          var sFilePathJson = DS.GetDataDir(SD) + "conf/MapImageBounds.json";
          var sFilePathMapImage = DS.GetDataDir(SD) + "conf/MapImage.jpg";
          if (System.IO.File.Exists(sFilePathJson) && System.IO.File.Exists(sFilePathMapImage)) {
            var sJson = System.IO.File.ReadAllText(sFilePathJson);
            var bounds = JsonConvert.DeserializeObject<LatLngLiteral[]>(sJson);
            customMapOverlay = await GroundOverlay.CreateAsync(
              googleMap.JsRuntime,
              "data/conf/MapImage.jpg",
              new LatLngBoundsLiteral(bounds[0],bounds[1]),
              new GroundOverlayOptions {
                Opacity = 01,
            });
          }
        } catch { }
        #endregion
      }
      if (customMapOverlay!=null) {
        await customMapOverlay.SetMap(ShowCustomMap ? googleMap.InteropObject : null);
      }
      #region Add places.
      {
        bool bShowPlaces = (this.ShowPlaces && SD.CurrentUser.Level>=0);
        if (!this.PrevShowPlaces.HasValue || bShowPlaces!=this.PrevShowPlaces) {
          LatLngBoundsLiteral bounds=null;
          this.PrevShowPlaces=bShowPlaces;
          var dictPlaceCircles = new Dictionary<string,CircleOptions>();
          var dictPlaceMarkers = new Dictionary<string,MarkerOptions>();
          if (bShowPlaces) {
            foreach (var place in DS.GetPlaces(SD)) {
              var circleOptions = new CircleOptions {
                Map=googleMap.InteropObject,
                Center=new LatLngLiteral(place.LatLng.lng,place.LatLng.lat),
                Radius=place.Radius,
                StrokeColor="Orange",
                StrokeOpacity=0.8f,
                StrokeWeight=3,
                FillColor="Orange",
                FillOpacity=0.02f,
              };
              dictPlaceCircles[place.Name]=circleOptions;
              var markerOptions = new MarkerOptions {
                Map=googleMap.InteropObject,
                Position=new LatLngLiteral(place.LatLng.lng,place.LatLng.lat - 0.0000096 * place.Radius),
                Label=new MarkerLabel {
                  Text=place.Name,
                  FontSize="18px",
                  FontWeight="bold",
                  Color="DarkOrange",
                },
                Icon = new Symbol { Path="M -15,10 L 15,10 z",StrokeColor="DarkOrange",},
              };
              dictPlaceMarkers[place.Name]=markerOptions;
              LatLngBoundsLiteral.CreateOrExtend(ref bounds,new LatLngLiteral(place.LatLng.lng,place.LatLng.lat));
            }
          }
          this.placeCircleList = await CircleList.SyncAsync(this.placeCircleList,this.googleMap.JsRuntime,dictPlaceCircles);
          this.placeMarkerList = await MarkerList.SyncAsync(this.placeMarkerList,this.googleMap.JsRuntime,dictPlaceMarkers);
          this.placesBounds = bounds;
        }
      }
      #endregion
    }
    protected virtual async Task FitBounds() {
      if (this.placesBounds==null || this.placesBounds.IsEmpty()) {
        if (this.aoiBounds==null || this.aoiBounds.IsEmpty()) {
          return;
        } else {
          await this.googleMap.InteropObject.FitBounds(this.aoiBounds,OneOf.OneOf<int,Padding>.FromT0(5));
        }
      } else {
        await this.googleMap.InteropObject.FitBounds(this.placesBounds,OneOf.OneOf<int,Padding>.FromT0(5));
      }
    }
    public void DelayedStateHasChanged() {
      Utilities.CallDelayed(900,()=>{
        base.InvokeAsync(StateHasChanged).Wait();
      });
    }
  }
}
