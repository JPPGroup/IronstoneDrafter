using System;
using Jpp.Ironstone.Core.ServiceInterfaces;
using Autodesk.AutoCAD.ApplicationServices;
using Jpp.Ironstone.Core;
using Jpp.Ironstone.DocumentManagement.ObjectModel;
using Autodesk.AutoCAD.DatabaseServices;
using Jpp.ProjectDocuments.Register;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jpp.Ironstone.Draughter.TaskPayloads
{
    class DrawingRegister1 : ITaskPayload
    {
        public string DrawingRegisterPath { get; set; }
        public string JobNumber { get; set; }
        
        private WorkingDirectory _workingDirectory;
        private ILogger<DrawingRegister1> _logger;
        private DrawingRegister _register;
        private IConfiguration _settings;

        public DrawingRegister1()
        {
            _logger = CoreExtensionApplication._current.Container.GetRequiredService<ILogger<DrawingRegister1>>();
            _settings = CoreExtensionApplication._current.Container.GetRequiredService<IConfiguration>();
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
                _register = new DrawingRegister(_workingDirectory.GetPath($"{JobNumber} - Drawing Register.xlsx"));
            }

            foreach (string f in _workingDirectory)
            {
                if (f.EndsWith(".dwg"))
                {
                    OpenScanDrawing(f);
                }
            }

            _register.Write();
        }

        private void OpenScanDrawing(string filePath)
        {
            Document openedDocument = Application.DocumentManager.Open(filePath, true);
            using (openedDocument.LockDocument())
            {
                using (Transaction trans = openedDocument.TransactionManager.StartTransaction())
                {
                    var coreLogger = CoreExtensionApplication._current.Container.GetRequiredService<ILogger<CoreExtensionApplication>>();
                    LayoutSheetController controller = new LayoutSheetController(coreLogger, openedDocument, _settings);
                    controller.Scan();

                    foreach (LayoutSheet sheet in controller.Sheets.Values)
                    {
                        // TODO: Add some way of determining if civil or structural drawing
                        WriteLayoutSheet(sheet, DrawingType.Structural);
                    }
                }
            }

            openedDocument.CloseAndDiscard();
        }

        private void WriteLayoutSheet(LayoutSheet sheet, DrawingType type)
        {
            DrawingInformation drawingInformation = _register.Drawings.FirstOrDefault(di => di.DrawingNumber == sheet.TitleBlock.DrawingNumber);
            if (drawingInformation == null)
            {
                drawingInformation = new DrawingInformation();
                drawingInformation.DrawingNumber = sheet.TitleBlock.DrawingNumber;
                _register.Drawings.Add(drawingInformation);

                // TODO: Add code here for sorting into a sensible order
            }

            drawingInformation.DrawingTitle = sheet.TitleBlock.Title;
            drawingInformation.Type = type;
            // drawingInformation.IssueType = sheet.TitleBlock. // TODO: This needs to be found
            drawingInformation.CurrentIssue = sheet.TitleBlock.Revision;
            
        }
    }
}
