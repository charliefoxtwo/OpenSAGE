﻿using System.Collections.Generic;
using OpenSage.Data.Ini;
using OpenSage.Mathematics;

namespace OpenSage.Logic.Object
{
    public class TransportContain : OpenContainModule
    {
        public override int TotalSlots => _moduleData.Slots;

        private readonly TransportContainModuleData _moduleData;

        private LogicFrame _nextEvacAllowedAfter; // unsure if this is correct, but seems plausible from testing?

        internal TransportContain(GameObject gameObject, TransportContainModuleData moduleData): base(gameObject, moduleData)
        {
            _moduleData = moduleData;
        }

        protected override int SlotValueForUnit(GameObject unit)
        {
            return unit.Definition.TransportSlotCount;
        }

        private protected override void UpdateModuleSpecific(BehaviorUpdateContext context)
        {
            if (_moduleData.HealthRegenPercentPerSecond != 0)
            {
                HealUnits(100_000 / _moduleData.HealthRegenPercentPerSecond); // (100% / regenpercentpersecond) * 1000ms
            }
            ModelConditionFlags.Set(ModelConditionFlag.Loaded, ContainedObjectIds.Count > 0);
        }

        protected override bool TryEvacUnit(LogicFrame currentFrame, uint unitId)
        {
            if (_nextEvacAllowedAfter < currentFrame)
            {
                RemoveUnit(unitId, false);
                if (_moduleData.ExitDelay > 0)
                {
                    // todo: humvee had DOOR_1_CLOSING ModelConditionFlag when between exits and DOOR_1_OPENING before first exit
                    var exitDelayFrames = _moduleData.ExitDelay / 1000f * Game.LogicFramesPerSecond;
                    _nextEvacAllowedAfter = currentFrame + new LogicFrameSpan((uint)exitDelayFrames);
                }
                return true;
            }

            return false;
        }

        internal override void Load(StatePersister reader)
        {
            reader.PersistVersion(1);

            reader.BeginObject("Base");
            base.Load(reader);
            reader.EndObject();

            var unknownInt1 = 1;
            reader.PersistInt32(ref unknownInt1);
            if (unknownInt1 != 1)
            {
                throw new InvalidStateException();
            }

            reader.SkipUnknownBytes(1);

            reader.PersistLogicFrame(ref _nextEvacAllowedAfter);
        }
    }

