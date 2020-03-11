using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using Jpp.Ironstone.DocumentManagement.ObjectModel;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Jpp.Ironstone.Draughter.TaskPayloads
{
    public class JppPlotToPdf1 : ITaskPayload
    {
        public string[] DrawingNumbers { get; set; }
        public bool PlotAll { get; set; }

        private WorkingDirectory _workingDirectory;

        public void Execute(WorkingDirectory workingDirectory)
        {
            _workingDirectory = workingDirectory;

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
                    using (PlotEngine pe = PlotFactory.CreatePublishEngine())
                    {
                        using (PlotProgressDialog ppd = new PlotProgressDialog(false, 1, true))
                        {
                            ppd.OnBeginPlot();
                            ppd.IsVisible = false;
                            pe.BeginPlot(ppd, null);

                            LayoutSheetController controller = new LayoutSheetController();
                            controller.Scan(openedDocument);

                            foreach (LayoutSheet sheet in controller.Sheets.Values)
                            {
                                if (PlotAll || DrawingNumbers.Contains(sheet.DrawingNumber))
                                {
                                    string fileName = _workingDirectory.GetPath(
                                        $"{sheet.JobNumber} - {sheet.DrawingNumber}{sheet.Revision} - {sheet.Name}.pdf");

                                    sheet.Plot(fileName, pe, ppd);
                                }
                            }

                            ppd.PlotProgressPos = 100;
                            ppd.OnEndPlot();
                            pe.EndPlot(null);
                        }
                    }
                }
            }
        }
    }
}
