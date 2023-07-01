using System;
using System.Collections.Generic;
using System.Linq;
using Pulumi;
using Pulumi.AzureNative.ContainerInstance;
using Pulumi.AzureNative.ContainerInstance.Inputs;
using Pulumi.Random;

namespace gloomhavensecretariat.Resources;

public class GhsContainerGroupArgs : ResourceArgs
{
    [Input("containerPorts")] public Input<int[]> ContainerPorts { get; set; } = new[] { 80, 443, 2019 };
    [Input("cpu")] public Input<double> Cpu { get; set; } = 1;
    public new static GhsRegistryArgs Empty => new();
    [Input("groupName")] public Input<string> GroupName { get; set; } = "ghs";
    [Input("image")] public Input<GhsImage>? Image { get; set; } = null!;
    [Input("memory")] public Input<double> Memory { get; set; } = 1;
    [Input("registry")] public Input<GhsRegistry>? Registry { get; set; } = null!;

    [Input("resourceGroupName")] public Input<string> ResourceGroupName { get; set; } = "ghs";
    [Input("storage")] public Input<GhsStorage>? Storage { get; set; } = null!;
    [Input("domain")] public Input<string>? Domain { get; set; } = null!;
    [Input("location")] public Input<string>? Location { get; set; } = "CanadaCentral";
}

public class GhsContainerGroup : ComponentResource
{
    private const string ComponentName = "azure:ghs:containergroup";

    private static readonly Dictionary<string, string> VolumeMaps = new() {
        { "caddy-data", "/data" },
        { "caddy-config", "/config" },
        { "server-config", "/root/.ghs/" }
    };

    [Output("fqdn")] public Output<string> Fqdn { get; set; }

    [Output("ipAddress")] public Output<string> IpAddress { get; set; }

    public GhsContainerGroup(string name, ComponentResourceOptions? options = null) : this(name, null, options)
    {
    }

    public GhsContainerGroup(string name, GhsContainerGroupArgs? args, ComponentResourceOptions? options = null,
        bool remote = false) : base(ComponentName, name, args, options, remote)
    {
        var resourceGroupName = args?.ResourceGroupName ?? "ghs";
        var groupName = args?.GroupName ?? "ghs";
        var registry = args?.Registry ?? throw new Exception("Registry is required");
        var storage = args?.Storage ?? throw new Exception("Registry is required");
        var containerPorts = args?.ContainerPorts ?? new[] { 80, 443, 2019 };
        var image = args?.Image ?? throw new Exception("Image is required");
        var location = args?.Location ?? "CanadaCentral";
        
        // Use a random string to give the service a unique DNS name.
        var dnsName = new RandomString("dns-name", new RandomStringArgs
        {
            Length = 8,
            Special = false
        }).Result.Apply(result => $"{result.ToLower()}");

        var domain = args?.Domain is not null
            ? args.Domain.Apply(d => $"ghs.{d}")
            : Output.Format($"{groupName}-{dnsName}.${location}.azurecontainer.io");

        var containerGroup = new ContainerGroup("container-group", new ContainerGroupArgs
        {
            ResourceGroupName = resourceGroupName,
            ContainerGroupName = Output.Format($"{groupName}-ghs"),
            OsType = OperatingSystemTypes.Linux,
            RestartPolicy = ContainerGroupRestartPolicy.Always,
            ImageRegistryCredentials = new ImageRegistryCredentialArgs
            {
                Server = registry.Apply(r => r.LoginServer),
                Username = registry.Apply(r => r.UserName),
                Password = registry.Apply(r => r.Password)
            },
            Sku = ContainerGroupSku.Standard,
            Volumes = storage.Apply(s =>
            {
                return s.Volumes.Apply(v => v.Select(fileShare => new VolumeArgs
                {
                    AzureFile = new AzureFileVolumeArgs
                    {
                        ShareName = fileShare.Name,
                        StorageAccountName = s.Name,
                        StorageAccountKey = s.Key
                    },
                    Name = fileShare.Name
                }).ToList());
            }),
            Containers = new[] {
                new ContainerArgs {
                    Name = "ghs-with-caddy",
                    Image = image.Apply(i => i.ImageName),
                    Ports = containerPorts.Apply(ports => ports.Select(port => new ContainerPortArgs {
                        Port = port,
                        Protocol = ContainerNetworkProtocol.TCP
                    })),
                    Resources = new ResourceRequirementsArgs {
                        Requests = new ResourceRequestsArgs {
                            Cpu = args!.Cpu,
                            MemoryInGB = args.Memory
                        }
                    },
                    VolumeMounts = storage.Apply(s => s.Volumes.Apply(v => v.Select(
                        fileShare => new VolumeMountArgs {
                            MountPath = fileShare.Name.Apply(n => VolumeMaps[n]),
                            Name = fileShare.Name.Apply(n => n),
                            ReadOnly = false
                        }).ToList())),
                    EnvironmentVariables = new EnvironmentVariableArgs[] {
                        new() {
                            Name = "HOST_NAME",
                            Value = domain
                        }
                    }
                }
            },
            IpAddress = new IpAddressArgs
            {
                Type = ContainerGroupIpAddressType.Public,
                DnsNameLabel = Output.Format($"{groupName}-{dnsName}"),
                Ports = containerPorts.Apply(ports => ports.Select(port => new PortArgs
                {
                    Port = port,
                    Protocol = ContainerGroupNetworkProtocol.TCP
                }))
            }
        }, new CustomResourceOptions { ReplaceOnChanges = new List<string> { "containers[0].resources.requests.*" } });

        IpAddress = containerGroup.IpAddress.Apply(i => i?.Ip!);
        Fqdn = containerGroup.IpAddress.Apply(i => i!.Fqdn);

        RegisterOutputs(new Dictionary<string, object?> {
            { "ipAddress", IpAddress },
            { "fqdn", Fqdn }
        });
    }
}