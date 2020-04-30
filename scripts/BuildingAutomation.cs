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
    /**
     * Shitty display of full-grid power stats. Useful for seeing current power, but really ugly and hard to read. 
     */
    public class Program : MyGridProgram
    {
        private FixedSizedQueue<string> _logText;
        private string _tempText;


        private Dictionary<string, string> inventoryStats;
        private string[] buildingItemTypes;
        List<MyInventoryItem> tempItemList;
        private string[] toolTypes;
        private HashSet<string> unproducable;
        private Dictionary<string, string> inventoryToBlueprintMap;
        //private Dictionary<string, string> blueprintToInventoryMap;
        private int targetItemCount;
        private int bulkItemCount; 
        private HashSet<string> bulkItems;
        
        // Mandatory displays
        private Dictionary<string, IMyTextPanel> inventoryDisplayMapping;
        private IMyTextPanel _display_temp;
        private IMyTextPanel _display_perm;
        
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            inventoryDisplayMapping = new Dictionary<string, IMyTextPanel>();
            // ------------------------------ Begin things you can configure --------------------------
            /** These are the displays you need to run this. They're all mandatory and the script will fail if they don't exist. 
                Create the 5 displays, name them to match the strings below, and set them to display text/images BEFORE 
                running the script. If you read this after you ran the script, just do it and re-compile/rerun. 
            */
            inventoryDisplayMapping["MyObjectBuilder_Ore"] = GridTerminalSystem.GetBlockWithName("display_ore") as IMyTextPanel;
            inventoryDisplayMapping["MyObjectBuilder_Ingot"] =GridTerminalSystem.GetBlockWithName("display_ingot") as IMyTextPanel;
            inventoryDisplayMapping["MyObjectBuilder_Component"] =GridTerminalSystem.GetBlockWithName("display_comp") as IMyTextPanel;
            _display_temp = GridTerminalSystem.GetBlockWithName("display_debug_temp") as IMyTextPanel;
            _display_perm = GridTerminalSystem.GetBlockWithName("display_debug_perm") as IMyTextPanel;
            
            // NOTE: For the system to create these items, you must have at least ONE of it already in your inventory. 
            // Also NOTE: This script only takes into account the grid the prog block is part of, not subgrids. 
            // The amount of each non-bulk item you want to have on hand at all times 
            targetItemCount = 10000;
            // The amount of each bulk item you want to have on hand at all times (list of bulk items below)
            bulkItemCount = 20000;
            
            // List of items to create in bulk. You can add to or remove from this list, but be sure you're using the
            // name for the item that the code uses (Not always the same as in-game text). 
            bulkItems = new HashSet<string>();
            bulkItems.Add("SteelPlate");
            bulkItems.Add("ConstructionComponent");
            bulkItems.Add("InteriorPlate");
            bulkItems.Add("ThrustComponent");
            bulkItems.Add("BulletproofGlass");
            // --------------------------------- End things you can configure ------------------------------------
            
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
                displayBuildingItems(items);
                LogTemp("Queue:");
                foreach (KeyValuePair<string, MyFixedPoint> item in queue)
                {
                    LogTemp(item.Key+": "+item.Value.RawValue/1000000f);
                }

                string bob = String.Concat(tempItemList.ConvertAll(item => item.Amount));

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
                    toCreate = 20000;
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