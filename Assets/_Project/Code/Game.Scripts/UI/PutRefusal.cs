using Game.Scripts.Construction;
using Game.Scripts.Items;

namespace Game.Scripts.UI
{
    public static class PutRefusal
    {
        public static string Describe(IItemReceiver receiver, BaseItem item) => receiver switch
        {
            Rack rack => Describe(rack, item),
            ConstructionSite site => Describe(site, item),
            _ => $"{item.Def.Name} does not go here"
        };

        private static string Describe(Rack rack, BaseItem item)
        {
            var def = item.Def;
            if (rack.Type != RackType.None && rack.Type != def.RackType)
            {
                return $"{def.Name} needs a {def.RackType.ToString().ToLowerInvariant()} rack";
            }

            if (rack.Filter == RackFilter.Trash && item is not TrashItem) return "Only trash goes here";
            if (rack.Filter == RackFilter.Resource && item is not ResourceItem) return "Only resources go here";
            if (rack.StoredDef != null && !Equals(rack.StoredDef, def)) return $"This rack holds {rack.StoredDef.Name}";

            return "This rack is full";
        }

        private static string Describe(ConstructionSite site, BaseItem item)
        {
            foreach (var requirement in site.Requirements)
            {
                if (Equals(requirement.Resource, item.Def)) return $"{site.Name} has enough {item.Def.Name}";
            }

            return $"{site.Name} does not need {item.Def.Name}";
        }
    }
}
