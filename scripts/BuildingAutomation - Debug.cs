using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xml.Serialization.GeneratedAssembly;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI.Ingame;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;


namespace ClassLibrary2

{
    public class Program : MyGridProgram
    {
        private FixedSizedQueue<string> _logText;
        private string _tempText;
        private IMyTextPanel _display1;
        private IMyTextPanel _display2;

        private Dictionary<string, string> inventoryStats;
        private string[] buildingItemTypes;
        List<MyInventoryItem> tempItemList;
        private string[] toolTypes;
        private HashSet<string> unproducable;
        private Dictionary<string, string> inventoryToBlueprintMap;
        //private Dictionary<string, string> blueprintToInventoryMap;
        int targetItemCount = 1000;
        private HashSet<string> bulkItems;
        
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            _display1 = GridTerminalSystem.GetBlockWithName("display_debug") as IMyTextPanel;
            _display2 = GridTerminalSystem.GetBlockWithName("display_debug2") as IMyTextPanel;
            
            inventoryStats = new Dictionary<string, string>();
            buildingItemTypes = new[] {"Ore", "Ingot", "Component"};
            toolTypes = new[] {"Weld", "Grind", "Drill", "Rifle"};
            inventoryToBlueprintMap = new Dictionary<string, string>();
            inventoryToBlueprintMap["Computer"] = "ComputerComponent";
            inventoryToBlueprintMap["Construction"] = "ConstructionComponent";
            inventoryToBlueprintMap["Detector"] = "DetectorComponent";
            inventoryToBlueprintMap["Girder"] = "GirderComponent";
            inventoryToBlueprintMap["Medical"] = "MedicalComponent";
            inventoryToBlueprintMap["Motor"] = "MotorComponent";
            inventoryToBlueprintMap["RadioCommunication"] = "RadioCommunicationComponent";
            inventoryToBlueprintMap["Thrust"] = "ThrustComponent";
            inventoryToBlueprintMap["Reactor"] = "ReactorComponent";
            inventoryToBlueprintMap["Reactor"] = "ReactorComponent";
            inventoryToBlueprintMap["GravityGenerator"] = "GravityGeneratorComponent";
            bulkItems = new HashSet<string>();
            bulkItems.Add("SteelPlate");
            bulkItems.Add("ConstructionComponent");
            bulkItems.Add("InteriorPlate");
            bulkItems.Add("ThrustComponent");
            bulkItems.Add("BulletproofGlass");


            //blueprintToInventoryMap = inventoryToBlueprintMap.ToDictionary(i => i.Value, i => i.Key);

            tempItemList = new List<MyInventoryItem>();

            _logText = new FixedSizedQueue<string>(15);
            unproducable = new HashSet<string>();
        }

