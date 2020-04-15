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
        private IMyTextPanel _display1;
        private IMyTextPanel _display2;
        private IMyTextPanel _displayPower;

        private IMyPowerProducer _solar1;
        private IMyPowerProducer _solar2;
        private IMyPowerProducer _solar3;
        private IMyPowerProducer _solar4;
        private Dictionary<string, FixedSizedQueue<double>> batteryInfo;
        private Dictionary<string, string> inventoryStats; 
        List<MyInventoryItem> tempItemList;
        private Dictionary<string, IMyTextPanel> inventoryDisplayMapping;
        private string[] buildingItemTypes;

        
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _solar1 = GridTerminalSystem.GetBlockWithName("s1") as IMyPowerProducer;
            _solar2 = GridTerminalSystem.GetBlockWithName("s2") as IMyPowerProducer;
            _solar3 = GridTerminalSystem.GetBlockWithName("s3") as IMyPowerProducer;
            _solar4 = GridTerminalSystem.GetBlockWithName("s4") as IMyPowerProducer;
            _display1 = GridTerminalSystem.GetBlockWithName("display1") as IMyTextPanel;
            _display2 = GridTerminalSystem.GetBlockWithName("display2") as IMyTextPanel;
            _displayPower = GridTerminalSystem.GetBlockWithName("display-power") as IMyTextPanel;

            batteryInfo = new Dictionary<string, FixedSizedQueue<double>>();
            batteryInfo["currentPower"] = new FixedSizedQueue<double>(5);
            batteryInfo["maxPower"] = new FixedSizedQueue<double>(1);
            batteryInfo["currentInput"] = new FixedSizedQueue<double>(20);
            batteryInfo["currentOutput"] = new FixedSizedQueue<double>(50);
            batteryInfo["currentNet"] = new FixedSizedQueue<double>(50);
            inventoryStats = new Dictionary<string, string>();
            inventoryDisplayMapping = new Dictionary<string, IMyTextPanel>();
            inventoryDisplayMapping["MyObjectBuilder_Ore"] = GridTerminalSystem.GetBlockWithName("display_ore") as IMyTextPanel;
            inventoryDisplayMapping["MyObjectBuilder_Ingot"] =GridTerminalSystem.GetBlockWithName("display_ingot") as IMyTextPanel;
            inventoryDisplayMapping["MyObjectBuilder_Component"] =GridTerminalSystem.GetBlockWithName("display_comp") as IMyTextPanel;
            tempItemList = new List<MyInventoryItem>();
            buildingItemTypes = new[] {"Ore", "Ingot", "Component"};

            _logText = new FixedSizedQueue<string>(15);
        }

        void Main()
        {
            _tempText = "";
            try
            {
                if (_solar1.Enabled)
                {
                    RotateStaionToSun();
                }

                displayPowerStats();
                Dictionary<string, Dictionary<string,MyFixedPoint>> items =  GetAllBuildingItemsInShip();
                displayBuildingItems(items);
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


        public Dictionary<string, Dictionary<string,MyFixedPoint>> GetAllBuildingItemsInShip()
        {
            Dictionary<String, Dictionary<string,MyFixedPoint>> itemList = new Dictionary<string, Dictionary<string,MyFixedPoint>>();
            List<IMyTerminalBlock> blocksWithInventory =
                GetFunctionalBlocksOfType<IMyTerminalBlock>().FindAll(block => block.HasInventory);
            foreach (IMyTerminalBlock block in blocksWithInventory)
            {
                tempItemList.Clear();
                block.GetInventory().GetItems(tempItemList, 
                    item => buildingItemTypes.Any(type => item.Type.TypeId.Contains(type)));
                foreach(MyInventoryItem item in tempItemList)
                {
                    
                    if (!itemList.ContainsKey(item.Type.TypeId))
                    {
                        itemList[item.Type.TypeId] = new Dictionary<string, MyFixedPoint>();
                    }
                    if (!itemList[item.Type.TypeId].ContainsKey(item.Type.SubtypeId))
                    {
                        MyFixedPoint bob = item.Amount;
                        itemList[item.Type.TypeId][item.Type.SubtypeId] = item.Amount;
                    }
                    else
                    {
                        itemList[item.Type.TypeId][item.Type.SubtypeId] += item.Amount;
                    }
                }
            }
            return itemList;
        }
        
        public void displayBuildingItems(Dictionary<string, Dictionary<string,MyFixedPoint>> itemList)
        {
            foreach (KeyValuePair<string, Dictionary<String,MyFixedPoint>> category in itemList)
            {
                inventoryStats[category.Key] = "";
                LogTemp(category.Key +":");
                foreach (KeyValuePair<string, MyFixedPoint> item in category.Value)
                {
                    double itemQuantity = Math.Round(item.Value.RawValue / 1000000f, 2);
                    inventoryStats[category.Key] += item.Key +": "+ itemQuantity + "\n";
                }

                inventoryDisplayMapping[category.Key].WriteText(inventoryStats[category.Key]);
            }
        }

        public void RotateStaionToSun()
        {
            float[] solarPanelArray =
                {_solar1.CurrentOutput, _solar2.CurrentOutput, _solar3.CurrentOutput, _solar4.CurrentOutput};
            float solarVarianceSum = solarPanelArray.Sum();
            string powerOutput = "S1: " + _solar1.CurrentOutput.ToString() + "\n" +
                                 "S2: " + _solar2.CurrentOutput.ToString() + "\n" +
                                 "S3: " + _solar3.CurrentOutput.ToString() + "\n" +
                                 "S4: " + _solar4.CurrentOutput.ToString() + "\n" +
                                 "Sum: " + solarVarianceSum.ToString();
            LogTemp(powerOutput);

            // Resetting gyro to 0, as pitch changes persist once updated. 
            bool rotate = false;
            IMyGyro gyro = GridTerminalSystem.GetBlockWithName("g1") as IMyGyro;
            gyro.Yaw = 0;
            gyro.Pitch = 0;
            gyro.Roll = 0;

            // If none of the test panels can see the sun, roll a bunch until they can. 
            if (solarPanelArray.Sum() == 0.00)
            {
                gyro.Roll = .1f;
                gyro.Yaw = .05f;
                rotate = true;
            }

            // The rotation values specific to my station and solar test setup. 
            float roll = solarPanelArray[0] - solarPanelArray[2]; // 2 > 0, -Roll
            float yaw = solarPanelArray[3] - solarPanelArray[1]; // 1 > 3, -Yaw

            // If the panels are out of alignment with the sun, within a threshold, fix it. 
            if (Math.Abs(roll) > .01 || Math.Abs(yaw) > .01)
            {
                gyro.Yaw = yaw;
                gyro.Roll = roll;
                LogPerm("Adjusting: Roll:" + roll.ToString() + " Yaw:" + yaw.ToString());
                rotate = true;
            }

            gyro.GyroOverride = rotate;
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