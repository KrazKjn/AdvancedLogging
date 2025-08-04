using AdvancedLogging.Logging.Interfaces;
using log4net;

namespace AdvancedLogging.Logging.Loggers
{
    public class Log4NetLoggingContext : ILoggingContext
    {
        public void SetProperty(string key, object value)
        {
            ThreadContext.Properties[key] = value;
        }

        public void PushProperty(string key, object value)
        {
            ThreadContext.Stacks[key].Push(value.ToString());
        }

        public void PopProperty(string key)
        {
            ThreadContext.Stacks[key].Pop();
        }
    }
}
