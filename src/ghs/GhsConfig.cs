using Pulumi;

namespace gloomhavensecretariat;

public class GhsConfig {
    public string AppName { get; }
    public int[] ContainerPorts { get; }
    public double Cpu { get; }
    public string? Domain { get; }
    public string ImageTag { get; }
    public double Memory { get; }
    public string RegistryName { get; }

    public GhsConfig(Config config) {
        AppName = config.Get("appName") ?? "ghs";
        RegistryName = config.Get("registryName") ?? "ghsreg";
        ContainerPorts = config.GetObject<int[]>("containerPorts") ?? new[] { 80 };
        Cpu = config.GetDouble("cpu") ?? 1;
        Memory = config.GetDouble("memory") ?? 2;
        ImageTag = config.Get("imageTag") ?? "latest";
        Domain = config.Get("domain");
    }
}