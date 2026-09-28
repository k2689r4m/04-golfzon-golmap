using System.Collections.Generic;
using NetTopologySuite.Geometries;
using System.Text.Json;
using System.IO;

namespace GolfZonWebApp.Types
{
    public class FeaturesMore
    {
        public string type { get; set; }
        public Geometry geometry { get; set; }
        public PropertiesMore properties { get; set; }
    }
}
