using System;

namespace AdvancedLogging.Interfaces
{
    public interface IWebClientExtended : IDisposable
    {
        string BaseAddress { get; set; }
        System.Net.WebHeaderCollection Headers { get; set; }
        // Required methods (subset of `System.Net.WebClient` methods).
        string DownloadString(string address);
    }
}
