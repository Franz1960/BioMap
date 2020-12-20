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
    protected LatLngBounds aoiBounds;
    protected LatLngBounds placesBounds;
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
        this.aoiBounds = await LatLngBounds.CreateAsync(googleMap.JsRuntime);
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
          foreach (var latLng in path) {
            await this.aoiBounds.Extend(latLng);
          }
          if (!await this.aoiBounds.IsEmpty()) {
            var boundsLiteral = await this.aoiBounds.ToJson();
            await this.googleMap.InteropObject.FitBounds(boundsLiteral,OneOf.OneOf<int,Padding>.FromT0(5));
          }
        } catch { }
      }
      this.placesBounds = await LatLngBounds.CreateAsync(googleMap.JsRuntime);
      bool bShowPlaces = (this.ShowPlaces && SD.CurrentUser.Level>=400);
      if (!this.PrevShowPlaces.HasValue || bShowPlaces!=this.PrevShowPlaces) {
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
            await this.placesBounds.Extend(new LatLngLiteral(place.LatLng.lng,place.LatLng.lat));
            await marker.AddListener("click",async () => {
              var s = place.Name;
            });
          }
        }
      }
    }
    protected virtual async Task FitBounds() {
      if (this.placesBounds==null || await this.placesBounds.IsEmpty()) {
        if (this.aoiBounds==null || await this.aoiBounds.IsEmpty()) {
          return;
        } else {
          var boundsLiteral = await this.aoiBounds.ToJson();
          await this.googleMap.InteropObject.FitBounds(boundsLiteral,OneOf.OneOf<int,Padding>.FromT0(5));
        }
      } else {
        var boundsLiteral = await this.placesBounds.ToJson();
        await this.googleMap.InteropObject.FitBounds(boundsLiteral,OneOf.OneOf<int,Padding>.FromT0(5));
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
