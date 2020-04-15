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
using VRage.Game;
using VRageRender.Animations;

namespace ClassLibrary2

{
    public class Program : MyGridProgram
    {
        // Update these display names to match the LCDs you want to display your temp/perm log output on. 
        private string tempDebugDisplayName = "display1";
        private string permDebugDisplayName = "display2";
        private FixedSizedQueue<string> _logText;
        private string _tempText;
        private IMyTextPanel tempDebugDisplay;
        private IMyTextPanel permDebugDisplay;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            tempDebugDisplay = GridTerminalSystem.GetBlockWithName("display1") as IMyTextPanel;
            permDebugDisplay = GridTerminalSystem.GetBlockWithName("display2") as IMyTextPanel;
            _logText = new FixedSizedQueue<string>(15);
        }

        void Main()
        {
            _tempText = "";
            try
            {
            }
            catch (Exception e)
            {
                LogPerm(e.Message);
                LogPerm(e.StackTrace);
            }

            WriteDisplayText();
        }
        
        /**
         * Get all the functional blocks of the given type in the same grid as the programmable block running this.
         * By default, only returns blocks on the same grid. If thisGridOnly is set to false, returns subgrids as well.
         * "Functional" blocks are all those which are welded above the "functional" mark. 
         */
        private List<T> GetFunctionalBlocksOfType<T>(bool thisGridOnly = true) where T : class, IMyTerminalBlock
        {
            List<T> blockList = new List<T>();
            GridTerminalSystem.GetBlocksOfType(blockList);
            return thisGridOnly ? blockList.FindAll(block => block.IsSameConstructAs(Me) && block.IsFunctional)
                : blockList;
        }

        /**
         * The print method for my logs. I have 2 widescreen LCD displays named:
         *  display1 and display2
         *  Prints dynamic values to monitor in one screen, and a constantly rotating log in the other. 
         */
        void WriteDisplayText()
        {
            if (tempDebugDisplay != null)
            {
                tempDebugDisplay.WriteText(_tempText);
            }

            string logString = "";
            foreach (string line in _logText.ToArray())
            {
                logString += line + "\n";
            }

            if (permDebugDisplay != null)
            {
                permDebugDisplay.WriteText(logString);
            }
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