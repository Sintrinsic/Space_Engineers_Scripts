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
using VRageRender.Animations;

namespace ClassLibrary2

{
    public class ProgramRayCastTest : MyGridProgram
    {
        private IMyCameraBlock _camera1;
        private DateTime lastSampled = DateTime.Now;
        private int raycastDistanceInMeters = 1000;
        private IMyRemoteControl _remote;


        public ProgramRayCastTest()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _camera1 = GridTerminalSystem.GetBlockWithName("camera1") as IMyCameraBlock;
            _remote = GridTerminalSystem.GetBlockWithName("remote1") as IMyRemoteControl;
            _camera1.EnableRaycast = true;
        }

        void Main()
        {
            if (_camera1.AvailableScanRange > raycastDistanceInMeters)
            {
                MyDetectedEntityInfo hitinfo = _camera1.Raycast(raycastDistanceInMeters);
                if (!hitinfo.IsEmpty())
                {
                    _remote.DampenersOverride = true;
                }
            }
        }
    }
}