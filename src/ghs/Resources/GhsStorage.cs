using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Pulumi;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;

namespace gloomhavensecretariat.Resources;

public class GhsStorageArgs : ResourceArgs {
    [Input("resourceGroupName")] public required Input<string> ResourceGroupName { get; init; }
    [Input("storageAccountName")] public required Input<string> StorageAccountName { get; init; }
}

public class GhsStorage : ComponentResource {
    private const string ComponentName = "azure:ghs:storage";
    private const int ShareQuota = 5120;

    private static readonly string[] FileShares = { "server-config", "caddy-data", "caddy-config" };

    [Output("key")] public Output<string> Key { get; set; }
    [Output("name")] public Output<string> Name { get; set; }
    [Output("volumes")] public Output<ImmutableArray<FileShare>> Volumes { get; set; }

    public GhsStorage(string name, GhsStorageArgs args, ComponentResourceOptions? options = null,
        bool remote = false) :
        base(ComponentName, name, args, options, remote) {
        var encryptionArgs = new EncryptionServiceArgs {
            Enabled = true,
            KeyType = KeyType.Account
        };

        var storageAccount = new StorageAccount("storageAccount", new StorageAccountArgs {
            AccessTier = AccessTier.Hot,
            AccountName = args.StorageAccountName,
            AllowBlobPublicAccess = false,
            AllowSharedKeyAccess = true,
            EnableHttpsTrafficOnly = true,
            Encryption = new EncryptionArgs {
                KeySource = KeySource.Microsoft_Storage,
                RequireInfrastructureEncryption = false,
                Services = new EncryptionServicesArgs {
                    Blob = encryptionArgs,
                    File = encryptionArgs
                }
            },
            Kind = Kind.StorageV2,
            MinimumTlsVersion = MinimumTlsVersion.TLS1_2,
            NetworkRuleSet = new NetworkRuleSetArgs {
                Bypass = Bypass.AzureServices,
                DefaultAction = DefaultAction.Allow
            },
            ResourceGroupName = args.ResourceGroupName,
            Sku = new SkuArgs {
                Name = SkuName.Standard_LRS
            }
        }, new CustomResourceOptions {
            Protect = true
        });

        var storageKeys = ListStorageAccountKeys.Invoke(new ListStorageAccountKeysInvokeArgs {
            ResourceGroupName = args.ResourceGroupName,
            AccountName = storageAccount.Name
        });
        Key = storageKeys.Apply(result => result.Keys[0].Value);

        Volumes = Output.Create(FileShares.Select(fileShare => new FileShare(fileShare, new FileShareArgs {
            AccountName = storageAccount.Name,
            ResourceGroupName = args.ResourceGroupName,
            ShareName = fileShare,
            AccessTier = ShareAccessTier.Hot,
            ShareQuota = ShareQuota
        })).ToImmutableArray());
        Name = storageAccount.Name;

        RegisterOutputs(new Dictionary<string, object?> {
            { "key", Key },
            { "volumes", Volumes },
            { "name", storageAccount.Name }
        });
    }
}