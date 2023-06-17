using System.Collections.Immutable;
using Pulumi.Experimental.Provider;
using Ibasa.Pulumi.Dynamic;

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

    private static void CheckProperties(ImmutableDictionary<string, PropertyValue> properties)
    {
        // Throw if we get any properties except "version"
        if (properties.Count == 0)
        {
            return;
        }
        if (properties.Count == 1 && properties.Single().Key == "version")
        {
            return;
        }

        var values = string.Join(", ", properties.Select(p => string.Format("{0} = {1}", p.Key, p.Value.ToString())));
        throw new Exception(string.Format("Config is not supported by dynamic providers, got: {0}", values));
    }

    public override Task<CheckResponse> CheckConfig(CheckRequest request, CancellationToken ct)
    {
        CheckProperties(request.NewInputs);

        return Task.FromResult(new CheckResponse()
        {
            Inputs = request.NewInputs,
        });
    }

    public override Task<DiffResponse> DiffConfig(DiffRequest request, CancellationToken ct)
    {
        CheckProperties(request.NewInputs);

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
        CheckProperties(request.Args);

        var response = new ConfigureResponse();
        response.AcceptSecrets = false;
        response.AcceptOutputs = false;
        response.SupportsPreview = false;
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
        var (providerValue, provider) = GetProvider(request.NewInputs);
        var response = await provider.Check(request, ct);
        response.Inputs = AddProvider(response.Inputs, providerValue);
        return response;
    }

    public override async Task<DiffResponse> Diff(DiffRequest request, CancellationToken ct)
    {
        var (_, provider) = GetProvider(request.NewInputs);
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
        // Make sure this matches the assembly version
        await Provider.Serve(args, "0.0.1", host => new DynamicResourceProvider(host), CancellationToken.None);
    }
}