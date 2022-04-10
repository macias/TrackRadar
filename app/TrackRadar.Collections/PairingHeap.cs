using System;
using System.Collections.Generic;

namespace TrackRadar.Collections
{
    public static class PairingHeap
    {
        public static PairingHeapNode<TWeight, TTag> CreateRoot<TWeight, TTag>(TWeight weight, TTag tag)
            where TWeight : IComparable<TWeight>
        {
            return CreateRoot(weight, tag, Comparer<TWeight>.Default);
        }

        public static PairingHeapNode<TWeight, TTag> CreateRoot<TWeight, TTag>(TWeight weight, TTag tag,
            IComparer<TWeight> weightComparer)
        {
            return new PairingHeapNode<TWeight, TTag>(weight, tag, weightComparer);
        }

        public static PairingHeapNode<TWeight, TTag> Add<TWeight, TTag>(ref PairingHeapNode<TWeight, TTag> root, TWeight weight, TTag tag)
            where TWeight : IComparable<TWeight>
        {
            return Add(ref root, weight, tag, Comparer<TWeight>.Default);
        }

        public static PairingHeapNode<TWeight, TTag> Add<TWeight, TTag>(ref PairingHeapNode<TWeight, TTag> root, TWeight weight, TTag tag,
            IComparer<TWeight> weightComparer)
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