using AdvancedLogging.Enumerations;
using AdvancedLogging.Logging;
using AdvancedLogging.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace AdvancedLogging.Services
{
    /// <summary>
    /// Base class for web services that provides logging and security protocol setup.
    /// </summary>
    public class WebServiceBase : System.Web.HttpApplication
    {
        // Indicates whether to log the SOAP body.
        public bool m_bLogSoapBody = false;

        /// <summary>
        /// Gets or sets a value indicating whether to log the SOAP body.
        /// </summary>
        public bool LogSoapBody
        {
            get { return m_bLogSoapBody; }
            set { m_bLogSoapBody = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServiceBase"/> class.
        /// Sets up security protocols.
        /// </summary>
        public WebServiceBase()
        {
            using (var vAutoLogFunction = new AutoLogFunction())
            {
                try
                {
                    SecurityProtocol.AvailableSecurityProtocols = SecurityProtocolTypeCustom.SystemDefault;
                    SecurityProtocol.EnableAllTlsSupport();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Handles the BeginRequest event to log SOAP actions and optionally the SOAP body.
        /// </summary>
        public void Application_BeginRequest(Object Sender, EventArgs e)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { Sender, e }))
            {
                try
                {
                    SecurityProtocol.EnableAllTlsSupport();
                    if (Request.ServerVariables.AllKeys.Contains("HTTP_SOAPACTION"))
                    {
                        // Only SOAP action will be logged.
                        if (Request.ServerVariables["HTTP_SOAPACTION"].Length > 0)
                        {
                            string httpSOAPAction = Request.ServerVariables["HTTP_SOAPACTION"];
                            httpSOAPAction = httpSOAPAction.Substring(1, httpSOAPAction.Length - 2);
                            Uri uriSOAP = new Uri(httpSOAPAction);
                            string functionName = Uri.UnescapeDataString(uriSOAP.Segments.Last());
                            string log = $"function={functionName}";

                            if (LogSoapBody)
                            {
                                try
                                {
                                    if (ResolveSoapMessage(Request, out string soapBody))
                                    {
                                        log += $"?{soapBody}";
                                    }
                                }
                                catch (Exception ex)
                                {
                                    vAutoLogFunction.WriteError("Error Getting SoapBody via ResolveSoapMessage.", ex);
                                }
                            }

                            int queryStringLength = 0;
                            if (Request.QueryString.Count > 0)
                            {
                                queryStringLength = Request.ServerVariables["QUERY_STRING"].Length;
                                Response.AppendToLog("&");
                            }

                            const int maxLogLength = 4100;
                            const int bufferExceededIndicatorLength = 9;
                            if ((queryStringLength + log.Length) < maxLogLength)
                            {
                                Response.AppendToLog(log);
                            }
                            else
                            {
                                // Append only the first 4090 characters; the limit is a total of 4100 characters.
                                Response.AppendToLog(log.Substring(0, (maxLogLength - bufferExceededIndicatorLength - queryStringLength)));
                                // Indicate buffer exceeded. 9 characters so we are still under the 4100 character limit.
                                Response.AppendToLog("|||...|||");
                            }
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { Sender, e }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Resolves the SOAP message from the HTTP request.
        /// </summary>
        /// <param name="httpRequest">The HTTP request.</param>
        /// <param name="soapBody">The SOAP body extracted from the request.</param>
        /// <returns>True if the SOAP body was successfully resolved; otherwise, false.</returns>
        internal static bool ResolveSoapMessage(System.Web.HttpRequest httpRequest, out string soapBody)
        {
            soapBody = "";

            Stream httpStream = httpRequest.InputStream;

            // Save the current position of the stream.
            long posStream = httpStream.Position;

            // If the request contains an HTTP_SOAPACTION header, look at this message.
            if (httpRequest.ServerVariables["HTTP_SOAPACTION"] == null)
                return false;

            // Load the body of the HTTP message into an XML document.
            XmlDocument dom = new XmlDocument();
            try
            {
                dom.Load(httpStream);
                // Reset the stream position.
                httpStream.Position = posStream;

                // Bind to the SOAP body.
                XmlNode xmlSOAPBody = (dom.DocumentElement.GetElementsByTagName("soap:Body")[0] ?? dom.DocumentElement.GetElementsByTagName("soapenv:Body")[0]) ?? dom.DocumentElement.GetElementsByTagName("s:Body")[0];
                if (xmlSOAPBody != null)
                {
                    soapBody = xmlSOAPBody.InnerXml;
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                // Reset the position of the stream.
                httpStream.Position = posStream;
                throw;
            }
        }
    }
}
