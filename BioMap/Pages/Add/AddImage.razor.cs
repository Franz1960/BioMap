using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BioMap.Shared;
using Blazorise;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap.Pages.Add
{
  public partial class AddImage : ComponentBase
  {
    private Modal progressModalRef;
    private int progressCompletion = 0;
    private List<string> messages = new List<string>();
    //
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
        await this.JSRuntime.InvokeAsync<string>("UploadImageFiles_Init", "inputImageFiles", "uploadUI");
      }
    }
    private async void OnSelectClick() {
      string sResult = await this.JSRuntime.InvokeAsync<string>("UploadImageFiles_Click", "inputImageFiles");
    }
    //
    private readonly IList<string> imageDataUrls = new List<string>();
    //
    private async Task OnInputFileChange(InputFileChangeEventArgs e) {
      int maxAllowedFiles = 30;
      int nUploadedFiles = 0;
      this.messages.Clear();
      this.progressCompletion = 0;
      await this.progressModalRef.Show();
      try {
        IReadOnlyList<IBrowserFile> files = e.GetMultipleFiles(maxAllowedFiles);
        for (int idxFile = 0; idxFile < files.Count; idxFile++) {
          IBrowserFile imageFile = files[idxFile];
          this.progressCompletion = (int)Math.Round(((idxFile + 0.5) * 100) / files.Count);
          await this.InvokeAsync(() => this.StateHasChanged());
          try {
            Stream imageStream = imageFile.OpenReadStream(20000000);
            string sDestTempFilePath = System.IO.Path.Combine(this.DS.GetTempDir(this.SD.CurrentUser.Project), imageFile.Name);
            using (var destStream = new System.IO.FileStream(sDestTempFilePath, System.IO.FileMode.Create)) {
              await imageStream.CopyToAsync(destStream);
              destStream.Close();
            }
            var el = Element.CreateFromImageFile(this.SD.CurrentUser.Project, sDestTempFilePath, this.SD);
            if (el.ElementProp.CreationTime < DateTime.Parse("1971-01-01 00:00:00")) {
              this.messages.Add("Date and time in image is not valid: " + el.ElementProp.CreationTime.ToString());
            } else if (!this.SD.CurrentUser.MayUploadElements) {
              this.messages.Add("User access level (" + this.SD.CurrentUser.Level + ") is too low.");
            } else if (this.DS.GetElementCount(this.SD) >= this.SD.CurrentProject.MaxAllowedElements) {
              this.messages.Add("Maximum element count (" + this.SD.CurrentProject.MaxAllowedElements + ") is exceeded. Contact the website administrator.");
            } else {
              var sbLog = new System.Text.StringBuilder("Uploaded file " + el.ElementName);
              if (el.ElementProp.MarkerInfo.position == null) {
                this.messages.Add("No GPS location in \"" + el.ElementName + "\"");
                sbLog.Append(" without GPS location.");
                el.ElementProp.MarkerInfo.position = new LatLng {
                  lat = 0,
                  lng = 0,
                };
                el.ElementProp.MarkerInfo.PlaceName = "";
              } else if (!this.SD.CurrentProject.IsLocationInsideAoi(el.ElementProp.MarkerInfo.position)) {
                this.messages.Add("GPS location is outside project area \"" + el.ElementName + "\"");
                sbLog.Append(" with GPS location outside project area.");
              }
              this.DS.WriteElement(this.SD, el);
              string sDestFilePath = this.DS.GetFilePathForImage(this.SD.CurrentUser.Project, imageFile.Name, true);
              Migration.CopyImageCompressed(sDestTempFilePath, sDestFilePath, this.SD.CurrentProject.SaveGpsDataInOriginalImages, 2000);
              this.DS.AddLogEntry(this.SD, sbLog.ToString());
              nUploadedFiles++;
            }
            System.IO.File.Delete(sDestTempFilePath);
          } catch (Exception ex) {
            this.messages.Add(ex.ToString());
            await this.InvokeAsync(() => this.StateHasChanged());
          }
          this.progressCompletion = (int)Math.Round(((idxFile + 1.0) * 100) / files.Count);
          await this.InvokeAsync(() => this.StateHasChanged());
        }
        this.messages.Add(string.Format(this.Localize["{0} files uploaded."], nUploadedFiles.ToString()));
        await this.InvokeAsync(() => this.StateHasChanged());
      } finally {
        if (this.messages.Count < 1) {
          await this.InvokeAsync(() => { this.progressModalRef.Hide(); this.StateHasChanged(); });
        }
      }
    }
  }
}
