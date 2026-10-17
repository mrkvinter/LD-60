namespace Game.Scripts.Items
{
    public sealed class ResourceItem : BaseItem
    {
        public new ResourceDef Def => (ResourceDef)base.Def;

        public ResourceItem(ResourceDef def, IItemView view) : base(def, view)
        {
        }
    }
}
