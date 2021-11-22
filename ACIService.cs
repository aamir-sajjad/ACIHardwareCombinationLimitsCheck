using Microsoft.Azure.Management.ContainerInstance.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ACIHardwareCombinationTests
{
    public class ACIService
    {


        private readonly IConfiguration _configuration;
        private readonly ILogger<ACIService> _logger;
        private IAzure azure;

        public ACIService(
            IConfiguration configuration,
            ILogger<ACIService> logger
            )
        {
            _configuration = configuration;
            _logger = logger;
            azure = Connect();
        }

        public async Task<bool> CreateACIWithStandardCore(double cpu = 1, double ram = 4)
        {
            _logger.LogInformation($"CreateACI: at start");

            var region = _configuration["WindSim:Azure:Region"];
            var resourceGroup = _configuration["WindSim:Azure:ResourceGroup"];
            var server = _configuration["WindSim:Azure:Registry:Server"];
            var username = _configuration["WindSim:Azure:Registry:Username"];
            var password = _configuration["WindSim:Azure:Registry:Password"];

            var containerName = Guid.NewGuid().ToString().Substring(0, 5);
            //var imageName = @"mcr.microsoft.com/azuredocs/aci-helloworld";
            var imageName = @"windsimcoreacr.azurecr.io/testdeployapplication:latest";
            var containerGroupName = Guid.NewGuid().ToString().Substring(0, 5);
            var baseDefinition = azure.ContainerGroups.Define(containerGroupName)
                  .WithRegion(region)
                  .WithExistingResourceGroup(resourceGroup);

            var osDefinition = baseDefinition.WithLinux();

            await osDefinition.WithPrivateImageRegistry(
                  server,
                  username,
                  password)
              .WithoutVolume()
              .DefineContainerInstance(containerName)
                  .WithImage(imageName)
                  .WithoutPorts()
                  .WithCpuCoreCount(cpu)
                  .WithMemorySizeInGB(ram)
                  .Attach()
              .WithRestartPolicy(ContainerGroupRestartPolicy.Never)
              .CreateAsync();

            await Task.CompletedTask;
            _logger.LogInformation($"CreateACI: at the end");
            return true;
        }

        public async Task<bool> CreateACIWithStandardK80Cores(double cpu = 1, double ram = 17)
        {
            _logger.LogInformation($"CreateACI: at start");

            var region = _configuration["WindSim:Azure:Region"];
            var resourceGroup = _configuration["WindSim:Azure:ResourceGroup"];
            var server = _configuration["WindSim:Azure:Registry:Server"];
            var username = _configuration["WindSim:Azure:Registry:Username"];
            var password = _configuration["WindSim:Azure:Registry:Password"];

            var containerName = Guid.NewGuid().ToString().Substring(0, 5);
            //var imageName = @"mcr.microsoft.com/azuredocs/aci-helloworld";
            var imageName = @"windsimcoreacr.azurecr.io/testdeployapplication:latest";
            var containerGroupName = Guid.NewGuid().ToString().Substring(0, 5);
            var baseDefinition = azure.ContainerGroups.Define(containerGroupName)
                  .WithRegion(region)
                  .WithExistingResourceGroup(resourceGroup);

            var osDefinition = baseDefinition.WithLinux();

            await osDefinition.WithPrivateImageRegistry(
                  server,
                  username,
                  password)
              .WithoutVolume()
              .DefineContainerInstance(containerName)
                  .WithImage(imageName)
                  .WithoutPorts()
                  .WithCpuCoreCount(cpu)
                  .WithMemorySizeInGB(ram)
                  .WithGpuResource(1, GpuSku.K80)
                  .Attach()
              .WithRestartPolicy(ContainerGroupRestartPolicy.Never)
              .CreateAsync();

            await Task.CompletedTask;
            _logger.LogInformation($"CreateACI: at the end");
            return true;
        }

        private IAzure Connect()
        {
            // requires Service Principal creds
            // generate with `az ad sp create-for-rbac`
            // for other auth options see: https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md

            var clientId = _configuration["WindSim:Azure:ServicePrincipal:ClientId"];
            var clientSecret = _configuration["WindSim:Azure:ServicePrincipal:ClientSecret"];
            var subscriptionId = _configuration["WindSim:Azure:ServicePrincipal:SubscriptionId"];
            var tenantId = _configuration["WindSim:Azure:ServicePrincipal:TenantId"];

            var creds = new AzureCredentialsFactory().FromServicePrincipal(
                clientId,
                clientSecret,
                tenantId,
                AzureEnvironment.AzureGlobalCloud);
            var azure = Policy
                          .HandleResult<IAzure>(x => x == null)
                          .WaitAndRetry(15, retryAttempt => TimeSpan.FromMilliseconds(retryAttempt * 1500), (result, timeSpan, retryCount, context) =>
                          {
                              _logger.LogWarning($"Connect: Request to Azure failed with {result}. Waiting {timeSpan} before next retry. Retry attempt {retryCount}");
                          })
                          .Execute(() => Microsoft.Azure.Management.Fluent.Azure.Authenticate(creds).WithSubscription(subscriptionId));

            return azure;

        }

        // end of class
    }
}
