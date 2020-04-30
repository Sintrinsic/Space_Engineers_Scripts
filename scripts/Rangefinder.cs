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
using VRage.ModAPI;
using VRageMath;
using VRageRender.Animations;

namespace ClassLibrary2

{
    /**
     * Uses camera and remote to engage dampers on a ship before it runs into something it's looking at.
     * Good for long trips where you want to AFK while coasting without hitting shit. 
     */
    public class ProgramRayCastTest : MyGridProgram
    {
        private IMyCameraBlock _camera1;
        private IMyTextPanel _display1;
        private int raycastDistanceInMeters = 50000;
        private double range = 0; 
        private DateTime lastRegisterTime = DateTime.Now;;


        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _camera1 = GridTerminalSystem.GetBlockWithName("camera_range") as IMyCameraBlock;
            _camera1.EnableRaycast = true;
            _display1 = GridTerminalSystem.GetBlockWithName("display_range") as IMyTextPanel;

        }

        void Main()
        {
            if (_camera1.AvailableScanRange > raycastDistanceInMeters)
            {
                MyDetectedEntityInfo hitinfo = _camera1.Raycast(raycastDistanceInMeters);
                if (!hitinfo.IsEmpty())
                {
                    range = Vector3.Distance(hitinfo.HitPosition.Value, Me.Position);
                }
                else
                {
                    range = -1;
                }
                lastRegisterTime = DateTime.Now;
            }

            double timeSinceLast = Math.Round((DateTime.Now - lastRegisterTime).TotalSeconds,0);
            _display1.WriteText("Range to target:\n" + Math.Round(range,2) +"\n"+"Time since last cast: \n" + timeSinceLast);
        }
    }
}