using System;

namespace FT.AmoCRM.Http
{
    /// <summary>Represents a non-successful amoCRM API response.</summary>
    public sealed class AmoCrmApiException : Exception
    {
        /// <summary>Creates an API exception.</summary>
        public AmoCrmApiException(int statusCode, string responseBody)
            : base("amoCRM API request failed with HTTP " + statusCode + ".")
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }

        /// <summary>The HTTP status code.</summary>
        public int StatusCode { get; }
        /// <summary>The raw API response body.</summary>
        public string ResponseBody { get; }
    }
}
