using System.Threading;
using System.Threading.Tasks;

namespace FT.AmoCRM.Authentication
{
    /// <summary>Provides an access token for amoCRM API requests.</summary>
    public interface IAmoCrmAccessTokenProvider
    {
        /// <summary>Gets the access token to use for an API request.</summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The access token.</returns>
        Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
