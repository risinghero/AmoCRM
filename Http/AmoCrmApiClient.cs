using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using FT.AmoCRM.Authentication;
using Newtonsoft.Json;

namespace FT.AmoCRM.Http
{
    /// <summary>Sends authenticated requests to the amoCRM REST API.</summary>
    public sealed class AmoCrmApiClient
    {
        /// <summary>Creates an authenticated API client.</summary>
        private readonly HttpClient _httpClient;
        private readonly AmoCrmTokenProvider _tokenProvider;
        private readonly Uri _baseUri;
        private readonly AmoCrmClientOptions _options;

        public AmoCrmApiClient(HttpClient httpClient, string accountDomain, AmoCrmTokenProvider tokenProvider, AmoCrmClientOptions options = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            if (string.IsNullOrWhiteSpace(accountDomain)) throw new ArgumentException("Account domain is required.", nameof(accountDomain));
            _baseUri = new Uri("https://" + accountDomain.Replace("https://", string.Empty).Replace("http://", string.Empty).TrimEnd('/') + "/api/v4/");
            _options = options ?? new AmoCrmClientOptions();
        }

        public async Task<T> SendAsync<T>(HttpMethod method, string relativePath, object body = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (string.IsNullOrWhiteSpace(relativePath)) throw new ArgumentException("Relative path is required.", nameof(relativePath));

            for (var attempt = 0; ; attempt++)
            {
                using (var request = new HttpRequestMessage(method, CreateUri(relativePath)))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false));
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    if (body != null)
                        request.Content = new StringContent(JsonConvert.SerializeObject(body), System.Text.Encoding.UTF8, "application/json");

                    using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                    {
                        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (response.IsSuccessStatusCode)
                        {
                            if (typeof(T) == typeof(string)) return (T)(object)responseBody;
                            return string.IsNullOrWhiteSpace(responseBody) ? default(T) : JsonConvert.DeserializeObject<T>(responseBody);
                        }

                        if (ShouldRetry(response.StatusCode) && attempt < _options.MaxRetryAttempts)
                        {
                            await DelayAsync(response, attempt, cancellationToken).ConfigureAwait(false);
                            continue;
                        }
                        throw new AmoCrmApiException((int)response.StatusCode, responseBody);
                    }
                }
            }
        }

        public async Task<IReadOnlyList<T>> GetAllAsync<T>(string relativePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            var items = new List<T>();
            var next = relativePath;
            while (!string.IsNullOrWhiteSpace(next))
            {
                var page = await SendAsync<AmoCrmPage<T>>(HttpMethod.Get, next, null, cancellationToken).ConfigureAwait(false);
                if (page?.Items != null) items.AddRange(page.Items);
                next = page?.Links?.Next?.Href == null ? null : GetRelativePath(page.Links.Next.Href);
            }
            return items;
        }

        private Uri CreateUri(string path)
        {
            return Uri.TryCreate(path, UriKind.Absolute, out var absolute) ? absolute : new Uri(_baseUri, path.TrimStart('/'));
        }

        private string GetRelativePath(Uri uri)
        {
            return uri.IsAbsoluteUri && uri.Host.Equals(_baseUri.Host, StringComparison.OrdinalIgnoreCase)
                ? _baseUri.MakeRelativeUri(uri).ToString()
                : uri.ToString();
        }

        private static bool ShouldRetry(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.RequestTimeout || (int)statusCode == 429 || (int)statusCode >= 500;
        }

        private async Task DelayAsync(HttpResponseMessage response, int attempt, CancellationToken cancellationToken)
        {
            var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMilliseconds(_options.InitialRetryDelay.TotalMilliseconds * Math.Pow(2, attempt));
            if (delay > _options.MaxRetryDelay) delay = _options.MaxRetryDelay;
            if (delay > TimeSpan.Zero) await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }
}
