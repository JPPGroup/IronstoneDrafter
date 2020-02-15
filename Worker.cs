using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using BackgroundPipeline.Autocad;
using Jpp.Ironstone.Core;
using Jpp.Ironstone.Draughter.TaskPayloads;
using File = Jpp.BackgroundPipeline.File;

namespace Jpp.Ironstone.Draughter
{
    public class Worker
    {
        private WorkerConnection _connection;
        private RemoteTask _currentTask;
        private object _currentTaskLock = new Object();
        private bool _fetchingTask = false;
        private object _fetchingTaskLock = new Object();

        [IronstoneCommand]
        [CommandMethod("D_BeginWork", CommandFlags.Session)]
        public static void BeginWorkCommand()
        {
            Worker worker = new Worker();
            worker.BeginWork();
        }

        public void BeginWork()
        {
            DocumentCollection dm = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager;
            Editor ed = dm.MdiActiveDocument.Editor;

            // Create and add our message filter
            TerminateMessageFilter filter = new TerminateMessageFilter();
            System.Windows.Forms.Application.AddMessageFilter(filter);

            // TODO: Pull username and password from settings file
            using (_connection = new WorkerConnection("mq.group.cluster.jppuk.net", "jpp", "jpp"))
            {

                using (dm.MdiActiveDocument.LockDocument())
                {
                    // Start the loop
                    while (true)
                    {
                        // Check for user input events
                        System.Windows.Forms.Application.DoEvents();

                        // Check whether the filter has set the flag
                        if (filter.bCanceled == true)
                        {
                            ed.WriteMessage("\nWork cancelled.");
                            break;
                        }

                        lock (_currentTaskLock)
                        {
                            lock (_fetchingTaskLock)
                            {
                                if (_currentTask == null && !_fetchingTask)
                                {
                                    BeginFetchNextTask();
                                }
                            }

                            if (_currentTask != null)
                            {
                                WorkOnRemoteTask();
                                _connection.SendResponse(_currentTask);
                                _connection.TaskComplete();

                                _currentTask = null;
                            }
                        }

                        Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument
                            .TransactionManager.QueueForGraphicsFlush();
                        Thread.Sleep(500);
                    }
                }
            }

            // We're done - remove the message filter
            System.Windows.Forms.Application.RemoveMessageFilter(filter);
        }

        private void BeginFetchNextTask()
        {
            Task.Run(async () =>
            {
                lock (_fetchingTaskLock)
                {
                    _fetchingTask = true;
                }

                RemoteTask t = await _connection.GetRemoteTask();
                lock (_currentTaskLock)
                {
                    // Should never be able to double up on task
                    if(_currentTask != null)
                        throw new ArgumentOutOfRangeException();

                    _currentTask = t;
                }

                lock (_fetchingTaskLock)
                {
                    _fetchingTask = false;
                }

            });
        }

        private void WorkOnRemoteTask()
        {
            WorkingDirectory workDir = new WorkingDirectory(_currentTask.WorkingDirectory);

            foreach (ITaskPayload taskPayload in _currentTask.TaskPayload)
            {
                taskPayload.Execute(workDir);
            }          

            _currentTask.WorkingDirectory = workDir.Export();
        }
    
    public class TerminateMessageFilter : IMessageFilter
    {
        public const int WM_KEYDOWN = 0x0100;
        public bool bCanceled = false;

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_KEYDOWN)
            {
                // Check for the Escape keypress
                Keys kc = (Keys) (int) m.WParam & Keys.KeyCode;
                if (m.Msg == WM_KEYDOWN && kc == Keys.Escape)
                {
                    bCanceled = true;
                }

                // Return true to filter all keypresses
                return true;

            }

            // Return false to let other messages through
            return false;
        }
    }
}
