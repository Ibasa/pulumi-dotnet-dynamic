using Pulumi;
using Pulumi.Experimental.Dynamic;


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
