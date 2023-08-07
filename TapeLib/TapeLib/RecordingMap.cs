using ReflectionUtility;
using System;
using System.Collections.Generic;
using TapeLib;
using UnityEngine;

namespace TapeLib
{
    class RecordingMap // [LEGACY CODE]
    {
        public class MapKeyFrame
        {
            public List<TileAction> actions = new List<TileAction>();
        }

        public enum ActionType { changeTileType }

        public class TileAction
        {
            // === RECORDING DATA ===
            private static Dictionary<string, string> lastTypes = new Dictionary<string, string>();

            // === ACTION DATA ===
            public ActionType   type;
            public string       tileType;
            public string       ID;

            public void execute()
            {
                switch (type)
                {
                    case ActionType.changeTileType:
                        WorldTile tile = World.world.tilesList[Int32.Parse(ID)];
                        TileType type = AssetManager.tiles.get(tileType);
                        TopTileType topType = AssetManager.topTiles.get(tileType);
                        MapAction.terraformTile(tile, type, topType, TerraformLibrary.draw);
                        return;
                }
            }

            public TileAction(string tileId, ActionType type, string tileType = null)
            {
                this.type = type;
                this.tileType = tileType;
                ID = tileId;


                Tape tape = Tape.currentTape;
                tape.currentKeyFrame.map.actions.Add(this);
            }

            public static string TryGetLastType(string tileId)
            {
                if (!lastTypes.ContainsKey(tileId)) { return null; }
                return lastTypes[tileId];
            }

            public static void ChangeLastType(string tileId, string tileType)
            {
                if (TryGetLastType(tileId) == null) { lastTypes.Add(tileId, tileType); return; }
                lastTypes[tileId] = tileType;
            }

        }

        public static void RecordMap()
        {
            MapKeyFrame keyframe = new MapKeyFrame();
            Tape.currentTape.currentKeyFrame.map = keyframe;

            int index = 0;
            foreach (WorldTile tile in World.world.tilesList)
            {
                string lastType = TileAction.TryGetLastType(index.ToString());
                string currentType = tile.cur_tile_type.drawLayerName;
                if (currentType != lastType)
                {
                    new TileAction(index.ToString(), ActionType.changeTileType, currentType);
                    TileAction.ChangeLastType(index.ToString(), currentType);
                }
                index++;
            }
        }

        public static void DisplayMap()
        {
            foreach (TileAction info in Tape.currentTape.currentDisplayKeyFrame.map.actions)
            {
                info.execute();
            }
        }
    }
}
