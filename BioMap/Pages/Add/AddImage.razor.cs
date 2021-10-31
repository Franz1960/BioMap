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
        await JSRuntime.InvokeAsync<string>("UploadImageFiles_Init", "inputImageFiles", "uploadUI");
      }
    }
    private async void OnSelectClick() {
      var sResult = await JSRuntime.InvokeAsync<string>("UploadImageFiles_Click", "inputImageFiles");
    }
    //
    private IList<string> imageDataUrls = new List<string>();
    //
    private async Task OnInputFileChange(InputFileChangeEventArgs e) {
      var maxAllowedFiles = 30;
      int nUploadedFiles = 0;
      messages.Clear();
      progressCompletion = 0;
      progressModalRef.Show();
      try {
        var files = e.GetMultipleFiles(maxAllowedFiles);
        for (int idxFile = 0; idxFile < files.Count; idxFile++) {
          var imageFile = files[idxFile];
          progressCompletion = (int)Math.Round(((idxFile + 0.5) * 100) / files.Count);
          await this.InvokeAsync(() => { this.StateHasChanged(); });
          try {
            var imageStream = imageFile.OpenReadStream(20000000);
            var sDestTempFilePath = System.IO.Path.Combine(DS.GetTempDir(SD.CurrentUser.Project), imageFile.Name);
            using (var destStream = new System.IO.FileStream(sDestTempFilePath, System.IO.FileMode.Create)) {
              await imageStream.CopyToAsync(destStream);
              destStream.Close();
            }
            var el = Element.CreateFromImageFile(SD.CurrentUser.Project, sDestTempFilePath, SD);
            if (el.ElementProp.CreationTime < DateTime.Parse("1971-01-01 00:00:00")) {
              messages.Add("Date and time in image is not valid: " + el.ElementProp.CreationTime.ToString());
            } else if (!SD.CurrentUser.MayUploadElements) {
              messages.Add("User access level (" + SD.CurrentUser.Level + ") is too low.");
            } else if (DS.GetElementCount(SD) >= SD.CurrentProject.MaxAllowedElements) {
              messages.Add("Maximum element count (" + SD.CurrentProject.MaxAllowedElements + ") is exceeded. Contact the website administrator.");
            } else {
              var sbLog = new System.Text.StringBuilder("Uploaded file " + el.ElementName);
              if (el.ElementProp.MarkerInfo.position == null) {
                messages.Add("No GPS location in \"" + el.ElementName + "\"");
                sbLog.Append(" without GPS location.");
                el.ElementProp.MarkerInfo.position = new LatLng {
                  lat = 0,
                  lng = 0,
                };
                el.ElementProp.MarkerInfo.PlaceName = "";
              } else if (!SD.CurrentProject.IsLocationInsideAoi(el.ElementProp.MarkerInfo.position)) {
                messages.Add("GPS location is outside project area \"" + el.ElementName + "\"");
                sbLog.Append(" with GPS location outside project area.");
              }
              DS.WriteElement(SD, el);
              var sDestFilePath = DS.GetFilePathForImage(SD.CurrentUser.Project, imageFile.Name, true);
              Migration.CopyImageCompressed(sDestTempFilePath, sDestFilePath, 2000);
              DS.AddLogEntry(SD, sbLog.ToString());
              nUploadedFiles++;
            }
            System.IO.File.Delete(sDestTempFilePath);
          } catch (Exception ex) {
            messages.Add(ex.ToString());
            await this.InvokeAsync(() => { this.StateHasChanged(); });
          }
          progressCompletion = (int)Math.Round(((idxFile + 1.0) * 100) / files.Count);
          await this.InvokeAsync(() => { this.StateHasChanged(); });
        }
        messages.Add(string.Format(Localize["{0} files uploaded."], nUploadedFiles.ToString()));
        await this.InvokeAsync(() => { this.StateHasChanged(); });
      } finally {
        if (messages.Count < 1) {
          await this.InvokeAsync(() => { progressModalRef.Hide(); this.StateHasChanged(); });
        }
      }
    }
  }
}
