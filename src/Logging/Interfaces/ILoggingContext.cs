namespace AdvancedLogging.Logging.Interfaces
{
    public interface ILoggingContext
    {
        void SetProperty(string key, object value);
        void PushProperty(string key, object value);
        void PopProperty(string key);
    }
}
