﻿using System.Collections.Generic;
using System.Numerics;
using OpenSage.Graphics;
using OpenSage.Logic.Object;

namespace OpenSage.Logic
{
    public sealed class GhostObject
    {
        public uint OriginalObjectId;

        private GameObject _gameObject;

        private ObjectGeometry _geometryType;
        private bool _geometryIsSmall;
        private float _geometryMajorRadius;
        private float _geometryMinorRadius;

        private float _angle;
        private Vector3 _position;

        private readonly List<ModelInstance>[] _modelsPerPlayer;

        private bool _hasUnknownThing;
        private byte _unknownByte;
        private uint _unknownInt;

        internal GhostObject()
        {
            _modelsPerPlayer = new List<ModelInstance>[Player.MaxPlayers];
            for (var i = 0; i < _modelsPerPlayer.Length; i++)
            {
                _modelsPerPlayer[i] = new List<ModelInstance>();
            }
        }

        internal void Load(SaveFileReader reader, GameLogic gameLogic, Game game)
        {
            reader.ReadVersion(1);
            reader.ReadVersion(1);

            uint objectId = 0;
            reader.ReadObjectID(ref objectId);
            _gameObject = gameLogic.GetObjectById(objectId);

            reader.ReadEnum<ObjectGeometry>(ref _geometryType);

            // Sometimes there's a 0xC0, when it should be 0x0.
            byte geometryIsSmall = 0;
            reader.ReadByte(ref geometryIsSmall);
            _geometryIsSmall = geometryIsSmall == 1;

            _geometryMajorRadius = reader.ReadSingle();
            _geometryMinorRadius = reader.ReadSingle();

            _angle = reader.ReadSingle();
            reader.ReadVector3(ref _position);

            reader.SkipUnknownBytes(12);

            for (var i = 0; i < Player.MaxPlayers; i++)
            {
                byte numModels = 0;
                reader.ReadByte(ref numModels);

                for (var j = 0; j < numModels; j++)
                {
                    var modelName = "";
                    reader.ReadAsciiString(ref modelName);

                    var model = game.AssetStore.Models.GetByName(modelName);
                    var modelInstance = model.CreateInstance(game.AssetStore.LoadContext);

                    _modelsPerPlayer[i].Add(modelInstance);

                    var scale = reader.ReadSingle();
                    if (scale != 1.0f)
                    {
                        throw new InvalidStateException();
                    }

                    modelInstance.HouseColor = reader.ReadColorRgba();

                    reader.ReadVersion(1);

                    var modelTransform = reader.ReadMatrix4x3Transposed();

                    modelInstance.SetWorldMatrix(modelTransform.ToMatrix4x4());

                    var numMeshes = reader.ReadUInt32();
                    if (numMeshes > 0 && numMeshes != model.SubObjects.Length)
                    {
                        throw new InvalidStateException();
                    }

                    for (var k = 0; k < numMeshes; k++)
                    {
                        var meshName = "";
                        reader.ReadAsciiString(ref meshName);

                        if (meshName != model.SubObjects[k].FullName)
                        {
                            throw new InvalidStateException();
                        }

                        modelInstance.UnknownBools[k] = reader.ReadBoolean();

                        var meshTransform = reader.ReadMatrix4x3Transposed();

                        // TODO: meshTransform is actually absolute, not relative.
                        modelInstance.RelativeBoneTransforms[model.SubObjects[k].Bone.Index] = meshTransform.ToMatrix4x4();
                    }
                }
            }

            _hasUnknownThing = reader.ReadBoolean();
            if (_hasUnknownThing)
            {
                reader.ReadByte(ref _unknownByte);
                _unknownInt = reader.ReadUInt32();
            }
        }
    }
}
