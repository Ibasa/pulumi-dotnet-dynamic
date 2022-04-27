// Copyright 2016-2022, Pulumi Corporation

using System;
using System.IO;

namespace Pulumi.Dynamic
{
    public class DynamicResourceArgs : ResourceArgs
    {
        [Input("__provider", required: true)]
        public Input<string> Provider { get; set; } = null!;
    }

    public class DynamicResource : CustomResource
    {
        private static string GetTypeName()
        {
            // We need to pass the resource type name down to CustomResource, but due to how constructors run in C# this makes it _really_ hard 
            // to get hold of the current type being constructed.

            var stack = new System.Diagnostics.StackTrace(false);

            // Find this method in the stack, then grab the .ctor just above it
            System.Reflection.MethodBase? ctor = null;
            for(int i = 0; i < stack.FrameCount; ++i)
            {
                var frame = stack.GetFrame(i);
                System.Diagnostics.Debug.Assert(frame != null, "StackTrace.GetFrame returned null for an access within bounds");

                var method = frame.GetMethod();
                if (method != null && method.Name == "GetTypeName")
                {
                    // Look up the stack chain for the top .ctor
                    for (; i < stack.FrameCount; ++i)
                    {
                        frame = stack.GetFrame(i + 1);
                        System.Diagnostics.Debug.Assert(frame != null, "StackTrace.GetFrame returned null for an access within bounds");

                        method = frame.GetMethod();
                        if (method is System.Reflection.ConstructorInfo)
                        {
                            ctor = method;
                        }
                        else
                        {
                            break;
                        }
                    }
                    break;
                }
            }

            if (ctor == null)
            {
                throw new Exception("Internal error: could not find resource constructor in stack frames");
            }

            var type = ctor.DeclaringType;
            var typeName = string.IsNullOrEmpty(type.Namespace) ? $"dynamic:{type.Name}" : $"dynamic/{type.Namespace}:{type.Name}"; ;
            return $"pulumi-dotnet:{typeName}";
        }

        private static Ibasa.Pikala.AssemblyPickleMode ByValueFilter(System.Reflection.Assembly assembly)
        {
            // Assemblies known to be used for defining dynamic providers
            var knownAssemblies = new string[] {
              "Pulumi", "System.Collections.Immutable"
          };
            var assemblyName = assembly.GetName().Name;
            if (!Array.Exists(knownAssemblies, name => name == assemblyName))
            {
                return Ibasa.Pikala.AssemblyPickleMode.PickleByValue;
            }
            return Ibasa.Pikala.AssemblyPickleMode.Default;
        }

        private static ResourceArgs SetProvider(ResourceProvider provider, DynamicResourceArgs? args)
        {
            if (args == null)
            {
                args = new DynamicResourceArgs();
            }

            var pickler = new Ibasa.Pikala.Pickler(ByValueFilter);
            var memoryStream = new MemoryStream();
            pickler.Serialize(memoryStream, provider);
            var base64String = Convert.ToBase64String(memoryStream.ToArray());
            args.Provider = base64String;
            return args;
        }


#pragma warning disable RS0022 // Constructor make noninheritable base class inheritable
        public DynamicResource(ResourceProvider provider, string name, DynamicResourceArgs? args, CustomResourceOptions? options = null)
            : base(GetTypeName(), name, SetProvider(provider, args), options)
#pragma warning restore RS0022 // Constructor make noninheritable base class inheritable
        {
        }
    }
}
