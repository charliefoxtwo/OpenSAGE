using System.IO;
using System.Numerics;
using OpenSage.Data.Ini;
using OpenSage.FileFormats;
using OpenSage.Mathematics;

namespace OpenSage.Logic.Object
{
    public sealed class DozerAIUpdate : AIUpdate, IBuilderAIUpdate
    {
        private GameObject _buildTarget;

        internal DozerAIUpdate(GameObject gameObject, DozerAIUpdateModuleData moduleData)
            : base(gameObject, moduleData)
        {
        }

        internal override void Load(BinaryReader reader)
        {
            var version = reader.ReadVersion();
            if (version != 1)
            {
                throw new InvalidDataException();
            }

            base.Load(reader);

            // TODO
        }

        public void SetBuildTarget(GameObject gameObject)
        {
            // note that the order here is important, as SetTargetPoint will clear any existing buildTarget
            // TODO: target should not be directly on the building, but rather a point along the foundation perimeter
            SetTargetPoint(gameObject.Translation);
            _buildTarget = gameObject;
        }

        protected override void ArrivedAtDestination()
        {
            base.ArrivedAtDestination();

            if (_buildTarget is not null)
            {
                _buildTarget.Construct(_buildTarget.GameContext.Scene3D.Game.MapTime);
                GameObject.ModelConditionFlags.Set(ModelConditionFlag.ActivelyConstructing, true);
            }
        }

        internal override void SetTargetPoint(Vector3 targetPoint)
        {
            base.SetTargetPoint(targetPoint);
            GameObject.ModelConditionFlags.Set(ModelConditionFlag.ActivelyConstructing, false);
            _buildTarget?.PauseConstruction(_buildTarget.GameContext.Scene3D.Game.MapTime);
            ClearBuildTarget();
        }

        internal override void Update(BehaviorUpdateContext context)
        {
            base.Update(context);

            if (_buildTarget != null && _buildTarget.BuildProgress >= 1)
            {
                ClearBuildTarget();
                GameObject.ModelConditionFlags.Set(ModelConditionFlag.ActivelyConstructing, false);
            }
        }

        private void ClearBuildTarget()
        {
            _buildTarget = null;
        }
    }

    /// <summary>
    /// Allows the use of VoiceRepair, VoiceBuildResponse, VoiceNoBuild and VoiceTaskComplete 
    /// within UnitSpecificSounds section of the object.
    /// Requires Kindof = DOZER.
    /// </summary>
    public sealed class DozerAIUpdateModuleData : AIUpdateModuleData
    {
        internal new static DozerAIUpdateModuleData Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private new static readonly IniParseTable<DozerAIUpdateModuleData> FieldParseTable = AIUpdateModuleData.FieldParseTable
            .Concat(new IniParseTable<DozerAIUpdateModuleData>
            {
                { "RepairHealthPercentPerSecond", (parser, x) => x.RepairHealthPercentPerSecond = parser.ParsePercentage() },
                { "BoredTime", (parser, x) => x.BoredTime = parser.ParseInteger() },
                { "BoredRange", (parser, x) => x.BoredRange = parser.ParseInteger() },
            });

        public Percentage RepairHealthPercentPerSecond { get; private set; }
        public int BoredTime { get; private set; }
        public int BoredRange { get; private set; }

        internal override AIUpdate CreateAIUpdate(GameObject gameObject)
        {
            return new DozerAIUpdate(gameObject, this);
        }
    }
}
