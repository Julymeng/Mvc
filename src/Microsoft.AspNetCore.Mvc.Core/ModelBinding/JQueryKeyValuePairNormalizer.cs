// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    // Normalizes keys, in a keyvaluepair collection, from jQuery format to a format that MVC understands.
    internal static class JQueryKeyValuePairNormalizer
    {
        public static IDictionary<string, StringValues> GetValues(
            IEnumerable<KeyValuePair<string, StringValues>> originalValues,
            int valueCount)
        {
            var builder = new StringBuilder();
            var dictionary = new Dictionary<string, StringValues>(
                valueCount,
                StringComparer.OrdinalIgnoreCase);
            foreach (var originalValue in originalValues)
            {
                var normalizedKey = NormalizeJQueryToMvc(builder, originalValue.Key);
                builder.Clear();

                dictionary[normalizedKey] = originalValue.Value;
            }

            return dictionary;
        }

        // This is a helper method for Model Binding over a JQuery syntax.
        // Normalize from JQuery to MVC keys. The model binding infrastructure uses MVC keys.
        // x[] --> x
        // [] --> ""
        // x[12] --> x[12]
        // x[field]  --> x.field, where field is not a number
        private static string NormalizeJQueryToMvc(StringBuilder builder, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            var indexOpen = key.IndexOf('[');
            if (indexOpen == -1)
            {

                // Fast path, no normalization needed.
                // This skips string conversion and allocating the string builder.
                return key;
            }

            var position = 0;
            while (position < key.Length)
            {
                if (indexOpen == -1)
                {
                    // No more brackets.
                    builder.Append(key, position, key.Length - position);
                    break;
                }

                builder.Append(key, position, indexOpen - position); // everything up to "["

                // Find closing bracket.
                var indexClose = key.IndexOf(']', indexOpen);
                if (indexClose == -1)
                {
                    throw new ArgumentException(
                        message: Resources.FormatJQueryFormValueProviderFactory_MissingClosingBracket(key),
                        paramName: nameof(key));
                }

                if (indexClose == indexOpen + 1)
                {
                    // Empty brackets signify an array. Just remove.
                }
                else if (char.IsDigit(key[indexOpen + 1]))
                {
                    // Array index. Leave unchanged.
                    builder.Append(key, indexOpen, indexClose - indexOpen + 1);
                }
                else
                {
                    // Field name. Convert to dot notation.
                    builder.Append('.');
                    builder.Append(key, indexOpen + 1, indexClose - indexOpen - 1);
                }

                position = indexClose + 1;
                indexOpen = key.IndexOf('[', position);
            }

            return builder.ToString();
        }
    }
}