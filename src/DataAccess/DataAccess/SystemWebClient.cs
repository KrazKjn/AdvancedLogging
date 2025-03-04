using AdvancedLogging.Interfaces;
using System;
using System.Net;

namespace AdvancedLogging.DataAccess
{
    /// <summary>
    /// Provides a web client for sending and receiving data from a URI.
    /// </summary>
    public class SystemWebClient : WebClient, IWebClient
    {
        /// <summary>
        /// Gets or sets the base address of the URI.
        /// </summary>
        public new string BaseAddress
        {
            get => base.BaseAddress;
            set => base.BaseAddress = value;
        }

        /// <summary>
        /// Gets or sets the collection of header name/value pairs associated with the request.
        /// </summary>
        public new WebHeaderCollection Headers
        {
            get => base.Headers;
            set => base.Headers = value;
        }

        /// <summary>
        /// Downloads the requested resource as a string.
        /// </summary>
        /// <param name="address">The URI from which to download data.</param>
        /// <returns>A <see cref="string"/> containing the requested resource.</returns>
        public new string DownloadString(string address)
        {
            try
            {
                return base.DownloadString(address);
            }
            catch (WebException ex)
            {
                // Log the exception or handle it as needed
                throw new InvalidOperationException("An error occurred while downloading the string.", ex);
            }
        }
    }
}
