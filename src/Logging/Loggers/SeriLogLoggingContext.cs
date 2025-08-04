using AdvancedLogging.Logging.Interfaces;
using Serilog.Context;
using System;

namespace AdvancedLogging.Logging.Loggers
{
    public class SeriLogLoggingContext : ILoggingContext
    {
        public void SetProperty(string key, object value)
        {
            // Serilog doesn't have a direct equivalent of ThreadContext.Properties.
            // LogContext.PushProperty is the closest, but it's scoped to a using block.
            // This is a limitation of this abstraction.
        }

        public void PushProperty(string key, object value)
        {
            LogContext.PushProperty(key, value);
        }

        public void PopProperty(string key)
        {
            // In Serilog, properties are popped when the using block is exited.
            // This method is a no-op.
        }
    }
}
