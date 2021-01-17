using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace BioMap
{
  [JsonObject(MemberSerialization.Fields)]
  public class Project
  {
    public string Owner;
    public DateTime? StartDate;
    public int MaxAllowedElements=20;
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
    //
    public readonly List<Species> Species = new List<Species>();
    public Species GetSpecies(string sSciName) {
      return this.Species.Find((s)=>string.CompareOrdinal(s.SciName,sSciName)==0);
    }
    public void InitSpeciesByGroupForYellowBelliedToad() {
      this.Species.Clear();
      {
        this.Species.AddRange(new[] {
          new Species { SciName="Anura",Name_de="Froschlurche",Name_en="Frogs" },
          new Species { SciName="Bombina variegata",Name_de="Gelbbauchunke",Name_en="Yellow-bellied toad" },
          new Species { SciName="Bombina bombina",Name_de="Rotbauchunke",Name_en="Fire-bellied toad" },
          new Species { SciName="Bufo bufo",Name_de="Erdkröte",Name_en="Common toad" },
          new Species { SciName="Rana temporaria",Name_de="Grasfrosch",Name_en="Grass frog" },
          new Species { SciName="Pelophylax",Name_de="Grünfrosch",Name_en="Green frog" },
          new Species { SciName="Caudata",Name_de="Schwanzlurche",Name_en="Caudate amphibians" },
          new Species { SciName="Salamandra salamandra",Name_de="Feuersalamander",Name_en="Fire salamender" },
          new Species { SciName="Lissotriton vulgaris",Name_de="Teichmolch",Name_en="Pond newt" },
          new Species { SciName="Ichthyosaura alpestris",Name_de="Bergmolch",Name_en="Alpine newt" },
          new Species { SciName="Insecta",Name_de="Insekten",Name_en="Insects" },
          new Species { SciName="Nepa cinerea",Name_de="Wasserskorpion",Name_en="Water scorpion" },
          new Species { SciName="Odonata",Name_de="Libellen",Name_en="Dragonflies" },
        });
      }
    }
  }
}
