using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;

namespace JazzBox.Json
{
    public static class Helpers
    {        
        public static IEnumerable<Diff> GetDiff(this JToken expected, JToken actual)
        {
            if (expected.Type != actual.Type)
            {
                return new[] {
                    new Diff(DiffType.Type, expected, actual)
                };
            }
            switch (expected.Type)
            {
                case JTokenType.Object:
                    var ao = actual   as JObject;
                    var eo = expected as JObject;

                    var propComparer = new PropertyComparer();
                    var aProps = new HashSet<JProperty>(ao.Properties(), propComparer);
                    aProps.IntersectWith(eo.Properties());
                    var valueDiffs = aProps.SelectMany(p => GetDiff(eo.Property(p.Name).Value, ao.Property(p.Name).Value));

                    IEnumerable<Diff> propDiff = new Diff[0];

                    if (aProps.Count != ao.Count)
                    {
                        var symmEx = new HashSet<JProperty>(ao.Properties(), propComparer);
                        symmEx.SymmetricExceptWith(eo.Properties());
                        propDiff = symmEx.Select(p => new Diff(
                            eo.Property(p.Name) != null ? DiffType.ExtraActual : DiffType.ExpectedNotFound,
                            eo.Property(p.Name),
                            ao.Property(p.Name)));
                    }
                    return propDiff.Concat(valueDiffs);
                case JTokenType.Array:
                    var aa = actual as JArray;
                    var ea = expected as JArray;

                    if (aa.Count != ea.Count)
                        return new List<Diff> { new Diff(DiffType.ArrayCountMismatch, aa, ea) };
                    else
                        return aa.Children()
                            .Zip(ea.Children(), (a, e) => GetDiff(e, a))
                            .SelectMany(i => i);
                case JTokenType.Property:
                    var ap = actual as JProperty;
                    var ep = expected as JProperty;
                    var pDiffs = new List<Diff>(2);
                    if (ap.Name != ep.Name)
                        pDiffs.Add(new Diff(DiffType.NameDiff, ep, ap));
                    pDiffs.AddRange(GetDiff(ap.Value, ep.Value));
                    return pDiffs;
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
                        return new[] {
                            new Diff(DiffType.ValueDiff, expected, actual)
                        };
                    }
                    return new List<Diff>();
                default:
                    throw new NotSupportedException("Unsupported JToken type " + expected.Type);
            }
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
        NameDiff,
        ExpectedNotFound,
        ExtraActual,
        ArrayCountMismatch
    }
}
