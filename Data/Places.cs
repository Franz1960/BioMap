using Newtonsoft.Json;

namespace BioMap
{
  public class Places
  {
    public class Trait
    {
      public string Name;
      public string Description;
      public string[] ValueNames;
    }
    public static Trait[] Traits = new Trait[] {
      new Trait {
        Name="Größe",
        Description="Gesamtgröße aller Gewässer",
        ValueNames=new [] {
          "",
          "Pfütze klein (< 1 m²)",
          "Pfütze mittel (1-2 m²)",
          "Pfütze groß (> 2 m²)",
          "Tümpel",
          "Pfütze durchflossen",
        },
      },
      new Trait {
        Name="Boden",
        Description="Beschaffenheit des Untergrunds",
        ValueNames=new [] {
          "",
          "felsig",
          "humos",
          "schlammig",
        },
      },
      new Trait {
        Name="Bewuchs",
        Description="Bewuchs in und um die Gewässer",
        ValueNames=new [] {
          "",
          "frei",
          "einseitig bewachsen",
          "beidseitig bewachsen",
          "außen und innen",
        },
      },
      new Trait {
        Name="Besonnung",
        Description="Sonneneinstrahlung über den ganzen Tag gerechnet",
        ValueNames=new [] {
          "",
          "dauerhaft besonnt",
          "südexponiert",
          "Fahrweg oder Schneise",
          "schattig",
        },
      },
      new Trait {
        Name="Gliederung",
        Description="Verteilung der Gewässer",
        ValueNames=new [] {
          "",
          "1 Gewässer",
          "2-3 Gewässer",
          "4-5 Gewässer, homogen",
          "5+ Gewässer homogen",
          "4-5 Gewässer heterogen",
          "5+ Gewässer heterogen",
        },
      },
      new Trait {
        Name="Störung Habitat",
        Description="Gravierende Änderungen Habitat im Verlauf der Saison oder über zwei Saisonen",
        ValueNames=new [] {
          "",
          "Störung während Saison (Harvester)",
          "Austrocknung während Saison",
          "Wesentliche Änderung von einer zur nächsten Saison",
        },
      },
    };
  }
}
