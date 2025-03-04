using System;
using System.Net;

namespace AdvancedLogging.Models
{
    public class WebClientExt : WebClient
    {
        private WebRequest _webRequest = null;
        private int? _timeout;
        protected override WebRequest GetWebRequest(Uri uri)
        {
            _webRequest = base.GetWebRequest(uri);
            if (Timeout > 0)
                _webRequest.Timeout = Timeout;
            return _webRequest;
        }

        public int Timeout
        {
            get
            {
                return _timeout ?? 0;
            }
            set
            {
                if (_webRequest != null)
                    _webRequest.Timeout = value;
                _timeout = value;
            }
        }

        public WebRequest GetWebRequest()
        {
            return _webRequest;
        }

        public WebRequest GetWebRequest(string address)
        {
            _webRequest = base.GetWebRequest(new Uri(address));
            if (Timeout > 0)
                _webRequest.Timeout = Timeout;
            return _webRequest;
        }

        public new WebResponse GetWebResponse(WebRequest webRequest)
        {
            return base.GetWebResponse(webRequest);
        }
    }
}
