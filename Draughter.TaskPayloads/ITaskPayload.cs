using System.Collections.Generic;
using Jpp.BackgroundPipeline;

namespace Jpp.Ironstone.Draughter.TaskPayloads
{
    public interface ITaskPayload
    {
        void Execute(List<File> workingDirectory, string decompressedPath);
    }
}
