using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BioMap.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap.Pages.Workflow
{
  public partial class MonitoringCalendar : ComponentBase
  {
    public MonitoringCalendar() {
    }
    //
    protected override void OnInitialized() {
      base.OnInitialized();
      this.mt = new Monitoring(this.SD);
      this.mt.RefreshData();
    }
    //
    private Monitoring mt { get; set; }
  }
}
