using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SQLite;
using Newtonsoft.Json;

namespace BioMap
{
    public static class WhereClauses
    {
        public static readonly string Is_ID_photo = "(elements.classification LIKE '%\"ClassName\":\"ID photo\"%')";
        public static readonly string Is_Individuum = Is_ID_photo + " AND indivdata.iid>=1";
        public static string Is_FromPlace(string sPlaceName)
        {
            return "elements.place='" + sPlaceName + "'";
        }
    }
}
