using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

namespace BioMap
{
  public static class DataCalculator
  {
    public static async Task CalculatePlacesOfElements(SessionData sd, Action<int> callbackCompletion) {
      await Task.Run(() => {
        Element[] aElements = sd.DS.GetElements(sd, null, "", "elements.creationtime DESC");
        for (int idxElement = 0; idxElement < aElements.Length; idxElement++) {
          if (callbackCompletion != null) {
            callbackCompletion(((idxElement) * 100) / aElements.Length);
          }
          Element el = aElements[idxElement];
          string sPlaceName = Place.GetNearestPlace(sd, el.ElementProp.MarkerInfo.position)?.Name;
          if (!string.IsNullOrEmpty(sPlaceName)) {
            el.ElementProp.MarkerInfo.PlaceName = sPlaceName;
          } else {
            el.ElementProp.MarkerInfo.PlaceName = "";
          }
          sd.DS.WriteElement(sd, el);
        }
      });
    }
  }
  public static class SavePlacesAsGpxFile
  {
    public static async Task CalculatePlacesOfElements(SessionData sd, Action<int> callbackCompletion) {
      await Task.Run(() => {
        Element[] aElements = sd.DS.GetElements(sd, null, "", "elements.creationtime DESC");
        for (int idxElement = 0; idxElement < aElements.Length; idxElement++) {
          if (callbackCompletion != null) {
            callbackCompletion(((idxElement) * 100) / aElements.Length);
          }
          Element el = aElements[idxElement];
          string sPlaceName = Place.GetNearestPlace(sd, el.ElementProp.MarkerInfo.position)?.Name;
          if (!string.IsNullOrEmpty(sPlaceName)) {
            el.ElementProp.MarkerInfo.PlaceName = sPlaceName;
          }
          sd.DS.WriteElement(sd, el);
        }
      });
    }
  }
}
