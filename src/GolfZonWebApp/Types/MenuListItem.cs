using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GolfZonWebApp.Types
{
    public class MenuListItem
    {
        public string Name;
        public double? Price;
        public string Description;
        public int? ImageFileId;
        public bool? Representative;
        public int? ImageIndex;
        //[ValidateNever]
        //public Models.File ImageFile { get; set; }
    }
}


// {["normal" | "breakfast"]:{ Name, Price,
// Description, ImageFileId, Representative}[]}