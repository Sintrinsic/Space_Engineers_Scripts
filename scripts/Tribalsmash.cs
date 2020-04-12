using System;
using System.Collections.Generic;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using SpaceEngineers.Game.Weapons.Guns;


namespace ClassLibrary2.bob
{
    public class Program: MyGridProgram
    {
        // 0000
        public int smashTimer = 0;
        public bool smashed = false;
        public bool shouldsmash = false;
        public bool readyToSmash = true;
        public float originalGravity;
        public IMyGravityGenerator gravGen;
        public IMySensorBlock tribalSensor;
        // 0000


        public Program()
        {
            // 0000
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            tribalSensor = GridTerminalSystem.GetBlockWithName("sensor1") as IMySensorBlock;
            List<IMyGravityGenerator> gravity = new List<IMyGravityGenerator>();
            GridTerminalSystem.GetBlocksOfType<IMyGravityGenerator>(gravity);
            gravGen = gravity[0];
            // 0000


        }

        void Main()
        {
            // 0000
            tick();
        }
        
        
        // 0000
        void tick()
        {
            string PlayerName = "";
            if (!tribalSensor.LastDetectedEntity.IsEmpty())
            {
                PlayerName = tribalSensor.LastDetectedEntity.Name;
            }
            if (!readyToSmash && smashTimer++ > 100)
            {
                readyToSmash = true;
                smashed = false;
                shouldsmash = false;
                smashTimer = 0;
            }
            if (PlayerName.Equals("tribalinstincts") && !shouldsmash && !smashed && readyToSmash) 
            {
                shouldsmash = true;
                gravGen.GravityAcceleration = -9.5f;
            }
            
            if (shouldsmash && !smashed && readyToSmash)
            {
                if (smashTimer++ > 8)
                {
                    gravGen.GravityAcceleration = 9.5f;
                    smashTimer = 0;
                    smashed = true;
                }
            }

            if (smashed && readyToSmash && smashTimer++ > 30)
            {
                gravGen.GravityAcceleration = originalGravity;
                readyToSmash = false;
                smashTimer = 0;
            }
        }
        // 0000

        
    }
}