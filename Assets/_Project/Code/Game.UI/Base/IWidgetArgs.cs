namespace Game.UI.Base
{
    public interface IWidgetArgs
    {
        public bool IsInstantShow { get; }
    }
    
    public abstract class BaseWidgetArgs : IWidgetArgs
    {
        public bool IsInstantShow { get; }

        protected BaseWidgetArgs(bool isInstantShow = false)
        {
            IsInstantShow = isInstantShow;
        }
    }
    
    public class WidgetArgs : BaseWidgetArgs
    {

        private WidgetArgs(bool isInstantShow) : base(isInstantShow)
        {
        }
        
        public static WidgetArgs Create(bool isInstantShow = false)
        {
            return new WidgetArgs(isInstantShow);
        }
    }
}