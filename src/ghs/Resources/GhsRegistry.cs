using System.Collections.Generic;
using Pulumi;
using Pulumi.AzureNative.ContainerRegistry;
using Pulumi.AzureNative.ContainerRegistry.Inputs;

namespace gloomhavensecretariat.Resources;

public class GhsRegistryArgs : ResourceArgs {
    [Input("registryName")] public required Input<string> RegistryName { get; init; }
    [Input("resourceGroupName")] public required Input<string> ResourceGroupName { get; init; }
}

public class GhsRegistry : ComponentResource {
    private const string ComponentName = "azure:ghs:registry";
    [Output("loginServer")] public Output<string> LoginServer { get; set; }
    [Output("password")] public Output<string> Password { get; set; }

    [Output("username")] public Output<string> UserName { get; set; }

    public GhsRegistry(string name, GhsRegistryArgs args, ComponentResourceOptions? options = null,
        bool remote = false) :
        base(ComponentName, name, args, options, remote) {
        var registry = new Registry("registry",
            new RegistryArgs {
                ResourceGroupName = args.ResourceGroupName,
                RegistryName = args.RegistryName,
                AdminUserEnabled = true,
                Sku = new SkuArgs {
                    Name = SkuName.Basic
                }
            });
        LoginServer = registry.LoginServer;

        var credentials = ListRegistryCredentials.Invoke(new ListRegistryCredentialsInvokeArgs {
            ResourceGroupName = args.ResourceGroupName,
            RegistryName = registry.Name
        });
        UserName = credentials.Apply(result => result.Username!);
        Password = credentials.Apply(result => result.Passwords[0]!.Value!);

        RegisterOutputs(new Dictionary<string, object?> {
            { "username", UserName },
            { "password", Password },
            { "loginServer", LoginServer }
        });
    }
}