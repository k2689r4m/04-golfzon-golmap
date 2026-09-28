using GolfZonWebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GolfZonWebApp.Repositories
{
    public class MockPointRepository : IPointRepository
    {
        private List<PointObject> PointObjects;
        private List<PointHistory> PointHistories;

        public MockPointRepository()
        {
            PointObjects = new List<PointObject>();
            PointHistories = new List<PointHistory>();
        }
        public List<PointObject> GetPointObjects()
        {
            return PointObjects;
        }

        public IEnumerable<PointObject> GetPointObject(int? id, int? type)
        {
            var pointObject = from po in PointObjects
                              where id == null ? true : po.Id == id
                              where type == null ? true : po.Type == type
                              select po;

            return pointObject;
        }
        public PointObject AddPointObject(PointObject po)
        {
            if (PointObjects.Count > 0)
            {
                po.Id = PointObjects.Max(p => p.Id);
            }
            else
            {
                po.Id = 1;
            }

            PointObjects.Add(po);

            return po;
        }

        public PointObject UpdatePointObject(PointObject po)
        {
            for (int i = 0;i < PointObjects.Count(); i++)
            {
                if (PointObjects.ElementAt(i).Id == po.Id)
                {
                    PointObjects.ElementAt(i).Accumulate = po.Accumulate;
                    PointObjects.ElementAt(i).Type = po.Type;
                    PointObjects.ElementAt(i).Point = po.Point;
                    PointObjects.ElementAt(i).StartDate = po.StartDate;
                    PointObjects.ElementAt(i).EndDate = po.EndDate;
                    PointObjects.ElementAt(i).State = po.State;

                    return po;
                }
            }

            return null;
        }

        public List<PointHistory> GetPointHistories()
        {
            return PointHistories;
        }

        public IEnumerable<PointHistory> GetPointHistory(int? id, int? type, int? userId)
        {
            var pointHistory = from oh in PointHistories
                               where id == null ? true : oh.Id == id
                               where type == null ? true : oh.Type == type
                               where userId == null ? true : oh.UserId == type
                               select oh;

            return pointHistory;
        }
        public PointHistory AddPointHostory(PointHistory ph)
        {
            if (PointHistories.Count > 0)
            {
                ph.Id = PointHistories.Max(p => p.Id);
            }
            else
            {
                ph.Id = 1;
            }

            PointHistories.Add(ph);

            return ph;
        }

        public PointHistory UpdatePointHistory(PointHistory ph)
        {
            for (int i = 0; i < PointHistories.Count(); i++)
            {
                if (PointHistories.ElementAt(i).Id == ph.Id)
                {
                    PointHistories.ElementAt(i).UserId = ph.UserId;
                    PointHistories.ElementAt(i).Accumulate = ph.Accumulate;
                    PointHistories.ElementAt(i).Type = ph.Type;
                    PointHistories.ElementAt(i).Point = ph.Point;

                    return ph;
                }
            }

            return null;
        }
    }
}
