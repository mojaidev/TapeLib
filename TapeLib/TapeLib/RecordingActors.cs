using ReflectionUtility;
using System.Collections.Generic;
using System.Linq;
using TapeLib;
using UnityEngine;

namespace TapeLib
{
    class RecordingActors // [LEGACY CODE]
    {
        // changeSprite is very hacky: but it would be more compatible with further updates and mods.
        public enum ActionType { changeSprite, moveTo, newActor, killActor }

        public class ActorAction
        {
            private static Dictionary<ActorBase, Sprite> lastSprites = new Dictionary<ActorBase, Sprite>();
            private static Dictionary<ActorBase, Vector3> lastPositions = new Dictionary<ActorBase, Vector3>();
            public static Dictionary<string, GameObject> fakeActors = new Dictionary<string, GameObject>();
            public static List<ActorBase> deadActors = new List<ActorBase>();

            public ActionType type;
            public Vector3 vectorData;
            public Sprite sprite;
            public string ID;

            public void execute()
            {
                switch (type)
                {
                    case ActionType.killActor:
                        GetFakeActorById(ID).SetActive(false);
                        return;
                    case ActionType.newActor:
                        GetFakeActorById(ID);
                        return;
                    case ActionType.moveTo:
                        GetFakeActorById(ID).transform.position = vectorData;
                        return;
                    case ActionType.changeSprite:
                        if (GetFakeActorById(ID) == null) { return; }
                        GetFakeActorById(ID).GetComponent<SpriteRenderer>().sprite = sprite;
                        return;
                }
            }

            /// <param name="position">In case the action type is not moveActor set to Vector3.zero</param>
            /// <param name="angles">In case the action type is not moveActor set to Vector3.zero</param>
            public ActorAction(ActorBase actor, ActionType type, Vector3 vectorData, Sprite sprite = null)
            {
                this.type = type;
                this.vectorData = vectorData;
                this.sprite = sprite;

                ID = actor.data.id;

                Tape tape = Tape.currentTape;
                tape.currentKeyFrame.creatures.actions.Add(this);
            }

            public static Sprite TryGetLastSprite(ActorBase actor)
            {
                if (!lastSprites.ContainsKey(actor)) { return null; }
                return lastSprites[actor];
            }

            public static Vector3 TryGetLastPos(ActorBase actor)
            {

                if (lastPositions.ContainsKey(actor)) { return lastPositions[actor]; }
                Vector3 position = actor.currentPosition;
                lastPositions.Add(actor, position);
                return position;
            }

            public static void ChangeLastPos(ActorBase actor, Vector3 position)
            {
                lastPositions[actor] = position;
            }

            public static void ChangeLastSprite(ActorBase actor, Sprite sprite)
            {
                if (TryGetLastSprite(actor) == null) { lastSprites.Add(actor, sprite); return; }
                lastSprites[actor] = sprite;
            }

            private static GameObject GetActorObjectTemplate()
            {
                GameObject returnThis = new GameObject("fake_actor_template");
                SpriteRenderer renderer = returnThis.AddComponent<SpriteRenderer>();
                returnThis.transform.localScale = new Vector3(0.10f, 0.10f);
                returnThis.transform.parent = GameObject.Find("WorldBox/Creatures/Units").transform;
                renderer.sharedMaterial = LibraryMaterials.instance.mat_world_object;
                renderer.sortingLayerName = "Objects";
                return returnThis;
            }

            private static GameObject GetFakeActorById(string id)
            {
                if (fakeActors.ContainsKey(id))
                {
                    fakeActors[id].SetActive(true);
                    return fakeActors[id];
                }
                GameObject returnObject = GetActorObjectTemplate();
                returnObject.name = "fake_actor_" + id;
                fakeActors.Add(id, returnObject);
                return returnObject;
            }
        }

        public class CreaturesKeyFrame
        {
            public List<ActorAction> actions = new List<ActorAction>();
        }

        public static void RecordCreatures()
        {
            //WorldLayer layer = Reflection.GetField(typeof(MapBox), World.world, "worldLayer") as WorldLayer;
            //layer.setRendererEnabled(true);
            Tape tape = Tape.currentTape;

            CreaturesKeyFrame keyframe = new CreaturesKeyFrame();
            tape.currentKeyFrame.creatures = keyframe;

            foreach (Actor actor in MapBox.instance.units)
            {
                if (!actor.isAlive())
                {
                    Debug.Log("actor aint alive!");
                    if (!ActorAction.deadActors.Contains(actor))
                    {
                        new ActorAction(actor, ActionType.killActor, Vector3.zero);
                        ActorAction.deadActors.Add(actor);
                    }

                    continue;
                }

                // Heres a special one!!  you see turns out that Reflection is very expensive when calling every frame,
                // thats why ive made an assembly (wich is in Assemblies folder) that makes checkSpriteToRender() public.
                Sprite curSprite = actor.checkSpriteToRender();

                Sprite lastSprite = ActorAction.TryGetLastSprite(actor);
                if (lastSprite == null) { ActorAction.ChangeLastSprite(actor, curSprite); }
                if (lastSprite != curSprite)
                {
                    new ActorAction(actor, ActionType.changeSprite, Vector2.zero, curSprite);
                    ActorAction.ChangeLastSprite(actor, curSprite);
                }

                actor.updatePos();
                Vector3 position = actor.currentPosition;
                if (ActorAction.TryGetLastPos(actor) != position)
                {
                    new ActorAction(actor, ActionType.moveTo, position);
                    ActorAction.ChangeLastPos(actor, position);
                }
            }
        }

        public static void DisplayCreatures()
        {
            foreach (ActorAction info in Tape.currentTape.currentDisplayKeyFrame.creatures.actions)
            {
                info.execute();
            }
        }

        public static void RemoveAllDisplay()
        {
            foreach (var fakeActor in ActorAction.fakeActors)
            {
                fakeActor.Value.SetActive(false);
            }
        }
    }
}
