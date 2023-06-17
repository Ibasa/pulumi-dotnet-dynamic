using Pulumi;

namespace Ibasa.Pulumi.Dynamic
{
    public class DynamicResourceArgs : ResourceArgs
    {
        [Input("__provider", required: true)]
        internal Input<string> Provider { get; set; } = null!;
    }

    public class DynamicResource<T> : CustomResource where T : DynamicResourceArgs, new()
    {
        private static string GetTypeName(string? module, string type)
        {
            module = module == null ? "index" : $"index/{module}";
            return $"dotnet-dynamic:{module}:{type}";
        }

        private static Ibasa.Pikala.AssemblyPickleMode ByValueFilter(System.Reflection.Assembly assembly)
        {
            // Assemblies known to be used for defining dynamic providers
            var knownAssemblies = new string[] {
              "Pulumi", "Ibasa.Pulumi.Dynamic", "System.Collections.Immutable"
          };
            var assemblyName = assembly.GetName().Name;
            if (!Array.Exists(knownAssemblies, name => name == assemblyName))
            {
                return Ibasa.Pikala.AssemblyPickleMode.PickleByValue;
            }
            return Ibasa.Pikala.AssemblyPickleMode.Default;
        }

        private static ResourceArgs SetProvider(DynamicProvider provider, T? args)
        {
            if (args == null)
            {
                args = new T();
            }

            var pickler = new Ibasa.Pikala.Pickler(ByValueFilter);
            var memoryStream = new MemoryStream();
            pickler.Serialize(memoryStream, provider);
            var base64String = Convert.ToBase64String(memoryStream.ToArray());
            args.Provider = base64String;
            return args;
        }

        private static CustomResourceOptions SetDefaultOptions(CustomResourceOptions? options)
        {
            options = options ?? new CustomResourceOptions();
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (version == null)
            {
                throw new Exception("Expected assembly version to be set");
            }
            options.Version = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
            options.PluginDownloadURL = "github://api.github.com/Ibasa";

            return options;
        }

        public DynamicResource(DynamicProvider provider, string name, T? args, CustomResourceOptions? options = null, string? module = null, string type = "Resource")
            : base(GetTypeName(module, type), name, SetProvider(provider, args), SetDefaultOptions(options))
        {
        }
    }
}
