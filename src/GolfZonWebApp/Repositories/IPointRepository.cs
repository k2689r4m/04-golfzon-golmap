using GolfZonWebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GolfZonWebApp.Repositories
{
    public interface IPointRepository
    {
        public List<PointObject> GetPointObjects();
        public IEnumerable<PointObject> GetPointObject(int? id, int? type);
        public PointObject AddPointObject(PointObject po);
        public PointObject UpdatePointObject(PointObject po);
        public List<PointHistory> GetPointHistories();
        public IEnumerable<PointHistory> GetPointHistory(int? id, int? type, int? userId);
        public PointHistory AddPointHostory(PointHistory ph);
        public PointHistory UpdatePointHistory(PointHistory ph);
    }
}