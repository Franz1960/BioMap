using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Blazor.ImageSurveyor;
using Newtonsoft.Json;

namespace BioMap
{
  [JsonObject(MemberSerialization.Fields)]
  public class Project
  {
    public string Owner;
    public DateTime? StartDate;
    public int MaxAllowedElements = 20;
    public double AoiCenterLat;
    public double AoiCenterLng;
    public double AoiMinLat;
    public double AoiMinLng;
    public double AoiMaxLat;
    public double AoiMaxLng;
    public double AoiTolerance;
    public string SpeciesSciName;
    public int MinLevelToSeeElements = 200;
    public int MinLevelToSeeExactLocations = 400;
    //
    public bool IsLocationInsideAoi(BioMap.LatLng latLng) {
      double tolLat = (this.AoiMaxLat - this.AoiMinLat) * 0.5 * this.AoiTolerance;
      double tolLng = (this.AoiMaxLng - this.AoiMinLng) * 0.5 * this.AoiTolerance;
      if (latLng.lat < this.AoiMinLat - tolLat) {
      } else if (latLng.lat > this.AoiMaxLat + tolLat) {
      } else if (latLng.lng < this.AoiMinLng - tolLng) {
      } else if (latLng.lng > this.AoiMaxLng + tolLng) {
      } else {
        return true;
      }
      return false;
    }
    //
    public readonly TaxaTree TaxaTree = new TaxaTree();
    public Taxon GetTaxon(string sSciName) {
      return this.TaxaTree.RootNode.FindFirst(sSciName)?.Data as Taxon;
    }
    public ImageSurveyorNormalizer ImageNormalizer { get; set; } = new ImageSurveyorNormalizer("HeadToCloakInPetriDish");
    public bool MaleGenderFeatures { get; set; }
    public bool FemaleGenderFeatures { get; set; }
    public double AdultMinLength { get; set; }
    public double MinHeadBodyLength { get; set; }
    public double MaxHeadBodyLength { get; set; }
    public void InitTaxaForYellowBelliedToad() {
      this.TaxaTree.FromTaxaList(new[] {
        new Taxon { ParentSciNames="",SciName="Amphibia",Name_de="Amphibien",Name_en="Amphibia" },
        new Taxon { ParentSciNames="Amphibia",SciName="Anura",Name_de="Froschlurche",Name_en="Frogs" },
        new Taxon { ParentSciNames="Anura",SciName="Bombinatoridae",Name_de="Unken und Barbourfrösche",Name_en="Frogs" },
        new Taxon { ParentSciNames="Bombinatoridae",SciName="Bombina variegata",Name_de="Gelbbauchunke",Name_en="Yellow-bellied toad" },
        new Taxon { ParentSciNames="Bombinatoridae",SciName="Bombina bombina",Name_de="Rotbauchunke",Name_en="Fire-bellied toad" },
        new Taxon { ParentSciNames="Anura",SciName="Bufonidae",Name_de="Kröten",Name_en="Toads" },
        new Taxon { ParentSciNames="Bufonidae",SciName="Bufo",Name_de="Echte Kröten",Name_en="True toads" },
        new Taxon { ParentSciNames="Bufo",SciName="Bufo bufo",Name_de="Erdkröte",Name_en="Common toad" },
        new Taxon { SciName="Rana temporaria",Name_de="Grasfrosch",Name_en="Grass frog" },
        new Taxon { SciName="Pelophylax",Name_de="Grünfrosch",Name_en="Green frog" },
        new Taxon { SciName="Caudata",Name_de="Schwanzlurche",Name_en="Caudate amphibians" },
        new Taxon { SciName="Salamandra salamandra",Name_de="Feuersalamander",Name_en="Fire salamender" },
        new Taxon { SciName="Lissotriton vulgaris",Name_de="Teichmolch",Name_en="Pond newt" },
        new Taxon { SciName="Ichthyosaura alpestris",Name_de="Bergmolch",Name_en="Alpine newt" },
        new Taxon { SciName="Triturus cristatus",Name_de="Kammmolch",Name_en="Northern crested newt" },
        new Taxon { SciName="Natrix natrix",Name_de="Ringelnatter",Name_en="Grass snake" },
        new Taxon { SciName="Oncorhynchus mykiss",Name_de="Regenbogenforelle",Name_en="Rainbow trout" },
        new Taxon { SciName="Ciconia nigra",Name_de="Schwarzstorch",Name_en="Black stork" },
        new Taxon { SciName="Insecta",Name_de="Insekten",Name_en="Insects" },
        new Taxon { SciName="Nepa cinerea",Name_de="Wasserskorpion",Name_en="Water scorpion" },
        new Taxon { SciName="Odonata",Name_de="Libellen",Name_en="Dragonflies" },
      });
    }
  }
}
