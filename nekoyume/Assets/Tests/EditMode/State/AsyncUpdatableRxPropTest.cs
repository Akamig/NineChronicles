﻿using System.Collections;
using Cysharp.Threading.Tasks;
using Nekoyume.State;
using NUnit.Framework;
using UnityEngine.TestTools;
using UniRx;

namespace Tests.EditMode.State
{
    public class AsyncUpdatableRxPropTest
    {
        [DatapointSource]
        public int[] values = { default, int.MaxValue, int.MinValue };

        [Test]
        public void ConstructTest()
        {
            var rp = new AsyncUpdatableRxProp<int>(UpdateValueAsync);
            Assert.AreEqual(default(int), rp.Value);
        }

        [Theory]
        public void ConstructWithDefaultValueTest(int value)
        {
            var rp = new AsyncUpdatableRxProp<int>(value, UpdateValueAsync);
            Assert.AreEqual(value, rp.Value);
        }

        [UnityTest]
        public IEnumerator UpdateAsyncTest() => UniTask.ToCoroutine(async () =>
        {
            var rp = new AsyncUpdatableRxProp<int>(UpdateValueAsync);
            var value = await rp.UpdateAsync();
            Assert.AreEqual(1, value);
            Assert.AreEqual(value, rp.Value);
        });

        [UnityTest]
        public IEnumerator UpdateAsObservableTest() => UniTask.ToCoroutine(async () =>
        {
            var rp = new AsyncUpdatableRxProp<int>(UpdateValueAsync);
            var value = await rp.UpdateAsObservable().ToUniTask();
            Assert.AreEqual(1, value);
            value = await rp.UpdateAsObservable().ToUniTask();
            Assert.AreEqual(2, value);
        });
        
        [UnityTest]
        public IEnumerator SubscribeWithUpdateOnceTest() => UniTask.ToCoroutine(async () =>
        {
            var done = false;
            var expected = default(int);
            var rp = new AsyncUpdatableRxProp<int>(UpdateValueAsync);
            var disposable = rp.SubscribeWithUpdateOnce(value =>
            {
                Assert.AreEqual(expected, value);
                if (expected == 1)
                {
                    done = true;
                    return;
                }

                expected++;
            });
            await UniTask.WaitUntil(() => done);
            disposable.Dispose();
        });

        [UnityTest]
        public IEnumerator SubscribeTest() => UniTask.ToCoroutine(async () =>
        {
            var done = false;
            var expected = default(int);
            var rp = new AsyncUpdatableRxProp<int>(UpdateValueAsync);
            var disposable = rp.Subscribe(value =>
            {
                Assert.AreEqual(expected, value);
                if (expected == 2)
                {
                    done = true;
                    return;
                }

                rp.UpdateAsync().Forget();
                expected++;
            });
            await UniTask.WaitUntil(() => done);
            disposable.Dispose();
        });

        private static async UniTask<int> UpdateValueAsync(int previous)
        {
            await UniTask.Delay(10);
            return previous + 1;
        }
    }
}