    /// <summary>
    /// Requires ExitStartN and ExitEndN bones defined unless overridden by <see cref="ExitBone"/>.
    /// Allows the use of SoundEnter And SoundExit UnitSpecificSounds.
    /// </summary>
    public class TransportContainModuleData : OpenContainModuleData
    {
        internal static TransportContainModuleData Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        internal static new readonly IniParseTable<TransportContainModuleData> FieldParseTable = OpenContainModuleData.FieldParseTable
            .Concat(new IniParseTable<TransportContainModuleData>
            {
                { "PassengersAllowedToFire", (parser, x) => x.PassengersAllowedToFire = parser.ParseBoolean() },
                { "Slots", (parser, x) => x.Slots = parser.ParseInteger() },
                { "HealthRegen%PerSec", (parser, x) => x.HealthRegenPercentPerSecond = parser.ParseInteger() },
                { "BurnedDeathToUnits", (parser, x) => x.BurnedDeathToUnits = parser.ParseBoolean() },
                { "ExitDelay", (parser, x) => x.ExitDelay = parser.ParseInteger() },
                { "GoAggressiveOnExit", (parser, x) => x.GoAggressiveOnExit = parser.ParseBoolean() },
                { "DoorOpenTime", (parser, x) => x.DoorOpenTime = parser.ParseInteger() },
                { "ScatterNearbyOnExit", (parser, x) => x.ScatterNearbyOnExit = parser.ParseBoolean() },
                { "OrientLikeContainerOnExit", (parser, x) => x.OrientLikeContainerOnExit = parser.ParseBoolean() },
                { "KeepContainerVelocityOnExit", (parser, x) => x.KeepContainerVelocityOnExit = parser.ParseBoolean() },
                { "ExitPitchRate", (parser, x) => x.ExitPitchRate = parser.ParseInteger() },
                { "ExitBone", (parser, x) => x.ExitBone = parser.ParseBoneName() },
                { "DestroyRidersWhoAreNotFreeToExit", (parser, x) => x.DestroyRidersWhoAreNotFreeToExit = parser.ParseBoolean() },
                { "InitialPayload", (parser, x) => x.InitialPayload = Payload.Parse(parser) },
                { "ArmedRidersUpgradeMyWeaponSet", (parser, x) => x.ArmedRidersUpgradeMyWeaponSet = parser.ParseBoolean() },
                { "WeaponBonusPassedToPassengers", (parser, x) => x.WeaponBonusPassedToPassengers = parser.ParseBoolean() },
                { "DelayExitInAir", (parser, x) => x.DelayExitInAir = parser.ParseBoolean() },
                { "ObjectStatusOfContained", (parser, x) => x.ObjectStatusOfContained = parser.ParseEnumBitArray<ObjectStatus>() },
                { "PassengerFilter", (parser, x) => x.PassengerFilter = ObjectFilter.Parse(parser) },
                { "ShowPips", (parser, x) => x.ShowPips = parser.ParseBoolean() },
                { "TypeOneForWeaponSet", (parser, x) => x.TypeOneForWeaponSet = parser.ParseEnum<ObjectKinds>() },
                { "TypeTwoForWeaponSet", (parser, x) => x.TypeTwoForWeaponSet = parser.ParseEnum<ObjectKinds>() },
                { "TypeOneForWeaponState", (parser, x) => x.TypeOneForWeaponState = parser.ParseEnum<ObjectKinds>() },
                { "TypeTwoForWeaponState", (parser, x) => x.TypeTwoForWeaponState = parser.ParseEnum<ObjectKinds>() },
                { "PassengerBonePrefix", (parser, x) => x.PassengerBonePrefixes.Add(PassengerBonePrefix.Parse(parser)) },
                { "KillPassengersOnDeath", (parser, x) => x.KillPassengersOnDeath = parser.ParseBoolean() },
                { "ManualPickUpFilter", (parser, x) => x.ManualPickUpFilter = ObjectFilter.Parse(parser) },
                { "EjectPassengersOnDeath", (parser, x) => x.EjectPassengersOnDeath = parser.ParseBoolean() },
                { "CanGrabStructure", (parser, x) => x.CanGrabStructure = parser.ParseBoolean() },
                { "GrabWeapon", (parser, x) => x.GrabWeapon = parser.ParseIdentifier() },
                { "FireGrabWeaponOnVictim", (parser, x) => x.FireGrabWeaponOnVictim = parser.ParseBoolean() },
                { "ReleaseSnappyness", (parser, x) => x.ReleaseSnappyness = parser.ParseFloat() },
                { "ForceOrientationContainer", (parser, x) => x.ForceOrientationContainer = parser.ParseBoolean() },
                { "CollidePickup", (parser, x) => x.CollidePickup = parser.ParseBoolean() },
                { "AllowOwnPlayerInsideOverride", (parser, x) => x.AllowOwnPlayerInsideOverride = parser.ParseBoolean() },
                { "BoneSpecificConditionState", (parser, x) => x.BoneSpecificConditionStates.Add(BoneSpecificConditionState.Parse(parser)) },
                { "FadeFilter", (parser, x) => x.FadeFilter = ObjectFilter.Parse(parser) },
                { "UpgradeCreationTrigger", (parser, x) => x.UpgradeCreationTriggers.Add(UpgradeCreationTrigger.Parse(parser)) },
                { "FadePassengerOnEnter", (parser, x) => x.FadePassengerOnEnter = parser.ParseBoolean() },
                { "EnterFadeTime", (parser, x) => x.EnterFadeTime = parser.ParseInteger() },
                { "FadePassengerOnExit", (parser, x) => x.FadePassengerOnExit = parser.ParseBoolean() },
                { "ExitFadeTime", (parser, x) => x.ExitFadeTime = parser.ParseInteger() },
                { "ConditionForEntry", (parser, x) => x.ConditionForEntry = parser.ParseAttributeEnum<ModelConditionFlag>("ModelConditionState") }
            });

        public bool PassengersAllowedToFire { get; private set; }
        public int Slots { get; private set; }
        public int HealthRegenPercentPerSecond { get; private set; }

        [AddedIn(SageGame.CncGeneralsZeroHour)]
        public bool BurnedDeathToUnits { get; private set; }

