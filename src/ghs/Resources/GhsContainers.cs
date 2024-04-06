using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Pulumi;
using Pulumi.AzureNative.ContainerInstance;
using Pulumi.AzureNative.ContainerInstance.Inputs;
using Pulumi.Random;
using FileShare = Pulumi.AzureNative.Storage.FileShare;

namespace gloomhavensecretariat.Resources;

public class GhsContainerGroupArgs : ResourceArgs {
    [Input("containerPorts")] public required Input<int[]> ContainerPorts { get; init; }
    [Input("cpu")] public required Input<double> Cpu { get; init; }
    [Input("domain")] public required Input<string?> Domain { get; init; }
    [Input("groupName")] public required Input<string> GroupName { get; init; }
    [Input("image")] public required Input<GhsImage> Image { get; init; }
    [Input("location")] public required Input<string> Location { get; init; }
    [Input("memory")] public required Input<double> Memory { get; init; }
    [Input("registry")] public required Input<GhsRegistry> Registry { get; init; }
    [Input("resourceGroupName")] public required Input<string> ResourceGroupName { get; init; }
    [Input("storage")] public required Input<GhsStorage> Storage { get; init; }
}

public class GhsContainerGroup : ComponentResource {
    private const string ComponentName = "azure:ghs:containergroup";

    private static readonly Dictionary<string, string> VolumeMaps = new() {
        { "caddy-data", "/data" },
        { "caddy-config", "/config" },
        { "server-config", "/root/.ghs/" }
    };

    [Output("fqdn")] public Output<string> Fqdn { get; set; }

    [Output("ipAddress")] public Output<string> IpAddress { get; set; }

    public GhsContainerGroup(string name, GhsContainerGroupArgs args, ComponentResourceOptions? options = null,
        bool remote = false) : base(ComponentName, name, args, options, remote) {
        // Use a random string to give the service a unique DNS name.
        var dnsName = new RandomString("dns-name", new RandomStringArgs {
            Length = 8,
            Special = false
        }).Result.Apply(result => $"{result.ToLower()}");

        var domain = args.Domain is not null
            ? args.Domain.Apply(d => $"ghs.{d}")
            : Output.Format($"{args.GroupName}-{dnsName}.${args.Location}.azurecontainer.io");

        var containerGroup = new ContainerGroup("container-group", new ContainerGroupArgs {
            ResourceGroupName = args.ResourceGroupName,
            ContainerGroupName = Output.Format($"{args.GroupName}-ghs"),
            OsType = OperatingSystemTypes.Linux,
            RestartPolicy = ContainerGroupRestartPolicy.Always,
            ImageRegistryCredentials = CredentialsFromRegistry(args.Registry),
            Sku = ContainerGroupSku.Standard,
            Volumes = args.Storage.Apply(s => s.Volumes.Apply(v => v.Select(VolumeFromFileShare(s)).ToList())),
            Containers = new[] {
                new ContainerArgs {
                    Name = "ghs-with-caddy",
                    Image = args.Image.Apply(i => i.ImageName),
                    Ports = args.ContainerPorts.Apply(ports => ports.Select(ContainerPortFromInt)),
                    Resources = new ResourceRequirementsArgs {
                        Requests = new ResourceRequestsArgs {
                            Cpu = args!.Cpu,
                            MemoryInGB = args.Memory
                        }
                    },
                    VolumeMounts = args.Storage.Apply(storage => storage.Volumes.Apply(FileSharesToVolumes)),
                    EnvironmentVariables = new EnvironmentVariableArgs[] {
                        new() {
                            Name = "HOST_NAME",
                            Value = domain
                        }
                    }
                }
            },
            IpAddress = new IpAddressArgs {
                Type = ContainerGroupIpAddressType.Public,
                DnsNameLabel = Output.Format($"{args.GroupName}-{dnsName}"),
                Ports = args.ContainerPorts.Apply(ports => ports.Select(port => new PortArgs {
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

    private static Func<FileShare, VolumeArgs> VolumeFromFileShare(GhsStorage s) =>
        fileShare => new VolumeArgs {
            AzureFile = new AzureFileVolumeArgs {
                ShareName = fileShare.Name,
                StorageAccountName = s.Name,
                StorageAccountKey = s.Key
            },
            Name = fileShare.Name
        };

    private static ImageRegistryCredentialArgs CredentialsFromRegistry(Input<GhsRegistry> registry) =>
        new() {
            Server =   registry.Apply(r => r.LoginServer),
            Username = registry.Apply(r => r.UserName),
            Password = registry.Apply(r => r.Password)
        };
    private static ContainerPortArgs ContainerPortFromInt(int port) =>
        new() { Port = port, Protocol = ContainerNetworkProtocol.TCP };
    private static VolumeMountArgs VolumeMountFromFileShare(Pulumi.AzureNative.Storage.FileShare fs) =>
        new() { MountPath = fs.Name.Apply(n => VolumeMaps[n]), Name = fs.Name.Apply(n => n), ReadOnly = false };
    private static List<VolumeMountArgs> FileSharesToVolumes(ImmutableArray<FileShare> fs) =>
        fs.Select(VolumeMountFromFileShare).ToList();
}