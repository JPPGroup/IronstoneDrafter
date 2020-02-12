using System.Collections.Generic;

namespace Jpp.Ironstone.Draughter.TaskPayloads
{
    public interface ITaskPayload
    {
        void Execute(List<File> workingDirectory, string decompressedPath);
    }
}
