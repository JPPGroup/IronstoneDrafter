using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Jpp.Ironstone.Core.Autocad;

namespace Jpp.Ironstone.Draughter.TaskPayloads.Subtasks
{
    class ClosedCurveSelector1 : ISelector
    {
        public Document Document { get; set; }
        public double MinimumArea { get; set; }
        public double MaximumArea { get; set; }

        public void Execute(WorkingDirectory workingDirectory)
        {
            Transaction trans = Document.TransactionManager.TopTransaction;
            BlockTableRecord modelSpace = Document.Database.GetModelSpace();
            List<ObjectId> found = new List<ObjectId>();
            
            foreach (ObjectId id in modelSpace)
            {
                DBObject dbObject = trans.GetObject(id, OpenMode.ForRead);
                Curve c = dbObject as Curve;
                if (c == null)
                    break;

                if (c.Closed != true)
                    break;

                if (c.Area > MinimumArea && c.Area < MaximumArea)
                {
                    found.Add(id);
                }
            }

            Document.Editor.SetImpliedSelection(found.ToArray());
        }
    }
}
