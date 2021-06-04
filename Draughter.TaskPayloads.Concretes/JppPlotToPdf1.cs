using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Jpp.BackgroundPipeline;
using Jpp.Ironstone.Core;
using Jpp.Ironstone.Core.ServiceInterfaces;
using Jpp.Ironstone.DocumentManagement.ObjectModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Jpp.Ironstone.Draughter.TaskPayloads
{
    public class JppPlotToPdf1 : ITaskPayload
    {
        public string[] DrawingNumbers { get; set; }
        public bool PlotAll { get; set; }

        private WorkingDirectory _workingDirectory;
        private ILogger<JppPlotToPdf1> _logger;
        private IConfiguration _settings;

        public JppPlotToPdf1()
        {
            _logger = CoreExtensionApplication._current.Container.GetRequiredService<ILogger<JppPlotToPdf1>>();
            _settings = CoreExtensionApplication._current.Container.GetRequiredService<IConfiguration>();
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

                            var coreLogger = CoreExtensionApplication._current.Container.GetRequiredService<ILogger<CoreExtensionApplication>>();
                            LayoutSheetController controller = new LayoutSheetController(coreLogger, openedDocument, _settings);
                            controller.Scan();

                            List<string> expectedFiles = new List<string>();

                            foreach (LayoutSheet sheet in controller.Sheets.Values)
                            {
                                if (PlotAll || DrawingNumbers.Contains(sheet.TitleBlock.DrawingNumber))
                                {
                                    string fileName = _workingDirectory.GetPath(
                                        $"{sheet.TitleBlock.ProjectNumber} - {sheet.TitleBlock.DrawingNumber}{sheet.TitleBlock.Revision} - {sheet.TitleBlock.Title}.pdf");
                                    expectedFiles.Add(fileName);

                                    try
                                    {
                                        sheet.Plot(fileName, pe, ppd);
                                    }
                                    catch (Exception e)
                                    {
                                        _logger.LogError(e, $"Sheet {sheet.Name} in {openedDocument.Name} failed to plot.");
                                    }
                                }
                            }

                            ppd.PlotProgressPos = 100;
                            ppd.OnEndPlot();
                            pe.EndPlot(null);

                            //Check files have been created
                            bool allPresent = false;

                            while (!allPresent)
                            {
                                Thread.Sleep(1000);
                                bool present = true;
                                foreach (string expectedFile in expectedFiles)
                                {
                                    if (!System.IO.File.Exists(expectedFile))
                                    {
                                        present = false;
                                    }
                                }

                                allPresent = present;
                            }

                            Application.SetSystemVariable("BACKGROUNDPLOT", bpValue);
                        }
                    }
                }
            }

            openedDocument.CloseAndDiscard();
        }
    }
}
