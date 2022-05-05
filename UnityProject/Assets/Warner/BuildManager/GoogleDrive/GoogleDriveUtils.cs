using System.Collections.Generic;

namespace GDrive
{
    partial class GoogleDrive
    {
        static T TryGet<T>(Dictionary<string, object> dict, string key)
        {
            object v;
            if (dict.TryGetValue(key, out v) && v is T)
                return (T)v;
            else
                return default(T);
        }
    }

}