namespace LFramework.RedDotSystem
{
    public abstract class BaseRedDotCondition
    {
        public abstract string Path { get; }

        public virtual bool IsAutoCheck { get; } = false;

        public abstract int TriggerCondition();
    }
}