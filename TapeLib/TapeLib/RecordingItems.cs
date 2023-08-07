using ReflectionUtility;
using System.Collections.Generic;
using System.Linq;
using TapeLib;
using UnityEngine;

namespace TapeLib
{
    class RecordingItems
    {
        public class ItemsKeyFrame // [LEGACY CODE]
        {
            public List<ItemAction> actions = new List<ItemAction>();
        }

        public enum ActionType { changeSprite, moveTo }
        public class ItemAction
        {
            private static Dictionary<ActorBase, Sprite> lastSprites = new Dictionary<ActorBase, Sprite>();
            private static Dictionary<ActorBase, Vector3> lastPositions = new Dictionary<ActorBase, Vector3>();
            public static Dictionary<string, GameObject> fakeItems = new Dictionary<string, GameObject>();

            public ActionType type;
            public Vector3 vectorData;
            public Sprite sprite;
            public string ID;

            public void execute()
            {
                switch (type)
                {
                    case ActionType.moveTo:
                        GetFakeItemById(ID).transform.position = vectorData;
                        return;
                    case ActionType.changeSprite:
                        if (GetFakeItemById(ID) == null) { return; }
                        GetFakeItemById(ID).GetComponent<SpriteRenderer>().sprite = sprite;
                        return;
                }
            }

            /// <param name="position">In case the action type is not moveActor set to Vector3.zero</param>
            /// <param name="angles">In case the action type is not moveActor set to Vector3.zero</param>
            public ItemAction(ActorBase holder, ActionType type, Vector3 vectorData, Sprite sprite = null)
            {
                this.type = type;
                this.vectorData = vectorData;
                this.sprite = sprite;
                ActorData data = Reflection.GetField(typeof(ActorBase), holder, "data") as ActorData;
                ID = data.id;

                Tape tape = Tape.currentTape;
                tape.currentKeyFrame.items.actions.Add(this);
            }

            private static GameObject GetFakeItemTemplate()
            {
                GameObject returnThis = new GameObject("fake_item_template");
                SpriteRenderer renderer = returnThis.AddComponent<SpriteRenderer>();
                returnThis.transform.localScale = new Vector3(0.10f, 0.10f);
                returnThis.transform.parent = GameObject.Find("WorldBox/unit_items").transform;
                renderer.sharedMaterial = LibraryMaterials.instance.mat_world_object;
                renderer.sortingLayerName = "Objects";
                return returnThis;
            }

            private static GameObject GetFakeItemById(string id)
            {
                if (fakeItems.ContainsKey(id))
                {
                    fakeItems[id].SetActive(true);
                    return fakeItems[id];
                }
                GameObject returnObject = GetFakeItemTemplate();
                returnObject.name = "fake_item_" + id;
                fakeItems.Add(id, returnObject);
                return returnObject;
            }

            public static Sprite TryGetLastSprite(ActorBase holder)
            {
                if (!lastSprites.ContainsKey(holder)) { return null; }
                return lastSprites[holder];
            }

            public static Vector3 TryGetLastPos(ActorBase holder)
            {

                if (lastPositions.ContainsKey(holder)) { return lastPositions[holder]; }
                Vector3 position = Vector3.zero;//ItemData.gameObject.transform.position;
                lastPositions.Add(holder, position);
                return position;
            }

            public static void ChangeLastPos(ActorBase holder, Vector3 position)
            {
                lastPositions[holder] = position;
            }

            public static void ChangeLastSprite(ActorBase holder, Sprite sprite)
            {
                if (TryGetLastSprite(holder) == null)
                {
                    if (lastSprites.ContainsKey(holder)) { lastSprites[holder] = sprite; return; }
                    lastSprites.Add(holder, sprite); return;
                }
                lastSprites[holder] = sprite;
            }
        }

        private static Sprite GetItemSpriteWithoutRendering(Actor actor)
        {
            string texture = actor.getTextureToRenderInHand();
            return ActorAnimationLoader.getItem(texture);
        }

        public static void RecordItems()
        {
            ItemsKeyFrame keyframe = new ItemsKeyFrame();
            Tape.currentTape.currentKeyFrame.items = keyframe;

            foreach (Actor actor in MapBox.instance.units)
            {
                Sprite currentSprite = GetItemSpriteWithoutRendering(actor);
                if (ItemAction.TryGetLastSprite(actor) != currentSprite)
                {
                    new ItemAction(actor, ActionType.changeSprite, Vector3.zero, currentSprite);
                    ItemAction.ChangeLastSprite(actor, currentSprite);
                }

                AnimationFrameData animationFrameData = actor.getAnimationFrameData();
                if (animationFrameData == null) { continue; }
                var posItemNonVector = Reflection.GetField(typeof(AnimationFrameData), animationFrameData, "posItem");
                Vector2 posItem = (Vector2)posItemNonVector;
                float num2 = actor.transform.position.x + posItem.x * actor.currentScale.x;
                float num3 = actor.transform.position.y + posItem.y * actor.currentScale.y;
                Vector3 currentPosition = new Vector3(num2, num3);

                if (ItemAction.TryGetLastPos(actor) != currentPosition)
                {
                    new ItemAction(actor, ActionType.moveTo, currentPosition);
                }
            }
        }

        public static void DisplayItems()
        {
            foreach (ItemAction info in Tape.currentTape.currentDisplayKeyFrame.items.actions)
            {
                info.execute();
            }
        }

        public static void RemoveAllDisplay()
        {
            foreach (var fakeItem in ItemAction.fakeItems)
            {
                fakeItem.Value.SetActive(false);
            }
        }
    }
}
