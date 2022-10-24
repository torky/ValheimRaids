using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimRaids.Scripts
{
    public class RaidBuilding
    {
        public static Piece FloorPiecePrefab;

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
            WearNTear wearNTear = gameObject.GetComponent<WearNTear>();
            wearNTear.OnPlaced();
            FloorPiecePrefab.m_placeEffect.Create(position, rotation, gameObject.transform);
            Jotunn.Logger.LogMessage("Placed Piece " + FloorPiecePrefab.name + " at " + position + "::" + rotation);
            return gameObject.GetComponent<RaidTowerPiece>();
        }
    }
}
