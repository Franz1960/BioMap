using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using BioMap.Shared;

namespace BioMap.Pages.Lists
{
  public partial class NewElements : ComponentBase
  {
    private Element[] Elements = new Element[0];
    private PhotoPopup PhotoPopup1;
    private Element elementToMeasure=null;
    private Element elementToIdentify=null;
    protected Blazor.ImageSurveyor.ImageSurveyor imageSurveyor;
    private bool disableSetImage=false;
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
      this.disableSetImage=false;
      await this.InvokeAsync(()=>StateHasChanged());
    }
    private async Task Identify_Clicked(Element el) {
      this.elementToMeasure=null;
      this.elementToIdentify=el;
      this.disableSetImage=false;
      await this.InvokeAsync(()=>StateHasChanged());
    }
    private async Task LoadElementToMeasure(Element el) {
      if (!this.disableSetImage) {
        if (elementToMeasure!=null) {
          await this.imageSurveyor.SetImageUrlAsync("api/photos/"+elementToMeasure.ElementName+"?Project="+SD.CurrentUser.Project+"&ForceOrig=1",elementToMeasure.MeasureData);
          this.disableSetImage=true;
        }
      }
    }
    private void MeasureDataChanged(Element el,Blazor.ImageSurveyor.ImageSurveyorMeasureData md) {
      el.MeasureData=md;
      Utilities.CallDelayed(2000,()=>{
        DS.WriteElement(SD,el);
        });
    }
    private async Task ResetPositions_Clicked(Element el) {
      var sFilePath = DS.GetFilePathForImage(SD.CurrentUser.Project,el.ElementName,true);
      if (System.IO.File.Exists(sFilePath)) {
        using (var img = Image.Load(sFilePath)) {
          int w=img.Width;
          int h=img.Height;
          el.MeasureData.normalizePoints=new [] {
            new Blazor.ImageSurveyor.ImageSurveyorMeasureData.Point2d { x=w*0.25f,y=h*0.50f },
            new Blazor.ImageSurveyor.ImageSurveyorMeasureData.Point2d { x=w*0.75f,y=h*0.25f },
            new Blazor.ImageSurveyor.ImageSurveyorMeasureData.Point2d { x=w*0.75f,y=h*0.75f },
          };
          el.MeasureData.measurePoints[0]=new Blazor.ImageSurveyor.ImageSurveyorMeasureData.Point2d { x=w*0.50f,y=h*0.25f };
          el.MeasureData.measurePoints[1]=new Blazor.ImageSurveyor.ImageSurveyorMeasureData.Point2d { x=w*0.50f,y=h*0.75f };
        }
        this.disableSetImage=false;
        await this.LoadElementToMeasure(el);
      }
    }
    private async Task DeleteElement(Element el) {
      DS.DeleteElement(SD,el);
      DS.AddLogEntry(SD,"Deleted element "+el.ElementName);
      await RefreshData();
      await this.InvokeAsync(()=>StateHasChanged());
    }
  }
}