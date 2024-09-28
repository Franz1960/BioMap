using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazorise;
using GoogleMapsComponents;
using GoogleMapsComponents.Maps;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap.Shared
{
  public partial class LocationEdit : AreaMap
  {
    [Parameter]
    public EventCallback<LatLngLiteral> LocationChanged { get; set; }
    [Parameter]
    public double Radius { get; set; } = 30;
    //
    public LatLngLiteral Location {
      get => this._Location;
      set {
        if (!object.Equals(value, this._Location)) {
          this._Location = value;
          this.StateHasChanged();
        }
      }
    }
    private LatLngLiteral _Location = null;
    //
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
    }
    private Circle LocationCircle = null;
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
        if (this.Location == null) {
          this.Location = new LatLngLiteral {
            Lat = (this.aoiBounds.South + this.aoiBounds.North) / 2,
            Lng = (this.aoiBounds.West + this.aoiBounds.East) / 2,
          };
          await this.googleMap.InteropObject.SetCenter(this.Location);
        }
        var circleOptions = new CircleOptions {
          Map = this.googleMap.InteropObject,
          Center = this.Location,
          Radius = this.Radius,
          StrokeColor = "Turquoise",
          StrokeOpacity = 0.8f,
          StrokeWeight = 8,
          FillColor = "Turquoise",
          FillOpacity = 0.02f,
        };
        this.LocationCircle = await Circle.CreateAsync(this.googleMap.JsRuntime, circleOptions);
        await this.OnCenterChanged();
      }
    }
    private async Task OnAfterInitAsync() {
      await this.googleMap.InteropObject.AddListener("center_changed", async () => await this.OnCenterChanged());
    }
    private async Task OnCenterChanged() {
      try {
        LatLngBoundsLiteral bounds = await this.googleMap.InteropObject.GetBounds();
        if (bounds != null) {
          double fHeight = Math.Abs(bounds.North - bounds.South) * 111000;
          this.Location = new LatLngLiteral { Lat = (bounds.North + bounds.South) / 2, Lng = (bounds.East + bounds.West) / 2 };
          if (this.LocationCircle != null) {
            await this.LocationCircle.SetCenter(this.Location);
            await this.LocationCircle.SetRadius(fHeight * 0.05);
          }
          await this.LocationChanged.InvokeAsync(this.Location);
        }
      } catch { }
    }
  }
}
