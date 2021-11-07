using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar.Collections
{    
    public sealed class PairingHeapNode<TWeight, TData>
    {
        // https://www.cs.cmu.edu/~sleator/papers/pairing-heaps.pdf
        // https://en.wikipedia.org/wiki/Pairing_heap#Structure
        // https://brilliant.org/wiki/pairing-heap/
        // https://arxiv.org/pdf/1709.01152.pdf
        // http://www.cs.princeton.edu/courses/archive/spr09/cos423/Lectures/rp-heaps.pdf
        // https://pdfs.semanticscholar.org/737c/99f3fc947c8715909bfe4339946dabaf83fc.pdf

        // there pointers we use are not "official", but thanks to this choice Update (aka decrease-key) is O(1)
        private PairingHeapNode<TWeight, TData> previous;
        private PairingHeapNode<TWeight, TData> leftChild;
        private PairingHeapNode<TWeight, TData> rightSibling;

        private readonly IComparer<TWeight> comparer;

        public TWeight Weight { get; private set; }
        public TData Tag { get; private set; }

        public PairingHeapNode(TWeight weight, TData tag, IComparer<TWeight> comparer)
        {
            this.Weight = weight;
            this.Tag = tag;
            this.comparer = comparer;
        }

        public PairingHeapNode<TWeight, TData> Add(ref PairingHeapNode<TWeight, TData> root, TWeight weight, TData tag)
        {
            if (this != root)
                throw new ArgumentException("You need to pass root of the heap");

            var node = new PairingHeapNode<TWeight, TData>(weight, tag, this.comparer);
            root = merge(this, node);
            return node;
        }

        /// <summary>
        /// `this` has to be the same as `root`
        /// </summary>
        /// <param name="root"></param>
        public void Pop(ref PairingHeapNode<TWeight, TData> root)
        {
            if (this != root)
                throw new ArgumentException("You need to pass root of the heap");

            if (this.leftChild == null)
                root = null;
            else
            {
                var children = getSiblings(this.leftChild).ToList();
                children.Reverse();

                foreach (var child in children)
                {
                    child.previous = null;
                    child.rightSibling = null;
                }

                int idx;
                if (children.Count % 2 == 0)
                {
                    root = merge(children[0], children[1]);
                    idx = 2;
                }
                else
                {
                    root = children[0];
                    idx = 1;
                }

                for (; idx < children.Count; idx += 2)
                {
                    root = merge(root, merge(children[idx], children[idx + 1]));
                }
            }
        }

        /// <summary>
        /// `this` has to be the same as `root`
        /// </summary>
        /// <param name="root">root of the heap</param>
        /// <param name="node">node to update the weight</param>
        /// <param name="weight"></param>
        /// <param name="tag"></param>
        public void DecreaseWeight(ref PairingHeapNode<TWeight, TData> root, PairingHeapNode<TWeight, TData> node, TWeight weight, TData tag)
        {
            if (this != root)
                throw new ArgumentException("You need to pass root of the heap");

            int comparison = root.comparer.Compare(node.Weight, weight);
            if (comparison < 0)
                throw new ArgumentException($"Cannot increase the {nameof(weight)} from {node.Weight} to {weight}");

            node.Weight = weight;
            node.Tag = tag;

            if (comparison == 0)
                return;

            if (node.previous != null)
            {
                node.detachSubHeap();
                root = merge(root, node);
            }
        }

        private static PairingHeapNode<TWeight, TData> merge(PairingHeapNode<TWeight, TData> rootA, PairingHeapNode<TWeight, TData> rootB)
        {
            if (rootA.comparer.Compare(rootA.Weight, rootB.Weight) <= 0)
            {
                rootA.attach(rootB);
                return rootA;
            }
            else
            {
                rootB.attach(rootA);
                return rootB;
            }
        }

        private void attach(PairingHeapNode<TWeight, TData> other)
        {
            if (this.leftChild != null)
            {
                other.rightSibling = this.leftChild;
                this.leftChild.previous = other;
            }

            other.previous = this;
            this.leftChild = other;
        }

        // keep it static because node can be null
        private static IEnumerable<PairingHeapNode<TWeight, TData>> getSiblings(PairingHeapNode<TWeight, TData> node)
        {
            while (node != null)
            {
                yield return node;
                node = node.rightSibling;
            }
        }

        private void detachSubHeap()
        {
            if (this.previous.leftChild == this)
                this.previous.leftChild = this.rightSibling;
            else if (this.previous.rightSibling == this)
                this.previous.rightSibling = this.rightSibling;
            else
                throw new InvalidOperationException("Not possible");

            if (this.rightSibling != null)
                this.rightSibling.previous = this.previous;

            this.previous = null;
            this.rightSibling = null;
        }
    }
}