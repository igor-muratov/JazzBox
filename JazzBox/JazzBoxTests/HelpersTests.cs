﻿using NUnit.Framework;
using JazzBox.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace JazzBox.Json.Tests
{
    [TestFixture()]
    public class HelpersTests
    {
        [Test()]
        public void GetDiffTest()
        {
            var x = JToken.Parse(@"{
                a: 1,
                b: ['2', 'w'],
                e: 9,
                x: []
            }");

            var y = JToken.Parse(@"{
                a: 3,
                b: ['2', 'p'],
                c: 'u',
                x: [1]
            }");

            var d = x.GetDiff(y)
                .OrderBy(i => i.Type)
                .ToList();

            Assert.AreEqual(5, d.Count);
            Assert.IsTrue(d.Select(e => e.Type).SequenceEqual(new[] {
               DiffType.ValueDiff,
               DiffType.ValueDiff,
               DiffType.ExpectedNotFound,
               DiffType.ExtraActual,
               DiffType.ArrayCountMismatch
           }));
        }
    }
}