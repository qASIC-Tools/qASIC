using Microsoft.VisualBasic;
using qASIC.Parsing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace qASIC.Console.Autocomplete
{
    public class ACData : IEnumerable<ACVariant>
    {
        public List<ACVariant> Variants { get; private set; } = new List<ACVariant>();

        public IEnumerator<ACVariant> GetEnumerator() =>
            Variants.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        public ACVariant AddVariant()
        {
            var variant = new ACVariant(this);
            Variants.Add(variant);
            return variant;
        }

        public List<ACVariant> GetValidVariants(qCommandArgument[] args)
        {
            List<ACVariant> variants = new List<ACVariant>();
            variants.AddRange(Do(Variants.Where(x => x.Arguments.Count == args.Length), 0));
            variants.AddRange(Do(Variants.Where(x => x.Arguments.Count > args.Length), 0));

            IEnumerable<ACVariant> Do(IEnumerable<ACVariant> v, int i)
            {
                if (i >= args.Length)
                    return v;

                var types = args[i].values.Select(x => x.GetType())
                    .Concat(args[i].Parser.Select(x => x.ValueType))
                    .Distinct();

                List<ACVariant> res = new List<ACVariant>();

                foreach (var t in types)
                {
                    if (!args[i].CanGetValue(t))
                        continue;

                    var val = v.Where(x => i < x.Arguments.Count)
                        .Where(x => x.Arguments[i].type == t);

                    res.AddRange(Do(val, i + 1));
                }

                return res;
            }

            return variants;
        }
    }
}
