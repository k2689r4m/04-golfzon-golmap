using GolfZonWebApp.Data;
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Drawing;

namespace GolfZonWebApp.Lib
{
    public class Upload
    {
        //public static string IMAGE_ROOT_PATH = Path.Combine("Upload", "Images");
        public static string IMAGE_ROOT_PATH = @"\\nas.golfzon.local\golmap_wwwimg";
        //public static string IMAGE_ROOT_ADMIN_PATH = @"\\nas.golfzon.local\golmap_wwwimg\Images";
        //public static string IMAGE_ROOT_REVIEW_PATH = @"\\nas.golfzon.local\golmap_wwwimg\ReviewImages";
        public static string IMAGE_TYPE_ADMIN = "images";
        public static string IMAGE_TYPE_REVIEW = "reviewimages";
        public class UploadedFile
        {
            public string OriginalName;
            public string Name;
            public string Uri;
        }

        //// 생성한 파일 이름 반환
        public static async Task<string> UploadImage(IFormFile file, string type, string name = null)
        {
            if (file.Length > 0)
            {
                //var folderName = path;
                //var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
                //var pathToSave = Path.Combine(IMAGE_ROOT_PATH, type);
                var pathToSave = Path.Combine(IMAGE_ROOT_PATH, type);
                var fileName = name != null ? name : Guid.NewGuid().ToString();
                var fullPath = Path.Combine(pathToSave, fileName);

                Console.WriteLine(fullPath);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);

                    using (Image image = Image.FromStream(stream))
                    {
                        var w = (int)(image.Width * (512.0 / image.Width));
                        var h = (int)(image.Height * (512.0 / image.Width));


                        Bitmap bitmap = new Bitmap(w, h);

                        using (var g = Graphics.FromImage(bitmap))
                        {
                            g.DrawImage(image, 0, 0, w, h);
                        }

                        bitmap.Save(fullPath + "_s");
                    }
                }

                return fileName;
            }
            else
            {
                throw new Exception("Empty File");
            }
        }

        // 생성한 파일 이름 리스트 반환
        public static async Task<List<string>> UploadImage(IFormFile[] files, string type)
        {
            List<string> paths = new List<string>();

            foreach (var file in files)
            {
                paths.Add(await UploadImage(file, type));
            }

            return paths;
        }

        public static async Task<UploadedFile> UploadImageAndReturnInfo(IFormFile file, string type)
        {
            //var path = GetFilePath(type);
            var createdFileName = await UploadImage(file, type);

            UploadedFile newFileInfo = new UploadedFile
            {
                Name = createdFileName,
                OriginalName = file.FileName,
                Uri = Path.Combine(type, createdFileName)
            };

            return newFileInfo;
        }

        // 디비 저장 후 파일 객체 리스트 반환
        public static async Task<List<UploadedFile>> UploadImageAndReturnInfo(IFormFile[] files, string type)
        {
            //var path = GetFilePath(type);
            List<UploadedFile> newFilesInfo = new List<UploadedFile>();

            foreach (IFormFile file in files)
            {
                var createdFileName = await UploadImage(file, type);

                UploadedFile fileInfo = new UploadedFile
                {
                    Name = createdFileName,
                    OriginalName = file.FileName,
                    Uri = Path.Combine(type, createdFileName)
                };

                newFilesInfo.Add(fileInfo);
            }

            return newFilesInfo;
        }

        public static void DeleteImage(string imageUri, string type)
        {
            //string path = Path.Combine(Directory.GetCurrentDirectory(), "Upload");
            string path = Path.Combine(IMAGE_ROOT_PATH, type);
            path = Path.Combine(path, imageUri);

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}