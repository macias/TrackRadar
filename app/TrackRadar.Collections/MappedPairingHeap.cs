using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar.Collections
{
    /// <summary>
    /// this is helper type for PairingHeap, it helps to keep inversed relation -- from the values hold in heap (tags) and the heap nodes
    /// </summary>
    /// <typeparam name="TWeight"></typeparam>
    /// <typeparam name="TTag"></typeparam>
    public sealed class MappedPairingHeap<TWeight, TTag>
        where TWeight : IComparable<TWeight>
    {
        // current (after updates) tag values are stored at heap, not in dict
        private readonly Dictionary<TTag, PairingHeapNode<TWeight, TTag>> dict;
        private PairingHeapNode<TWeight, TTag> root;

        //public (TTag tag, TWeight weight) Top => (root.Tag, root.Weight);
        public int Count => this.dict.Count;

        public MappedPairingHeap(IEqualityComparer<TTag> comparer = null)
        {
            this.dict = new Dictionary<TTag, PairingHeapNode<TWeight, TTag>>(comparer ?? EqualityComparer<TTag>.Default);
            this.root = null;
        }

        public void AddOrUpdate(TWeight weight, TTag tag)
        {
            if (this.dict.TryGetValue(tag, out PairingHeapNode<TWeight, TTag> heap_node))
            {
                if (weight.CompareTo(heap_node.Weight) < 0)
                {
                    // only heap get its tag updated, but not dict
                    root.DecreaseWeight(ref root, heap_node, weight, tag);
                }
            }
            else
            {
                heap_node = PairingHeap.Add(ref root, weight, tag);
                this.dict.Add(tag, heap_node);
            }
        }

        public bool TryPop(out TWeight weight, out TTag tag)
        {
            if (root==null)
            {
                weight = default;
                tag = default;
                return false;
            }

            tag = root.Tag;
            weight = root.Weight;
            this.root.Pop(ref root);
            this.dict.Remove(tag);
            return true;
        }
    }
}