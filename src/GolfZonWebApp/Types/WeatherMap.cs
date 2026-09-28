using System.Collections.Generic;
using NetTopologySuite.Geometries;
using System.Text.Json;
using System.IO;
using System;

namespace GolfZonWebApp.Types
{
    public class WeatherMap
    {
        public string Cod { get; set; }

        public List<WeatherItem> List { get; set; }


        public class WeatherItem
        {
            public long Dt { get; set; }        //예측 시간
            public WeatherTemp Temp { get; set; }
            public double Speed { get; set; }         //풍속 
            public double Deg { get; set; }         //풍향, 도(기상)
            public List<WeatherIf> Weather { get; set; }         
        }



        public class WeatherTemp
        {
            public double Min { get; set; }         //최소 일일 온도. 단위 기본값: 켈빈, 미터법: 섭씨, 영국식: 화씨.
            public double Max { get; set; }         //최대 일일 온도. 단위 기본값: 켈빈, 미터법: 섭씨, 영국식: 화씨.
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
