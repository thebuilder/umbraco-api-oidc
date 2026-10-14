using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using TheBuilder.ApiOidc.Configuration;
using TheBuilder.ApiOidc.DeliveryApi;
using TheBuilder.ApiOidc.Tokens;
using Umbraco.Cms.Core.Configuration.Models;

namespace TheBuilder.ApiOidc.Tests;

public sealed class DeliveryApiEndpointGateTests
{
    private const string DictionaryPath = "/umbraco/delivery/api/v1/dictionary";

    private readonly DefaultHttpContext _context = new();
    private readonly DeliveryApiSettings _deliveryApi = new() { Enabled = true, PublicAccess = false, ApiKey = "secret" };
    private ApiOidcOptions _options = FakeIssuers.Options(FakeIssuers.Vercel());
    private bool _nextCalled;

    private async Task<int> Send(string path = DictionaryPath, params object[] metadata)
    {
        _context.Request.Path = path;
        _context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(metadata), "test"));

        var gate = new DeliveryApiEndpointGate(
            _ =>
            {
                _nextCalled = true;
                return Task.CompletedTask;
            },
            new TestOptionsMonitor<ApiOidcOptions>(_options),
            new OidcApiAccessService(
                new HttpContextAccessor { HttpContext = _context },
                new TestOptionsMonitor<DeliveryApiSettings>(_deliveryApi)));

        await gate.InvokeAsync(_context);
        return _context.Response.StatusCode;
    }

    private static ControllerActionDescriptor ControllerFrom(Type type) => new() { ControllerTypeInfo = type.GetTypeInfo() };

    private static Type TypeFromAssembly(string assemblyName) =>
        AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run)
            .DefineDynamicModule(assemblyName)
            .DefineType("TestController", TypeAttributes.Public)
            .CreateType();

    [Fact]
    public async Task Refuses_a_custom_endpoint_without_credentials()
    {
        Assert.Equal(StatusCodes.Status401Unauthorized, await Send());
        Assert.False(_nextCalled);
    }

    [Fact]
    public async Task Serves_a_custom_endpoint_with_a_valid_token()
    {
        _context.Features.Set(new OidcAccessFeature(new OidcIdentity("Vercel", "https://oidc.vercel.com/acme", "owner:acme", true)));

        await Send();

        Assert.True(_nextCalled);
    }

    [Fact]
    public async Task Serves_a_custom_endpoint_with_the_api_key()
    {
        _context.Request.Headers["Api-Key"] = "secret";

        await Send();

        Assert.True(_nextCalled);
    }

    [Fact]
    public async Task Serves_a_custom_endpoint_when_the_delivery_api_is_public()
    {
        _deliveryApi.PublicAccess = true;

        await Send();

        Assert.True(_nextCalled);
    }

    [Fact]
    public async Task Leaves_umbracos_own_endpoints_to_umbraco()
    {
        // Umbraco checks preview and member access on its own controllers, so the gate stays out of the way.
        await Send("/umbraco/delivery/api/v1/security/member/authorize", ControllerFrom(TypeFromAssembly("Umbraco.Cms.Api.Delivery")));

        Assert.True(_nextCalled);
    }

    [Fact]
    public async Task Gates_a_community_package_with_an_umbraco_prefixed_name()
    {
        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            await Send(DictionaryPath, ControllerFrom(TypeFromAssembly("Umbraco.Community.DictionaryApi"))));
    }

    [Fact]
    public async Task Can_be_turned_off()
    {
        _options.ProtectCustomDeliveryApiEndpoints = false;

        await Send();

        Assert.True(_nextCalled);
    }

    [Fact]
    public async Task Gates_a_custom_controller()
    {
        Assert.Equal(StatusCodes.Status401Unauthorized, await Send(DictionaryPath, ControllerFrom(typeof(DeliveryApiEndpointGateTests))));
    }

    [Fact]
    public async Task Lets_an_endpoint_opt_out_with_allow_anonymous()
    {
        await Send(DictionaryPath, new AllowAnonymousAttribute());

        Assert.True(_nextCalled);
    }

    [Fact]
    public async Task Lets_cors_preflights_through()
    {
        _context.Request.Method = HttpMethods.Options;
        _context.Request.Headers["Access-Control-Request-Method"] = "GET";

        await Send();

        Assert.True(_nextCalled);
    }

    [Fact]
    public async Task Ignores_endpoints_outside_the_delivery_api()
    {
        await Send("/api/search");

        Assert.True(_nextCalled);
    }

    [Fact]
    public async Task Stays_off_when_no_issuers_are_configured()
    {
        _options = new ApiOidcOptions();

        await Send();

        Assert.True(_nextCalled);
    }
}
