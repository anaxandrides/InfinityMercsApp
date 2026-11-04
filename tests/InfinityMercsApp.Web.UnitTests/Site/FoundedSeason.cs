using Bunit;
using InfinityMercsApp.Web.Pages;

namespace InfinityMercsApp.Web.UnitTests.Site;

/// <summary>A founded company's season page, opened, and the season's id.</summary>
internal sealed record FoundedSeason(IRenderedComponent<Season> Page, string Id);
