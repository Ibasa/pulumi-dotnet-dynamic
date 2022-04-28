// Copyright 2016-2022, Pulumi Corporation

using System.Threading.Tasks;
using Xunit;

namespace Pulumi.Dynamic.Test
{
    sealed class SomeDynamicResourceProvider : DynamicResourceProvider { 
    
    }


    public sealed class SomeDynamicResource : DynamicResource
    {
        
        private static DynamicResourceProvider provider = new SomeDynamicResourceProvider();

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