using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Compressor.Extensions;
using NUnit.Framework;

namespace Compressor.Tests.ExtensionsTests
{
    internal class DictionaryExtensionsTests
    {
        public static IEnumerable<TestCaseData> ParamsModelValidationData
        {
            get
            {
                yield return new TestCaseData(CreateDictionaryWithValues<int>(10))
                    .SetName("Remove all values (10) from dictionary with not disposable values");
                yield return new TestCaseData(CreateDictionaryWithValues<int>(50))
                    .SetName("Remove all values (50) from dictionary with not disposable values");
                yield return new TestCaseData(CreateDictionaryWithValues<int>(100))
                    .SetName("Remove all values (100) from dictionary with not disposable values");
                yield return new TestCaseData(CreateDictionaryWithValues<int>(500))
                    .SetName("Remove all values (500) from dictionary with not disposable values");
                yield return new TestCaseData(CreateDictionaryWithValues<int>(1000))
                    .SetName("Remove all values (1000) from dictionary with not disposable values");
                yield return new TestCaseData(CreateDictionaryWithValues<int>(5000))
                    .SetName("Remove all values (5000) from dictionary with not disposable values");
                yield return new TestCaseData(CreateDictionaryWithValues<int>(10000))
                    .SetName("Remove all values (10000) from dictionary with not disposable values");
                yield return new TestCaseData(CreateDictionaryWithValues<Stream>(10))
                    .SetName("Remove all values (10) from dictionary with disposable values");
                yield return new TestCaseData(CreateDictionaryWithValues<Stream>(50))
                    .SetName("Remove all values (50) from dictionary with disposable values");
                yield return new TestCaseData(CreateDictionaryWithValues<Stream>(100))
                    .SetName("Remove all values (100) from dictionary with disposable values");
                yield return new TestCaseData(CreateDictionaryWithValues<Stream>(500))
                    .SetName("Remove all values (500) from dictionary with disposable values");
                yield return new TestCaseData(CreateDictionaryWithValues<Stream>(1000))
                    .SetName("Remove all values (1000) from dictionary with disposable values");
                yield return new TestCaseData(CreateDictionaryWithValues<Stream>(5000))
                    .SetName("Remove all values (5000) from dictionary with disposable values");
                yield return new TestCaseData(CreateDictionaryWithValues<Stream>(10000))
                    .SetName("Remove all values (10000) from dictionary with disposable values");
            }
        }

        [Test]
        public void TryAddDuplicateValue_GetArgumentException()
        {
            var dictionary = new Dictionary<int, int>();
            var locker = new object();
            dictionary.SafeAdd(0, 0, locker);
            Assert.Throws<ArgumentException>(() => dictionary.SafeAdd(0, 0, locker));
        }

        [Test]
        [TestCase(10, TestName = "Add 10 numbers to Dictionary asynchronously")]
        [TestCase(50, TestName = "Add 50 numbers to Dictionary asynchronously")]
        [TestCase(100, TestName = "Add 100 numbers to Dictionary asynchronously")]
        [TestCase(500, TestName = "Add 500 numbers to Dictionary asynchronously")]
        [TestCase(1000, TestName = "Add 1000 numbers to Dictionary asynchronously")]
        [TestCase(5000, TestName = "Add 5000 numbers to Dictionary asynchronously")]
        [TestCase(10000, TestName = "Add 10000 numbers to Dictionary asynchronously")]
        [Parallelizable(ParallelScope.All)]
        public void TryAddValue_InDifferentThreads_AllValuesWillBeAdded(int dictionaryLength)
        {
            var locker = new object();
            var dictionary = new Dictionary<int, int>();
            var threads = new List<Thread>();

            // Act
            for (var i = 0; i < dictionaryLength; i++)
            {
                var key = i;
                var thread = new Thread(() => dictionary.SafeAdd(key, key, locker));
                thread.Start();
                threads.Add(thread);
            }

            threads.ForEach(thread => thread.Join());

            // Assert
            for (var i = 0; i < dictionaryLength; i++)
                Assert.IsTrue(dictionary.ContainsKey(i));
        }

        [Test]
        [TestCaseSource(nameof(ParamsModelValidationData))]
        [Parallelizable(ParallelScope.All)]
        public void TryDeleteAllValue_WithExistedValues_GetEmptyDictionary<TValue>(Dictionary<int, TValue> dictionary)
        {
            var locker = new object();
            var dictionaryLength = dictionary.Count;
            var threads = new List<Thread>();

            for (var i = 0; i < dictionaryLength; i++)
            {
                var key = i;
                var thread = new Thread(() => dictionary.SafeDelete(key, locker));
                thread.Start();
                threads.Add(thread);
            }

            threads.ForEach(thread => thread.Join());

            // Assert
            Assert.AreEqual(0, dictionary.Count);
        }

        [Test]
        public void TryDeleteValue_WithNotExistingKey_GetKeyNotFoundException()
        {
            var dictionary = new Dictionary<int, int>();
            var locker = new object();
            Assert.Throws<KeyNotFoundException>(() => dictionary.SafeDelete(0, locker));
        }

        private static Dictionary<int, TValue> CreateDictionaryWithValues<TValue>(int dictionaryLength)
        {
            var dictionary = new Dictionary<int, TValue>();
            for (var i = 0; i < dictionaryLength; i++) dictionary.Add(i, default(TValue));

            return dictionary;
        }
    }
}