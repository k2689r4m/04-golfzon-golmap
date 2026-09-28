using GolfZonWebApp.Models;
using GolfZonWebApp.Scheduler.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GolfZonWebApp.Scheduler.Repositories
{
    public interface IManagerRepository
    {
        public Mileage AddMileage(Mileage data);
        public List<Mileage> GetAllMileages();
        public Mileage GetMileage(int userId);
        public Tag AddTag(Tag data);
        public Tag GetTag(string tagName);
        public Tag GetTag(int id);
        public ReviewTag AddReviewTag(ReviewTag data);
        public ReviewTag RelateTag(string tagName, int reviewId);
    }
}
