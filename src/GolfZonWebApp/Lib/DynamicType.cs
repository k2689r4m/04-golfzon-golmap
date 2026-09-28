using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Dynamic;

namespace GolfZonWebApp.Lib
{
    public class DynamicType
    {
        public static IDictionary<string, object> Convert(object obj)
        {
            dynamic dyObj = new ExpandoObject();
            var dict = dyObj as IDictionary<string, object>;

            foreach (var prop in obj.GetType().GetProperties())
            {
                dict[Char.ToLower(prop.Name[0]) + prop.Name.Substring(1)] = prop.GetValue(obj, null);
            }

            return dyObj;
        }
    }
}
