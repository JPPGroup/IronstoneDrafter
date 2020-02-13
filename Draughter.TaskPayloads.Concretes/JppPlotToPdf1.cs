using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using Jpp.Ironstone.DocumentManagement.ObjectModel;
using File = Jpp.BackgroundPipeline.File;

namespace Jpp.Ironstone.Draughter.TaskPayloads
{
    public class JppPlotToPdf1 : ITaskPayload
    {
        public string[] DrawingNumbers { get; set; }
        public bool PlotAll { get; set; }

        public void Execute(List<File> workingDirectory, string decompressedPath)
        {
            foreach (File f in workingDirectory)
            {
                if (f.Name.EndsWith(".dwg"))
                {
                    OpenScanDrawing(Path.Combine(decompressedPath, f.Name));
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

            psv.SetPlotConfigurationName(ps, "DWG To PDF.pc3", "ISO A1(841.00 x 594.00 MM)");

            plotInfo.OverrideSettings = ps;
            PlotInfoValidator piv = new PlotInfoValidator();
            piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
            piv.Validate(plotInfo);

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

                        // Let's start the plot, at last

                        ppd.OnBeginPlot();
                        ppd.IsVisible = false;
                        pe.BeginPlot(ppd, null);
                        
                        // We'll be plotting a single document

                        /*pe.BeginDocument(
                          plotInfo,
                          doc.Name,
                          null,
                          1,
                          true, // Let's plot to file
                          "c:\\test-output"
                        );*/

                        // Which contains a single sheet

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
