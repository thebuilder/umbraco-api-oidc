using Microsoft.AspNetCore.Http;
using TheBuilder.VercelOidc.Configuration;
using TheBuilder.VercelOidc.DeliveryApi;
using TheBuilder.VercelOidc.Tokens;
using Umbraco.Cms.Core.Configuration.Models;

namespace TheBuilder.VercelOidc.Tests;

public sealed class VercelOidcApiAccessServiceTests
{
    private readonly DefaultHttpContext _context = new();
    private readonly DeliveryApiSettings _deliveryApi = new() { Enabled = true, PublicAccess = false, Media = { Enabled = true } };
    private readonly VercelOidcOptions _vercel = FakeVercelIssuer.Options();

    private VercelOidcApiAccessService Service() => new(
        new HttpContextAccessor { HttpContext = _context },
        new TestOptionsMonitor<DeliveryApiSettings>(_deliveryApi),
        new TestOptionsMonitor<VercelOidcOptions>(_vercel));

    private void WithValidToken() => _context.Features.Set(
        new VercelOidcAccessFeature(new VercelOidcIdentity("acme", "team_123", "acme-web", "prj_123", "production")));

    [Fact]
    public void Refuses_a_request_with_nothing()
    {
        var service = Service();

        Assert.False(service.HasPublicAccess());
        Assert.False(service.HasPreviewAccess());
        Assert.False(service.HasMediaAccess());
    }

    [Fact]
    public void Grants_content_preview_and_media_to_a_valid_token()
    {
        WithValidToken();
        var service = Service();

        Assert.True(service.HasPublicAccess());
        Assert.True(service.HasPreviewAccess());
        Assert.True(service.HasMediaAccess());
    }

    [Fact]
    public void Withholds_preview_when_turned_off()
    {
        _vercel.AllowPreview = false;
        WithValidToken();

        Assert.False(Service().HasPreviewAccess());
        Assert.True(Service().HasPublicAccess());
    }

    [Fact]
    public void Ignores_the_token_when_the_package_is_not_configured()
    {
        _vercel.TeamSlug = null;
        WithValidToken();

        Assert.False(Service().HasPublicAccess());
    }

    [Fact]
    public void Ignores_the_token_when_the_delivery_api_is_off()
    {
        _deliveryApi.Enabled = false;
        WithValidToken();

        Assert.False(Service().HasPublicAccess());
    }

    [Fact]
    public void Ignores_the_token_for_media_when_media_is_off()
    {
        _deliveryApi.Media.Enabled = false;
        WithValidToken();

        Assert.False(Service().HasMediaAccess());
    }

    [Fact]
    public void Keeps_accepting_the_api_key()
    {
        _deliveryApi.ApiKey = "secret";
        _context.Request.Headers["Api-Key"] = "secret";
        var service = Service();

        Assert.True(service.HasPublicAccess());
        Assert.True(service.HasPreviewAccess());
        Assert.True(service.HasMediaAccess());
    }

    [Fact]
    public void Refuses_the_wrong_api_key()
    {
        _deliveryApi.ApiKey = "secret";
        _context.Request.Headers["Api-Key"] = "guess";

        Assert.False(Service().HasPublicAccess());
    }

    [Fact]
    public void Keeps_public_access_public()
    {
        _deliveryApi.PublicAccess = true;
        _deliveryApi.Media.PublicAccess = true;
        var service = Service();

        Assert.True(service.HasPublicAccess());
        Assert.True(service.HasMediaAccess());
        Assert.False(service.HasPreviewAccess());
    }
}
