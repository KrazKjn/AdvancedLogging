using System;
using System.Web;

namespace AdvancedLogging.Extensions
{
    [CLSCompliant(false)]
    public static class HttpApplicationStateExtensions
    {
#if !__IOS__
        public static object Get(this HttpApplicationState httpAppState, int index, object defaultvalue)
        {
            try
            {
                if (httpAppState.Get(index) is null)
                    return defaultvalue;
                else
                    return httpAppState.Get(index);
            }
            catch
            {
                return defaultvalue;
            }
        }
        public static object Get(this HttpApplicationState httpAppState, string name, object defaultvalue)
        {
            try
            {
                if (httpAppState.Get(name) is null)
                    return defaultvalue;
                else
                    return httpAppState.Get(name);
            }
            catch
            {
                return defaultvalue;
            }
        }
#endif
   }
}