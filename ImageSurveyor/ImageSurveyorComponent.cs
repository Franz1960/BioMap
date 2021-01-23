using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

#nullable enable

namespace Blazor.ImageSurveyor
{
  public class ImageSurveyorComponent : ComponentBase, IDisposable
  {
    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public ImageSurveyorOptions? Options { get; set; }

    [Parameter]
    public EventCallback AfterRenderEvent { get; set; }

    [Parameter]
    public string? CssClass { get; set; }

    private string _height = "500px";

    /// <summary>
    /// Default height 500px
    /// Used as style atribute "height: {Height}"
    /// </summary>
    [Parameter]
    public string Height {
      get => _height;
      set => _height = value ?? "500px";
    }

    protected string StyleStr => $"height: {Height};";

    protected ElementReference divMain { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender) {
      if (firstRender) {
        await InitAsync(Options);
      }
      await AfterRenderEvent.InvokeAsync(null);
    }

    [Inject]
    public IJSRuntime JsRuntime { get; protected set; } = default!;

    public async Task InitAsync(ImageSurveyorOptions? options = null) {
      await this.JsRuntime.InvokeVoidAsync("PrepPic.PrepareDisplay",this.divMain);
    }
    public string ImageUrl { get; private set; }="";
    public string MeasureDataJson { get; private set; }="";
    public async Task SetImageUrlAsync(string sImageUrl,ImageSurveyorMeasureData measureData) {
      var sMeasureDataJson=JsonConvert.SerializeObject(measureData);
      if (string.CompareOrdinal(sImageUrl,this.ImageUrl)!=0 || string.CompareOrdinal(sMeasureDataJson,this.MeasureDataJson)!=0) {
        this.ImageUrl=sImageUrl;
        this.MeasureDataJson=sMeasureDataJson;
        await this.JsRuntime.InvokeVoidAsync("PrepPic.setImage",this.ImageUrl,measureData);
      }
    }

    public void Dispose() {
    }
  }
}
