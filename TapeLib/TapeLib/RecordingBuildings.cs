using UnityEngine;
using System.Collections.Generic;
using TapeLib;

namespace TapeLib
{
    class RecordingBuildings // [LEGACY CODE]
    {
        public class BuildingsKeyFrame
        {
            public List<BuildingAction> actions = new List<BuildingAction>();
        }

        // changeScale is for whenever someone hits or builds a construction.
        public enum ActionType { changeScale, spawnBuilding, changeSprite }
        public class BuildingAction
        {
            // === RECORDING DATA ===
            private static Dictionary<Building, Sprite>     lastSprites = new Dictionary<Building, Sprite>();
            private static Dictionary<Building, Vector3>    lastScales = new Dictionary<Building, Vector3>();
            public static Dictionary<string, GameObject>    fakeBuildings = new Dictionary<string, GameObject>();

            // === ACTION DATA ===
            public ActionType   type;
            public Sprite       sprite;
            public Vector3      vectorData;
            public string       ID;

            public void execute()
            {
                switch (type)
                {
                    case ActionType.changeScale:
                        GameObject building = GetFakeBuildingById(ID);
                        if (building == null) { return; }
                        building.transform.localScale = vectorData;
                        return;
                    case ActionType.spawnBuilding:
                        building = GetFakeBuildingById(ID);
                        if (building == null) { return; }
                        building.transform.position = vectorData;
                        return;
                    case ActionType.changeSprite:
                        building = GetFakeBuildingById(ID);
                        if (building == null) { return; }
                        building.GetComponent<SpriteRenderer>().sprite = sprite;
                        return;
                }
            }

            /// <param name="vectorData">In case the action type does is not required set to Vector3.zero</param>
            public BuildingAction(Building building, ActionType type, Vector3 vectorData, Sprite sprite = null)
            {
                this.type = type;
                this.vectorData = vectorData;
                this.sprite = sprite;

                ID = building.data.id;

                Tape tape = Tape.currentTape;
                tape.currentKeyFrame.buildings.actions.Add(this);
            }

            public static Sprite TryGetLastSprite(Building building)
            {
                if (!lastSprites.ContainsKey(building)) { return null; }
                return lastSprites[building];
            }

            public static void ChangeLastSprite(Building building, Sprite sprite)
            {
                if (TryGetLastSprite(building) == null) { lastSprites.Add(building, sprite); return; }
                lastSprites[building] = sprite;
            }

            public static Vector3 TryGetLastScale(Building building)
            {
                if (!lastScales.ContainsKey(building)) { return Vector3.zero; }
                return lastScales[building];
            }

            public static void ChangeLastScale(Building building, Vector3 scale)
            {
                if (TryGetLastSprite(building) == null) { lastScales.Add(building, scale); return; }
                lastScales[building] = scale;
            }

            private static GameObject GetBuildingTemplate()
            {
                GameObject returnThis = new GameObject("fake_building_template");
                SpriteRenderer renderer = returnThis.AddComponent<SpriteRenderer>();
                returnThis.transform.localScale = new Vector3(0.25f, 0.25f);
                returnThis.transform.parent = GameObject.Find("WorldBox/Buildings").transform;
                renderer.sharedMaterial = LibraryMaterials.instance.mat_world_object;
                renderer.sortingLayerName = "Objects";
                return returnThis;
            }

            private static GameObject GetFakeBuildingById(string id)
            {
                if (fakeBuildings.ContainsKey(id))
                {
                    fakeBuildings[id].SetActive(true);
                    return fakeBuildings[id];
                }
                GameObject returnObject = GetBuildingTemplate();
                returnObject.name = "fake_building_" + id;
                fakeBuildings.Add(id, returnObject);
                return returnObject;
            }
        }

        public static void RecordBuildings()
        {
            BuildingsKeyFrame keyframe = new BuildingsKeyFrame();
            Tape.currentTape.currentKeyFrame.buildings = keyframe;

            foreach (Building building in MapBox.instance.buildings)
            {
                Sprite sprite = GetSpriteWithoutRendering(building);
                Sprite lastSprite = BuildingAction.TryGetLastSprite(building);
                Vector3 scale = BuildingAction.TryGetLastScale(building);

                if (lastSprite == null)
                {
                    new BuildingAction(building, ActionType.spawnBuilding, building.currentPosition, sprite);
                }

                if (scale != (Vector3)building.currentScale)
                {
                    new BuildingAction(building, ActionType.changeScale, building.currentScale, sprite);
                }

                if (sprite != lastSprite)
                {
                    new BuildingAction(building, ActionType.changeSprite, Vector3.zero, sprite);
                }
            }
        }

        public static void DisplayBuildings()
        {
            BuildingsKeyFrame keyFrame = Tape.currentTape.currentDisplayKeyFrame.buildings;
            foreach (BuildingAction action in keyFrame.actions)
            {
                action.execute();
            }
        }

        public static void RemoveAllDisplay()
        {
            foreach (var fakeBuilding in BuildingAction.fakeBuildings)
            {
                fakeBuilding.Value.SetActive(false);
            }
        }

        private static Sprite GetSpriteWithoutRendering(Building building)
        {
            // CODE STOLEN FROM ASSEMBLY
            bool flag = true;
            bool flag2 = building.isRuin();
            if (flag2)
            {
                flag = false;
            }
            if (building.isUnderConstruction())
            {
                return building.asset.sprites.construction;
            }
            Sprite[] array;
            if (building.asset.has_special_animation_state)
            {
                if (building.hasResources)
                {
                    array = building.animData.main;
                }
                else
                {
                    array = building.animData.special;
                }
            }
            else if (flag2 && building.asset.has_ruins_graphics)
            {
                flag = false;
                array = building.animData.ruins;
            }
            else if (building.asset.spawn_drops && building.data.hasFlag(S.stop_spawn_drops))
            {
                array = building.animData.main_disabled;
            }
            else if (building.data.state == BuildingState.CivAbandoned)
            {
                if (building.animData.main_disabled.Length != 0)
                {
                    array = building.animData.main_disabled;
                }
                else
                {
                    array = building.animData.main;
                }
                flag = false;
            }
            else
            {
                array = building.animData.main;
                if (building.asset.get_override_sprite_main != null)
                {
                    Sprite[] array2 = building.asset.get_override_sprite_main(building);
                    if (array2 != null)
                    {
                        array = array2;
                    }
                }
            }
            Sprite sprite;
            if (building.check_spawn_animation)
            {
                sprite = building.getSpawnFrameSprite();
            }
            else if (!flag || array.Length == 1)
            {
                sprite = array[0];
            }
            else
            {
                sprite = AnimationHelper.getSpriteFromList(building.GetHashCode(), array, building.asset.animation_speed);
            }
            return UnitSpriteConstructor.getRecoloredSpriteBuilding(sprite, building.kingdom.getColor());
        }
    }
}
