using gloomhavensecretariat.Resources;
using Pulumi;
using Pulumi.AzureNative.Resources;

namespace gloomhavensecretariat;

public class GhsStack : Stack {
    [Output("domain")] public Output<string> Domain { get; set; } = Output.Create(string.Empty);
    [Output("hostname")] public Output<string> HostName { get; set; }
    [Output("url")] public Output<string> Url { get; set; }

    public GhsStack() {
        var config = new Config();
        var appName = config.Get("appName") ?? "ghs";
        var registryName = config.Get("registryName") ?? "ghsreg";
        var containerPorts = config.GetObject<int[]>("containerPorts") ?? new[] { 80 };
        var cpu = config.GetDouble("cpu") ?? 1;
        var memory = config.GetDouble("memory") ?? 2;
        var imageTag = config.Get("imageTag") ?? "latest";
        var domain = config.Get("domain");

        var resourceGroup = new ResourceGroup("resourcegroup", new ResourceGroupArgs {
            ResourceGroupName = appName
        });

        var registry = new GhsRegistry("registry", new GhsRegistryArgs {
            ResourceGroupName = resourceGroup.Name,
            RegistryName = registryName
        });

        var image = new GhsImage("ghs-with-caddy", new GhsImageArgs {
            AppName = appName,
            ImageTag = imageTag,
            Registry = registry
        });

        var storageAccount = new GhsStorage("storageAccount", new GhsStorageArgs {
            ResourceGroupName = resourceGroup.Name,
            StorageAccountName = appName
        });

        var containerGroup = new GhsContainerGroup("container-group", new GhsContainerGroupArgs {
            ResourceGroupName = resourceGroup.Name,
            ContainerPorts = containerPorts,
            Cpu = cpu,
            Memory = memory,
            Image = image,
            GroupName = appName,
            Registry = registry,
            Storage = storageAccount,
            Location = resourceGroup.Location,
            Domain = domain!
        });

        if (domain is not null) {
            var subDomainClient = new GhsRecordSet("subdomain-client", new GhsRecordSetArgs {
                ResourceGroupName = resourceGroup.Name,
                IpAddress = containerGroup.IpAddress.Apply(ip => ip),
                Domain = domain
            });
            Domain = subDomainClient.Fqdn;
        }

        HostName = containerGroup.Fqdn.Apply(fqdn => fqdn);
        Url = containerGroup.IpAddress.Apply(ip => $"http://{ip}:443");
    }
}