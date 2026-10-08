# FT.AmoCRM

.NET Standard 2.0/2.1 client for the amoCRM API v4.

## Возможности

- OAuth 2.0: получение токена по authorization code и автоматическое обновление по refresh token.
- Работа с готовым access token.
- Типизированные сервисы: сделки, контакты, компании, пользователи и воронки.
- Асинхронный API на базе `HttpClient`.
- Автоматическая пагинация коллекций.
- Повтор запросов для HTTP 408, 429 и 5xx с exponential backoff и поддержкой `Retry-After`.
- Сохранение неизвестных полей сущности в `AdditionalData`.

## Установка

Добавьте ссылку на проект или установите собранный NuGet-пакет `FT.AmoCRM`. Библиотека поддерживает .NET Standard 2.0 и 2.1.

## OAuth-приложение

```csharp
var httpClient = new HttpClient();
var oauthOptions = new AmoCrmOAuthOptions(
	clientId: "client-id",
	clientSecret: "client-secret",
	redirectUri: new Uri("https://my-app.example.com/oauth/callback"));

var client = new AmoCrmClient(
	httpClient,
	accountDomain: "my-account.amocrm.ru",
	token: new AmoCrmToken(),
	oauthOptions);

var loginUrl = client.OAuth.GetAuthorizationUri("my-account.amocrm.ru", "csrf-state");
// Перенаправьте пользователя на loginUrl.

// После получения параметра code из callback:
var token = await client.AuthorizeAsync(code);
```

Токены следует безопасно сохранить в приложении. `AmoCrmToken.CreatedAt` должен соответствовать моменту получения токена, если токен загружается из хранилища. При истечении access token библиотека автоматически использует refresh token.

## Готовый токен

```csharp
var client = new AmoCrmClient(
	new HttpClient(),
	"my-account.amocrm.ru",
	new AmoCrmToken
	{
		AccessToken = accessToken,
		RefreshToken = refreshToken,
		ExpiresIn = expiresIn,
		CreatedAt = DateTimeOffset.UtcNow
	},
	oauthOptions);
```

Если refresh token отсутствует, по истечении access token будет выброшено `InvalidOperationException`.

## Фиксированный токен интеграции

Для интеграций, которым amoCRM выдает готовый токен, можно использовать его напрямую как Bearer-токен. Получите токен в настройках интеграции amoCRM и не передавайте OAuth credentials:

```csharp
var client = new AmoCrmClient(
	new HttpClient(),
	"my-account.amocrm.ru",
	"integration-access-token");

var deals = await client.Deals.ListAsync();
```

Клиент не обновляет такой токен: он отправляется как `Authorization: Bearer ...` при каждом запросе. Храните его в защищенной конфигурации/секрет-хранилище, не публикуйте в исходном коде. Отозванный или недействительный токен приведет к `AmoCrmApiException` от API. Для OAuth-токенов с refresh используйте OAuth-сценарий выше.

