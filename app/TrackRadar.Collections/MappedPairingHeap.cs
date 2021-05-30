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
    public sealed class MappedPairingHeap<TWeight, TTag, TTagExtract>
        where TWeight : IComparable<TWeight>
    {
        // current (after updates) tag values are stored at heap, not in dict (dict keeps only equal -- by comparer -- tag value)
        private readonly Dictionary<TTagExtract, PairingHeapNode<TWeight, TTag>> dict;
        private readonly Func<TTag, TTagExtract> tagExtractor;
        private PairingHeapNode<TWeight, TTag> root;

        public int Count => this.dict.Count;

        public IEnumerable<TTag> Tags => dict.Values.Select(it => it.Tag);

        public MappedPairingHeap(Func<TTag,TTagExtract> tagExtractor, IEqualityComparer<TTagExtract> comparer = null)
        {
            this.dict = new Dictionary<TTagExtract, PairingHeapNode<TWeight, TTag>>(comparer ?? EqualityComparer<TTagExtract>.Default);
            this.root = null;
            this.tagExtractor = tagExtractor;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="weight"></param>
        /// <param name="tag"></param>
        /// <returns>true when weight was added/updated, false otherwise</returns>
        public bool TryAddOrUpdate(TWeight weight, TTag tag)
        {
            TTagExtract extract = tagExtractor(tag);
            if (this.dict.TryGetValue(extract, out PairingHeapNode<TWeight, TTag> heap_node))
            {
                if (weight.CompareTo(heap_node.Weight) >= 0)
                    return false;

                // only heap get its tag updated, but not dict
                root.DecreaseWeight(ref root, heap_node, weight, tag);
            }
            else
            {
                heap_node = PairingHeap.Add(ref root, weight, tag);
                this.dict.Add(extract, heap_node);
            }

            return true;
        }

        public bool TryPop(out TWeight weight, out TTag tag)
        {
            if (root == null)
            {
                weight = default;
                tag = default;
                return false;
            }

            tag = root.Tag;
            weight = root.Weight;
            this.root.Pop(ref root);
            this.dict.Remove(tagExtractor(tag));
            return true;
        }

        public bool Contains(TTagExtract tagExtract)
        {
            return this.dict.ContainsKey(tagExtract);
        }

        public bool TryGetTagValue(TTagExtract tagExtract, out TTag tagValue)
        {
            if (!dict.TryGetValue(tagExtract, out var heap_node))
            {
                tagValue = default;
                return false;
            }

            tagValue = heap_node.Tag;
            return true;
        }

    }
}