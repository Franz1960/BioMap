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
    private async Task LoadElementToMeasure(Element el) {
      await this.imageSurveyor.SetImageUrlAsync("api/photos/"+elementToMeasure.ElementName+"?Project="+SD.CurrentUser.Project+"&ForceOrig=1",new Blazor.ImageSurveyor.ImageSurveyorMeasureData {
        method="HeadToCloakInPetriDish",
        normalizePoints=new[] {
          new { x=elementToMeasure.ElementProp.IndivData.MeasuredData.PtsOnCircle[0].X,y=elementToMeasure.ElementProp.IndivData.MeasuredData.PtsOnCircle[0].Y },
          new { x=elementToMeasure.ElementProp.IndivData.MeasuredData.PtsOnCircle[1].X,y=elementToMeasure.ElementProp.IndivData.MeasuredData.PtsOnCircle[1].Y },
          new { x=elementToMeasure.ElementProp.IndivData.MeasuredData.PtsOnCircle[2].X,y=elementToMeasure.ElementProp.IndivData.MeasuredData.PtsOnCircle[2].Y },
        },
        measurePoints=new[] {
          new {x=elementToMeasure.ElementProp.IndivData.MeasuredData.OrigHeadPosition.X,y=elementToMeasure.ElementProp.IndivData.MeasuredData.OrigHeadPosition.Y},
          new {x=elementToMeasure.ElementProp.IndivData.MeasuredData.OrigBackPosition.X,y=elementToMeasure.ElementProp.IndivData.MeasuredData.OrigBackPosition.Y},
        },
      });
    }
    private async Task ResetPositions_Clicked(Element el) {
      var sFilePath = DS.GetFilePathForImage(SD.CurrentUser.Project,el.ElementName,true);
      if (System.IO.File.Exists(sFilePath)) {
        using (var img = Image.Load(sFilePath)) {
          int w=img.Width;
          int h=img.Height;
          el.ElementProp.IndivData.MeasuredData.PtsOnCircle=new [] {
            new System.Numerics.Vector2(w*0.25f,h*0.50f),
            new System.Numerics.Vector2(w*0.75f,h*0.25f),
            new System.Numerics.Vector2(w*0.75f,h*0.75f),
          };
          el.ElementProp.IndivData.MeasuredData.OrigHeadPosition=new System.Numerics.Vector2(w*0.50f,h*0.25f);
          el.ElementProp.IndivData.MeasuredData.OrigBackPosition=new System.Numerics.Vector2(w*0.50f,h*0.75f);
        }
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