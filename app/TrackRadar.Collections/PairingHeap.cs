using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar.Collections
{
    public static class PairingHeap
    {
        public static PairingHeapNode<TWeight, TTag> CreateRoot<TWeight, TTag>(TWeight weight, TTag tag,
            IComparer<TWeight> weightComparer = null)
            where TWeight : IComparable<TWeight>
        {
            return new PairingHeapNode<TWeight, TTag>(weight, tag, weightComparer ?? Comparer<TWeight>.Default);
        }

        public static PairingHeapNode<TWeight, TTag> Add<TWeight, TTag>(ref PairingHeapNode<TWeight, TTag> root, TWeight weight, TTag tag,
            IComparer<TWeight> weightComparer = null)
            where TWeight : IComparable<TWeight>
        {
            if (root == null)
            {
                root = CreateRoot(weight, tag, weightComparer);
                return root;
            }
            else
                return root.Add(ref root, weight, tag);
        }
    }
}