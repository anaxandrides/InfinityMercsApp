using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Infrastructure.API.UserDocuments;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>The signed-in user's companies, under me/companies.</summary>
public sealed class CompanyFunctions(UserDocumentEndpoints endpoints, CompanyFileChecks checks)
{
    private const UserDocumentKind Kind = UserDocumentKind.Company;

    [Function("ListCompanies")]
    public Task<IActionResult> ListAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "me/companies")] HttpRequest request,
        CancellationToken cancellationToken) =>
        endpoints.ListAsync<SavedCompanyFile>(request, Kind, cancellationToken);

    [Function("GetCompany")]
    public Task<IActionResult> GetAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "me/companies/{documentId}")] HttpRequest request,
        string documentId,
        CancellationToken cancellationToken) =>
        endpoints.GetAsync<SavedCompanyFile>(request, Kind, documentId, cancellationToken);

    [Function("SaveCompany")]
    public Task<IActionResult> SaveAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "me/companies/{documentId}")] HttpRequest request,
        string documentId,
        CancellationToken cancellationToken) =>
        endpoints.SaveAsync(request, Kind, documentId, checks, cancellationToken);

    [Function("DeleteCompany")]
    public Task<IActionResult> DeleteAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "me/companies/{documentId}")] HttpRequest request,
        string documentId,
        CancellationToken cancellationToken) =>
        endpoints.DeleteAsync<SavedCompanyFile>(request, Kind, documentId, cancellationToken);
}
