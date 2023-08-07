using System.Collections.Generic;
using UnityEngine;
using ReflectionUtility;
using TapeLib;

namespace TapeLib
{
    class Tape
    {
        // === USER INTERACTIVE ===
        public static List<Tape>    tapes = new List<Tape>();
        public static Tape          currentTape;
        public static bool          IsRecording = false;
        public static bool          IsReplaying = false;
        public string               recordingName;
        public float                recordingDelay = 1 / 120;
        public float                displayDelay = 1 / 120;
        public int                  recordedFrames = 0;

        // === RECORD & DISPLAY ===
        private float               localTimer;
        public KeyFrame             currentKeyFrame;
        public KeyFrame             lastRecordedKeyFrame;
        public KeyFrame             currentDisplayKeyFrame;
        public int                  displayIndex = 0;
        public List<KeyFrame>       timeLine = new List<KeyFrame>();

        public Tape(string name)
        {
            recordingName = name;
            tapes.Add(this);
            currentTape = this;
        }

        public class KeyFrame
        {

            // ==== STORAGE (LEGACY CODE SUPPORT) ====
            public RecordingActors.CreaturesKeyFrame        creatures;
            public RecordingItems.ItemsKeyFrame             items;
            public RecordingMap.MapKeyFrame                 map;
            public RecordingBuildings.BuildingsKeyFrame     buildings;

            public void Record()
            {
                // ==== LEGACY CODE SUPPORT ====
                RecordingActors.RecordCreatures();
                RecordingItems.RecordItems();
                RecordingMap.RecordMap();
                RecordingBuildings.RecordBuildings();
            }

            public void Display()
            {
                // ==== LEGACY CODE SUPPORT ====
                RecordingActors.DisplayCreatures();
                RecordingItems.DisplayItems();
                RecordingMap.DisplayMap();
                RecordingBuildings.DisplayBuildings();
            }

        }

        public void RecordKeyFrame()
        {
            if (Config.paused) { return; }

            localTimer += Time.deltaTime;

            if (localTimer < recordingDelay) { return; }
            localTimer -= recordingDelay;

            if (!IsRecording) { return; }

            currentKeyFrame = new KeyFrame();
            currentKeyFrame.Record();
            recordedFrames++;
            lastRecordedKeyFrame = currentKeyFrame;
            timeLine.Add(currentKeyFrame);
        }

        public void DisplayKeyFrame()
        {
            // DEMO CODE FOR DISPLAYING
            if (!IsReplaying) { return; }

            localTimer += Time.deltaTime;

            if (localTimer < displayDelay)
            {
                return;
            }
            localTimer -= displayDelay;

            if (displayIndex == timeLine.Count)
            {
                displayIndex = 0;
                return;
            }

            currentDisplayKeyFrame = timeLine[displayIndex];
            timeLine[displayIndex].Display();
            currentDisplayKeyFrame = timeLine[displayIndex];
            displayIndex++;
        }

        public void RemoveAllDisplay()
        {
            // ==== LEGACY CODE SUPPORT ====
            RecordingActors.RemoveAllDisplay();
            RecordingItems.RemoveAllDisplay();
            RecordingBuildings.RemoveAllDisplay();
        }
    }

}
