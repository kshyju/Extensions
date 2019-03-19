// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.Configuration.Ini
{
    /// <summary>
    /// An INI file based <see cref="ConfigurationProvider"/>.
    /// Files are simple line structures (<a href="https://en.wikipedia.org/wiki/INI_file">INI Files on Wikipedia</a>)
    /// </summary>
    /// <examples>
    /// [Section:Header]
    /// key1=value1
    /// key2 = " value2 "
    /// ; comment
    /// # comment
    /// / comment
    /// </examples>
    public class IniConfigurationProvider : FileConfigurationProvider
    {
        /// <summary>
        /// Initializes a new instance with the specified source.
        /// </summary>
        /// <param name="source">The source settings.</param>
        public IniConfigurationProvider(IniConfigurationSource source) : base(source) { }

        /// <summary>
        /// Loads the INI data from a stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        public override void Load(Stream stream)
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (var reader = new StreamReader(stream))
            {
                var sectionPrefix = string.Empty;

                while (reader.Peek() != -1)
                {
                    var rawLine = reader.ReadLine();
                    var line = rawLine.AsSpan().Trim();

                    // Ignore blank lines
                    if (line.IsEmpty || line.IsWhiteSpace())
                    {
                        continue;
                    }
                    // Ignore comments
                    if (line[0] == ';' || line[0] == '#' || line[0] == '/')
                    {
                        continue;
                    }
                    // [Section:header] 
                    if (line[0] == '[' && line[line.Length - 1] == ']')
                    {
                        // remove the brackets
                        sectionPrefix = line.Slice(1, line.Length - 2).ToString() + ConfigurationPath.KeyDelimiter;
                        continue;
                    }

                    // key = value OR "value"
                    int separator = line.IndexOf('=');
                    if (separator < 0)
                    {
                        throw new FormatException(Resources.FormatError_UnrecognizedLineFormat(rawLine));
                    }

                    var keySpan = line.Slice(0, separator).Trim();
                    string key = sectionPrefix + keySpan.ToString();

                    if (data.ContainsKey(key))
                    {
                        throw new FormatException(Resources.FormatError_KeyIsDuplicated(key));
                    }

                    var valueSpan = line.Slice(separator + 1).Trim();

                    // Remove quotes
                    if (valueSpan.Length > 1 && valueSpan[0] == '"' && valueSpan[valueSpan.Length - 1] == '"')
                    {
                        var value = valueSpan.Slice(1, valueSpan.Length - 2);

                        data[key] = value.ToString();
                        continue;
                    }

                    data[key] = valueSpan.ToString();                    
                }
            }

            Data = data;
        }
    }
}
