using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace ValheimRaids.Scripts
{
    public class RaidBuilding
    {
        public static Piece FloorPiecePrefab;
        public static Piece RaidRampPrefab;

        public static RaidTowerPiece PlacePiece(Piece piece, Vector3 position, Quaternion rotation)
        {
            TerrainModifier.SetTriggerOnPlaced(trigger: true);
            GameObject gameObject = UnityEngine.Object.Instantiate(piece.gameObject, position, rotation);
            TerrainModifier.SetTriggerOnPlaced(trigger: false);
            WearNTear wearNTear = gameObject.GetComponent<WearNTear>();
            wearNTear.OnPlaced();
            piece.m_placeEffect.Create(position, rotation, gameObject.transform);
            Jotunn.Logger.LogMessage("Placed Piece " + piece.name + " at " + position + "::" + rotation);
            return gameObject.GetComponent<RaidTowerPiece>();
        }

        public static RaidTowerPiece PlaceFloorPiece(Vector3 position, Quaternion rotation)
        {
            TerrainModifier.SetTriggerOnPlaced(trigger: true);
            GameObject gameObject = UnityEngine.Object.Instantiate(FloorPiecePrefab.gameObject, position, rotation);
            TerrainModifier.SetTriggerOnPlaced(trigger: false);
            FloorPiecePrefab.m_placeEffect.Create(position, rotation, gameObject.transform);
            Jotunn.Logger.LogMessage("Placed Piece " + FloorPiecePrefab.name + " at " + position + "::" + rotation);
            return gameObject.GetComponent<RaidTowerPiece>();
        }

        public static RaidRamp[] PlaceRampPieces(RaidTowerPiece floorPiece)
        {
            var position1 = floorPiece.ramp1.position + floorPiece.ramp1.right;
            var position2 = floorPiece.ramp2.position - floorPiece.ramp2.right;
            var rotation1 = floorPiece.transform.rotation * Quaternion.Euler(0, 180, 0);
            var rotation2 = floorPiece.transform.rotation;
            TerrainModifier.SetTriggerOnPlaced(trigger: true);
            GameObject ramp1 = UnityEngine.Object.Instantiate(RaidRampPrefab.gameObject, position1, rotation1);
            GameObject ramp2 = UnityEngine.Object.Instantiate(RaidRampPrefab.gameObject, position2, rotation2);
            TerrainModifier.SetTriggerOnPlaced(trigger: false);

            RaidRampPrefab.m_placeEffect.Create(position1, rotation1, ramp1.transform);
            RaidRampPrefab.m_placeEffect.Create(position2, rotation2, ramp2.transform);
            Jotunn.Logger.LogMessage("Placed Piece " + RaidRampPrefab.name + " at " + position1 + "::" + rotation1);
            Jotunn.Logger.LogMessage("Placed Piece " + RaidRampPrefab.name + " at " + position2 + "::" + rotation2);

            RaidRamp[] ramps = { ramp1.GetComponent<RaidRamp>(), ramp2.GetComponent<RaidRamp>() };
            return ramps;
        }
    }
}
