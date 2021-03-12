using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar.Collections
{
    public static class PairingHeap
    {
        public static PairingHeapNode<TWeight, TTag> CreateRoot<TWeight, TTag>(TWeight weight, TTag tag)
            where TWeight : IComparable<TWeight>
        {
            return new PairingHeapNode<TWeight, TTag>(weight, tag, Comparer<TWeight>.Default);
        }

        public static PairingHeapNode<TWeight, TTag> Add<TWeight, TTag>(ref PairingHeapNode<TWeight, TTag> root, TWeight weight, TTag tag)
            where TWeight : IComparable<TWeight>
        {
            if (root == null)
            {
                root = CreateRoot(weight, tag);
                return root;
            }
            else
                return root.Add(ref root, weight, tag);
        }
    }

    public sealed class PairingHeapNode<TWeight, TTag>
    {
        // https://www.cs.cmu.edu/~sleator/papers/pairing-heaps.pdf
        // https://en.wikipedia.org/wiki/Pairing_heap#Structure
        // https://brilliant.org/wiki/pairing-heap/
        // https://arxiv.org/pdf/1709.01152.pdf
        // http://www.cs.princeton.edu/courses/archive/spr09/cos423/Lectures/rp-heaps.pdf
        // https://pdfs.semanticscholar.org/737c/99f3fc947c8715909bfe4339946dabaf83fc.pdf

        // there pointers we use are not "official", but thanks to this choice Update (aka decrease-key) is O(1)
        private PairingHeapNode<TWeight, TTag> previous;
        private PairingHeapNode<TWeight, TTag> leftChild;
        private PairingHeapNode<TWeight, TTag> rightSibling;

        private readonly IComparer<TWeight> comparer;

        public TWeight Weight { get; private set; }
        public TTag Tag { get; private set; }

        public PairingHeapNode(TWeight value, TTag tag, IComparer<TWeight> comparer)
        {
            this.Weight = value;
            this.Tag = tag;
            this.comparer = comparer;
        }

        public PairingHeapNode<TWeight, TTag> Add(ref PairingHeapNode<TWeight, TTag> root, TWeight weight, TTag tag)
        {
            if (this != root)
                throw new ArgumentException("You need to pass root of the heap");

            var node = new PairingHeapNode<TWeight, TTag>(weight, tag, this.comparer);
            root = merge(this, node);
            return node;
        }

        /// <summary>
        /// `this` has to be the same as `root`
        /// </summary>
        /// <param name="root"></param>
        public void Pop(ref PairingHeapNode<TWeight, TTag> root)
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
        public void DecreaseWeight(ref PairingHeapNode<TWeight, TTag> root, PairingHeapNode<TWeight, TTag> node, TWeight weight, TTag tag)
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

        private static PairingHeapNode<TWeight, TTag> merge(PairingHeapNode<TWeight, TTag> rootA, PairingHeapNode<TWeight, TTag> rootB)
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

        private void attach(PairingHeapNode<TWeight, TTag> other)
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
        private static IEnumerable<PairingHeapNode<TWeight, TTag>> getSiblings(PairingHeapNode<TWeight, TTag> node)
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
                throw new NotImplementedException("Not possible");

            if (this.rightSibling != null)
                this.rightSibling.previous = this.previous;

            this.previous = null;
            this.rightSibling = null;
        }
    }
}