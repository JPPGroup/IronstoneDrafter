using System;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using Jpp.Ironstone.Core;
using Jpp.Ironstone.Core.ServiceInterfaces;
using Jpp.Ironstone.DocumentManagement.ObjectModel;
using Unity;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Jpp.Ironstone.Draughter.TaskPayloads
{
    public class JppPlotToPdf1 : ITaskPayload
    {
        public string[] DrawingNumbers { get; set; }
        public bool PlotAll { get; set; }

        private WorkingDirectory _workingDirectory;
        private ILogger _logger;
        private IUserSettings _settings;

        public JppPlotToPdf1()
        {
            _logger = CoreExtensionApplication._current.Container.Resolve<ILogger>();
            _settings = CoreExtensionApplication._current.Container.Resolve<IUserSettings>();
        }

        public void Execute(WorkingDirectory workingDirectory)
        {
            _workingDirectory = workingDirectory;

            foreach (string f in _workingDirectory)
            {
                if (f.EndsWith(".dwg") && !f.ToLower().Contains("xref"))
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
                    using (PlotEngine pe = PlotFactory.CreatePublishEngine())
                    {
                        using (PlotProgressDialog ppd = new PlotProgressDialog(false, 1, true))
                        {
                            int bpValue = Convert.ToInt32(Application.GetSystemVariable("BACKGROUNDPLOT"));
                            Application.SetSystemVariable("BACKGROUNDPLOT", 0);

                            ppd.OnBeginPlot();
                            ppd.IsVisible = false;
                            pe.BeginPlot(ppd, null);

                            LayoutSheetController controller = new LayoutSheetController(_logger, openedDocument, _settings);
                            controller.Scan();

                            foreach (LayoutSheet sheet in controller.Sheets.Values)
                            {
                                if (PlotAll || DrawingNumbers.Contains(sheet.TitleBlock.DrawingNumber))
                                {
                                    string fileName = _workingDirectory.GetPath(
                                        $"{sheet.TitleBlock.ProjectNumber} - {sheet.TitleBlock.DrawingNumber}{sheet.TitleBlock.Revision} - {sheet.TitleBlock.Title}.pdf");

                                    sheet.Plot(fileName, pe, ppd);
                                }
                            }

                            ppd.PlotProgressPos = 100;
                            ppd.OnEndPlot();
                            pe.EndPlot(null);

                            Application.SetSystemVariable("BACKGROUNDPLOT", bpValue);
                        }
                    }
                }
            }

            openedDocument.CloseAndDiscard();
        }
    }
}
