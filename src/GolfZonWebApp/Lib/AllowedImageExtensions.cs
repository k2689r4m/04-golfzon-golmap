using System;
using System.Linq;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace GolfZonWebApp.Lib
{
    public class AllowedImageExtensions
    {
        private static string[] _extensions = new string[]{".jpg", ".png", ".jpeg", ".gif", ".bmp", ".tif", ".tiff"};

        public static bool Validate(IFormFile file)
        {
            return _extensions.Contains(Path.GetExtension(file.FileName).ToLower());
        }

        public static bool Validate(IFormFile[] files)
        {
            foreach (var file in files) 
            {
                string extension = Path.GetExtension(file.FileName).ToLower();

                if (!_extensions.Contains(extension))
                {
                    return false;
                }
            }

            return true;
        }
    }
}