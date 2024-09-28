using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BioMap.Shared;
using GoogleMapsComponents;
using GoogleMapsComponents.Maps;
using GoogleMapsComponents.Maps.Extension;
using GoogleMapsComponents.Maps.Visualization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap.Shared
{
  public partial class TimeIntervalSlider : ComponentBase
  {
    [Inject]
    protected DataService DS { get; set; }
    [Inject]
    protected SessionData SD { get; set; }
    /// <summary>
    /// Hide time interval selection; only slider and play button will be displayed.
    /// </summary>
    [Parameter]
    public bool HideIntervalSelection { get; set; }
    /// <summary>
    /// Delay between value changes when playing in milliseconds.
    /// </summary>
    [Parameter]
    public int PlayDelayMs { get; set; } = 100;
    /// <summary>
    /// Fired if anything changed.
    /// </summary>
    [Parameter]
    public EventCallback<EventArgs> AnyChanged { get; set; }
    /// <summary>
    /// Stop playing.
    /// </summary>
    public void StopPlaying() {
      this.RequestStopPlaying = true;
    }
    private bool RequestStopPlaying = false;
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
      if (this.SD?.CurrentProject?.StartDate != null) {
        this.MinWeek = 0;
        this.MaxWeek = (DateTime.Now - this.SD.CurrentProject.StartDate.Value).Days / 7;
        int nMinYear = this.SD.CurrentProject.StartDate.Value.Year;
        int nMaxYear = DateTime.Now.Year;
        for (int nYear = nMinYear; nYear <= nMaxYear; nYear++) {
          int nWeek = (new DateTime(nYear, 7, 1) - this.SD.CurrentProject.StartDate.Value).Days / 7;
          this.YearByWeek.Add(nWeek, nYear);
        }
      }
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
      }
    }
    private Blazorise.Slider<int> IntervalSlider;
    private async Task PlayIntervalSlider() {
      //await Task.Run(this.Play);
      //return;
      int nStartWeek = (this.Week == this.MaxWeek) ? this.MinWeek : this.Week;
      this.RequestStopPlaying = false;
      if (this.HideIntervalSelection) {
        {
          int nStep = 1;
          for (int nWeek = nStartWeek; nWeek <= this.MaxWeek; nWeek += nStep) {
            if (this.RequestStopPlaying) {
              break;
            }
            this.Week = nWeek;
            this.StateHasChanged();
            await Task.Delay(this.PlayDelayMs);
          }
        }
      } else {
        if (this.TimeIntervalWeeks >= 2) {
          int nStep = Math.Max(1, this.TimeIntervalWeeks / 12);
          for (int nWeek = nStartWeek; nWeek <= this.MaxWeek; nWeek += nStep) {
            if (this.RequestStopPlaying) {
              break;
            }
            this.Week = nWeek;
            this.StateHasChanged();
            await Task.Delay(this.PlayDelayMs);
          }
        }
      }
    }
    /// <summary>
    /// Time interval size in weeks.
    /// </summary>
    public int TimeIntervalWeeks {
      get => this.SD.CurrentUser.Prefs.TimeIntervalWeeks;
      private set {
        this.SD.CurrentUser.Prefs.TimeIntervalWeeks = value;
        this.DS.WriteUser(this.SD, this.SD.CurrentUser);
        this.StateHasChanged();
        Task.Run(async () => await this.AnyChanged.InvokeAsync(EventArgs.Empty));
      }
    }
    /// <summary>
    /// The currently selected week, starting with 0 at project start.
    /// </summary>
    public int Week {
      get => this._Week;
      set {
        if (value != this._Week) {
          this._Week = value;
          Task.Run(async () => await this.AnyChanged.InvokeAsync(EventArgs.Empty));
        }
      }
    }
    private int _Week;
    public int MinWeek { get; private set; }
    public int MaxWeek { get; private set; }
    private readonly Dictionary<int, int> YearByWeek = new Dictionary<int, int>();
    private void Play() {
      int nStep = Math.Max(1, this.HideIntervalSelection ? 1 : (this.TimeIntervalWeeks / 12));
      int nWeek = (this.Week == this.MaxWeek) ? this.MinWeek : this.Week;
      this.RequestStopPlaying = false;
      while (true) {
        if (this.RequestStopPlaying) {
          break;
        } else {
          this._Week = nWeek;
          this.StateHasChanged();
          this.AnyChanged.InvokeAsync(EventArgs.Empty).Wait();
          Task.Delay(this.PlayDelayMs);
          //
          nWeek += nStep;
          if (nWeek > this.MaxWeek) {
            this.RequestStopPlaying = true;
          }
        }
      }
    }
  }
}
