using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace BioMap
{
  [JsonObject(MemberSerialization.Fields)]
  public class Project
  {
    public double AoiCenterLat;
    public double AoiCenterLng;
    public double AoiMinLat;
    public double AoiMinLng;
    public double AoiMaxLat;
    public double AoiMaxLng;
    public double AoiTolerance;
    public string SpeciesSciName;
    //
    public bool IsLocationInsideAoi(BioMap.LatLng latLng) {
      double tolLat=(this.AoiMaxLat-this.AoiMinLat)*0.5*this.AoiTolerance;
      double tolLng=(this.AoiMaxLng-this.AoiMinLng)*0.5*this.AoiTolerance;
      if (latLng.lat<this.AoiMinLat-tolLat) {
      } else if (latLng.lat>this.AoiMaxLat+tolLat) {
      } else if (latLng.lng<this.AoiMinLng-tolLng) {
      } else if (latLng.lng>this.AoiMaxLng+tolLng) {
      } else {
        return true;
      }
      return false;
    }
  }
}
