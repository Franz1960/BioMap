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
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }
        [Parameter]
        public bool ShowPlaces
        {
            get
            {
                return SD.CurrentUser.Prefs.ShowPlaces;
            }
            set
            {
                SD.CurrentUser.Prefs.ShowPlaces = value;
                DS.WriteUser(SD, SD.CurrentUser);
                this.DelayedStateHasChanged();
            }
        }
        private bool? PrevShowPlaces = null;
        [Parameter]
        public bool ShowCustomMap
        {
            get
            {
                return SD.ShowCustomMap;
            }
            set
            {
                SD.ShowCustomMap = value;
                this.DelayedStateHasChanged();
            }
        }
        //
        [Parameter]
        public bool AoiEditable
        {
            get
            {
                return this._AoiEditable;
            }
            set
            {
                this._AoiEditable = value;
                try
                {
                    if (this.AoiPolygon != null)
                    {
                        this.AoiPolygonOptions.Editable = this.AoiEditable;
                        this.AoiPolygon.SetOptions(this.AoiPolygonOptions);
                    }
                }
                catch { }
                this.DelayedStateHasChanged();
            }
        }
        private bool _AoiEditable = false;
        public async Task<IEnumerable<LatLngLiteral>> GetAoiPath()
        {
            var latLngLiterals = await this.AoiPolygon.GetPath();
            return latLngLiterals;
        }
        public async Task ClearAoiPath()
        {
            var b = await this.googleMap.InteropObject.GetBounds();
            var path = new List<LatLngLiteral>(new[] {
        new LatLngLiteral((b.West+b.East)/2,(2*b.North+b.South)/3),
        new LatLngLiteral((2*b.East+b.West)/3,(b.North+2*b.South)/3),
        new LatLngLiteral((b.East+2*b.West)/3,(b.North+2*b.South)/3),
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
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            mapOptions = new MapOptions()
            {
                Zoom = 12,
                Center = new LatLngLiteral()
                {
                    Lat = SD.CurrentProject.AoiCenterLat,
                    Lng = SD.CurrentProject.AoiCenterLng
                },
                MapTypeId = (string.IsNullOrEmpty(SD.CurrentUser.Prefs.MaptypeId) ? MapTypeId.Roadmap : Enum.Parse<MapTypeId>(SD.CurrentUser.Prefs.MaptypeId)),
                StreetViewControl = false,
            };
            if (DS.GetAoi(SD) == null)
            {
                mapOptions.Center = new LatLngLiteral()
                {
                    Lat = 49.1433,
                    Lng = 12.3847
                };
                mapOptions.Zoom = 8;
            }
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                while (googleMap.InteropObject == null)
                {
                    await Task.Delay(100);
                }
                await this.googleMap.InteropObject.AddListener("maptypeid_changed", async () =>
                {
                    var mt = await this.googleMap.InteropObject.GetMapTypeId();
                    SD.CurrentUser.Prefs.MaptypeId = mt.ToString();
                    DS.WriteUser(SD, SD.CurrentUser);
                });
                #region Add area of interest.
                try
                {
                    var path = new List<LatLngLiteral>();
                    var vertices = DS.GetAoi(SD);
                    if (vertices != null)
                    {
                        path.AddRange(vertices);
                    }
                    this.AoiPolygonOptions = new PolygonOptions
                    {
                        Map = googleMap.InteropObject,
                        Editable = this.AoiEditable,
                        StrokeColor = "#FF0000",
                        StrokeOpacity = 0.8f,
                        StrokeWeight = 3,
                        FillColor = "#FF0000",
                        FillOpacity = 0.02f,
                        ZIndex = -1000000,
                        Paths = new List<List<LatLngLiteral>>(new[] { path }),
                    };
                    this.AoiPolygon = await Polygon.CreateAsync(googleMap.JsRuntime, this.AoiPolygonOptions);
                    LatLngBoundsLiteral bounds = null;
                    foreach (var latLng in path)
                    {
                        LatLngBoundsLiteral.CreateOrExtend(ref bounds, latLng);
                    }
                    this.aoiBounds = bounds;
                    if (this.aoiBounds != null && !this.aoiBounds.IsEmpty())
                    {
                        await this.googleMap.InteropObject.FitBounds(this.aoiBounds, OneOf.OneOf<int, Padding>.FromT0(3));
                    }
                }
                catch { }
                #endregion
                #region Add custom map.
                try
                {
                    var sFilePathJson = DS.GetDataDir(SD) + "conf/MapImageBounds.json";
                    var sFilePathMapImage = DS.GetDataDir(SD) + "conf/MapImage.jpg";
                    if (System.IO.File.Exists(sFilePathJson) && System.IO.File.Exists(sFilePathMapImage))
                    {
                        var sJson = System.IO.File.ReadAllText(sFilePathJson);
                        var bounds = JsonConvert.DeserializeObject<LatLngLiteral[]>(sJson);
                        customMapOverlay = await GroundOverlay.CreateAsync(
                          googleMap.JsRuntime,
                          "data/conf/MapImage.jpg",
                          new LatLngBoundsLiteral(bounds[0], bounds[1]),
                          new GroundOverlayOptions
                          {
                              Opacity = 01,
                          });
                    }
                }
                catch { }
                #endregion
            }
            if (customMapOverlay != null)
            {
                await customMapOverlay.SetMap(ShowCustomMap ? googleMap.InteropObject : null);
            }
            #region Add places.
            {
                bool bShowPlaces = (this.ShowPlaces && SD.CurrentUser.Level >= 0);
                if (!this.PrevShowPlaces.HasValue || bShowPlaces != this.PrevShowPlaces || this.refreshPlacesReq)
                {
                    this.refreshPlacesReq = false;
                    LatLngBoundsLiteral bounds = null;
                    this.PrevShowPlaces = bShowPlaces;
                    var dictPlaceCircles = new Dictionary<string, CircleOptions>();
                    var dictPlaceMarkers = new Dictionary<string, MarkerOptions>();
                    if (bShowPlaces)
                    {
                        foreach (var place in DS.GetPlaces(SD))
                        {
                            var circleOptions = new CircleOptions
                            {
                                Map = googleMap.InteropObject,
                                Center = new LatLngLiteral(place.LatLng.lng, place.LatLng.lat),
                                Radius = place.Radius,
                                StrokeColor = "Orange",
                                StrokeOpacity = 0.8f,
                                StrokeWeight = 3,
                                FillColor = "Orange",
                                FillOpacity = 0.02f,
                            };
                            dictPlaceCircles[place.Name] = circleOptions;
                            var markerOptions = new MarkerOptions
                            {
                                Map = googleMap.InteropObject,
                                Position = new LatLngLiteral(place.LatLng.lng, place.LatLng.lat - 0.0000096 * place.Radius),
                                Label = new MarkerLabel
                                {
                                    Text = place.Name,
                                    FontSize = "18px",
                                    FontWeight = "bold",
                                    Color = "DarkOrange",
                                },
                                Icon = new Symbol { Path = "M -15,10 L 15,10 z", StrokeColor = "DarkOrange", },
                            };
                            dictPlaceMarkers[place.Name] = markerOptions;
                            LatLngBoundsLiteral.CreateOrExtend(ref bounds, new LatLngLiteral(place.LatLng.lng, place.LatLng.lat));
                        }
                    }
                    this.placeCircleList = await CircleList.SyncAsync(this.placeCircleList, this.googleMap.JsRuntime, dictPlaceCircles);
                    this.placeMarkerList = await MarkerList.SyncAsync(this.placeMarkerList, this.googleMap.JsRuntime, dictPlaceMarkers);
                    this.placesBounds = bounds;
                }
            }
            #endregion
        }
        public async Task RefreshPlaces()
        {
            this.refreshPlacesReq = true;
            await this.InvokeAsync(() => this.StateHasChanged());
        }
        private bool refreshPlacesReq = true;
        public virtual async Task FitBounds(bool bConsiderPlaces = true)
        {
            if (!bConsiderPlaces || this.placesBounds == null || this.placesBounds.IsEmpty())
            {
                if (this.aoiBounds == null || this.aoiBounds.IsEmpty())
                {
                    return;
                }
                else
                {
                    await this.googleMap.InteropObject.FitBounds(this.aoiBounds, OneOf.OneOf<int, Padding>.FromT0(3));
                }
            }
            else
            {
                await this.googleMap.InteropObject.FitBounds(this.placesBounds, OneOf.OneOf<int, Padding>.FromT0(3));
            }
        }
        public void DelayedStateHasChanged()
        {
            Utilities.CallDelayed(900, (oaArgs) =>
            {
                base.InvokeAsync(this.StateHasChanged).Wait();
            });
        }
    }
}
