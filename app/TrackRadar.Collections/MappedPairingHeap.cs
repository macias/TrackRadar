using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar.Collections
{
    /// <summary>
    /// this is helper type for PairingHeap, it helps to keep inversed relation -- from the values hold in heap (tags) and the heap nodes
    /// </summary>
    /// <typeparam name="TWeight"></typeparam>
    /// <typeparam name="TKey">key for dictionary, should be immutable</typeparam>
    /// <typeparam name="TValue"></typeparam>
    public sealed class MappedPairingHeap<TKey, TWeight, TValue>
        where TWeight : IComparable<TWeight>
    {
        // current (after updates) tag values are stored at heap, not in dict (dict keeps only equal -- by comparer -- tag value)
        private readonly Dictionary<TKey, PairingHeapNode<TWeight, (TKey key, TValue value)>> dict;
        private readonly IComparer<TWeight> weightComparer;
        private PairingHeapNode<TWeight, (TKey key, TValue value)> root;

        public int Count => this.dict.Count;

        public IEnumerable<(TKey key, TWeight weight, TValue value)> Data => dict.Select(it => (it.Key, it.Value.Weight, it.Value.Tag.value));

        public MappedPairingHeap(IEqualityComparer<TKey> keyComparer = null, IComparer<TWeight> weightComparer = null)
        {
            this.dict = new Dictionary<TKey, PairingHeapNode<TWeight, (TKey, TValue)>>(keyComparer ?? EqualityComparer<TKey>.Default);
            this.root = null;
            this.weightComparer = weightComparer ?? Comparer<TWeight>.Default;
        }

        /// <summary>
        /// maintain key the same exactly like with Dictionary
        /// </summary>
        /// <param name="weight"></param>
        /// <param name="value"></param>
        /// <returns>true when weight was added/updated, false otherwise</returns>
        public bool TryAddOrUpdate(TKey key, TWeight weight, TValue value)
        {
            if (this.dict.TryGetValue(key, out var heap_node))
            {
                return root.TryDecreaseWeight(ref root, heap_node, weight, (key, value));
            }
            else
            {
                heap_node = PairingHeap.Add(ref root, weight, (key, value), weightComparer);
                this.dict.Add(key, heap_node);

                return true;
            }
        }

        public bool TryPop(out TKey key, out TWeight weight, out TValue value)
        {
            if (root == null)
            {
                weight = default;
                value = default;
                key = default;
                return false;
            }

            value = root.Tag.value;
            key = root.Tag.key;
            weight = root.Weight;
            this.root.Pop(ref root);
            this.dict.Remove(key);
            return true;
        }

        public bool ContainsKey(TKey key)
        {
            return this.dict.ContainsKey(key);
        }

        public bool TryGetData(TKey key, out TWeight weight, out TValue value)
        {
            if (!dict.TryGetValue(key, out var heap_node))
            {
                value = default;
                weight = default;
                return false;
            }

            value = heap_node.Tag.value;
            weight = heap_node.Weight;
            return true;
        }

    }
}