using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jpp.Common;
using File = Jpp.BackgroundPipeline.File;

namespace Jpp.Ironstone.Draughter.TaskPayloads
{
    public class WorkingDirectory : DisposableManagedObject, IEnumerable<string>
    {
        private string _path;
        private List<File> _fileRecord;

        public WorkingDirectory(List<File> fileRecord)
        {
            _fileRecord = fileRecord;

            _path = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

            //Save the files
            foreach (File file in _fileRecord)
            {
                using (FileStream fs = System.IO.File.Create(Path.Combine(_path, file.Name)))
                {
                    fs.Write(file.Data, 0, file.Data.Length);
                }
            }
        }

        public List<File> Export()
        {
            List<File> outFiles = new List<File>();
            foreach (string file in Directory.GetFiles(_path))
            {
                File newFile = new File()
                {
                    Name = Path.GetFileName(file),
                    Data = System.IO.File.ReadAllBytes(file)
                };

                outFiles.Add(newFile);
            }

            return outFiles;
        }

        public string GetPath(string fileName)
        {
            return Path.Combine(_path, fileName);
        }

        protected override void DisposeManagedResources()
        {
            Directory.Delete(_path, true);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return Directory.GetFiles(_path).ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Directory.GetFiles(_path).GetEnumerator();
        }
    }
}
