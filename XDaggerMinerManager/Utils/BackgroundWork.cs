using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace XDaggerMinerManager.Utils
{
    public class BackgroundWork<T>
    {
        private Window Window = null;

        private Action BeginAction = null;

        private Func<T> WorkFunction = null;

        private Action<BackgroundWorkResult<T>> EndAction = null;

        public static BackgroundWork<T> CreateWork(Window window, Action begin, Func<T> func, Action<BackgroundWorkResult<T>> end)
        {
            BackgroundWork<T> work = new BackgroundWork<T>();

            work.Window = window;
            work.BeginAction = begin;
            work.EndAction = end;
            work.WorkFunction = func;

            return work;
        }

        /// <summary>
        /// Main function to execute the work
        /// </summary>
        public void Execute()
        {
            if (WorkFunction == null)
            {
                throw new ArgumentNullException("Work Function cannot be null.");
            }

            BeginAction?.Invoke();

            Func<BackgroundWorkResult<T>> functionWrapper = (() =>
                {
                    try
                    {
                        T resultValue = WorkFunction();
                        return BackgroundWorkResult<T>.CreateResult(resultValue);
                    }
                    catch (Exception ex)
                    {
                        return BackgroundWorkResult<T>.ErrorResult(ex);
                    }
                }
            );

            if (EndAction == null)
            {
                Task.Factory.StartNew(functionWrapper);
                return;
            }
            
            Action<Task<BackgroundWorkResult<T>>> endActionWrapper = ((taskResult) =>
                this.Window.Dispatcher.Invoke(new Action(() => this.EndAction(taskResult.Result)))
            );

            try
            {
                Task.Factory.StartNew(functionWrapper).ContinueWith(endActionWrapper);
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException("Execute BackgroundWork Failed.", ex);
            }
        }
    }

    public class BackgroundWorkResult<T>
    {
        public T Result
        {
            get; private set;
        }

        public Exception Exception
        {
            get; private set;
        }

        public BackgroundWorkResult()
        {

        }

        public static BackgroundWorkResult<T> CreateResult(T resultVal)
        {
            BackgroundWorkResult<T> result = new BackgroundWorkResult<T>();
            result.Result = resultVal;
            return result;
        }

        public static BackgroundWorkResult<T> ErrorResult(Exception ex)
        {
            BackgroundWorkResult<T> result = new BackgroundWorkResult<T>();
            result.Exception = ex;
            return result;
        }

        public bool HasError
        {
            get
            {
                return this.Exception != null;
            }
        }
    }



}
