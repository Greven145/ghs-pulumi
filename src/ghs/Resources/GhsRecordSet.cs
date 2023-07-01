using System.Collections.Generic;
using Pulumi;
using Pulumi.AzureNative.Network;
using Pulumi.AzureNative.Network.Inputs;

namespace gloomhavensecretariat.Resources;

public class GhsRecordSetArgs : ResourceArgs {
    [Input("domain")] public required Input<string> Domain { get; init; }
    [Input("ipAddress")] public required Input<string> IpAddress { get; init; }
    [Input("resourceGroupName")] public required Input<string> ResourceGroupName { get; init; }
}

public class GhsRecordSet : ComponentResource {
    private const string ComponentName = "azure:ghs:ghsrecordset";
    [Output("fqdn")] public Output<string> Fqdn { get; set; }

    public GhsRecordSet(string name, GhsRecordSetArgs args, ComponentResourceOptions? options = null,
        bool remote = false) : base(ComponentName, name, args, options, remote) {
        var subDomainClient = new RecordSet("subdomain", new RecordSetArgs {
            ResourceGroupName = args.ResourceGroupName,
            RecordType = "A",
            ARecords = new[] {
                new ARecordArgs {
                    Ipv4Address = args.IpAddress
                }
            },
            ZoneName = args.Domain,
            Ttl = 60,
            RelativeRecordSetName = "ghs"
        });
        Fqdn = subDomainClient.Fqdn;

        RegisterOutputs(new Dictionary<string, object?> {
            { "fqdn", Fqdn }
        });
    }
}