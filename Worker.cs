using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using BackgroundPipeline.Autocad;
using Jpp.BackgroundPipeline;
using Jpp.Ironstone.Core;
using Jpp.Ironstone.Core.ServiceInterfaces;
using Jpp.Ironstone.Draughter.TaskPayloads;
using Unity;
using MessageBox = System.Windows.MessageBox;

namespace Jpp.Ironstone.Draughter
{
    public class Worker
    {
        //Milliseconds between work loops
        // TODO: Move to config
        public const int WORK_DELAY = 250;

        private WorkerConnection _connection;
        private RemoteTask _currentTask;
        private object _currentTaskLock = new Object();
        private bool _fetchingTask = false;
        private object _fetchingTaskLock = new Object();
        private bool _running;
        private TerminateMessageFilter _filter;

        private ILogger _logger;

        public Worker(ILogger log)
        {
            _logger = log;
        }

        [IronstoneCommand]
        [CommandMethod("D_BeginWork", CommandFlags.Session)]
        public static void BeginWorkCommand()
        {
            Worker worker = new Worker(CoreExtensionApplication._current.Container.Resolve<ILogger>());
            worker.BeginWork();
        }

        public void BeginWork()
        {
            DocumentCollection dm = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager;

            // TODO: Pull username and password from settings file
            using (_connection = new WorkerConnection("mq.group.cluster.jppuk.net", "jpp", "jpp"))
            {
                using (dm.MdiActiveDocument.LockDocument())
                {
                    _running = true;
                    RegisterForInput();
                    
                    while (_running)
                    {
                        WorkLoop();
                    }
                }
            }

            System.Windows.Forms.Application.RemoveMessageFilter(_filter);
        }

        private void RegisterForInput()
        {
            // Create and add our message filter
            _filter = new TerminateMessageFilter();
            System.Windows.Forms.Application.AddMessageFilter(_filter);

            string messageBoxText = "Drafter connected and running.";
            string caption = "Drafter";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Information;

            Task.Run(() =>
            {
                var result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.OK)
                {
                    _running = false;
                }
            });
        }

        private async void WorkLoop()
        {
            // Check for user input events
            System.Windows.Forms.Application.DoEvents();
            if (_filter.Canceled == true)
            {
                _logger.Entry("\nWork cancelled", Severity.Information);
                _running = false;
                return;
            }

            try
            {
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
                        Debugger.Launch();
                        WorkOnRemoteTask();
                        _connection.SendResponse(_currentTask);
                        //_connection.TaskComplete();

                        _currentTask = null;
                    }
                }

            }
            catch (System.Exception e)
            {
                _logger.LogException(e);
                HandleError();
            }

            Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument
                .TransactionManager.QueueForGraphicsFlush();
            await Task.Delay(WORK_DELAY);
        }

        private void HandleError()
        {
            if (_currentTask != null)
            {
                _currentTask.ResponseStatus = ResponseStatus.UnkownFailure;
                _connection.SendResponse(_currentTask);
                _connection.TaskFailed();

                _currentTask = null;
            }
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
                    if (_currentTask != null)
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
            public bool Canceled = false;

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == WM_KEYDOWN)
                {
                    // Check for the Escape keypress
                    Keys kc = (Keys) (int) m.WParam & Keys.KeyCode;
                    if (m.Msg == WM_KEYDOWN && kc == Keys.Escape)
                    {
                        Canceled = true;
                    }

                    // Return true to filter all keypresses
                    return true;
                }

                // Return false to let other messages through
                return false;
            }
        }
    }
}
