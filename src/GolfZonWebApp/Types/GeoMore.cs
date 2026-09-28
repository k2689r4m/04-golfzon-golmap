using System.Collections.Generic;
using NetTopologySuite.Geometries;
using System.Text.Json;
using System.IO;

namespace GolfZonWebApp.Types
{
    public class GeoMore
    {
        //public Features()
        //{
        //    Geo.Add(new Types.Geo { 
        //        Data = null,
        //        Info = new FeatureInfo { CTPRVN_CD = "111",  CTP_ENG_NM ="222", CTP_KOR_NM ="333"}
        //    });
        //}
        public List<FeaturesMore> features { get; set; }
    }
}
