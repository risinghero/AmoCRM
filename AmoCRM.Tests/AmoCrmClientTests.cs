using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FT.AmoCRM;
using FT.AmoCRM.Authentication;
using FT.AmoCRM.Http;
using Xunit;

namespace FT.AmoCRM.Tests
{
    public sealed class AmoCrmClientTests
    {
        [Fact]
        public void AuthorizationUriContainsRequiredParameters()
        {
            var oauth = new AmoCrmOAuthClient(new HttpClient(), new AmoCrmOAuthOptions("id", "secret", new Uri("https://localhost/callback")));
            var uri = oauth.GetAuthorizationUri("example.amocrm.ru", "state-value");
            Assert.Contains("client_id=id", uri.Query);
            Assert.Contains("response_type=code", uri.Query);
            Assert.Contains("state=state-value", uri.Query);
        }

        [Fact]
        public async Task TokenProviderRefreshesExpiredToken()
        {
            var handler = new StubHandler((request, count) => JsonResponse("{\"access_token\":\"new-token\",\"refresh_token\":\"new-refresh\",\"expires_in\":86400}"));
            var oauth = new AmoCrmOAuthClient(new HttpClient(handler), new AmoCrmOAuthOptions("id", "secret", new Uri("https://localhost/callback")));
            var token = new AmoCrmToken { AccessToken = "old-token", RefreshToken = "refresh", ExpiresIn = 1, CreatedAt = DateTimeOffset.UtcNow.AddHours(-1) };
            var provider = new AmoCrmTokenProvider("example.amocrm.ru", token, oauth);

            var accessToken = await provider.GetAccessTokenAsync();

            Assert.Equal("new-token", accessToken);
            Assert.Equal("new-token", provider.CurrentToken.AccessToken);
        }

        [Fact]
        public async Task ApiClientFollowsNextLink()
        {
            var handler = new StubHandler((request, count) => count == 1
                ? JsonResponse("{\"_embedded\":{\"items\":[{\"id\":1,\"name\":\"First\"}]},\"_links\":{\"next\":{\"href\":\"https://example.amocrm.ru/api/v4/leads?page=2\"}}}")
                : JsonResponse("{\"_embedded\":{\"items\":[{\"id\":2,\"name\":\"Second\"}]}}") );
            var oauth = new AmoCrmOAuthClient(new HttpClient(handler), new AmoCrmOAuthOptions("id", "secret", new Uri("https://localhost/callback")));
            var provider = new AmoCrmTokenProvider("example.amocrm.ru", new AmoCrmToken { AccessToken = "token", ExpiresIn = 3600 }, oauth);
            var api = new AmoCrmApiClient(new HttpClient(handler), "example.amocrm.ru", provider);

            var leads = await api.GetAllAsync<Resources.AmoCrmLead>("leads");

            Assert.Equal(2, leads.Count);
            Assert.Equal("Second", leads[1].Name);
        }

        [Fact]
        public async Task DealsServiceSendsFilters()
        {
            Uri requestedUri = null;
            var handler = new StubHandler((request, count) =>
            {
                requestedUri = request.RequestUri;
                return JsonResponse("{\"_embedded\":{\"items\":[]}}");
            });
            var oauth = new AmoCrmOAuthClient(new HttpClient(handler), new AmoCrmOAuthOptions("id", "secret", new Uri("https://localhost/callback")));
            var provider = new AmoCrmTokenProvider("example.amocrm.ru", new AmoCrmToken { AccessToken = "token", ExpiresIn = 3600 }, oauth);
            var api = new AmoCrmApiClient(new HttpClient(handler), "example.amocrm.ru", provider);
            var deals = new Resources.AmoCrmDealsService(api);

            await deals.ListAsync(new AmoCrmQuery()
                .Filter("pipeline_id", 10)
                .Filter("status_id", 20)
                .FilterFrom("created_at", 1700000000));

            Assert.Contains("filter%5Bpipeline_id%5D%5B%5D=10", requestedUri.Query);
            Assert.Contains("filter%5Bstatus_id%5D%5B%5D=20", requestedUri.Query);
            Assert.Contains("filter%5Bcreated_at%5D%5Bfrom%5D=1700000000", requestedUri.Query);
        }

