using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Blazorise;
using GoogleMapsComponents;
using GoogleMapsComponents.Maps;

namespace BioMap.Shared
{
  public partial class LocationEdit : AreaMap
  {
    [Parameter]
    public LatLngLiteral Location { get; set; } = null;
    [Parameter]
    public EventCallback<LatLngLiteral> LocationChanged { get; set; }
    //
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
    }
    private Circle LocationCircle=null;
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
        if (this.Location==null) {
          this.Location=new LatLngLiteral {
            Lat=(this.aoiBounds.South+this.aoiBounds.North)/2,
            Lng=(this.aoiBounds.West+this.aoiBounds.East)/2,
          };
          await this.googleMap.InteropObject.SetCenter(this.Location);
        }
        var circleOptions = new CircleOptions {
          Map=googleMap.InteropObject,
          Center=this.Location,
          Radius=30,
          StrokeColor="Turquoise",
          StrokeOpacity=0.8f,
          StrokeWeight=8,
          FillColor="Turquoise",
          FillOpacity=0.02f,
        };
        this.LocationCircle = await Circle.CreateAsync(this.googleMap.JsRuntime,circleOptions);
        await this.OnCenterChanged();
      }
    }
    private async Task OnAfterInitAsync()
    {
        await this.googleMap.InteropObject.AddListener("center_changed", async () => await OnCenterChanged());
    }
    private async Task OnCenterChanged()
    {
      try {
        var bounds = await this.googleMap.InteropObject.GetBounds();
        var fHeight = Math.Abs(bounds.North-bounds.South)*111000;
        this.Location = new LatLngLiteral { Lat=(bounds.North+bounds.South)/2,Lng=(bounds.East+bounds.West)/2 };
        if (this.LocationCircle!=null) {
          await this.LocationCircle.SetCenter(this.Location);
          await this.LocationCircle.SetRadius(fHeight*0.05);
        }
        await this.LocationChanged.InvokeAsync(this.Location);
      } catch { }
    }
  }
}
