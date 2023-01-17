// Copyright 2016-2022, Pulumi Corporation

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
            void program()
            {
                var dyanmic = new SomeDynamicResource("dyn");
            }

            var args = new Automation.InlineProgramArgs("Test", "test", Automation.PulumiFn.Create(program));
            var stack = await Automation.LocalWorkspace.CreateStackAsync(args);
            try
            {
                var up = await stack.UpAsync();

                System.Console.WriteLine(up);
            }
            finally
            {
                stack.Dispose();
            }
        }
    }
}