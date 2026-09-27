using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;

namespace MysticEyeStudios.Threading
{
    /// <summary>
    /// Controls Execution, queueing , dequeueing of Promises. auto starts
    /// </summary>
    public class TaskMaster : IDisposable
    {
        // private readonly FileLogger fileLogger;
        private Queue<Action> _runAnytime = new();
        private Queue<Action> _runOnMainThread = new();
        private object _anyTimeLock = new();
        private object _mainThreadLock = new();
        private Thread thread;
        private bool isMainThreadActive = false;
        private bool isDisposed = false;
        public int __RunningTasks = 0;
        public TaskMaster()//(FileLogger fileLogger)
        {

            ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxIOThreads);

            // Set the minimum threads to match your actual CPU logical core count (e.g., 8 or 16)
            int coreCount = System.Environment.ProcessorCount;
            ThreadPool.SetMinThreads(coreCount, coreCount);


            //this.fileLogger = fileLogger;
            thread = new(_threadTick);
            thread.Start();
            // this.fileLogger.Log("Starting Thread Master", "ThreadMaster");

        }

        public Promise<bool, Exception> RunAnytime(Action action, TaskPriority taskPriority = TaskPriority.None)
        {
            Promise<bool, Exception> promise = new();

            if (taskPriority == TaskPriority.High)
            {
                Task.Run(() =>
                {

                    try
                    {
                        __RunningTasks++;
                        action();
                        __RunningTasks--;
                    }
                    catch (Exception e)
                    {
                        promise.Error(e);
                    }
                    promise.Success(true);

                });
                return promise;
            }
            lock (_anyTimeLock)
            {

                this._runAnytime.Enqueue(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        promise.Error(e);
                    }
                    promise.Success(true);

                });
            }
            return promise;
        }


        public Promise<bool, Exception> RunOnMainThread(Action action, TaskPriority taskPriority = TaskPriority.None)
        {
            Promise<bool, Exception> promise = new();

            if (taskPriority == TaskPriority.High)
            {

                try
                {
                    __RunningTasks++;
                    action();
                    __RunningTasks--;
                }
                catch (Exception e)
                {
                    promise.Error(e);
                }
                promise.Success(true);

                return promise;
            }
            lock (_mainThreadLock)
            {

                this._runOnMainThread.Enqueue(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        promise.Error(e);
                    }
                    promise.Success(true);

                });
            }
            return promise;
        }//
        private void _threadTick()
        {
            this.isMainThreadActive = true;
            while (!isDisposed)
            {
                if (this.__RunningTasks > 15)
                {
                    Thread.Sleep(5);
                    continue;
                }

                bool hasActionQueued = false;
                Action action;
                lock (_anyTimeLock)
                {
                    hasActionQueued = this._runAnytime.TryDequeue(out action);
                }
                if (hasActionQueued)
                {
                    __RunningTasks++;
                    Task.Run(() => { action(); __RunningTasks--; });
                }
                Thread.Sleep(1);
            }
            this.isMainThreadActive = false;

        }
        /// <summary>
        /// This runs promises queued in RunOnMainThread.
        /// </summary>
        public void M_MainThreadTick()
        {
            bool hasActionQueued = false;
            Action action;
            lock (_mainThreadLock)
            {
                hasActionQueued = this._runOnMainThread.TryDequeue(out action);
            }
            if (hasActionQueued)
            {
                try
                {
                    __RunningTasks++;
                    action();
                    __RunningTasks--;
                }
                catch (Exception e)
                {
                    throw;
                    // fileLogger.Log($@"Error Executing Task on main thread  {e.Message} stack {e.StackTrace}", "ThreadMaster");//
                }
                //promise.Dispose();
            }

        }

        public void Dispose()
        {
            //this.fileLogger.Log("Disposing Thread master", "ThreadMaster");

            this.isDisposed = true;
            while (isMainThreadActive)
                Thread.Sleep(5);
        }
    }
}