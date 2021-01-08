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
  public partial class LocationPopup : AreaMap
  {
    [Parameter]
    public LatLngLiteral Location { get; set; } = null;
    [Parameter]
    public EventCallback<LatLngLiteral> Closed { get; set; }
    //
    private Modal modalRef;
    private IconName sizeButtonIconName = IconName.Expand;
    private bool centered = false;
    private ModalSize modalSize = ModalSize.Large;
    private int? maxHeight = 80;
    //
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
    }
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
      }
    }
    public void Show(LatLng latLng=null) {
      this.modalRef.Show();
    }
    public void Hide() {
      this.modalRef.Hide();
    }
    public async Task Save() {
      this.Location=await this.googleMap.InteropObject.GetCenter();
    }
    private void sizeButtonClicked() {
      if (this.modalSize==ModalSize.Large) {
        this.modalSize=ModalSize.ExtraLarge;
        this.sizeButtonIconName=IconName.Compress;
      } else {
        this.modalSize=ModalSize.Large;
        this.sizeButtonIconName=IconName.Expand;
      }
    }
  }
}
