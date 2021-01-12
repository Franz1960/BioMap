using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SQLite;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

namespace BioMap
{
  public static class DataCalculator
  {
    private static DataService DS { get=>DataService.Instance; }
    public static async Task CalculatePlacesOfElements(SessionData sd,Action<int> callbackCompletion) {
      await Task.Run(()=>{
        var aElements = DS.GetElements(sd,null,"","elements.creationtime DESC");
        for (int idxElement=0;idxElement<aElements.Length;idxElement++) {
          if (callbackCompletion!=null) {
            callbackCompletion(((idxElement)*100)/aElements.Length);
          }
          var el=aElements[idxElement];
          el.ElementProp.MarkerInfo.PlaceName=Place.GetNearestPlace(sd,el.ElementProp.MarkerInfo.position)?.Name;
          DS.WriteElement(sd,el);
        }
      });
    }
  }
}
