using System.Collections;

namespace LFramework
{
    public abstract class YieldInstruction
    {
        public virtual bool IsDone => IsCompleted();
        protected abstract bool IsCompleted();

        internal IEnumerator AsEnumerator()
        {
            while (!IsDone)
            {
                yield return false;
            }

            yield return true;
        }
    }
}