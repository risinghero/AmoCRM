using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FT.AmoCRM.Http;

namespace FT.AmoCRM.Resources
{
    /// <summary>Provides common CRUD and collection operations for an amoCRM resource.</summary>
    public abstract class AmoCrmResourceService<T> where T : AmoCrmEntity
    {
        /// <summary>Creates a resource service.</summary>
        protected AmoCrmResourceService(AmoCrmApiClient apiClient, string resourceName)
        {
            ApiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            ResourceName = resourceName;
        }

        protected AmoCrmApiClient ApiClient { get; }
        protected string ResourceName { get; }

        /// <summary>Gets all entities with automatic pagination.</summary>
        public Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return ApiClient.GetAllAsync<T>(ResourceName, cancellationToken);
        }

        /// <summary>Gets all entities matching a query with automatic pagination.</summary>
        public Task<IReadOnlyList<T>> ListAsync(AmoCrmQuery query, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ApiClient.GetAllAsync<T>((query ?? new AmoCrmQuery()).AppendTo(ResourceName), cancellationToken);
        }

        /// <summary>Gets one entity by identifier.</summary>
        public Task<T> GetAsync(long id, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ApiClient.SendAsync<T>(HttpMethod.Get, ResourceName + "/" + id, null, cancellationToken);
        }

        /// <summary>Creates an entity.</summary>
        public async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var result = await ApiClient.SendAsync<List<T>>(HttpMethod.Post, ResourceName, new[] { entity }, cancellationToken).ConfigureAwait(false);
            return result == null || result.Count == 0 ? null : result[0];
        }

        /// <summary>Updates an entity.</summary>
        public async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (entity.Id <= 0) throw new ArgumentException("An entity ID is required for update.", nameof(entity));
            var result = await ApiClient.SendAsync<List<T>>(new HttpMethod("PATCH"), ResourceName + "/" + entity.Id, entity, cancellationToken).ConfigureAwait(false);
            return result == null || result.Count == 0 ? null : result[0];
        }
    }

    /// <summary>Provides operations for deals.</summary>
    public sealed class AmoCrmDealsService : AmoCrmResourceService<AmoCrmLead>
    {
        /// <summary>Creates the deals service.</summary>
        public AmoCrmDealsService(AmoCrmApiClient apiClient) : base(apiClient, "leads") { }
    }

    /// <summary>Provides operations for contacts.</summary>
    public sealed class AmoCrmContactsService : AmoCrmResourceService<AmoCrmContact>
    {
        /// <summary>Creates the contacts service.</summary>
        public AmoCrmContactsService(AmoCrmApiClient apiClient) : base(apiClient, "contacts") { }
    }

    /// <summary>Provides operations for companies.</summary>
    public sealed class AmoCrmCompaniesService : AmoCrmResourceService<AmoCrmCompany>
    {
        /// <summary>Creates the companies service.</summary>
        public AmoCrmCompaniesService(AmoCrmApiClient apiClient) : base(apiClient, "companies") { }
    }

    /// <summary>Provides operations for users.</summary>
    public sealed class AmoCrmUsersService : AmoCrmResourceService<AmoCrmUser>
    {
        /// <summary>Creates the users service.</summary>
        public AmoCrmUsersService(AmoCrmApiClient apiClient) : base(apiClient, "users") { }
    }

    /// <summary>Provides operations for tasks.</summary>
    public sealed class AmoCrmTasksService : AmoCrmResourceService<AmoCrmTask>
    {
        /// <summary>Creates the tasks service.</summary>
        public AmoCrmTasksService(AmoCrmApiClient apiClient) : base(apiClient, "tasks") { }
    }

    /// <summary>Provides operations for sales pipelines.</summary>
    public sealed class AmoCrmPipelinesService : AmoCrmResourceService<AmoCrmPipeline>
    {
        /// <summary>Creates the pipelines service.</summary>
        public AmoCrmPipelinesService(AmoCrmApiClient apiClient) : base(apiClient, "leads/pipelines") { }
    }
}
