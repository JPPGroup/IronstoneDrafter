using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.Civil.ApplicationServices;
using Jpp.Ironstone.Core;
using Jpp.Ironstone.Core.Autocad;
using Jpp.Ironstone.Core.Autocad.DrawingObjects.Primitives;
using Jpp.Ironstone.Core.ServiceInterfaces;
using Jpp.Ironstone.DocumentManagement.ObjectModel;
using Jpp.Ironstone.Draughter.TaskPayloads.Subtasks;
using Jpp.Ironstone.Housing.ObjectModel;
using Jpp.Ironstone.Housing.ObjectModel.Concept;
using Jpp.Ironstone.Structures.ObjectModel;
using Unity;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Jpp.Ironstone.Draughter.TaskPayloads
{
    class Foundation1
    {
        public string JobNumber { get; set; }
        public bool CreateDrawing { get; set; }
        public string OutlineInputDrawing { get; set; }
        public string ExistingLevelSurfaceName { get; set; }
        public string ProposedLevelSurfaceName { get; set; }

        private WorkingDirectory _workingDirectory;
        private ILogger _logger;
        private IUserSettings _settings;
        private ProjectController _controller;

        public Foundation1()
        {
            _logger = CoreExtensionApplication._current.Container.Resolve<ILogger>();
            _settings = CoreExtensionApplication._current.Container.Resolve<IUserSettings>();
        }

        public void Execute(WorkingDirectory workingDirectory)
        {
            _workingDirectory = workingDirectory;
            _controller = new ProjectController(workingDirectory.GetPath(""));

            CreateXref();

            if(CreateDrawing)
                CreateIssueDrawing();
        }

        private void CreateXref()
        {
            using (Document foundationXref = _controller.CreateXref(@"xref\ConceptFoundations.dwg"))
            {
                CopyOutlineFromSource(foundationXref);
                CreateConceptFoundations(foundationXref);

                foundationXref.Database.Save();
                foundationXref.CloseAndDiscard();
            }
        }

        private void CopyOutlineFromSource(Document targetDocument)
        {
            using (Document source = Application.DocumentManager.Open(OutlineInputDrawing))
            {
                Document oldActiveDocument = Application.DocumentManager.MdiActiveDocument;
                Application.DocumentManager.MdiActiveDocument = source;

                using (Transaction sourceTransaction = source.TransactionManager.StartTransaction())
                {
                    using (DocumentLock foundationLock = targetDocument.LockDocument())
                    using (Transaction foundationTransaction = targetDocument.TransactionManager.StartTransaction())
                    {
                        ClosedCurveSelector1 closedCurve = new ClosedCurveSelector1();
                        closedCurve.Document = source;
                        // TODO: Refine these values
                        closedCurve.MinimumArea = 10;
                        closedCurve.MaximumArea = 1000;

                        closedCurve.Execute(_workingDirectory);

                        // TODO: Extract this copy code to a common subsystem
                        BlockTableRecord desTableRecord = targetDocument.Database.GetModelSpace();
                        ObjectIdCollection sourecObjects = new ObjectIdCollection();
                        SelectionSet set = source.Editor.SelectImplied().Value;
                        foreach (SelectedObject selectedObject in set)
                        {
                            sourecObjects.Add(selectedObject.ObjectId);
                        }

                        // TODO: Remove copying of surfaces once proper xref subsystem is in place
                        ObjectIdCollection surfaceIds = CivilApplication.ActiveDocument.GetSurfaceIds();
                        foreach (ObjectId surfaceId in surfaceIds)
                        {
                            sourecObjects.Add(surfaceId);
                        }

                        source.Database.WblockCloneObjects(sourecObjects, desTableRecord.ObjectId, new IdMapping(),
                            DuplicateRecordCloning.MangleName, false);

                        foundationTransaction.Commit();
                    }
                }

                Application.DocumentManager.MdiActiveDocument = oldActiveDocument;
                source.CloseAndDiscard();
            }
        }

        private void CreateConceptFoundations(Document document)
        {
            Application.DocumentManager.MdiActiveDocument = document;

            using (Transaction trans = document.TransactionManager.StartTransaction())
            {
                SoilProperties properties =
                    DataService.Current.GetStore<StructureDocumentStore>(document.Name).SoilProperties;
                properties.ExistingGroundSurfaceName = ExistingLevelSurfaceName;
                properties.ProposedGroundSurfaceName = ProposedLevelSurfaceName;

                // TODO: Change this just to get all objects
                ClosedCurveSelector1 closedCurve = new ClosedCurveSelector1();
                closedCurve.Document = document;
                // TODO: Refine these values
                closedCurve.MinimumArea = 10;
                closedCurve.MaximumArea = 1000;

                closedCurve.Execute(_workingDirectory);

                PromptSelectionResult selectionResult = document.Editor.SelectImplied();

                ConceptualPlotManager manager = DataService.Current.GetStore<HousingDocumentStore>(document.Name)
                    .GetManager<ConceptualPlotManager>();

                SelectionSet selectionSet = selectionResult.Value;

                // Step through the objects in the selection set
                foreach (SelectedObject selectedObject in selectionSet)
                {
                    // Check to make sure a valid SelectedObject object was returned
                    if (selectedObject != null)
                    {
                        // Open the selected object for write
                        Entity entity = trans.GetObject(selectedObject.ObjectId,
                            OpenMode.ForWrite) as Entity;

                        PolylineDrawingObject polylineDrawingObject;

                        switch (entity)
                        {
                            case Polyline polyline:
                                polylineDrawingObject = new PolylineDrawingObject(polyline);
                                break;

                            case Polyline2d polyline:
                                polylineDrawingObject = new PolylineDrawingObject(polyline);
                                break;

                            case Polyline3d polyline:
                                polylineDrawingObject = new PolylineDrawingObject(polyline);
                                break;

                            default:
                                continue;
                        }

                        if (polylineDrawingObject.IsClosed())
                        {
                            ConceptualPlot plot = new ConceptualPlot(polylineDrawingObject);
                            plot.FoundationsEnabled = true;
                            manager.Add(plot);
                        }
                    }
                }

                manager.UpdateAll();
                trans.Commit();
            }
        }

        private void CreateIssueDrawing()
        {
            Document issueDrawing = _controller.CreateDrawing($"{JobNumber} - 000P1 - Preliminary Foundation Assessment.dwg");
            Application.DocumentManager.MdiActiveDocument = issueDrawing;

            LayoutSheetController controller = new LayoutSheetController(_logger, issueDrawing, _settings);
            controller.AddLayout("000 - PFA", PaperSize.A1Landscape);

            issueDrawing.Database.Save();
        }
    }
}
