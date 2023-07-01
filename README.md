# Gloomhavne Secretariat hosting on Azure with automatic TLS support

This deploys a container registry, a storage account, a docker image that hosts Caddy for automatic TLS support, and a container group that runs the Gloomhaven Secretariat.

If a domain name is supplied as well, the stack will create a custom DNS entry for the container group.

## Deploying the App

To deploy your infrastructure, follow the below steps.

### Prerequisites

1. [Install Pulumi](https://www.pulumi.com/docs/get-started/install/)
1. [Configure Pulumi for Azure](https://www.pulumi.com/docs/intro/cloud-providers/azure/setup/)
1. [Install .NET Core 7.0+](https://dotnet.microsoft.com/download)

## Deploying and running the program

1.  Create a new stack:

    ```
    $ pulumi stack init ghs
    ```

1.  Set the Azure region:

    ```
    $ pulumi config set azure-native:location CanadaCentral
    ```

1.  Run `pulumi up` to preview and deploy changes:

    ```
    $ pulumi up
    Previewing changes:
    ...

    Previewing update (test)

         Type                                              Name                        Plan
     +   pulumi:pulumi:Stack                               gloomhavensecretariat-test  create
     +   ├─ azure-native:resources:ResourceGroup           resourcegroup               create
     +   ├─ random:index:RandomString                      dns-name                    create
     +   ├─ azure:ghs:storage                              storageAccount              create
     +   ├─ azure:ghs:registry                             registry                    create
     +   ├─ azure-native:containerregistry:Registry        registry                    create
     +   ├─ azure:ghs:image                                ghs-with-caddy              create
     +   ├─ azure-native:storage:StorageAccount            storageAccount              create
     +   ├─ azure:ghs:containergroup                       container-group             create
     +   ├─ azure-native:storage:FileShare                 caddy-config                create
     +   ├─ azure-native:storage:FileShare                 server-config               create
     +   ├─ azure-native:storage:FileShare                 caddy-data                  create
     +   ├─ docker:index:Image                             ghs-with-caddy              create
     +   └─ azure-native:containerinstance:ContainerGroup  container-group             create

    Outputs:
        domain  : output<string>
        hostname: output<string>
        url     : output<string>

    Resources:
        + 16 to create
    ```

1.  View the host name and IP address of the instance via `stack output`:

    ```
    $ pulumi stack output
    Current stack outputs (3):
        OUTPUT    VALUE
        domain    
        hostname  test-qdwoqwqj.canadacentral.azurecontainer.io
        url       http://1.2.3.4:443
    ```

1. Wait a few minutes for the container to start up, then visit the application in a web browser:

## Sample Settings

    ```
    config:
      azure-native:location: CanadaCentral
      gloomhavensecretariat:appName: test
      gloomhavensecretariat:registryName: ghs-test
      gloomhavensecretariat:containerPorts:
        - 80
        - 443
        - 2019
      gloomhavensecretariat:cpu: 0.25
      gloomhavensecretariat:memory: 1
      gloomhavensecretariat:domain: optional.youdontneed.this
    ```