        void Main()
        {
            _tempText = "";
            LogTemp("in main");
            try
            {
                //Dictionary<string, Dictionary<string, MyFixedPoint>> items = GetAllBuildingItemsInShip();
                //displayBuildingItems(items);
                Dictionary<string, Dictionary<string, MyFixedPoint>> items = GetAllBuildingItemsInShip();
                Dictionary<string, MyFixedPoint> queue = getBuildingQueue();
                buildNeededItems(items["MyObjectBuilder_Component"], queue);
                LogTemp("Queue:");
                foreach (KeyValuePair<string, MyFixedPoint> item in queue)
                {
                    LogTemp(item.Key+": "+item.Value.RawValue/1000000f);
                }
                
                //buildNeededItems(items["MyObjectBuilder_Component"], queue);
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

        public Dictionary<string, Dictionary<string, MyFixedPoint>> GetAllBuildingItemsInShip()
        {
            Dictionary<String, Dictionary<string, MyFixedPoint>> itemList =
                new Dictionary<string, Dictionary<string, MyFixedPoint>>();
            List<IMyTerminalBlock> blocksWithInventory =
                GetFunctionalBlocksOfType<IMyTerminalBlock>().FindAll(block => block.HasInventory);
            foreach (IMyTerminalBlock block in blocksWithInventory)
            {
                tempItemList.Clear();
                block.GetInventory().GetItems(tempItemList,
                    item => buildingItemTypes.Any(type => item.Type.TypeId.Contains(type)));
                foreach (MyInventoryItem item in tempItemList)
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

        public void buildNeededItems(Dictionary<string, MyFixedPoint> items, Dictionary<string, MyFixedPoint> queue)
        {
            foreach (string type in queue.Keys)
            {
                LogPerm(type);
            }

            foreach (KeyValuePair<string, MyFixedPoint> item in items)
            {
                string queueKey = item.Key;
                if (queueKey.Equals("Datapad") || queueKey.Equals("Canvas"))
                {
                    continue;
                }
                if (inventoryToBlueprintMap.ContainsKey(queueKey))
                {
                    queueKey = inventoryToBlueprintMap[queueKey];
                }
                double itemcount = item.Value.RawValue / 1000000f;
                if (queue.ContainsKey(queueKey))
                {
                    //LogPerm(queueKey + "  in queue");
                    itemcount += queue[queueKey].RawValue / 1000000f;
                }

                int toCreate = targetItemCount;
                if (bulkItems.Contains(queueKey))
                {
                    toCreate = 5000;
                }
                if (itemcount < toCreate)
                {
                    LogPerm("Item less than 2k"+ queueKey +": "+itemcount);
                    addToBuildingQueue(queueKey, toCreate - itemcount);
                }
            }

            string unpro = "";
            foreach (string unp in unproducable)
            {
                unpro += unp + "\n";
            }
            LogTemp("unproducable:");
            LogTemp(unpro);
        }

        public void addToBuildingQueue(string type, double quantity)
        {
            List<IMyAssembler> assemblers = GetFunctionalBlocksOfType<IMyAssembler>();
            double quantPerAssembler = (quantity - quantity % assemblers.Count) / assemblers.Count;
            double leftover = quantity % assemblers.Count;
            MyDefinitionId itemType = MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/" + type);
            //LogPerm("successfully got def for "+itemType.SubtypeId);
            if (quantPerAssembler < 1)
            {
                return;
            }
            foreach (IMyAssembler assembler in assemblers)
            {
                try
                {
                    assembler.CanUseBlueprint(itemType);
                }
                catch (Exception e)
                {
                    unproducable.Add(itemType.SubtypeId.ToString());
                    continue;
                }

                //assembler.InsertQueueItem(0, itemType, quantPerAssembler);
                assembler.AddQueueItem(itemType, quantPerAssembler + leftover);
                leftover = 0;
            }
        }

        public Dictionary<string, MyFixedPoint> getBuildingQueue()
        {
            Dictionary<string, MyFixedPoint> quantities = new Dictionary<string, MyFixedPoint>();
            List<IMyAssembler> assemblers = GetFunctionalBlocksOfType<IMyAssembler>();
            foreach (IMyAssembler assembler in assemblers)
            {
                List<MyProductionItem> productionQueue = new List<MyProductionItem>();
                assembler.GetQueue(productionQueue);
                foreach (MyProductionItem item in productionQueue)
                {
                    string itemId = item.BlueprintId.SubtypeId.ToString();
                    //item.BlueprintId.TypeId this is MyObjectBuilder_BlueprintDefinition
                    if (toolTypes.Any(tool => itemId.Contains(tool)))
                    {
                        continue;
                    }

                    if (!quantities.ContainsKey(itemId))
                    {
                        quantities[itemId] = item.Amount;
                    }
                    else
                    {
                        quantities[itemId] += item.Amount;
                    }
                }
            }
            return quantities;
        }



        /**
         * Returns a list of functional (fully assembled and not below hack line) blocks of a given type.
         * Type must be a descendent of IMyTerminalBlock. 
         */
        private List<T> GetFunctionalBlocksOfType<T>(bool thisGridOnly = true) where T : class, IMyTerminalBlock
        {
            List<T> blockList = new List<T>();
            GridTerminalSystem.GetBlocksOfType(blockList);
            return thisGridOnly
                ? blockList.FindAll(block => block.IsSameConstructAs(Me) &&
                                             block.IsFunctional)
                : blockList;
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