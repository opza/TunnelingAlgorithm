using System;

namespace TunnelingAlgorithm
{
    internal abstract class Childer<ChildT, ParentT> where ChildT : Tunneler where ParentT : Tunneler
    {
        protected ParentT _parent;
        protected Random _rand;

        public Childer(ParentT parent, int? seed = null)
        {
            _parent = parent;
            if(seed.HasValue)
                _rand = new Random(seed.Value);
            else
                _rand = new Random();
        }

        public abstract ChildT CreateChild(int? childSeed);
    }
}
