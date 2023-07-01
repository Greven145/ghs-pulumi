using System.Collections.Generic;
using Pulumi;
using Pulumi.AzureNative.ContainerRegistry;
using Pulumi.AzureNative.ContainerRegistry.Inputs;

namespace gloomhavensecretariat.Resources;

public class GhsRegistryArgs : ResourceArgs
{
    public new static GhsRegistryArgs Empty => new();
    [Input("registryName")] public Input<string> RegistryName { get; set; } = "ghs";

    [Input("resourceGroupName")] public Input<string> ResourceGroupName { get; set; } = "ghs";
}

public class GhsRegistry : ComponentResource
{
    private const string ComponentName = "azure:ghs:registry";
    [Output("loginServer")] public Output<string> LoginServer { get; set; }
    [Output("password")] public Output<string> Password { get; set; }

    [Output("username")] public Output<string> UserName { get; set; }

    public GhsRegistry(string name, ComponentResourceOptions? options = null) : this(name, null, options)
    {
    }

    public GhsRegistry(string name, GhsRegistryArgs? args, ComponentResourceOptions? options = null,
        bool remote = false) :
        base(ComponentName, name, args, options, remote)
    {
        var resourceGroupName = args?.ResourceGroupName ?? "ghs";
        var registryName = args?.RegistryName ?? "ghs";

        var registry = new Registry("registry",
            new RegistryArgs
            {
                ResourceGroupName = resourceGroupName,
                RegistryName = registryName,
                AdminUserEnabled = true,
                Sku = new SkuArgs
                {
                    Name = SkuName.Basic
                }
            });
        LoginServer = registry.LoginServer;

        var credentials = ListRegistryCredentials.Invoke(new ListRegistryCredentialsInvokeArgs
        {
            ResourceGroupName = resourceGroupName,
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