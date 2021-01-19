using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BioMap.Shared;

namespace BioMap.Pages.Lists
{
  public partial class NewElements : ComponentBase
  {
    private Element[] Elements = new Element[0];
    private PhotoPopup PhotoPopup1;
    private Element elementToMeasure=null;
    private Element elementToIdentify=null;
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
      await RefreshData();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
      }
      if (this.elementToMeasure!=null) {
      }
    }
    private async Task RefreshData() {
      this.Elements = Elements = await DS.GetElementsAsync(SD,SD.Filters,"elements.classification LIKE '%\"ClassName\":\"New\"%'","elements.creationtime ASC");
    }
    private void OnSelectClick(Element el) {
      this.PhotoPopup1.Show(el);
    }
    private async Task Measure_Clicked(Element el) {
      this.elementToMeasure=el;
      this.elementToIdentify=null;
      await this.InvokeAsync(()=>StateHasChanged());
    }
    private async Task Identify_Clicked(Element el) {
      this.elementToMeasure=null;
      this.elementToIdentify=el;
      await this.InvokeAsync(()=>StateHasChanged());
    }
    private async Task DeleteElement(Element el) {
      DS.DeleteElement(SD,el);
      DS.AddLogEntry(SD,"Deleted element "+el.ElementName);
      await RefreshData();
      await this.InvokeAsync(()=>StateHasChanged());
    }
  }
}