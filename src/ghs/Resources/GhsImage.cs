using System;
using System.Collections.Generic;
using Pulumi;
using Pulumi.Docker;
using Pulumi.Docker.Inputs;

namespace gloomhavensecretariat.Resources;

public class GhsImageArgs : ResourceArgs
{
    [Input("appName")] public Input<string> AppName { get; set; } = "ghs";
    [Input("imageTag")] public Input<string> ImageTag { get; set; } = "latest";
    [Input("registry")] public Input<GhsRegistry>? Registry { get; set; } = null!;
}

public class GhsImage : ComponentResource
{
    private const string ComponentName = "azure:ghs:image";
    [Output("imageName")] public Output<string> ImageName { get; set; }

    public GhsImage(string name, ComponentResourceOptions? options = null) : this(name, null, options)
    {
    }

    public GhsImage(string name, GhsImageArgs? args, ComponentResourceOptions? options = null,
        bool remote = false) : base(ComponentName, name, args, options, remote)
    {
        var registry = args?.Registry ?? throw new Exception("Registry is required");
        var appName = args?.AppName ?? "ghs";
        var imageTag = args?.ImageTag ?? "ghs";

        var loginServer = registry.Apply(r => r.LoginServer);
        var userName = registry.Apply(r => r.UserName);
        var password = registry.Apply(r => r.Password);

        var image = new Image("ghs-with-caddy", new ImageArgs
        {
            ImageName = Output.Format($"{registry.Apply(r => r.LoginServer)}/{appName}/ghs-with-caddy:{imageTag}"),
            Build = new DockerBuildArgs
            {
                Context = "../ghs-with-caddy",
                Platform = "linux/amd64",
            },
            Registry = new RegistryArgs
            {
                Server = loginServer,
                Username = userName,
                Password = password
            }
        });

        ImageName = image.ImageName;
        RegisterOutputs(new Dictionary<string, object?> {
            { "ImageName", ImageName }
        });
    }
}