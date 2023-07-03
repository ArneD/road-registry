namespace RoadRegistry.BackOffice.Api.Infrastructure.Controllers.Attributes;

using System.Threading.Tasks;

internal interface IApiTokenReader
{
    Task<ApiToken> ReadAsync(string apiKey);
}
