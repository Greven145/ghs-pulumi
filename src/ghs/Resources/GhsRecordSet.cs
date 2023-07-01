using System;
using System.Collections.Generic;
using Pulumi;
using Pulumi.AzureNative.Network;
using Pulumi.AzureNative.Network.Inputs;

namespace gloomhavensecretariat.Resources;

public class GhsRecordSetArgs : ResourceArgs
{
    [Input("domain")] public Input<string> Domain { get; set; } = null!;
    public new static GhsRegistryArgs Empty => new();
    [Input("ipAddress")] public Input<string> IpAddress { get; set; } = null!;

    [Input("resourceGroupName")] public Input<string> ResourceGroupName { get; set; } = "ghs";
}

public class GhsRecordSet : ComponentResource
{
    private const string ComponentName = "azure:ghs:ghsrecordset";
    [Output("fqdn")] public Output<string> Fqdn { get; set; }

    public GhsRecordSet(string name, ComponentResourceOptions? options = null) : this(name, null, options)
    {
    }

    public GhsRecordSet(string name, GhsRecordSetArgs? args, ComponentResourceOptions? options = null,
        bool remote = false) : base(ComponentName, name, args, options, remote)
    {
        var resourceGroupName = args?.ResourceGroupName ?? "ghs";
        var ipAddress = args?.IpAddress ?? throw new ArgumentException("args.IpAddress cannot be null");
        var domain = args?.Domain ?? throw new ArgumentException("args.Domain cannot null");

        var subDomainClient = new RecordSet("subdomain", new RecordSetArgs
        {
            ResourceGroupName = resourceGroupName,
            RecordType = "A",
            ARecords = new[] {
                new ARecordArgs {
                    Ipv4Address = ipAddress
                }
            },
            ZoneName = domain!,
            Ttl = 60,
            RelativeRecordSetName = "ghs"
        });
        Fqdn = subDomainClient.Fqdn;

        RegisterOutputs(new Dictionary<string, object?> {
            { "fqdn", Fqdn }
        });
    }
}