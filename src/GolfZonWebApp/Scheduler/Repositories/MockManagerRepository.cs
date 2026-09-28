using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GolfZonWebApp.Models;
using GolfZonWebApp.Scheduler.Managers;

namespace GolfZonWebApp.Scheduler.Repositories
{
    public class MockManagerRepository : IManagerRepository
    {
        private List<Mileage> _mileages;
        private List<Tag> _tags;
        private List<ReviewTag> _reviewTags;

        public MockManagerRepository()
        {
            _mileages = new List<Mileage>();
            _tags = new List<Tag>();
            _reviewTags = new List<ReviewTag>();
        }

        public Mileage AddMileage(Mileage data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data", "data cannot be null");
            }

            if (_mileages.Count == 0)
            {
                data.Id = 1;
            }
            else
            {
                data.Id = _mileages.Max(m => m.Id) + 1;
            }

            var utcNow = DateTime.UtcNow.AddHours(9);
            data.CreatedAt = utcNow;
            data.UpdatedAt = utcNow;

            _mileages.Add(data);

            return data;
        }

        public List<Mileage >GetAllMileages()
        {
            return _mileages;
        }

        public Mileage GetMileage(int userId)
        {
            Mileage mileage = _mileages.Where(m => m.UserId == userId).FirstOrDefault();

            if (mileage == null)
            {
                throw new ArgumentException("userId deos not exist", "userId");
            }

            return mileage;
        }

        public Tag AddTag(Tag data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data", "data cannot be null");
            }

            if (data.Name == null)
            {
                throw new ArgumentNullException("Name", "Name cannot be null");
            }
            else if (data.Name.Length < 1)
            {
                throw new ArgumentOutOfRangeException("Name", "Name must be atleast 1 character");
            }

            Tag tag = GetTag(data.Name);

            if (tag != null)
            {
                throw new ArgumentException($"{data.Name} is already exists", "Name");
            }

            if (_tags.Count == 0)
            {
                data.Id = 1;
            }
            else
            {
                data.Id = _tags.Max(t => t.Id) + 1;
            }

            var utcNow = DateTime.UtcNow.AddHours(9);
            data.CreatedAt = utcNow;
            data.UpdatedAt = utcNow;

            _tags.Add(data);

            return data;
        }

        public Tag GetTag(string tagName)
        {
            if (tagName == null)
            {
                throw new ArgumentNullException("tagName", "tagName cannot be null");
            }

            Tag tag = _tags.Where(t => t.Name.Equals(tagName)).FirstOrDefault();
            return tag;
        }

        public Tag GetTag(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentNullException("id", "id cannot be null");
            }

            Tag tag = _tags.Where(t => t.Id == id).FirstOrDefault();
            return tag;
        }

        public ReviewTag AddReviewTag(ReviewTag data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data", "data cannot be null");
            }

            if (data.TagId <= 0)
            {
                throw new ArgumentNullException("TagId", "TagId cannot be null");
            }
            else if (data.ReviewId <= 0)
            {
                throw new ArgumentNullException("ReviewId", "ReviewId cannot be null"); 
            }

            Tag tag = null;
            try
            {
                tag = GetTag(data.TagId);
                if (tag == null)
                {
                    throw new ArgumentException("TagId does not exist", "TagId");
                }
            }
            catch
            {
                throw;
            }

            ReviewTag _reviewTag = _reviewTags.Where(r => r.TagId == data.TagId).Where(r => r.ReviewId == data.ReviewId).FirstOrDefault();
            if (_reviewTag != null)
            {
                throw new ArgumentException("{ TagId, ReviewId } does already exist", "Ids");
            }

            if (_reviewTags.Count == 0)
            {
                data.Id = 1;
            }
            else
            {
                data.Id = _reviewTags.Max(r => r.Id) + 1;
            }

            _reviewTags.Add(data);

            return data;
        }

        public ReviewTag RelateTag(string tagName, int reviewId)
        {
            Tag tag = null;
            try
            {
                tag = AddTag(new Tag() { Name = tagName });
            }
            catch (ArgumentException e)
            {
                tag = GetTag(tagName);
            }
            catch
            {
                throw;
            }

            ReviewTag reviewTag = null;
            try
            {
                reviewTag = AddReviewTag(new ReviewTag() { TagId = tag.Id, ReviewId = reviewId });
            }
            catch
            {
                throw;
            }

            return reviewTag;
        }
    }
}
