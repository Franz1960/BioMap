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
    public readonly List<Species> Species = new List<Species>();
    public readonly SpeciesTree SpeciesTree = new SpeciesTree();
    public Species GetSpecies(string sSciName) {
      return this.Species.Find((s) => string.CompareOrdinal(s.SciName, sSciName) == 0);
    }
    public ImageSurveyorNormalizer ImageNormalizer { get; set; } = new ImageSurveyorNormalizer("HeadToCloakInPetriDish");
    public bool MaleGenderFeatures { get; set; }
    public bool FemaleGenderFeatures { get; set; }
    public double AdultMinLength { get; set; }
    public double MinHeadBodyLength { get; set; }
    public double MaxHeadBodyLength { get; set; }
    public void InitSpeciesByGroupForYellowBelliedToad() {
      this.Species.Clear();
      {
        this.Species.AddRange(new[] {
                    new Species { ParentSciName="",SciName="Bilateria",Name_de="Bilateria",Name_en="Bilateria" },
                    new Species { ParentSciName="Bilateria",SciName="Deuterostomia",Name_de="Neumünder",Name_en="Deuterostome" },
                    new Species { ParentSciName="Deuterostomia",SciName="Chordata",Name_de="Chordatiere",Name_en="Chordate" },
                    new Species { ParentSciName="Chordata",SciName="Vertebrata",Name_de="Wirbeltiere",Name_en="Vertebrate" },
                    new Species { ParentSciName="Vertebrata",SciName="Gnathostomata",Name_de="Kiefermäuler",Name_en="Gnathostomata" },
                    new Species { ParentSciName="Gnathostomata",SciName="Tetrapoda",Name_de="Landwirbeltiere",Name_en="Tetrapod" },
                    new Species { ParentSciName="Tetrapoda",SciName="Amphibia",Name_de="Amphibien",Name_en="Amphibia" },
                    new Species { ParentSciName="Amphibia",SciName="Anura",Name_de="Froschlurche",Name_en="Frogs" },
                    new Species { ParentSciName="Anura",SciName="Bombinatoridae",Name_de="Unken und Barbourfrösche",Name_en="Frogs" },
                    new Species { ParentSciName="Bombinatoridae",SciName="Bombina variegata",Name_de="Gelbbauchunke",Name_en="Yellow-bellied toad" },
                    new Species { ParentSciName="Bombinatoridae",SciName="Bombina bombina",Name_de="Rotbauchunke",Name_en="Fire-bellied toad" },
                    new Species { ParentSciName="Anura",SciName="Bufonidae",Name_de="Kröten",Name_en="Toads" },
                    new Species { ParentSciName="Bufonidae",SciName="Bufo",Name_de="Echte Kröten",Name_en="True toads" },
                    new Species { ParentSciName="Bufo",SciName="Bufo bufo",Name_de="Erdkröte",Name_en="Common toad" },
                    new Species { SciName="Rana temporaria",Name_de="Grasfrosch",Name_en="Grass frog" },
                    new Species { SciName="Pelophylax",Name_de="Grünfrosch",Name_en="Green frog" },
                    new Species { SciName="Caudata",Name_de="Schwanzlurche",Name_en="Caudate amphibians" },
                    new Species { SciName="Salamandra salamandra",Name_de="Feuersalamander",Name_en="Fire salamender" },
                    new Species { SciName="Lissotriton vulgaris",Name_de="Teichmolch",Name_en="Pond newt" },
                    new Species { SciName="Ichthyosaura alpestris",Name_de="Bergmolch",Name_en="Alpine newt" },
                    new Species { SciName="Triturus cristatus",Name_de="Kammmolch",Name_en="Northern crested newt" },
                    new Species { SciName="Natrix natrix",Name_de="Ringelnatter",Name_en="Grass snake" },
                    new Species { SciName="Oncorhynchus mykiss",Name_de="Regenbogenforelle",Name_en="Rainbow trout" },
                    new Species { SciName="Ciconia nigra",Name_de="Schwarzstorch",Name_en="Black stork" },
                    new Species { SciName="Insecta",Name_de="Insekten",Name_en="Insects" },
                    new Species { SciName="Nepa cinerea",Name_de="Wasserskorpion",Name_en="Water scorpion" },
                    new Species { SciName="Odonata",Name_de="Libellen",Name_en="Dragonflies" },
                });
      }
    }
  }
}
