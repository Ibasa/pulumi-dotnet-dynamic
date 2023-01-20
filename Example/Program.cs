// Copyright 2016-2023, Pulumi Corporation

using Pulumi;
using Pulumi.Experimental.Dynamic;
using Pulumi.Experimental.Provider;

await Deployment.RunAsync(() =>
{
    var args = new SomeDynamicArgs()
    {
        Value = "hello world"
    };

    var d = new SomeDynamicResource("my-resource", args);

    return new Dictionary<string, object?>() {
        {
            "Count", d.Result
        }
    };
});


sealed class SomeDynamicResourceProvider : DynamicProvider
{
    public override Task<CreateResponse> Create(CreateRequest request, CancellationToken ct)
    {
        if (request.Type == "dotnet-dynamic:index:SomeDynamicResource")
        {
            if (!request.Properties["value"].TryGetString(out var valueString))
            {
                throw new Exception("value should be a string");
            }

            var properties = new Dictionary<string, PropertyValue>
            {
                { "result", new PropertyValue(valueString!.Length) }
            };

            return Task.FromResult(new CreateResponse()
            {
                Id = valueString,
                Properties = properties,
            });
        }
        throw new Exception("Unknown resource type: " + request.Type);
    }

    public override Task<UpdateResponse> Update(UpdateRequest request, CancellationToken ct)
    {
        if (request.Type == "dotnet-dynamic:index:SomeDynamicResource")
        {
            if (!request.News["value"].TryGetString(out var valueString))
            {
                throw new Exception("value should be a string");
            }

            var properties = new Dictionary<string, PropertyValue>
            {
                { "result", new PropertyValue(valueString!.Length) }
            };

            return Task.FromResult(new UpdateResponse()
            {
                Properties = properties,
            });
        }
        throw new Exception("Unknown resource type: " + request.Type);
    }
}

sealed class SomeDynamicArgs : DynamicResourceArgs
{
    [Input("value", required: true)]
    public Input<string> Value { get; set; } = null!;
}

sealed class SomeDynamicResource : DynamicResource<SomeDynamicArgs>
{
    private static SomeDynamicResourceProvider provider = new SomeDynamicResourceProvider();

    public SomeDynamicResource(string name, SomeDynamicArgs? args = null, CustomResourceOptions? options = null)
        : base(provider, name, args, options, null, "SomeDynamicResource")
    {
    }

    [Output("result")]
    public Output<int> Result { get; set; } = null!;
}
