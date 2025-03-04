using System.Collections.Generic;
using System.Net;

namespace AdvancedLogging.Extensions
{
    public static class WebHeaderCollectionExtensions
    {
        /// <summary>
        /// Gets all headers from the WebHeaderCollection as an array of key-value pairs.
        /// </summary>
        /// <param name="webHeaderCollection">The WebHeaderCollection instance.</param>
        /// <returns>An array of key-value pairs representing the headers.</returns>
        public static KeyValuePair<string, string>[] GetHeaders(this WebHeaderCollection webHeaderCollection)
        {
            string[] keys = webHeaderCollection.AllKeys;
            var keyVals = new KeyValuePair<string, string>[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                keyVals[i] = new KeyValuePair<string, string>(keys[i], webHeaderCollection[keys[i]]);
            return keyVals;
        }

        /// <summary>
        /// Serializes the WebHeaderCollection to a string.
        /// </summary>
        /// <param name="webHeaderCollection">The WebHeaderCollection instance.</param>
        /// <returns>A string representation of the headers.</returns>
        private static string Serialize(this WebHeaderCollection webHeaderCollection)
        {
            var response = new System.Text.StringBuilder();
            foreach (string k in webHeaderCollection.Keys)
                response.AppendLine(k + ": " + webHeaderCollection[k]);
            return response.ToString();
        }
    }
}
