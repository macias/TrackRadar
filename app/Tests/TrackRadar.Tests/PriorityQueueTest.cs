using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TrackRadar.Collections;

namespace TrackRadar.Tests
{
    [TestClass]
    public class PriorityQueueTest
    {
        [TestMethod]
        public void MapWithSameWeightTest()
        {
            var map = new MappedPairingHeap<string, double,ValueTuple>();
            map.TryAddOrUpdate( "hello",0, new ValueTuple());
            map.TryAddOrUpdate( "world", 0, new ValueTuple());
            map.TryAddOrUpdate( "!", 0, new ValueTuple());

            string s;
            Assert.IsTrue(map.TryPop(out s, out _,  out _));
            Assert.AreEqual("hello", s);
            Assert.IsTrue(map.TryPop(out s, out _, out _));
            Assert.AreEqual("world", s);
            Assert.IsTrue(map.TryPop(out s, out _, out _));
            Assert.AreEqual("!", s);

            Assert.IsFalse(map.TryPop(out s, out _,  out _));
        }

        [TestMethod]
        public void AddingTest()
        {
            var root = PairingHeap.CreateRoot(3, "hello");
            root.Add(ref root, 5, "world");
            root.Add(ref root, 1, "now");

            Assert.AreEqual(1, root.Weight);
            Assert.AreEqual("now", root.Tag);

            root.Pop(ref root);

            Assert.AreEqual(3, root.Weight);
            Assert.AreEqual("hello", root.Tag);

            root.Pop(ref root);

            Assert.AreEqual(5, root.Weight);
            Assert.AreEqual("world", root.Tag);

            root.Pop(ref root);

            Assert.IsNull(root);
        }

        [TestMethod]
        public void ChangingWeightsTest()
        {
            var a = PairingHeap.CreateRoot(13, "x");
            var root = a;
            var b= root.Add(ref root, 25, "y");
            var c = root.Add(ref root, 31, "z");

            root.DecreaseWeight(ref root, b, 5, "world");
            root.DecreaseWeight(ref root, a, 3, "hello");
            root.DecreaseWeight(ref root, c, 1, "now");


            Assert.AreEqual(1, root.Weight);
            Assert.AreEqual("now", root.Tag);

            root.Pop(ref root);

            Assert.AreEqual(3, root.Weight);
            Assert.AreEqual("hello", root.Tag);

            root.Pop(ref root);

            Assert.AreEqual(5, root.Weight);
            Assert.AreEqual("world", root.Tag);

            root.Pop(ref root);

            Assert.IsNull(root);
        }

        [TestMethod]
        public void MixedModificationsTest()
        {
            var a = PairingHeap.CreateRoot(33, "x");
            var root = a;
            var d = root.Add(ref root, 0, "nothing");

            Assert.AreEqual(0, root.Weight);
            Assert.AreEqual("nothing", root.Tag);

            root.Pop(ref root);

            var c = root.Add(ref root, 41, "z");
            root.DecreaseWeight(ref root, c, 1, "now");

            Assert.AreEqual(1, root.Weight);
            Assert.AreEqual("now", root.Tag);

            root.Pop(ref root);

            var b = root.Add(ref root, 25, "y");
            root.DecreaseWeight(ref root, a, 3, "hello");
            root.DecreaseWeight(ref root, b, 5, "world");


            Assert.AreEqual(3, root.Weight);
            Assert.AreEqual("hello", root.Tag);

            root.Pop(ref root);

            Assert.AreEqual(5, root.Weight);
            Assert.AreEqual("world", root.Tag);

            root.Pop(ref root);

            Assert.IsNull(root);
        }

    }
}