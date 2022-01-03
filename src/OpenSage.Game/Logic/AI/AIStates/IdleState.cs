﻿namespace OpenSage.Logic.AI.AIStates
{
    internal sealed class IdleState : State
    {
        private ushort _unknownShort;
        private bool _unknownBool1;
        private bool _unknownBool2;

        internal override void Load(StatePersister reader)
        {
            reader.PersistVersion(1);

            reader.PersistUInt16(ref _unknownShort);
            reader.PersistBoolean("UnknownBool1", ref _unknownBool1);
            reader.PersistBoolean("UnknownBool2", ref _unknownBool2);
        }
    }
}
