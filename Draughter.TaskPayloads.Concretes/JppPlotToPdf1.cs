using System.Collections.Generic;
using System.IO;

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
            
        }

        private void PlotLayout()
        {

        }
    }
}
