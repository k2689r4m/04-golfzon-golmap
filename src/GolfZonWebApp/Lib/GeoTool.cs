using GolfZonWebApp.Types;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GolfZonWebApp.Lib
{
    public class GeoTool
    {
        public static int GetCode(Point point, List<Features> features)
        {
            foreach (var item in features)
            {
                if (item.geometry.Contains(point))
                {
                    return item.properties.CODE;
                }
            }

            return -1;
        }

    }
}
