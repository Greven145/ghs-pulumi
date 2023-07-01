using System.Collections.Generic;
using Pulumi;
using Pulumi.Docker;
using Pulumi.Docker.Inputs;

namespace gloomhavensecretariat.Resources;

public class GhsImageArgs : ResourceArgs {
    [Input("appName")] public required Input<string> AppName { get; init; }
    [Input("imageTag")] public required Input<string> ImageTag { get; init; }
    [Input("registry")] public required Input<GhsRegistry> Registry { get; init; }
}

public class GhsImage : ComponentResource {
    private const string ComponentName = "azure:ghs:image";
    [Output("imageName")] public Output<string> ImageName { get; set; }

    public GhsImage(string name, GhsImageArgs args, ComponentResourceOptions? options = null,
        bool remote = false) : base(ComponentName, name, args, options, remote) {
        var loginServer = args.Registry.Apply(r => r.LoginServer);
        var userName = args.Registry.Apply(r => r.UserName);
        var password = args.Registry.Apply(r => r.Password);

        var image = new Image("ghs-with-caddy", new ImageArgs {
            ImageName = Output.Format(
                $"{args.Registry.Apply(r => r.LoginServer)}/{args.AppName}/ghs-with-caddy:{args.ImageTag}"),
            Build = new DockerBuildArgs {
                Context = "../ghs-with-caddy",
                Platform = "linux/amd64"
            },
            Registry = new RegistryArgs {
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