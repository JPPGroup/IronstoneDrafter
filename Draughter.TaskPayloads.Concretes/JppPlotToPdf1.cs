using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using Jpp.Ironstone.DocumentManagement.ObjectModel;

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
                    LayoutSheetController controller = new LayoutSheetController(openedDocument);
                    controller.Scan();

                    foreach (LayoutSheet sheet in controller.Sheets.Values)
                    {
                        if (PlotAll || DrawingNumbers.Contains(sheet.DrawingNumber))
                        {
                            PlotLayout(sheet, trans);
                        }
                    }
                }
            }
        }

        private void PlotLayout(LayoutSheet sheet, Transaction trans)
        {
            Layout layout = (Layout)trans.GetObject(sheet.LayoutID, OpenMode.ForRead);

            PlotInfo plotInfo = new PlotInfo();
            plotInfo.Layout = sheet.LayoutID;

            // Set plot settings
            PlotSettings ps = new PlotSettings(layout.ModelType);
            ps.CopyFrom(layout);

            PlotSettingsValidator psv = PlotSettingsValidator.Current;
            psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Layout);
            psv.SetUseStandardScale(ps, true);
            psv.SetStdScaleType(ps, StdScaleType.StdScale1To1);
            //psv.SetPlotCentered(ps, true);

            psv.SetPlotConfigurationName(ps, "DWG To PDF.pc3", "ISO expand A1(841.00 x 594.00 MM)");

            plotInfo.OverrideSettings = ps;
            PlotInfoValidator piv = new PlotInfoValidator();
            piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
            piv.Validate(plotInfo);

            string fileName = _workingDirectory.GetPath($"{sheet.JobNumber} - {sheet.DrawingNumber}{sheet.Revision} - {sheet.Name}.pdf");

            if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
            {
                using (PlotEngine pe = PlotFactory.CreatePublishEngine())
                {
                    using (PlotProgressDialog ppd = new PlotProgressDialog(false, 1, true))
                    {
                        /*ppd.set_PlotMsgString(PlotMessageIndex.DialogTitle,
                          "Custom Plot Progress"
                        );
                        ppd.set_PlotMsgString(
                          PlotMessageIndex.CancelJobButtonMessage,
                          "Cancel Job"
                        );
                        ppd.set_PlotMsgString(
                          PlotMessageIndex.CancelSheetButtonMessage,
                          "Cancel Sheet"
                        );
                        ppd.set_PlotMsgString(
                          PlotMessageIndex.SheetSetProgressCaption,
                          "Sheet Set Progress"
                        );
                        ppd.set_PlotMsgString(
                          PlotMessageIndex.SheetProgressCaption,
                          "Sheet Progress"
                        );
                        ppd.LowerPlotProgressRange = 0;
                        ppd.UpperPlotProgressRange = 100;
                        ppd.PlotProgressPos = 0;*/

                        ppd.OnBeginPlot();
                        ppd.IsVisible = false;
                        pe.BeginPlot(ppd, null);
                        
                        
                        pe.BeginDocument(
                          plotInfo,
                          fileName,
                          null,
                          1,
                          true, 
                          fileName
                        );

                        ppd.OnBeginSheet();
                        ppd.LowerSheetProgressRange = 0;
                        ppd.UpperSheetProgressRange = 100;
                        ppd.SheetProgressPos = 0;

                        PlotPageInfo ppi = new PlotPageInfo();
                        pe.BeginPage(
                          ppi,
                          plotInfo,
                          true,
                          null
                        );
                        pe.BeginGenerateGraphics(null);
                        pe.EndGenerateGraphics(null);

                        // Finish the sheet
                        pe.EndPage(null);
                        ppd.SheetProgressPos = 100;
                        ppd.OnEndSheet();

                        // Finish the document
                        pe.EndDocument(null);

                        // And finish the plot
                        ppd.PlotProgressPos = 100;
                        ppd.OnEndPlot();
                        pe.EndPlot(null);
                    }
                }
            }
            else
            {
                //
            }
            //Persist
        }

    }
}
