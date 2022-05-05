using System.Collections;
using System;

namespace GDrive
{
    public partial class GoogleDrive
    {
        public string ClientID { get; set; }

        public string ClientSecret { get; set; }

        public class AsyncSuccess
        {
            public object Result { get; private set; }

            public AsyncSuccess()
                : this(null)
            {
            }

            public AsyncSuccess(object o)
            {
                Result = o;
            }
        }

        public static T GetResult<T>(IEnumerator async)
        {
            var asyncSuccess = async.Current as AsyncSuccess;
            if (asyncSuccess != null)
                return (T)asyncSuccess.Result;
            else
                return default(T);
        }

        public static bool IsDone(IEnumerator async)
        {
            return (async.Current is AsyncSuccess || async.Current is Exception);
        }

        public GoogleDrive()
        {
        }
    }
}
