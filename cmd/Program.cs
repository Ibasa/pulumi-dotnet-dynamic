﻿// Copyright 2016-2023, Pulumi Corporation
using System.Collections.Immutable;
using Pulumi.Experimental.Dynamic;
using Pulumi.Experimental.Provider;

class DynamicResourceProvider : Provider
{
    private readonly IHost host;

    public DynamicResourceProvider(IHost host)
    {
        this.host = host;
    }

    private static (PropertyValue, DynamicProvider) GetProvider(ImmutableDictionary<string, PropertyValue> properties)
    {
        if (!properties.TryGetValue("__provider", out var providerValue))
        {
            throw new Exception("Dynamic resource had no '__provider' property");
        }

        if (providerValue.IsComputed)
        {
            throw new Exception("Dynamic resource '__provider' property was unknown");
        }

        if (!providerValue.TryGetString(out var providerString))
        {
            throw new Exception("Dynamic resource '__provider' property was not a string");
        }

        var pickler = new Ibasa.Pikala.Pickler();
        var memoryStream = new MemoryStream(Convert.FromBase64String(providerString!));
        var providerObject = pickler.Deserialize(memoryStream);
        var provider = providerObject as DynamicProvider;
        if (provider == null)
        {
            throw new Exception(string.Format("Dynamic resource could not deserialise provider implementation: {0}", providerObject));
        }
        return (providerValue, provider);
    }

    public override Task<CheckResponse> CheckConfig(CheckRequest request, CancellationToken ct)
    {
        if (request.News.Count != 0)
        {
            throw new Exception(string.Format("Config is not supported by dynamic providers, got: {0}", request.News));
        }

        return Task.FromResult(new CheckResponse()
        {
            Inputs = request.News,
        });
    }

    public override Task<DiffResponse> DiffConfig(DiffRequest request, CancellationToken ct)
    {
        if (request.News.Count != 0)
        {
            throw new Exception(string.Format("Config is not supported by dynamic providers, got: {0}", request.News));
        }

        return Task.FromResult(new DiffResponse());
    }

    public override Task<InvokeResponse> Invoke(InvokeRequest request, CancellationToken ct)
    {
        throw new Exception("Invoke is not supported by dynamic providers");
    }

    public override Task<GetSchemaResponse> GetSchema(GetSchemaRequest request, CancellationToken ct)
    {
        throw new Exception("GetSchema is not supported by dynamic providers");
    }

    public override Task<ConfigureResponse> Configure(ConfigureRequest request, CancellationToken ct)
    {
        if (request.Args.Count != 0)
        {
            throw new Exception(string.Format("Config is not supported by dynamic providers, got: {0}", request.Args));
        }

        var response = new ConfigureResponse();
        response.AcceptSecrets = false;
        response.AcceptOutputs = false;
        response.SupportsPreview = false;
        return Task.FromResult(response);
    }

    public override Task<GetPluginInfoResponse> GetPluginInfo(CancellationToken ct)
    {
        var response = new GetPluginInfoResponse();
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        if (version == null)
        {
            response.Version = "0.0.1";
        }
        else
        {
            response.Version = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Revision);
        }
        return Task.FromResult(response);
    }

    private static IDictionary<string, PropertyValue> AddProvider(IDictionary<string, PropertyValue>? properties, PropertyValue provider)
    {
        var newDictionary = properties == null ? ImmutableDictionary<string, PropertyValue>.Empty : ImmutableDictionary.CreateRange(properties);
        return newDictionary.Add("__provider", provider);
    }

    public override async Task<CreateResponse> Create(CreateRequest request, CancellationToken ct)
    {
        var (providerValue, provider) = GetProvider(request.Properties);
        var response = await provider.Create(request, ct);
        response.Properties = AddProvider(response.Properties, providerValue);
        return response;
    }

    public override Task<ReadResponse> Read(ReadRequest request, CancellationToken ct)
    {
        throw new Exception("Read is not supported by dynamic providers");
    }

    public override async Task<CheckResponse> Check(CheckRequest request, CancellationToken ct)
    {
        var (providerValue, provider) = GetProvider(request.News);
        var response = await provider.Check(request, ct);
        response.Inputs = AddProvider(response.Inputs, providerValue);
        return response;
    }

    public override async Task<DiffResponse> Diff(DiffRequest request, CancellationToken ct)
    {
        var (_, provider) = GetProvider(request.News);
        var response = await provider.Diff(request, ct);
        return response;
    }

    public override async Task<UpdateResponse> Update(UpdateRequest request, CancellationToken ct)
    {
        var (providerValue, provider) = GetProvider(request.News);
        var response = await provider.Update(request, ct);
        response.Properties = AddProvider(response.Properties, providerValue);
        return response;
    }

    public override async Task Delete(DeleteRequest request, CancellationToken ct)
    {
        var (_, provider) = GetProvider(request.Properties);
        await provider.Delete(request, ct);
    }
}

public static class Program
{
    public static async Task Main(string[] args)
    {
        await Provider.Serve(args, host => new DynamicResourceProvider(host), CancellationToken.None);
    }
}