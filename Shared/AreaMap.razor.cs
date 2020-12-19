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
    public bool ShowPlaces { get; set; } = true;
    //
    protected GoogleMap googleMap=new GoogleMap();
    protected MapOptions mapOptions;
    //
    protected override async void OnInitialized() {
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
        if (this.ShowPlaces && SD.CurrentUser.Level>=400) {
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
            var marker = await Marker.CreateAsync(googleMap.JsRuntime,new MarkerOptions {
              Map=googleMap.InteropObject,
              Position=new LatLngLiteral(place.LatLng.lng,place.LatLng.lat - 0.0000120 * place.Radius),
              Label=new MarkerLabel {
                Text=place.Name,
                FontSize="18px",
                FontWeight="bold",
                Color="DarkOrange",
              },
              Icon = "symbols/PlaceLabel.svg",
            });
            await marker.AddListener("click",async () => {
              var s = place.Name;
            });
          }
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
        } catch { }
      }
    }
  }
}
