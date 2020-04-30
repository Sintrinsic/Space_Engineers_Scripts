using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Schema;
using ParallelTasks;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.ObjectBuilders;
using VRageRender.Animations;
using IMyInventory = VRage.Game.ModAPI.Ingame.IMyInventory;
using IMyInventoryItem = VRage.Game.ModAPI.IMyInventoryItem;

namespace ClassLibrary2

{
    public class Program : MyGridProgram
    {
        private FixedSizedQueue<string> _logText;
        private string _tempText;
        private IMyTextPanel _display_temp;
        private IMyTextPanel _display_perm;
        private IMyTextPanel _displayPower;
        
        private Dictionary<string, FixedSizedQueue<double>> batteryInfo;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _display_temp = GridTerminalSystem.GetBlockWithName("display_power_temp") as IMyTextPanel;
            _display_perm = GridTerminalSystem.GetBlockWithName("display_power_perm") as IMyTextPanel;
            _displayPower = GridTerminalSystem.GetBlockWithName("display_power_output") as IMyTextPanel;

            batteryInfo = new Dictionary<string, FixedSizedQueue<double>>();
            batteryInfo["currentPower"] = new FixedSizedQueue<double>(5);
            batteryInfo["maxPower"] = new FixedSizedQueue<double>(1);
            batteryInfo["currentInput"] = new FixedSizedQueue<double>(20);
            batteryInfo["currentOutput"] = new FixedSizedQueue<double>(50);
            batteryInfo["currentNet"] = new FixedSizedQueue<double>(50);
            
            _logText = new FixedSizedQueue<string>(15);
        }

        void Main()
        {
            _tempText = "";
            try
            {
                displayPowerStats();
            }
            catch (Exception e)
            {
                LogPerm(e.Message);
                LogPerm(e.StackTrace);
            }

            WriteDisplayText();
        }

        /**
         * The print method for my logs. I have 2 widescreen LCD displays named:
         *  display1 and display2
         *  Prints dynamic values to monitor in one screen, and a constantly rotating log in the other. 
         */
        void WriteDisplayText()
        {
            _display_temp.WriteText(_tempText);
            string logString = "";
            foreach (string line in _logText.ToArray())
            {
                logString += line + "\n";
            }

            _display_perm.WriteText(logString);
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

        public void displayPowerStats()
        {
            try
            {
                double currentInput = 0;
                double currentOutput = 0;
                double currentCharge = 0;
                double maxPower = 0;
                List<IMyBatteryBlock> batteries = GetFunctionalBlocksOfType<IMyBatteryBlock>();
                foreach (IMyBatteryBlock battery in batteries)
                {
                    currentInput += battery.CurrentInput;
                    currentOutput += battery.CurrentOutput;
                    currentCharge += battery.CurrentStoredPower;
                    maxPower += battery.MaxStoredPower;
                }

                batteryInfo["currentPower"].Insert(currentCharge);
                batteryInfo["maxPower"].Insert(maxPower);
                batteryInfo["currentInput"].Insert(currentInput);
                batteryInfo["currentOutput"].Insert(currentOutput);
                batteryInfo["currentNet"].Insert(currentInput - currentOutput);

                Dictionary<string, double> sums = new Dictionary<string, double>();
                double percent = Math.Round(currentCharge / maxPower * 100, 2);
                string powerOutput = "Battery info: " + percent + "%\n";
                foreach (KeyValuePair<string, FixedSizedQueue<double>> item in batteryInfo)
                {
                    double[] itemValues = item.Value.ToArray();
                    double sum = itemValues.Sum() / itemValues.Length;
                    sums[item.Key] = sum;
                    powerOutput += item.Key + ":" + Math.Round(sum, 2) + "\n";
                }
                
                _displayPower.WriteText(powerOutput);
            }
            catch (Exception e)
            {
                LogPerm(e.Message);
                LogPerm(e.StackTrace);
            }
        }

        /**
         * Returns a list of functional (fully assembled and not below hack line) blocks of a given type.
         * Type must be a descendent of IMyTerminalBlock. 
         */
        private List<T> GetFunctionalBlocksOfType<T>(bool thisGridOnly = true) where T : class, IMyTerminalBlock
        {
            List<T> blockList = new List<T>();
            GridTerminalSystem.GetBlocksOfType(blockList);
            return thisGridOnly ? blockList.FindAll(block => block.IsSameConstructAs(Me) && 
                                                             block.IsFunctional) : blockList;
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