﻿using OpenSage.Data.Ini;
using OpenSage.Data.Ini.Parser;

namespace OpenSage.Logic.Object
{
    [AddedIn(SageGame.Bfme)]
    public sealed class DelayedDeathBodyModuleData : ActiveBodyModuleData
    {
        internal static new DelayedDeathBodyModuleData Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static new readonly IniParseTable<DelayedDeathBodyModuleData> FieldParseTable = ActiveBodyModuleData.FieldParseTable
            .Concat(new IniParseTable<DelayedDeathBodyModuleData>
            {
                { "DelayedDeathTime", (parser, x) => x.DelayedDeathTime = parser.ParseInteger() },
                { "CanRespawn", (parser, x) => x.CanRespawn = parser.ParseBoolean() },
            });

        public int DelayedDeathTime { get; private set; }
        public bool CanRespawn { get; private set; }
    }
}
