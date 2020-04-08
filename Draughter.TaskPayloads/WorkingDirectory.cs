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

            _path = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName()));

            //Save the files
            foreach (File file in _fileRecord)
            {
                string path = Path.Combine(_path, file.Name);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (FileStream fs = System.IO.File.Create(path))
                {
                    fs.Write(file.Data, 0, file.Data.Length);
                }
            }
        }

        public List<File> Export()
        {
            List<File> outFiles = new List<File>();
            
            ExportDirectory(outFiles, _path);

            return outFiles;
        }

        private void ExportDirectory(List<File> files, string path)
        {
            foreach (string s in Directory.GetDirectories(path))
            {
                ExportDirectory(files, s);
            }

            foreach (string file in Directory.GetFiles(path))
            {
                File newFile = new File()
                {
                    Name = Path.GetFileName(file),
                    Data = System.IO.File.ReadAllBytes(file)
                };

                files.Add(newFile);
            }
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
