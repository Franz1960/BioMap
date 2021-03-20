using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blazor.ImageSurveyor
{
    /// <summary>
    /// Method definition to normalize a photo.
    /// </summary>
    [JsonObject(MemberSerialization.Fields)]
    public class ImageSurveyorNormalizer
    {
        public string NormalizeMethod = "HeadToCloakInPetriDish";
        public double NormalizeReference = 100;
        public double NormalizePixelSize = 0.10;
        public int NormalizedWidthPx = 600;
        public int NormalizedHeightPx = 600;
        //
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
        public static ImageSurveyorNormalizer FromJson(string sJson)
        {
            return JsonConvert.DeserializeObject<ImageSurveyorNormalizer>(sJson);
        }
        //
        public IEnumerable<System.Numerics.Vector2> GetDefaultNormalizePoints(int nImageWidth, int nImageHeight)
        {
            int w = nImageWidth;
            int h = nImageHeight;
            if (string.CompareOrdinal(this.NormalizeMethod, "HeadToCloakInPetriDish") == 0)
            {
                return new[] {
          new System.Numerics.Vector2 { X=w*0.25f,Y=h*0.50f },
          new System.Numerics.Vector2 { X=w*0.75f,Y=h*0.25f },
          new System.Numerics.Vector2 { X=w*0.75f,Y=h*0.75f },
        };
            }
            else if (string.CompareOrdinal(this.NormalizeMethod, "CropRectangle") == 0)
            {
                return new[] {
          new System.Numerics.Vector2 { X=w*0.00f,Y=h*0.00f },
          new System.Numerics.Vector2 { X=w*1.00f,Y=h*1.00f },
        };
            }
            return Array.Empty<System.Numerics.Vector2>();
        }
        public IEnumerable<System.Numerics.Vector2> GetDefaultMeasurePoints(int nImageWidth, int nImageHeight)
        {
            int w = nImageWidth;
            int h = nImageHeight;
            if (string.CompareOrdinal(this.NormalizeMethod, "HeadToCloakInPetriDish") == 0)
            {
                return new[] {
          new System.Numerics.Vector2 { X=w*0.50f,Y=h*0.25f },
          new System.Numerics.Vector2 { X=w*0.50f,Y=h*0.75f },
          new System.Numerics.Vector2 { X=w*0.50f,Y=h*0.25f },
          new System.Numerics.Vector2 { X=w*0.50f,Y=h*0.75f },
        };
            }
            else if (string.CompareOrdinal(this.NormalizeMethod, "CropRectangle") == 0)
            {
            }
            return Array.Empty<System.Numerics.Vector2>();
        }
    }
}
