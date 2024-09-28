using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using BioMap.Shared;
using Blazorise;
using Geo.Gps.Serialization.Xml.Gpx.Gpx10;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap.Pages.Add
{
  public partial class AddGpxFile : ComponentBase
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
      int maxAllowedFiles = 1000;
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
            {
              string sDestFilePath = this.DS.GetFilePathForGpxFile(this.SD.CurrentUser.Project, imageFile.Name);
              string sDestPath = Path.GetDirectoryName(sDestFilePath);
              if (!Directory.Exists(sDestPath)) {
                Directory.CreateDirectory(sDestPath);
              }
              try {
                if (!this.DS.TryAddGpxFilename(this.SD, imageFile.Name)) {
                  this.messages.Add($"File '{imageFile.Name}' was already uploaded.");
                } else {
                  FSofTUtils.Geography.GpxFile gpxFile = GpxTool.Lib.ProcessGpxFiles(new string[] {
                    sDestTempFilePath,
                    $"--output={sDestFilePath}",
                    $"--simplifygpx",
                    $"--simplify=RW",
                  });
                  if (gpxFile != null) {
                    var project = this.SD.CurrentProject;
                    int nTrackCnt = gpxFile.TrackCount();
                    for (int iTrack = 0; iTrack < nTrackCnt; iTrack++) {
                      int nSegmentCnt = gpxFile.TrackSegmentCount(iTrack);
                      for (int iSegment = 0; iSegment < nSegmentCnt; iSegment++) {
                        int? nLatPrev = null;
                        int? nLonPrev = null;
                        DateTime? dtPrev = null;
                        var lSegmentPoints = gpxFile.GetTrackSegmentPointList(iTrack, iSegment);
                        for (int iPoint = 0; iPoint < lSegmentPoints.Count; iPoint++) {
                          GpxPoint gpxPoint = new GpxPoint() {
                            lat = (decimal)lSegmentPoints[iPoint].Lat,
                            lon = (decimal)lSegmentPoints[iPoint].Lon,
                            ele = (decimal)lSegmentPoints[iPoint].Elevation,
                            time = lSegmentPoints[iPoint].Time,
                          };
                          (int nLat, int nLon, int nTimeSlot) = project.GetStayPointFromGpxPoint(gpxPoint);
                          if (!dtPrev.HasValue) {
                            // No preceeding point.
                            dtPrev = gpxPoint.time;
                            nLatPrev = nLat;
                            nLonPrev = nLon;
                          } else if (nLat != nLatPrev || nLon != nLonPrev || iPoint == lSegmentPoints.Count - 1) {
                            // Location changed or last point.
                            TimeSpan ts = gpxPoint.time - dtPrev.Value;
                            if (ts.TotalSeconds > 0) {
                              if (this.SD.CurrentProject.IsLocationInsideAoi(new LatLng { lat = (double)gpxPoint.lat, lng = (double)gpxPoint.lon })) {
                                this.DS.AddLengthOfStay(this.SD, nLat, nLon, nTimeSlot, ts.TotalSeconds);
                              }
                              dtPrev = gpxPoint.time;
                              nLatPrev = nLat;
                              nLonPrev = nLon;
                            }
                          }
                        }
                      }
                    }
                    nUploadedFiles++;
                  } else {
                    this.messages.Add($"File '{imageFile.Name}' not processed; it was not a valid GPX or KML file.");
                  }
                  System.IO.File.Delete(sDestFilePath);
                }
              } catch (Exception ex) {
                this.messages.Add(ex.ToString());
              }
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
      } catch (Exception ex) {
        this.messages.Add(ex.ToString());
      } finally {
        if (this.messages.Count < 1) {
          await this.InvokeAsync(() => { this.progressModalRef.Hide(); this.StateHasChanged(); });
        }
      }
    }
    private async Task DeleteAllMyGpxFiles_Clicked() {
      try {
        await Task.Run(() => this.DS.DeleteAllMyGpxFiles(this.SD));
      } finally {
      }
    }
  }
}
