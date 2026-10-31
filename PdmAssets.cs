using System.Runtime.CompilerServices;
using GTA;
namespace RPCore
{
    // Read-only entity proof: no decorators, world sweeps or persistent ped registrations.
    internal static class PdmAssets
    {
        static readonly ConditionalWeakTable<Entity,PdmEntityProof> owned=new ConditionalWeakTable<Entity,PdmEntityProof>();
        public static T Track<T>(T entity) where T:Entity
        {
            if(entity!=null && entity.Exists()){
                owned.Remove(entity);owned.Add(entity,new PdmEntityProof(entity.Handle,entity.Model.Hash,entity.MemoryAddress.ToInt64()));
            }return entity;
        }
        public static bool Valid(Entity entity)
        {
            if(entity==null || !entity.Exists())return false;
            return !owned.TryGetValue(entity,out var p) || p.Matches(entity.Handle,entity.Model.Hash,entity.MemoryAddress.ToInt64());
        }
    }
}