Документация amoCRM: [одноразовые токены для интеграций](https://www.amocrm.ru/developers/content/oauth/disposable-tokens).

## Работа с ресурсами

```csharp
var deals = await client.Deals.ListAsync();
var deal = await client.Deals.GetAsync(123456);

var created = await client.Deals.CreateAsync(new AmoCrmLead
{
	Name = "Новая сделка",
	Price = 10000,
	PipelineId = 111,
	StatusId = 222
});

created.Name = "Обновленная сделка";
await client.Deals.UpdateAsync(created);

var contacts = await client.Contacts.ListAsync();
var companies = await client.Companies.ListAsync();
var users = await client.Users.ListAsync();
var pipelines = await client.Pipelines.ListAsync();
```

## Задачи

Задачи доступны через `client.Tasks` и используют endpoint `/api/v4/tasks`:

```csharp
var tasks = await client.Tasks.ListAsync();

var task = await client.Tasks.CreateAsync(new AmoCrmTask
{
	Text = "Позвонить клиенту",
	CompleteTill = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
	EntityId = 123456,
	EntityType = "leads",
	TaskTypeId = 1,
	ResponsibleUserId = 987654
});

task.Text = "Позвонить клиенту повторно";
await client.Tasks.UpdateAsync(task);
```

Задачи можно фильтровать тем же `AmoCrmQuery`, например по ответственному пользователю и сроку выполнения:

```csharp
var tasks = await client.Tasks.ListAsync(
	new AmoCrmQuery()
		.Filter("responsible_user_id", 987654)
		.FilterFrom("complete_till", DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
```

## Получение полей

Получить список пользовательских и предопределенных полей можно через `client.Fields`:

```csharp
var dealFields = await client.Fields.ListDealsAsync();
var contactFields = await client.Fields.ListContactsAsync();
var companyFields = await client.Fields.ListCompaniesAsync();
var userFields = await client.Fields.ListUsersAsync();
```

Для сделок используется API endpoint `leads/custom_fields`:

```csharp
var fields = await client.Fields.ListAsync(AmoCrmFieldEntity.Deals);

foreach (var field in fields)
{
	Console.WriteLine($"{field.Id}: {field.Name} ({field.Type})");
	foreach (var option in field.Enums ?? new List<AmoCrmFieldEnum>())
		Console.WriteLine($"  {option.Id}: {option.Value}");
}
```

Список загружается полностью с автоматической пагинацией. Свойство `Settings` содержит настройки поля, а `AdditionalData` — поля ответа, которые еще не представлены отдельными свойствами модели.

Сделки находятся в API amoCRM в endpoint `leads`, но в библиотеке доступны через
`client.Deals`.

Фильтрация выполняется через `AmoCrmQuery`. Метод `Filter` формирует параметры вида
`filter[field][]`, а `FilterFrom` и `FilterTo` — диапазоны amoCRM:

```csharp
var query = new AmoCrmQuery()
	.Filter("pipeline_id", 111)
	.Filter("status_id", 222)
	.Filter("responsible_user_id", 333)
	.FilterFrom("created_at", 1700000000)
	.FilterTo("created_at", 1800000000);

var filteredDeals = await client.Deals.ListAsync(query);
var filteredContacts = await client.Contacts.ListAsync(
	new AmoCrmQuery().Filter("responsible_user_id", 333));
```

Для параметров, которых нет в helpers, используйте `Add`:

```csharp
var query = new AmoCrmQuery()
	.Add("with", "contacts,companies")
	.Add("limit", 50)
	.Add("page", 1);
```

Пагинация для отфильтрованных запросов также выполняется автоматически по `_links.next`.

## Управление webhooks

Библиотека управляет webhook-подписками amoCRM через `/api/v4/webhooks`:

```csharp
var webhook = await client.Webhooks.RegisterAsync(new AmoCrmWebhookRequest
{
	Destination = "https://example.com/amocrm/webhook",
	Settings = new Dictionary<string, bool>
	{
		["add_lead"] = true,
		["update_lead"] = true,
		["delete_lead"] = true,
		["add_contact"] = true
	}
});

var webhooks = await client.Webhooks.ListAsync();
var current = await client.Webhooks.GetAsync(webhook.Id);

await client.Webhooks.UpdateAsync(webhook.Id, new AmoCrmWebhookRequest
{
	Destination = "https://example.com/amocrm/webhook-v2",
	Settings = new Dictionary<string, bool>
	{
		["update_lead"] = true
	}
});

await client.Webhooks.DeleteAsync(webhook.Id);
```

`Settings` является словарем, чтобы поддерживать новые типы событий amoCRM без обновления библиотеки. Получатель webhook должен быть реализован в вашем приложении; библиотека только управляет регистрацией подписок.

Для `CreateAsync` и `UpdateAsync` библиотека отправляет JSON в формате API amoCRM. Доступные поля, ограничения и права определяются API аккаунта.

## Обработка ошибок

- `AmoCrmAuthenticationException` — ошибка OAuth endpoint.
- `AmoCrmApiException` — API вернул неуспешный HTTP-код; доступны `StatusCode` и `ResponseBody`.

`HttpClient` рекомендуется создавать через `IHttpClientFactory` в ASP.NET Core и не создавать новый экземпляр для каждого запроса.
