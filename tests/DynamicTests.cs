using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Pulumi.Automation;

namespace Ibasa.Pulumi.Dynamic.Test
{
    sealed class DynamicResourceProvider : DynamicProvider
    {

    }

    sealed class SomeDynamicArgs : DynamicResourceArgs
    {
    }

    sealed class SomeDynamicResource : DynamicResource<SomeDynamicArgs>
    {
        private static DynamicResourceProvider provider = new DynamicResourceProvider();

        public SomeDynamicResource(string name) : base(provider, name, null)
        {

        }
    }

    public class DynamicTests
    {
        [Fact]
        public async Task TestSimple()
        {
            Dictionary<string, object?> program()
            {
                var res = new SomeDynamicResource("dyn");
                return new Dictionary<string, object?>(new[]
                {
                    KeyValuePair.Create("output", (object?)res.Urn)
                });
            }

            var args = new InlineProgramArgs("DotnetDynamic", "TestSimple", PulumiFn.Create(program));
            using var stack = await LocalWorkspace.CreateOrSelectStackAsync(args);
            var up = await stack.UpAsync();
        }
    }
}