using AdvancedLogging.Logging;
using System;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace AdvancedLogging.Utilities
{
    public static class Utils
    {
        public static bool IsNumeric(string text)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { text }))
            {
                try
                {
                    if (text == Regex.Replace(text, "[^0-9]", "") && text.Trim() != string.Empty)
                        return true;
                    else
                        return false;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { text }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static object Deserializer(string Xml, Type t)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { Xml, t }))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(t);

                    // Create an XmlSerializerNamespaces object.
                    XmlSerializerNamespaces ns = new XmlSerializerNamespaces();

                    //Create the XmlNamespaceManager.
                    NameTable nt = new NameTable();
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(nt);
                    //nsmgr.AddNamespace("<SomeNamePrefix>", "<SomeSite>");

                    //Create the XmlParserContext.
                    XmlParserContext context = new XmlParserContext(null, nsmgr, null, XmlSpace.None);

                    XmlReader reader = new XmlTextReader(Xml, XmlNodeType.Element, context);

                    // Deserialize using the XmlTextWriter.
                    object o = (object)serializer.Deserialize(reader);
                    return o;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { Xml, t }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static string Serializer(object obj)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { obj }))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(obj.GetType());

                    // Create an XmlTextWriter using a XmlStream.
                    System.IO.StringWriter sw = new System.IO.StringWriter();
                    XmlTextWriter writer = new XmlTextWriter(sw);

                    // Serialize using the XmlTextWriter.
                    serializer.Serialize(writer, obj);
                    writer.Close();
                    return sw.ToString();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { obj }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static byte[] EncryptPasswordToBytes(string newPassword)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { newPassword }))
            {
                try
                {
                    string strPasswd = newPassword.PadRight(32, ' ');
                    byte[] bytePassword = new byte[32];

                    try
                    {
                        int intIndex = 0;
                        while (intIndex < strPasswd.Length)
                        {
                            //convert each letter in byte array
                            Byte[] tempBytes = Encoding.ASCII.GetBytes(strPasswd.Substring(intIndex, 1));

                            if (strPasswd.Substring(intIndex, 1) == " ")
                                bytePassword[intIndex] = Convert.ToByte(Convert.ToInt32(tempBytes[0]));
                            else
                                //add 9 to that byte
                                bytePassword[intIndex] = Convert.ToByte((Convert.ToInt32(tempBytes[0]) + 9));

                            intIndex++;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message.ToString());
                    }
                    return bytePassword;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { newPassword }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static bool VerifyUrl(string url, out Uri _DetectedUri, int _timeoutSeconds = 100)
        {
            return VerifyUri(new Uri(url), out _DetectedUri, _timeoutSeconds);
        }

        public static bool VerifyUri(Uri url, out Uri _DetectedUri, int _timeoutSeconds = 100)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { url, _timeoutSeconds }))
            {
                try
                {
                    int maxRedirCount = 8;  // prevent infinite loops
                    Uri newUrl = url;
                    string strUrl = "";
                    _DetectedUri = null;
                    do
                    {
                        HttpWebRequest req = null;
                        HttpWebResponse resp = null;
                        try
                        {
                            req = (HttpWebRequest)HttpWebRequest.Create(url);
                            req.Method = "HEAD";
                            req.AllowAutoRedirect = false;
                            req.Timeout = _timeoutSeconds * 1000;
                            resp = (HttpWebResponse)req.GetResponse();
                            switch (resp.StatusCode)
                            {
                                case HttpStatusCode.OK:
                                    _DetectedUri = newUrl;
                                    return true;
                                case HttpStatusCode.Redirect:
                                case HttpStatusCode.MovedPermanently:
                                case HttpStatusCode.RedirectKeepVerb:
                                case HttpStatusCode.RedirectMethod:
                                    strUrl = resp.Headers["Location"];
                                    if (string.IsNullOrEmpty(strUrl))
                                        return false;

                                    if (strUrl.IndexOf("://", System.StringComparison.Ordinal) == -1)
                                    {
                                        // Doesn't have a URL Schema, meaning it's a relative or absolute URL
                                        Uri u = new Uri(new Uri(strUrl), newUrl);
                                        newUrl = u;
                                    }
                                    else
                                        newUrl = new Uri(strUrl);
                                    break;
                                default:
                                    return false;
                            }
                            url = newUrl;
                        }
                        catch (WebException ex)
                        {
                            vAutoLogFunction.WriteError(string.Format("{0}()", MethodBase.GetCurrentMethod().Name), ex);
                            return false;
                        }
                        catch (Exception ex)
                        {
                            vAutoLogFunction.WriteError(string.Format("{0}()", MethodBase.GetCurrentMethod().Name), ex);
                            return false;
                        }
                        finally
                        {
                            resp?.Close();
                        }
                    } while (maxRedirCount-- > 0);

                    return false;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { url, _timeoutSeconds }, error:true, exception:exOuter);
                    throw;
                }
            }
        }
    }
}
