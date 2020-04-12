using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ParallelTasks;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.ModAPI;
using VRageRender.Animations;

namespace ClassLibrary2

{
    public class Program : MyGridProgram
    {
        
        public int timer = 0;
        public bool smashed = false;
        public bool shouldsmash = false;
        public bool ready = true;
        public float originalGravity;
        public IMyGravityGenerator gravGen;
        public IMySensorBlock sensor;
        public int counter = 0;
        
        private FixedSizedQueue<string> _logText;
        private string _tempText;
        private IMyTextPanel _display1;
        private IMyTextPanel _display2;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _display1 = GridTerminalSystem.GetBlockWithName("display1") as IMyTextPanel;
            _display2 = GridTerminalSystem.GetBlockWithName("display2") as IMyTextPanel;
            sensor = GridTerminalSystem.GetBlockWithName("sensor1") as IMySensorBlock;
            List<IMyGravityGenerator> gravity = new List<IMyGravityGenerator>();
            GridTerminalSystem.GetBlocksOfType<IMyGravityGenerator>(gravity);
            gravGen = gravity[0];
            
            _logText = new FixedSizedQueue<string>(15);
        }

        void Main()
        {
            _tempText = "";
            try
            {
                LogPerm("HolyShit");
                tick();
                LogPerm("Aftertick");
            }
            catch (Exception e)
            {
                LogPerm(e.Message);
                LogPerm(e.StackTrace);
            }

            counter++;
            LogPerm(counter.ToString());
            WriteDisplayText();
        }
        
        
        void tick()
        {
            string PlayerName = "";
            if (!sensor.LastDetectedEntity.IsEmpty())
            {
                PlayerName = sensor.LastDetectedEntity.Name;
            }
            LogPerm("intick");
            if (!ready && timer++ > 100)
            {
                LogPerm("Resetting");
                ready = true;
                smashed = false;
                shouldsmash = false;
                timer = 0;
            }
            LogPerm("Checking for people");
            if (PlayerName.Equals("sintrinsic") && !shouldsmash && !smashed && ready) 
            {
                LogPerm("Detected and should mash");
                shouldsmash = true;
                gravGen.GravityAcceleration = -9.5f;
            }
            
            LogPerm("Waiting to smash");
            if (shouldsmash && !smashed && ready)
            {
                LogPerm("About to smash");
                if (timer++ > 7)
                {
                    gravGen.GravityAcceleration = 9.5f;
                    timer = 0;
                    smashed = true;
                }
            }

            LogPerm("Waiting to reset");
            if (smashed && ready && timer++ > 20)
            {
                LogPerm("Resetting");
                gravGen.GravityAcceleration = originalGravity;
                ready = false;
                timer = 0;
            }
            LogTemp("Playername: "+PlayerName);
            LogTemp("TickTimer: "+timer.ToString());
            LogTemp("ShouldSmash "+shouldsmash.ToString());
            LogTemp("Smashed "+smashed.ToString());
            LogTemp("Ready "+ready.ToString());
        }

        /**
         * The print method for my logs. I have 2 widescreen LCD displays named:
         *  display1 and display2
         *  Prints dynamic values to monitor in one screen, and a constantly rotating log in the other. 
         */
        void WriteDisplayText()
        {
            _display1.WriteText(_tempText);
            string logString = "";
            foreach (string line in _logText.ToArray())
            {
                logString += line + "\n";
            }

            _display2.WriteText(logString);
        }

        /**
         * Write to persistent log, which rotates on the screen constantly. 
         */
        void LogPerm(string input)
        {
            _logText.Insert(input);
        }

        /**
         * Writes dynamic output to a screen which is cleared with every execution. Good for monitoring variables. 
         */
        void LogTemp(string input)
        {
            _tempText += input + "\n";
        }

        public void Save()
        {
        }


        
        /**
         * Just a queue wrapper that keeps the length at the number of lines on my screen. 
         */
        public class FixedSizedQueue<T>
        {
            readonly Queue<T> _queue = new Queue<T>();

            public int Size { get; private set; }

            public FixedSizedQueue(int size)
            {
                Size = size;
            }

            public T[] ToArray()
            {
                return _queue.ToArray();
            }

            public void Insert(T obj)
            {
                _queue.Enqueue(obj);

                while (_queue.Count > Size)
                {
                    T outObj;
                    _queue.TryDequeue(out outObj);
                }
            }
        }
    }
}