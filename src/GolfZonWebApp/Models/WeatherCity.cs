using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfZonWebApp.Models
{
    public class WeatherCity : BaseEntity
    {
        public int Id { get; set; }
        public int CityCode { get; set; }
        public DateTime Dt { get; set; }        //예측 시간
        public double? Now { get; set; }         //현재 온도
        public double Min { get; set; }         //최소 일일 온도. 단위 기본값: 켈빈, 미터법: 섭씨, 영국식: 화씨.
        public double Max { get; set; }         //최대 일일 온도. 단위 기본값: 켈빈, 미터법: 섭씨, 영국식: 화씨.
        //단위 기본값: 미터/초, 미터법: 미터/초, 영국식: 마일/시간.
        public double Speed { get; set; }         //풍속 
        public double Deg { get; set; }         //풍향, 도(기상)
        public int WeatherId { get; set; }         //기상 조건 id
        public string Main { get; set; }         //날씨 매개변수 그룹(비, 눈, 극한 등)
        public string Description { get; set; }         //그룹 내 기상 조건.
        public string Icon { get; set; }            //날씨 아이콘 아이디


    }
}