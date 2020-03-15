using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Jpp.Ironstone.Core;
using Jpp.Ironstone.Core.ServiceInterfaces;
using Jpp.Ironstone.DocumentManagement.ObjectModel;
using Unity;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Jpp.Ironstone.Draughter.TaskPayloads
{
    class Foundation1
    {
        public string JobNumber { get; set; }
        public bool CreateDrawing { get; set; }

        private WorkingDirectory _workingDirectory;
        private ILogger _logger;
        private IUserSettings _settings;

        public Foundation1()
        {
            _logger = CoreExtensionApplication._current.Container.Resolve<ILogger>();
            _settings = CoreExtensionApplication._current.Container.Resolve<IUserSettings>();
        }

        public void Execute(WorkingDirectory workingDirectory)
        {
            _workingDirectory = workingDirectory;

            CreateXref();

            if(CreateDrawing)
                CreateIssueDrawing();
        }

        private void CreateXref()
        {
            //Create a new foundation xref
            DocumentCollection acDocMgr = Application.DocumentManager;
            Document acDoc = acDocMgr.Add(null);
            //acDocMgr.MdiActiveDocument = acDoc;
            string path = _workingDirectory.GetPath(@"xref\ConceptFoundations.dwg");
            acDoc.Database.SaveAs(path, DwgVersion.Current);
        }

        private void CreateIssueDrawing()
        {
            //Create a new document
            DocumentCollection acDocMgr = Application.DocumentManager;
            Document acDoc = acDocMgr.Add(null);
            acDocMgr.MdiActiveDocument = acDoc;

            LayoutSheetController controller = new LayoutSheetController(_logger, acDoc, _settings);
            controller.AddLayout("000 - PFA", PaperSize.A1Landscape);

            //Save the dwg file
            string path = _workingDirectory.GetPath($"{JobNumber} - 000P1 - Preliminary Foundation Assessment.dwg");
            acDoc.Database.SaveAs(path, DwgVersion.Current);
        }
    }
}
