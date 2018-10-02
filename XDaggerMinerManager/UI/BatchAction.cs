using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDaggerMinerManager.UI
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BatchAction
    {
        private List<T> iterateSubjects = null;

        private Action<T> mainAction = null;

        public BatchAction(List<T> subjects, Action<T> action)
        {
            this.iterateSubjects = subjects ?? throw new ArgumentNullException("Subjects cannot be null.");
            this.mainAction = action ?? throw new ArgumentNullException("MainAction cannot be null.");
        }

        public void Test<T>(List<T> sub)
        {

        }

        public bool IsSingleAction()
        {
            return this.iterateSubjects.Count == 1;
        }

        public bool IsEmptyAction()
        {
            return this.iterateSubjects.Count == 0;
        }

        public IEnumerable<Action> GenerateActions()
        {
            foreach(T subject in iterateSubjects)
            {
                yield return new Action(() => { mainAction(subject); });
            }
        }
    }
}
