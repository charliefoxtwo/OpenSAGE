﻿using System.Collections.Generic;
using System.Numerics;
using OpenSage.Graphics;
using OpenSage.Logic.Object;
using OpenSage.Mathematics;

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

        public GhostObject()
        {
            _modelsPerPlayer = new List<ModelInstance>[Player.MaxPlayers];
            for (var i = 0; i < _modelsPerPlayer.Length; i++)
            {
                _modelsPerPlayer[i] = new List<ModelInstance>();
            }
        }

        internal void Load(StatePersister reader)
        {
            reader.PersistVersion(1);
            reader.PersistVersion(1);

            uint objectId = 0;
            reader.PersistObjectID(ref objectId);
            _gameObject = reader.Game.Scene3D.GameLogic.GetObjectById(objectId);

            reader.PersistEnum(ref _geometryType);

            // Sometimes there's a 0xC, which is probably uninitialized data.
            byte geometryIsSmall = _geometryIsSmall ? (byte)1 : (byte)0;
            reader.PersistByte("GeometryIsSmall", ref geometryIsSmall);
            _geometryIsSmall = geometryIsSmall == 1;

            reader.PersistSingle(ref _geometryMajorRadius);
            reader.PersistSingle(ref _geometryMinorRadius);
            reader.PersistSingle(ref _angle);
            reader.PersistVector3(ref _position);

            reader.SkipUnknownBytes(12);

            for (var i = 0; i < Player.MaxPlayers; i++)
            {
                byte numModels = 0;
                reader.PersistByte("NumModels", ref numModels);

                for (var j = 0; j < numModels; j++)
                {
                    var modelName = "";
                    reader.PersistAsciiString(ref modelName);

                    var model = reader.AssetStore.Models.GetByName(modelName);
                    var modelInstance = model.CreateInstance(reader.AssetStore.LoadContext);

                    _modelsPerPlayer[i].Add(modelInstance);

                    var scale = 1.0f;
                    reader.PersistSingle(ref scale);
                    if (scale != 1.0f)
                    {
                        throw new InvalidStateException();
                    }

                    reader.PersistColorRgba(ref modelInstance.HouseColor);

                    reader.PersistVersion(1);

                    var modelTransform = Matrix4x3.Identity;
                    reader.PersistMatrix4x3(ref modelTransform, readVersion: false);

                    modelInstance.SetWorldMatrix(modelTransform.ToMatrix4x4());

                    var numMeshes = (uint)model.SubObjects.Length;
                    reader.PersistUInt32(ref numMeshes);
                    if (numMeshes > 0 && numMeshes != model.SubObjects.Length)
                    {
                        throw new InvalidStateException();
                    }

                    for (var k = 0; k < numMeshes; k++)
                    {
                        var meshName = "";
                        reader.PersistAsciiString(ref meshName);

                        if (meshName != model.SubObjects[k].FullName)
                        {
                            throw new InvalidStateException();
                        }

                        reader.PersistBoolean("UnknownBool", ref modelInstance.UnknownBools[k]);

                        var meshTransform = Matrix4x3.Identity;
                        reader.PersistMatrix4x3(ref meshTransform, readVersion: false);

                        // TODO: meshTransform is actually absolute, not relative.
                        modelInstance.RelativeBoneTransforms[model.SubObjects[k].Bone.Index] = meshTransform.ToMatrix4x4();
                    }
                }
            }

            reader.PersistBoolean("HasUnknownThing", ref _hasUnknownThing);
            if (_hasUnknownThing)
            {
                reader.PersistByte("UnknownByte", ref _unknownByte);
                reader.PersistUInt32(ref _unknownInt);
            }
        }
    }
}
