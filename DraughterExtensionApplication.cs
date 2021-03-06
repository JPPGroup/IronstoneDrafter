﻿using Autodesk.AutoCAD.Runtime;
using Jpp.Ironstone.Core;
using Jpp.Ironstone.Core.ServiceInterfaces;
using Jpp.Ironstone.Draughter;
using Unity;

[assembly: ExtensionApplication(typeof(DraughterExtensionApplication))]

namespace Jpp.Ironstone.Draughter
{
    public class DraughterExtensionApplication : IIronstoneExtensionApplication
    {
        public ILogger Logger { get; set; }
        public static DraughterExtensionApplication Current { get; private set; }

        public void CreateUI() { }

        public void Initialize()
        {
            Current = this;
            CoreExtensionApplication._current.RegisterExtension(this);
        }

        public void InjectContainer(IUnityContainer container)
        {
            Logger = container.Resolve<ILogger>();
        }

        public void Terminate() { }
    }
}
