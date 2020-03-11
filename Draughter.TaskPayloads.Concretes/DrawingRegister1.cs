using System;
using Jpp.Ironstone.Core.ServiceInterfaces;
using Autodesk.AutoCAD.ApplicationServices;
using Jpp.Ironstone.Core;
using Jpp.Ironstone.DocumentManagement.ObjectModel;
using Unity;
using Autodesk.AutoCAD.DatabaseServices;

namespace Jpp.Ironstone.Draughter.TaskPayloads
{
    class DrawingRegister1 : ITaskPayload
    {
        public string DrawingRegisterPath { get; set; }
        public string JobNumber { get; set; }
        
        private WorkingDirectory _workingDirectory;
        private ILogger _logger;
        private DrawingRegister _register;

        public DrawingRegister1()
        {
            _logger = CoreExtensionApplication._current.Container.Resolve<ILogger>();
        }

        public void Execute(WorkingDirectory workingDirectory)
        {
            _workingDirectory = workingDirectory;

            if (!String.IsNullOrEmpty(DrawingRegisterPath))
            {
                _register = new DrawingRegister(DrawingRegisterPath);
            }
            else
            {
                _register = new DrawingRegister($"{JobNumber} - Drawing Register.xlsx");
            }

            foreach (string f in _workingDirectory)
            {
                if (f.EndsWith(".dwg"))
                {
                    OpenScanDrawing(f);
                }
            }
        }

        private void OpenScanDrawing(string filePath)
        {
            Document openedDocument = Application.DocumentManager.Open(filePath, true);
            using (openedDocument.LockDocument())
            {
                using (Transaction trans = openedDocument.TransactionManager.StartTransaction())
                {
                    LayoutSheetController controller = new LayoutSheetController(_logger, openedDocument);
                    controller.Scan();

                    foreach (LayoutSheet sheet in controller.Sheets.Values)
                    {
                        //_register.WriteSheet(sheet);
                    }
                }
            }

            openedDocument.CloseAndDiscard();
        }
    }
}
