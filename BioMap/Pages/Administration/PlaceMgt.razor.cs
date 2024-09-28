using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BioMap.Shared;
using Blazorise;
using GoogleMapsComponents.Maps;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap.Pages.Administration
{
  public partial class PlaceMgt : ComponentBase
  {
    //
    private Modal progressModalRef;
    private int progressCompletion = 0;
    private List<string> messages = new List<string>();
    //
    private bool addPlaceVisible = false;
    private bool addPlaceFirstTimeVisible = true;
    private Place placeBeingEdited = null;
    private LocationEdit LocationEditRef;
    private string newPlaceName = "";
    private GoogleMapsComponents.Maps.LatLngLiteral newPlaceLocation;
    private double newPlaceRadius = 30;
    private string newPlaceErrorMessage = "";
    //
    private Place[] AllPlaces = new Place[0];
    protected override void OnInitialized() {
      base.OnInitialized();
      this.NM.LocationChanged += this.NM_LocationChanged;
      this.RefreshData();
    }
    private void NM_LocationChanged(object sender, LocationChangedEventArgs e) {
      this.NM.LocationChanged -= this.NM_LocationChanged;
      this.ChangeByUser(null);
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
      }
      if (this.addPlaceVisible && this.addPlaceFirstTimeVisible) {
        await this.LocationEditRef.FitBounds(false);
        this.addPlaceFirstTimeVisible = false;
      }
    }
    private void RefreshData() {
      this.AllPlaces = this.DS.GetPlaces(this.SD);
      this.StateHasChanged();
    }
    private async Task RecalculatePlaces_Clicked() {
      this.progressCompletion = 0;
      await this.progressModalRef.Show();
      try {
        await DataCalculator.CalculatePlacesOfElements(this.SD, (completion) => {
          if (this.progressCompletion != completion) {
            this.progressCompletion = completion;
            this.InvokeAsync(() => this.StateHasChanged());
          }
        });
      } finally {
        await this.InvokeAsync(() => { this.progressModalRef.Hide(); this.StateHasChanged(); });
      }
    }
    private async Task AddOrEditPlaceCancel_Clicked() {
      this.addPlaceVisible = false;
      this.placeBeingEdited = null;
      await this.InvokeAsync(() => this.StateHasChanged());
    }
    private async Task AddPlace_Opened() {
      if (this.addPlaceVisible) {
        this.addPlaceVisible = false;
      } else {
        this.newPlaceName = "";
        this.newPlaceRadius = 30;
        this.newPlaceErrorMessage = "";
        this.addPlaceVisible = true;
      }
      await this.InvokeAsync(() => this.StateHasChanged());
    }
    private async Task AddPlace_Acknowledged() {
      var latLng = new LatLng { lat = this.newPlaceLocation.Lat, lng = this.newPlaceLocation.Lng };
      if (!this.SD.CurrentProject.IsLocationInsideAoi(latLng)) {
        this.newPlaceErrorMessage = this.Localize["Location must be in project area."];
      } else if (string.IsNullOrEmpty(this.newPlaceName) || this.DS.GetPlaceByName(this.SD, this.newPlaceName) != null) {
        this.newPlaceErrorMessage = this.Localize["Provide a new name."];
      } else {
        this.DS.CreatePlace(this.SD, new Place {
          Name = this.newPlaceName,
          Radius = 150,
          LatLng = latLng,
        });
        this.newPlaceErrorMessage = "";
        this.addPlaceVisible = false;
        this.RefreshData();
        await this.LocationEditRef.RefreshPlaces();
      }
      await this.InvokeAsync(() => this.StateHasChanged());
    }
    private async Task EditPlace_Clicked(string sPlaceName) {
      this.placeBeingEdited = this.DS.GetPlaceByName(this.SD, sPlaceName);
      if (this.placeBeingEdited != null) {
        this.newPlaceName = sPlaceName;
        this.newPlaceLocation = new LatLngLiteral(this.placeBeingEdited.LatLng.lat, this.placeBeingEdited.LatLng.lng);
        this.newPlaceRadius = this.placeBeingEdited.Radius;
        this.LocationEditRef.Location = this.newPlaceLocation;
        await this.LocationEditRef.FitBounds();
      }
      await this.InvokeAsync(() => this.StateHasChanged());
    }
    private async Task EditPlace_Acknowledged() {
      var latLng = new LatLng { lat = this.newPlaceLocation.Lat, lng = this.newPlaceLocation.Lng };
      if (!this.SD.CurrentProject.IsLocationInsideAoi(latLng)) {
        this.newPlaceErrorMessage = this.Localize["Location must be in project area."];
      } else {
        this.placeBeingEdited.LatLng = latLng;
        this.ChangeByUser(this.placeBeingEdited);
        this.newPlaceErrorMessage = "";
        this.placeBeingEdited = null;
        this.RefreshData();
        await this.LocationEditRef.RefreshPlaces();
      }
      await this.InvokeAsync(() => this.StateHasChanged());
    }
    private void CheckBoxAlienateLocations_Changed(bool bChecked) {
      this.SD.AlienateLocations = bChecked;
      if (!bChecked) {
        this.AllPlaces = this.DS.GetPlaces(this.SD);
      }
    }
    private void TraitValueChanged(Place place, int idx, int value) {
      place.TraitValues[idx] = value;
      this.ChangeByUser(place);
    }
    private void ChangeByUser(Place place) {
      if (this.SD.MaySeeRealLocations) {
        if (place == null) {
          foreach (Place p in this.AllPlaces) {
            this.DS.WritePlace(this.SD, p);
          }
        } else {
          this.DS.WritePlace(this.SD, place);
        }
      }
    }
    private void RenamePlace(Place place, string sNewName) {
      if (this.SD.MaySeeRealLocations) {
        if (place != null) {
          this.DS.RenamePlace(this.SD, place, sNewName);
        }
      }
    }
  }
}
