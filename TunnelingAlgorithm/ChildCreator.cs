using System;

namespace TunnelingAlgorithm
{
    internal abstract class ChildCreator<ChildT, ParentT> where ChildT : Tunneler where ParentT : Tunneler
    {
        protected ParentT _parent;
        protected Random _rand;

        public ChildCreator(ParentT parent, int seed)
        {
            _parent = parent;
            _rand = new Random(seed);
        }

        public abstract ChildT CreateChild(int childSeed);
    }
}
