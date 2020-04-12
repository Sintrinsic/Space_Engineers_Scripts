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
        


        public Program()
        {
            
            sensor = GridTerminalSystem.GetBlockWithName("sensor1") as IMySensorBlock;
            List<IMyGravityGenerator> gravity = new List<IMyGravityGenerator>();
            GridTerminalSystem.GetBlocksOfType<IMyGravityGenerator>(gravity);
            gravGen = gravity[0];
            
        }

        void Main()
        {
            tick();
        }
        
        
        void tick()
        {
            string PlayerName = "";
            if (!sensor.LastDetectedEntity.IsEmpty())
            {
                PlayerName = sensor.LastDetectedEntity.Name;
            }
            if (!ready && timer++ > 100)
            {
                ready = true;
                smashed = false;
                shouldsmash = false;
                timer = 0;
            }
            if (PlayerName.Equals("sintrinsic") && !shouldsmash && !smashed && ready) 
            {
                shouldsmash = true;
                gravGen.GravityAcceleration = -29.5f;
            }
            
            if (shouldsmash && !smashed && ready)
            {
                if (timer++ > 8)
                {
                    gravGen.GravityAcceleration = 29.5f;
                    timer = 0;
                    smashed = true;
                }
            }

            if (smashed && ready && timer++ > 20)
            {
                gravGen.GravityAcceleration = originalGravity;
                ready = false;
                timer = 0;
            }
        }


    }
}