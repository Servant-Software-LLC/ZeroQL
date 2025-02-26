﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZeroQL.Json;

public static class ZeroQLJsonOptions
{
    static ZeroQLJsonOptions()
    {
        Options = Create();
    }

    public static JsonSerializerOptions Options { get; private set; }

    public static JsonSerializerOptions Create() => new()
    {
        Converters =
        {
            new ZeroQLEnumStringConverter(),
            new ZeroQLUploadJsonConverter(),
            new ZeroQLDateOnlyConverter()
        },
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static void Configure(Action<JsonSerializerOptions> configure)
    {
        var newOptions = Create();
        configure(newOptions);

        Options = newOptions;
    }
}