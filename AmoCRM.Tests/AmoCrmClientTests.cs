using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
        public async Task FixedTokenClientSendsBearerTokenWithoutOAuth()
        {
            AuthenticationHeaderValue authorization = null;
            var handler = new StubHandler((request, count) =>
            {
                authorization = request.Headers.Authorization;
                return JsonResponse("{\"_embedded\":{\"leads\":[]}}");
            });
            var client = new AmoCrmClient(new HttpClient(handler), "example.amocrm.ru", "integration-token");

            await client.Deals.ListAsync();

            Assert.Equal("Bearer", authorization.Scheme);
            Assert.Equal("integration-token", authorization.Parameter);
            Assert.IsType<AmoCrmFixedTokenProvider>(client.AccessTokenProvider);
            Assert.Null(client.OAuth);
        }

        [Fact]
        public async Task ApiClientFollowsNextLink()
        {
            var handler = new StubHandler((request, count) => count == 1
                ? JsonResponse("{\"_embedded\":{\"leads\":[{\"id\":1,\"name\":\"First\"}]},\"_links\":{\"next\":{\"href\":\"https://example.amocrm.ru/api/v4/leads?page=2\"}}}")
                : JsonResponse("{\"_embedded\":{\"leads\":[{\"id\":2,\"name\":\"Second\"}]}}") );
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
                return JsonResponse("{\"_embedded\":{\"leads\":[]}}");
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
        public async Task DealsListAsyncReadsEmbeddedLeadsAcrossPages()
        {
            var requestCount = 0;
            var handler = new StubHandler((request, count) =>
            {
                requestCount = count;
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/v4/leads", request.RequestUri.AbsolutePath);
                if (count == 1)
                {
                    return JsonResponse("{\"_page\":1,\"_embedded\":{\"leads\":[{\"id\":19619,\"name\":\"First deal\",\"status_id\":142}]},\"_links\":{\"next\":{\"href\":\"https://example.amocrm.ru/api/v4/leads?page=2\"}}}");
                }

                Assert.Equal(2, count);
                Assert.Equal("?page=2", request.RequestUri.Query);
                return JsonResponse("{\"_page\":2,\"_embedded\":{\"leads\":[{\"id\":14460,\"name\":\"Second deal\",\"status_id\":143}]}}");
            });
            using (var httpClient = new HttpClient(handler))
            {
                var client = new AmoCrmClient(httpClient, "example.amocrm.ru", "integration-token");

                var deals = await client.Deals.ListAsync();

                Assert.Equal(2, requestCount);
                Assert.Collection(deals,
                    deal =>
                    {
                        Assert.Equal(19619L, deal.Id);
                        Assert.Equal("First deal", deal.Name);
                        Assert.Equal(142L, deal.StatusId);
                    },
                    deal =>
                    {
                        Assert.Equal(14460L, deal.Id);
                        Assert.Equal("Second deal", deal.Name);
                        Assert.Equal(143L, deal.StatusId);
                    });
            }
        }

        [Fact]
        public async Task DealsUpdateAsyncReadsEmbeddedLeadAfterStatusChange()
        {
            var handler = new StubHandler((request, count) =>
            {
                Assert.Equal("PATCH", request.Method.Method);
                Assert.Equal("/api/v4/leads/54886", request.RequestUri.AbsolutePath);
                var body = Newtonsoft.Json.Linq.JObject.Parse(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                Assert.Equal(143L, body.Value<long>("status_id"));
                return JsonResponse("{\"_links\":{\"self\":{\"href\":\"https://example.amocrm.ru/api/v4/leads/54886\"}},\"_embedded\":{\"leads\":[{\"id\":54886,\"updated_at\":1589556420}]}}");
            });
            using (var httpClient = new HttpClient(handler))
            {
                var client = new AmoCrmClient(httpClient, "example.amocrm.ru", "integration-token");

                var updated = await client.Deals.UpdateAsync(new Resources.AmoCrmLead
                {
                    Id = 54886,
                    StatusId = 143
                });

                Assert.NotNull(updated);
                Assert.Equal(54886L, updated.Id);
                Assert.Equal(1589556420L, updated.UpdatedAt);
            }
        }

        [Fact]
        public async Task DealsCreateAsyncReadsEmbeddedLeadAcknowledgement()
        {
            var handler = new StubHandler((request, count) =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("/api/v4/leads", request.RequestUri.AbsolutePath);
                var body = Newtonsoft.Json.Linq.JArray.Parse(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                Assert.Single(body);
                Assert.Equal("New deal", body[0].Value<string>("name"));
                return JsonResponse("{\"_links\":{\"self\":{\"href\":\"https://example.amocrm.ru/api/v4/leads\"}},\"_embedded\":{\"leads\":[{\"id\":10185151,\"request_id\":\"0\"}]}}");
            });
            using (var httpClient = new HttpClient(handler))
            {
                var client = new AmoCrmClient(httpClient, "example.amocrm.ru", "integration-token");

                var created = await client.Deals.CreateAsync(new Resources.AmoCrmLead { Name = "New deal" });

                Assert.NotNull(created);
                Assert.Equal(10185151L, created.Id);
            }
        }

        [Theory]
        [InlineData("leads")]
        [InlineData("contacts")]
        [InlineData("companies")]
        [InlineData("users")]
        [InlineData("tasks")]
        [InlineData("pipelines")]
        [InlineData("custom_fields")]
        [InlineData("items")]
        public void PageReadsNamedEmbeddedCollection(string collectionName)
        {
            var json = "{\"_embedded\":{\"" + collectionName + "\":[{\"id\":123}]}}";

            var page = Newtonsoft.Json.JsonConvert.DeserializeObject<AmoCrmPage<Resources.AmoCrmLead>>(json);

            Assert.Single(page.Items);
            Assert.Equal(123L, page.Items[0].Id);
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
