using GTA.Math;
using LemonUI;

namespace RPCore
{
    public sealed class PizzaJob : DeliveryJobSystem
    {
        internal static readonly Vector3 StartLocation = PizzaLocation;
        public PizzaJob(ObjectPool pool, AchievementSystem achievements, ShiftGate gate) : base(pool, achievements, gate, true) { }
    }
}
