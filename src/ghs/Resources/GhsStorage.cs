using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Pulumi;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;

namespace gloomhavensecretariat.Resources;

public class GhsStorageArgs : ResourceArgs
{
    public new static GhsRegistryArgs Empty => new();

    [Input("resourceGroupName")] public Input<string> ResourceGroupName { get; set; } = "ghs";
    [Input("storageAccountName")] public Input<string> StorageAccountName { get; set; } = "ghs";
}

public class GhsStorage : ComponentResource
{
    private const string ComponentName = "azure:ghs:storage";
    private const int ShareQuota = 5120;

    private static readonly string[] FileShares = { "server-config", "caddy-data", "caddy-config" };

    [Output("key")] public Output<string> Key { get; set; }
    [Output("name")] public Output<string> Name { get; set; }
    [Output("volumes")] public Output<ImmutableArray<FileShare>> Volumes { get; set; }

    public GhsStorage(string name, ComponentResourceOptions? options = null) : this(name, null, options)
    {
    }

    public GhsStorage(string name, GhsStorageArgs? args, ComponentResourceOptions? options = null,
        bool remote = false) :
        base(ComponentName, name, args, options, remote)
    {
        var resourceGroupName = args?.ResourceGroupName ?? "ghs";
        Name = args?.StorageAccountName ?? "ghs";

        var encryptionArgs = new EncryptionServiceArgs
        {
            Enabled = true,
            KeyType = KeyType.Account
        };

        var storageAccount = new StorageAccount("storageAccount", new StorageAccountArgs
        {
            AccessTier = AccessTier.Hot,
            AccountName = Name,
            AllowBlobPublicAccess = false,
            AllowSharedKeyAccess = true,
            EnableHttpsTrafficOnly = true,
            Encryption = new EncryptionArgs
            {
                KeySource = KeySource.Microsoft_Storage,
                RequireInfrastructureEncryption = false,
                Services = new EncryptionServicesArgs
                {
                    Blob = encryptionArgs,
                    File = encryptionArgs
                }
            },
            Kind = Kind.StorageV2,
            MinimumTlsVersion = MinimumTlsVersion.TLS1_2,
            NetworkRuleSet = new NetworkRuleSetArgs
            {
                Bypass = Bypass.AzureServices,
                DefaultAction = DefaultAction.Allow
            },
            ResourceGroupName = resourceGroupName,
            Sku = new SkuArgs
            {
                Name = SkuName.Standard_LRS
            }
        }, new CustomResourceOptions
        {
            Protect = true
        });

        var storageKeys = ListStorageAccountKeys.Invoke(new ListStorageAccountKeysInvokeArgs
        {
            ResourceGroupName = resourceGroupName,
            AccountName = storageAccount.Name
        });
        Key = storageKeys.Apply(result => result.Keys[0].Value);

        Volumes = Output.Create(FileShares.Select(fileShare => new FileShare(fileShare, new FileShareArgs
        {
            AccountName = storageAccount.Name,
            ResourceGroupName = resourceGroupName,
            ShareName = fileShare,
            AccessTier = ShareAccessTier.Hot,
            ShareQuota = ShareQuota
        })).ToImmutableArray());


        RegisterOutputs(new Dictionary<string, object?> {
            { "key", Key },
            { "volumes", Volumes },
            { "name", storageAccount.Name }
        });
    }
}