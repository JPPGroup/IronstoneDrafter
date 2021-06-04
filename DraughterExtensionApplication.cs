using System;
using Autodesk.AutoCAD.Runtime;
using Jpp.Ironstone.Core;
using Jpp.Ironstone.Core.ServiceInterfaces;
using Jpp.Ironstone.Draughter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: ExtensionApplication(typeof(DraughterExtensionApplication))]

namespace Jpp.Ironstone.Draughter
{
    public class DraughterExtensionApplication : IIronstoneExtensionApplication
    {
        public ILogger<DraughterExtensionApplication> Logger { get; set; }
        public static DraughterExtensionApplication Current { get; private set; }

        public void RegisterServices(IServiceCollection container)
        {
        }

        public void InjectContainer(IServiceProvider provider)
        {
            Logger = provider.GetRequiredService<ILogger<DraughterExtensionApplication>>();
        }

        public void CreateUI() { }

        public void Initialize()
        {
            Current = this;
            CoreExtensionApplication._current.RegisterExtension(this);
        }

        public void Terminate() { }
    }
}