        /// <summary>
        /// Delay between successive exits
        /// </summary>
        public int ExitDelay { get; private set; }

        public bool GoAggressiveOnExit { get; private set; }
        /// <remarks>
        /// it seems like there's some default value for DoorOpenTime because
        /// 1) I was able to pause after starting an evac of a humvee and before anybody left
        /// 2) the inis have comments about how setting DoorOpenTime to 0 stops the DOOR_1_OPENING/CLOSING flags from being set
        /// </remarks>
        public int DoorOpenTime { get; private set; }
        public bool ScatterNearbyOnExit { get; private set; }
        public bool OrientLikeContainerOnExit { get; private set; }
        public bool KeepContainerVelocityOnExit { get; private set; }
        public int ExitPitchRate { get; private set; }
        public string ExitBone { get; private set; }
        public bool DestroyRidersWhoAreNotFreeToExit { get; private set; }
        public Payload InitialPayload { get; private set; }

        [AddedIn(SageGame.CncGeneralsZeroHour)]
        public bool ArmedRidersUpgradeMyWeaponSet { get; private set; }

        [AddedIn(SageGame.CncGeneralsZeroHour)]
        public bool WeaponBonusPassedToPassengers { get; private set; }

        [AddedIn(SageGame.CncGeneralsZeroHour)]
        public bool DelayExitInAir { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public BitArray<ObjectStatus> ObjectStatusOfContained { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public ObjectFilter PassengerFilter { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool ShowPips { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public ObjectKinds TypeOneForWeaponSet { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public ObjectKinds TypeTwoForWeaponSet { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public ObjectKinds TypeOneForWeaponState { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public ObjectKinds TypeTwoForWeaponState { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public List<PassengerBonePrefix> PassengerBonePrefixes { get; } = new List<PassengerBonePrefix>();

        [AddedIn(SageGame.Bfme)]
        public bool KillPassengersOnDeath { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public ObjectFilter ManualPickUpFilter { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool EjectPassengersOnDeath { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool CanGrabStructure { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public string GrabWeapon { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool FireGrabWeaponOnVictim { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public float ReleaseSnappyness { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public bool ForceOrientationContainer { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public bool CollidePickup { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public bool AllowOwnPlayerInsideOverride { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public List<BoneSpecificConditionState> BoneSpecificConditionStates { get; } = new List<BoneSpecificConditionState>();

        [AddedIn(SageGame.Bfme2)]
        public ObjectFilter FadeFilter { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public List<UpgradeCreationTrigger> UpgradeCreationTriggers { get; } = new List<UpgradeCreationTrigger>();

        [AddedIn(SageGame.Bfme2Rotwk)]
        public bool FadePassengerOnEnter { get; private set; }

        [AddedIn(SageGame.Bfme2Rotwk)]
        public int EnterFadeTime { get; private set; }

        [AddedIn(SageGame.Bfme2Rotwk)]
        public bool FadePassengerOnExit { get; private set; }

        [AddedIn(SageGame.Bfme2Rotwk)]
        public int ExitFadeTime { get; private set; }

        [AddedIn(SageGame.Bfme2Rotwk)]
        public ModelConditionFlag ConditionForEntry { get; private set; }

        internal override BehaviorModule CreateModule(GameObject gameObject, GameContext context)
        {
            return new TransportContain(gameObject, this);
        }
    }

    public sealed class PassengerBonePrefix
    {
        internal static PassengerBonePrefix Parse(IniParser parser)
        {
            return new PassengerBonePrefix
            {
                BoneName = parser.ParseAttribute("PassengerBone", parser.ScanBoneName),
                ObjectKind = parser.ParseAttributeEnum<ObjectKinds>("KindOf")
            };
        }

        public string BoneName { get; private set; }
        public ObjectKinds ObjectKind { get; private set; }
    }

    [AddedIn(SageGame.Bfme2)]
    public sealed class UpgradeCreationTrigger
    {
        internal static UpgradeCreationTrigger Parse(IniParser parser)
        {
            return new UpgradeCreationTrigger
            {
                Upgrade = parser.ParseAssetReference(),
                Model = parser.ParseAssetReference(),
                Unknown = parser.ParseInteger()
            };
        }

        public string Upgrade { get; private set; }
        public string Model { get; private set; }
        public int Unknown { get; private set; }
    }
}
