using System.Collections.Generic;
using NetTopologySuite.Geometries;
using System.Text.Json;
using System.IO;
using System;

namespace GolfZonWebApp.Types
{
    public class WeatherNow
    {
        public WeatherMain Main { get; set; }
        public WeatherWind Wind { get; set; }
        public List<WeatherIf> Weather { get; set; }

        public class WeatherMain
        {
            public double Temp { get; set; }
        }

        public class WeatherWind
        {
            public double Speed { get; set; }
            public double Deg { get; set; }
        }

        public class WeatherIf
        {
            public int Id { get; set; }         //기상 조건 id
            public string Main { get; set; }         //날씨 매개변수 그룹(비, 눈, 극한 등)
            public string Description { get; set; }         //그룹 내 기상 조건.
            public string Icon { get; set; }            //날씨 아이콘 아이디
        }

    }
}
