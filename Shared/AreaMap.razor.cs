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
    //
    protected GoogleMap googleMap;
    protected MapOptions mapOptions;
    protected LatLngBoundsLiteral aoiBounds;
    protected LatLngBoundsLiteral placesBounds;
    //
    private readonly List<Circle> visiblePlaceCircles = new List<Circle>();
    private readonly List<Marker> visiblePlaceMarkers = new List<Marker>();
    private Blazorise.Utils.ValueDelayer stateChangedDelayer;
    //
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
      this.stateChangedDelayer = new Blazorise.Utils.ValueDelayer(300);
      this.stateChangedDelayer.Delayed += async(sender,sValue) => await this.stateChangedDelayer_Delayed();
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
        try {
          var sJson = System.IO.File.ReadAllText(DS.DataDir + "conf/aoi.json");
          var vertices = JsonConvert.DeserializeObject<LatLngLiteral[]>(sJson);
          var path = new List<LatLngLiteral>(vertices);
          var polygon = Polygon.CreateAsync(googleMap.JsRuntime,new PolygonOptions {
            Map=googleMap.InteropObject,
            Editable=false,
            StrokeColor="#FF0000",
            StrokeOpacity=0.8f,
            StrokeWeight=3,
            FillColor="#FF0000",
            FillOpacity=0.02f,
            ZIndex=-1000000,
            Paths = new List<List<LatLngLiteral>>(new[] { path }),
          });
          LatLngBoundsLiteral bounds=null;
          foreach (var latLng in path) {
            LatLngBoundsLiteral.CreateOrExtend(ref bounds,latLng);
          }
          this.aoiBounds = bounds;
          if (!this.aoiBounds.IsEmpty()) {
            await this.googleMap.InteropObject.FitBounds(this.aoiBounds,OneOf.OneOf<int,Padding>.FromT0(5));
          }
        } catch { }
      }
      {
        bool bShowPlaces = (this.ShowPlaces && SD.CurrentUser.Level>=400);
        if (!this.PrevShowPlaces.HasValue || bShowPlaces!=this.PrevShowPlaces) {
          LatLngBoundsLiteral bounds=null;
          this.PrevShowPlaces=bShowPlaces;
          foreach (var circle in this.visiblePlaceCircles.ToArray()) {
            await circle.SetMap(null);
          }
          this.visiblePlaceCircles.Clear();
          foreach (var marker in this.visiblePlaceMarkers.ToArray()) {
            await marker.SetMap(null);
          }
          this.visiblePlaceMarkers.Clear();
          if (bShowPlaces) {
            foreach (var place in DS.AllPlaces) {
              var circle = await Circle.CreateAsync(googleMap.JsRuntime,new CircleOptions {
                Map=googleMap.InteropObject,
                Center=new LatLngLiteral(place.LatLng.lng,place.LatLng.lat),
                Radius=place.Radius,
                StrokeColor="Orange",
                StrokeOpacity=0.8f,
                StrokeWeight=3,
                FillColor="Orange",
                FillOpacity=0.02f,
              });
              this.visiblePlaceCircles.Add(circle);
              var marker = await Marker.CreateAsync(googleMap.JsRuntime,new MarkerOptions {
                Map=googleMap.InteropObject,
                Position=new LatLngLiteral(place.LatLng.lng,place.LatLng.lat - 0.0000096 * place.Radius),
                Label=new MarkerLabel {
                  Text=place.Name,
                  FontSize="18px",
                  FontWeight="bold",
                  Color="DarkOrange",
                },
                Icon = new Symbol { Path="M -15,10 L 15,10 z",StrokeColor="DarkOrange",},
              });
              this.visiblePlaceMarkers.Add(marker);
              LatLngBoundsLiteral.CreateOrExtend(ref bounds,new LatLngLiteral(place.LatLng.lng,place.LatLng.lat));
            }
          }
          this.placesBounds = bounds;
        }
      }
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
      this.stateChangedDelayer.Update(null);
    }
    private async Task stateChangedDelayer_Delayed() {
      await base.InvokeAsync(StateHasChanged);
    }
  }
}
