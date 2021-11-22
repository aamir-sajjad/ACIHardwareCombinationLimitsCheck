using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace ACIHardwareCombinationTests
{
    public class ACITests
    {
        private ServiceProvider _serviceProvider;
        [SetUp]
        public void Setup()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();


            var configuration = builder.Build();

            _serviceProvider = new ServiceCollection()
             .AddSingleton<IConfiguration>(configuration)
             .AddSingleton(Mock.Of<ILogger<ACIService>>())
             .AddTransient<ACIService>()
             .BuildServiceProvider();
        }

        [Test]
        [Explicit]
        [TestCase(1, 2)]
        [TestCase(2, 7)]
        [TestCase(2.5, 10)]
        [TestCase(3, 13)]
        [TestCase(4, 16)]
        public async Task Test_StandardCore_ShouldCreateACIWith_StandardCore(double cpu, double ram)
        {
            var aciService = _serviceProvider.GetService<ACIService>();

            await aciService.CreateACIWithStandardCore(cpu, ram);

            Assert.Pass();
        }

        [Test]
        [Explicit]
        [TestCase(6, 31)]
        [TestCase(5, 19)]
        [TestCase(5, 16)]
        [TestCase(3, 29)]
        [TestCase(1, 17)]
        [TestCase(2, 19)]
        [TestCase(2.5, 22)]
        [TestCase(1.5, 17)]
        [TestCase(1.5, 32)]
        public async Task Test_StandardK80Cores_ShouldCreateACIWith_StandardK80Cores(double cpu, double ram)
        {
            var aciService = _serviceProvider.GetService<ACIService>();

            await aciService.CreateACIWithStandardK80Cores(cpu, ram);

            Assert.Pass();
        }

        // end of class
    }
}