// Copyright 2016-2022, Pulumi Corporation

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Pulumi.Experimental.Dynamic.Test
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

            var args = new Automation.InlineProgramArgs("DotnetDynamic", "TestSimple", Automation.PulumiFn.Create(program));
            using var stack = await Automation.LocalWorkspace.CreateOrSelectStackAsync(args);
            var up = await stack.UpAsync();
        }
    }
}