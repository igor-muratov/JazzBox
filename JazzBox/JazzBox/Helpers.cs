using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;

namespace JazzBox.Json
{
    public static class Helpers
    {
        public static void PassForDiffs(JToken expected, JToken actual, Action<Diff> diffFound)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual   == null) throw new ArgumentNullException(nameof(actual));

            if (expected.Type != actual.Type)
            {
                diffFound(new Diff(DiffType.Type, expected, actual));
            }
            switch (expected.Type)
            {
                case JTokenType.Object:
                    var ao = actual   as JObject;
                    var eo = expected as JObject;

                    var propComparer = new PropertyComparer();
                    var aProps       = new HashSet<JProperty>(ao.Properties(), propComparer);

                    aProps.IntersectWith(eo.Properties());

                    foreach(var p in aProps)
                        PassForDiffs(eo.Property(p.Name).Value, ao.Property(p.Name).Value, diffFound);

                    if (aProps.Count != ao.Count)
                    {
                        var symmEx = new HashSet<JProperty>(ao.Properties(), propComparer);
                        symmEx.SymmetricExceptWith(eo.Properties());

                        foreach(var p in symmEx)
                        {
                            diffFound(new Diff(
                                eo.Property(p.Name) != null ? DiffType.ExtraActual : DiffType.ExpectedNotFound,
                                eo.Property(p.Name),
                                ao.Property(p.Name)));
                        }
                    }
                    break;
                case JTokenType.Array:
                    var aa = actual   as JArray;
                    var ea = expected as JArray;

                    if (aa.Count != ea.Count)
                        diffFound(new Diff(DiffType.ArrayCountMismatch, aa, ea));
                    else
                        for (int i = 0; i < aa.Count; i++)
                            PassForDiffs(ea[i], aa[i], diffFound);
                    break;
                case JTokenType.Property:
                    var ap = actual   as JProperty;
                    var ep = expected as JProperty;
                    var pDiffs = new List<Diff>(2);
                    if (ap.Name != ep.Name)
                        diffFound(new Diff(DiffType.PropNameDiff, ep, ap));

                    PassForDiffs(ap.Value, ep.Value, diffFound);
                    break;
                case JTokenType.Comment:
                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Null:
                case JTokenType.Undefined:
                case JTokenType.Date:
                case JTokenType.Raw:
                case JTokenType.Bytes:
                case JTokenType.Guid:
                case JTokenType.Uri:
                case JTokenType.TimeSpan:
                    if (!JValue.EqualityComparer.Equals(expected, actual))
                    {
                        diffFound(new Diff(DiffType.ValueDiff, expected, actual));
                    }
                    break;
                default:
                    throw new NotSupportedException("Unsupported JToken type " + expected.Type);
            }
        }

        public static IList<Diff> GetDiff(this JToken expected, JToken actual)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual   == null) throw new ArgumentNullException(nameof(actual));

            var diffs = new List<Diff>();
            PassForDiffs(expected, actual, d => diffs.Add(d));
            return diffs;
        }

        public class PropertyComparer : IEqualityComparer<JProperty>
        {
            public bool Equals(JProperty x, JProperty y)
            {
                return x.Name.Equals(y.Name);
            }

            public int GetHashCode(JProperty obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }

    public struct Diff
    {
        public DiffType Type   { get; private set; }
        public JToken Expected { get; private set; }
        public JToken Actual   { get; private set; }

        public Diff(DiffType tp, JToken expected, JToken actual)
        {
            Type     = tp;
            Expected = expected;
            Actual   = actual;
        }
    }

    public enum DiffType
    {
        Type,
        ValueDiff,
        PropNameDiff,
        ExpectedNotFound,
        ExtraActual,
        ArrayCountMismatch
    }
}
