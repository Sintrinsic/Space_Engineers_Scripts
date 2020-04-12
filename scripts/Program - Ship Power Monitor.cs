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
    public class ProgramMyShipPower : MyGridProgram
    {
        private FixedSizedQueue<string> _logText;
        private string _tempText;
        private IMyTextPanel _display1;
        private IMyTextPanel _display2;
        private IMyPowerProducer _solar1;
        private IMyPowerProducer _solar2;
        private IMyPowerProducer _solar3;
        private IMyPowerProducer _solar4;

        public ProgramMyShipPower()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _display1 = GridTerminalSystem.GetBlockWithName("display1") as IMyTextPanel;
            _display2 = GridTerminalSystem.GetBlockWithName("display2") as IMyTextPanel;
            _logText = new FixedSizedQueue<string>(15);
        }

        void Main()
        {
            _tempText = "";
            if (_solar1.Enabled)
            {
                RotateStaionToSun();
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