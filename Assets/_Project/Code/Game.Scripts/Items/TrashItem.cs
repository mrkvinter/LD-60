namespace Game.Scripts.Items
{
    public sealed class TrashItem : BaseItem
    {
        public new TrashDef Def => (TrashDef)base.Def;

        public TrashItem(TrashDef def, IItemView view) : base(def, view)
        {
        }
    }
}
