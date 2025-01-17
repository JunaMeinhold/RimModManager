using Hexa.NET.KittyUI;
using Hexa.NET.KittyUI.UI;
using RimModManager;
using System.Numerics;
using System.Reflection;

AppBuilder.Create()
    .AddWindow<MainWindow>(show: true, mainWindow: true)
    .AddTitleBar<TitleBar>()
    .EnableDebugTools(true)
    .AddFont(builder =>
    {
        var current = Assembly.GetExecutingAssembly();

        Span<uint> arialFull =
        [
            0x0020, 0x00FF, // Basic Latin + Latin Supplement
            0x0370, 0x03FF,
            0x2000, 0x206F, // General Punctuation
            0x3000, 0x30FF, // CJK Symbols and Punctuations, Hiragana, Katakana
            0x31F0, 0x31FF, // Katakana Phonetic Extensions
            0xFF00, 0xFFEF, // Half-width characters
            0xFFFD, 0xFFFD, // Invalid
            0x4e00, 0x9FAF, // CJK Ideograms
            0x3131, 0x3163, // Korean alphabets
            0xAC00, 0xD7A3, // Korean characters
            0xFFFD, 0xFFFD, // Invalid
            0x0400, 0x052F, // Cyrillic + Cyrillic Supplement
            0x2DE0, 0x2DFF, // Cyrillic Extended-A
            0xA640, 0xA69F, // Cyrillic Extended-B
            0x2010, 0x205E, // Punctuations
            0x0E00, 0x0E7F, // Thai
            0
        ];

        Span<uint> glyphMaterialRanges =
        [
            0xe003, 0xF8FF,
            0 // null terminator
        ];

        builder.AddFontFromEmbeddedResource(current, "RimModManager.assets.fonts.arialuni.ttf", 18f, arialFull)
        .SetOption(conf =>
        {
            conf.GlyphMinAdvanceX = 16f;
            conf.GlyphOffset = new Vector2(0f, 2f);
        })
        .AddFontFromEmbeddedResource("Hexa.NET.KittyUI.assets.fonts.MaterialSymbolsRounded.ttf", 18f, glyphMaterialRanges);
    })
    .AddFont("FA", builder =>
    {
        var current = Assembly.GetExecutingAssembly();

        Span<uint> arialFull =
        [
            0x0020, 0x00FF, // Basic Latin + Latin Supplement
            0x0370, 0x03FF,
            0x2000, 0x206F, // General Punctuation
            0x3000, 0x30FF, // CJK Symbols and Punctuations, Hiragana, Katakana
            0x31F0, 0x31FF, // Katakana Phonetic Extensions
            0xFF00, 0xFFEF, // Half-width characters
            0xFFFD, 0xFFFD, // Invalid
            0x4e00, 0x9FAF, // CJK Ideograms
            0x3131, 0x3163, // Korean alphabets
            0xAC00, 0xD7A3, // Korean characters
            0xFFFD, 0xFFFD, // Invalid
            0x0400, 0x052F, // Cyrillic + Cyrillic Supplement
            0x2DE0, 0x2DFF, // Cyrillic Extended-A
            0xA640, 0xA69F, // Cyrillic Extended-B
            0x2010, 0x205E, // Punctuations
            0x0E00, 0x0E7F, // Thai
            0
        ];

        Span<uint> glyphRanges =
        [
                0xe005, 0xe684,
                0xF000, 0xF8FF,
                0 // null terminator
        ];
        builder.AddFontFromEmbeddedResource(current, "RimModManager.assets.fonts.arialuni.ttf", 18f, arialFull)
        .SetOption(conf => conf.GlyphMinAdvanceX = 16)
        .AddFontFromEmbeddedResource(current, "RimModManager.assets.fonts.fa-solid-900.ttf", 18f, glyphRanges)
        .AddFontFromEmbeddedResource(current, "RimModManager.assets.fonts.fa-brands-400.ttf", 18f, glyphRanges);
    })
    .Run();