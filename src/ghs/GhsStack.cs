using gloomhavensecretariat.Resources;
using Pulumi;
using Pulumi.AzureNative.Resources;

namespace gloomhavensecretariat;

public class GhsStack : Stack {
    [Output("domain")] public Output<string> Domain { get; set; } = Output.Create(string.Empty);
    [Output("hostname")] public Output<string> HostName { get; set; }
    [Output("url")] public Output<string> Url { get; set; }
    [Output("imageTag")] public Output<string> ImageTag { get; set; }

    public GhsStack() {
        var config = new GhsConfig(new Config());

        var resourceGroup = new ResourceGroup("resourcegroup", new ResourceGroupArgs {
            ResourceGroupName = config.AppName
        });

        var registry = new GhsRegistry("registry", new GhsRegistryArgs {
            ResourceGroupName = resourceGroup.Name,
            RegistryName = config.RegistryName
        });

        var image = new GhsImage("ghs-with-caddy", new GhsImageArgs {
            AppName = config.AppName,
            ImageTag = config.ImageTag,
            Registry = registry
        });

        var storageAccount = new GhsStorage("storageAccount", new GhsStorageArgs {
            ResourceGroupName = resourceGroup.Name,
            StorageAccountName = config.AppName
        });

        var containerGroup = new GhsContainerGroup("container-group", new GhsContainerGroupArgs {
            Image = image,
            Registry = registry,
            Storage = storageAccount,
            Location = resourceGroup.Location,
            ResourceGroupName = resourceGroup.Name,
            ContainerPorts = config.ContainerPorts,
            Cpu = config.Cpu,
            Memory = config.Memory,
            GroupName = config.AppName,
            Domain = config.Domain
        });

        if (config.Domain is not null) {
            var subDomainClient = new GhsRecordSet("subdomain-client", new GhsRecordSetArgs {
                ResourceGroupName = resourceGroup.Name,
                HostName = containerGroup.Fqdn,
                Domain = config.Domain
            });
            Domain = subDomainClient.Fqdn;
        }

        HostName = containerGroup.Fqdn.Apply(fqdn => fqdn);
        Url = containerGroup.IpAddress.Apply(ip => $"http://{ip}:443");
        ImageTag = image.ImageName.Apply(name => name);
    }
}