        [Fact]
        public async Task WebhooksServiceRegistersWebhook()
        {
            HttpRequestMessage request = null;
            string requestBody = null;
            var handler = new StubHandler((currentRequest, count) =>
            {
                request = currentRequest;
                requestBody = currentRequest.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return JsonResponse("{\"id\":123,\"destination\":\"https://example.com/hook\",\"settings\":{\"add_lead\":true}}");
            });
            var oauth = new AmoCrmOAuthClient(new HttpClient(handler), new AmoCrmOAuthOptions("id", "secret", new Uri("https://localhost/callback")));
            var provider = new AmoCrmTokenProvider("example.amocrm.ru", new AmoCrmToken { AccessToken = "token", ExpiresIn = 3600 }, oauth);
            var api = new AmoCrmApiClient(new HttpClient(handler), "example.amocrm.ru", provider);
            var webhook = await new Resources.AmoCrmWebhooksService(api).RegisterAsync(new Resources.AmoCrmWebhookRequest
            {
                Destination = "https://example.com/hook",
                Settings = new System.Collections.Generic.Dictionary<string, bool> { ["add_lead"] = true }
            });

            Assert.Equal(123, webhook.Id);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Contains("add_lead", requestBody);
        }

        [Fact]
        public async Task FieldsServiceReadsCustomFieldsCollection()
        {
            Uri requestedUri = null;
            var handler = new StubHandler((request, count) =>
            {
                requestedUri = request.RequestUri;
                return JsonResponse("{\"_embedded\":{\"custom_fields\":[{\"id\":456,\"name\":\"Дата оплаты\",\"type\":\"date\"}]}}");
            });
            var oauth = new AmoCrmOAuthClient(new HttpClient(handler), new AmoCrmOAuthOptions("id", "secret", new Uri("https://localhost/callback")));
            var provider = new AmoCrmTokenProvider("example.amocrm.ru", new AmoCrmToken { AccessToken = "token", ExpiresIn = 3600 }, oauth);
            var api = new AmoCrmApiClient(new HttpClient(handler), "example.amocrm.ru", provider);
            var fields = await new Resources.AmoCrmFieldsService(api).ListDealsAsync();

            Assert.Single(fields);
            Assert.Equal(456, fields[0].Id);
            Assert.Equal("date", fields[0].Type);
            Assert.Equal("/api/v4/leads/custom_fields", requestedUri.AbsolutePath);
        }

        [Fact]
        public async Task TasksServiceReadsTasksCollection()
        {
            Uri requestedUri = null;
            var handler = new StubHandler((request, count) =>
            {
                requestedUri = request.RequestUri;
                return JsonResponse("{\"_embedded\":{\"items\":[{\"id\":789,\"text\":\"Позвонить клиенту\",\"complete_till\":1700000000,\"is_completed\":false}]}}");
            });
            var oauth = new AmoCrmOAuthClient(new HttpClient(handler), new AmoCrmOAuthOptions("id", "secret", new Uri("https://localhost/callback")));
            var provider = new AmoCrmTokenProvider("example.amocrm.ru", new AmoCrmToken { AccessToken = "token", ExpiresIn = 3600 }, oauth);
            var api = new AmoCrmApiClient(new HttpClient(handler), "example.amocrm.ru", provider);
            var tasks = await new Resources.AmoCrmTasksService(api).ListAsync();

            Assert.Single(tasks);
            Assert.Equal(789, tasks[0].Id);
            Assert.Equal("Позвонить клиенту", tasks[0].Text);
            Assert.Equal("/api/v4/tasks", requestedUri.AbsolutePath);
        }

        private static HttpResponseMessage JsonResponse(string json)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, int, HttpResponseMessage> _handler;
            private int _count;

            public StubHandler(Func<HttpRequestMessage, int, HttpResponseMessage> handler) { _handler = handler; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_handler(request, Interlocked.Increment(ref _count)));
            }
        }
    }
}
