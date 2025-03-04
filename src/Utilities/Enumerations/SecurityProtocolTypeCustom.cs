using System;
using System.Net;

namespace AdvancedLogging.Enumerations
{
    /// <summary>
    /// Specifies the security protocols that are supported by the Schannel securitypackage.
    /// </summary>
    [Flags]
    public enum SecurityProtocolTypeCustom
    {
        // Summary:
        //     Specifies the system default security protocol.
        SystemDefault = 0,
        // Summary:
        //     Specifies the Transport Layer Security (TLS) 1.0 security protocol.
        Tls = SecurityProtocolType.Tls,
        // Summary:
        //     Specifies the Transport Layer Security (TLS) 1.1 security protocol.
        Tls11 = 768, // missing on Framework 4.0
                     // Summary:
                     //     Specifies the Transport Layer Security (TLS) 1.2 security protocol.
        Tls12 = 3072, // missing on Framework 4.0
                      // Summary:
                      //     Specifies the Transport Layer Security (TLS) 1.3 security protocol.
        Tls13 = 12288,
        // Summary:
        //     Specifies the Secure Socket Layer (SSL) 3.0 security protocol.
        Ssl3 = SecurityProtocolType.Ssl3
    }
}
