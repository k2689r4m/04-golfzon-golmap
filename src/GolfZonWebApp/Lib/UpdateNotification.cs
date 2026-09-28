using GolfZonWebApp.Data;
using GolfZonWebApp.Models;
using GolfZonWebApp.Lib;
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;

namespace GolfZonWebApp.Lib
{
    public class UpdateNotification
    {
        //public static string IMAGE_ROOT_PATH = Path.Combine("Upload", "Images");
        //public class UploadedFile
        //{
        //    public string OriginalName;
        //    public string Name;
        //    public string Uri;
        //}

        public static async Task<Notification> Update(
            GolfzonContext context,
            int type,
            int aUserId,
            int tUserId,
            int? golfClubId,
            int? convenienceId,
            int? EventTemplateId,
            string contents,
            int? reviewId = null)
        {
            var item = new Notification
            {
                AuthorUserId = aUserId,
                TargetUserId = tUserId,
                GolfClubId = golfClubId,
                ConvenienceId = convenienceId,
                EventTemplateId = EventTemplateId,
                ReviewId = reviewId,
                Contents = contents,
                Type = type
            };

            try
            {
                context.Notifications.Add(item);
                await context.SaveChangesAsync();
                item.State = true;

                return item;
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                item.State = false;

                return item;
            }
        }

        public static async void Update(GolfzonContext context, int type, int tUserId, string contents)
        {
            var item = new Notification
            {
                AuthorUserId = null,
                TargetUserId = tUserId,
                GolfClubId = null,
                ConvenienceId = null,
                EventTemplateId = null,
                Contents = contents,
                Type = type
            };

            try
            {
                context.Notifications.Add(item);
                context.SaveChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static async void DeleteReview(
            GolfzonContext context,
            int tUserId,
            int? golfClubId,
            int? convenienceId,
            DateTime datetime)
        {
            string content = datetime.ToString("yyyy.MM.dd");
            content += "에 ";
            try {
                if (golfClubId != null)
                {
                    GolfClub g = context.GolfClubs.Find(golfClubId);
                    if (g != null)
                    {
                        content += $"'{g.Name}' 골프장에서 ";
                    }
                }
                else if (convenienceId != null)
                {
                    Convenience c = context.Conveniences.Find(convenienceId);
                    if (c != null)
                    {
                        content += $"'{c.Name}'에서 ";
                    }
                }
            } catch {

            }
            content += "작성한 리뷰가 관리자에 의해 삭제되었습니다.";

            var item = new Notification
            {
                TargetUserId = tUserId,
                GolfClubId = golfClubId,
                ConvenienceId = convenienceId,
                Contents = content,
                Type = 40
            };

            try
            {
                context.Notifications.Add(item);
                await context.SaveChangesAsync();
            }
            catch (Exception)
            {
            }
        }

        public static async void UpdateEvent(
            GolfzonContext context,
            List<EventTemplate> eventTemplates)
        {
            try
            {
                DateTime dt = DateTime.Now.AddHours(9);
                var items = context.Templates.Where(t => t.StartDate <= dt && t.EndDate >= dt).Select(t => t.Id)
                    .Join(eventTemplates, t => t, et => et.TemplateId, (t, et) => new Notification
                    {
                        TargetUserId = 0,
                        EventTemplateId = et.Id,
                        Contents = et.Content,
                        Type = 30
                    })
                    .Join(context.Users.Select(u => u.Id), n => true, u => true, (n, u) => new Notification
                    {
                        TargetUserId = u,
                        EventTemplateId = n.EventTemplateId,
                        Contents = n.Contents,
                        Type = n.Type
                    })
                    .Select(n => n);

                //var items = context.Users.Select(u => u.Id)
                //    .Join(EventTemplates, u => new { IsPush = true, IsTemp = false, Status = 1 }, et => new { et.IsPush, et.IsTemp, et.Status }, (u, et) => new Notification
                //    {
                //        TargetUserId = u,
                //        EventTemplateId = et.Id,
                //        Contents = et.Content,
                //        Type = type
                //    })
                //    .Join(context.Templates, n => n.)
                //    .ToList();

                context.Notifications.AddRange(items);
                await context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}