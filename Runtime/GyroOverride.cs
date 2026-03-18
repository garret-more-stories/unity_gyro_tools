using UnityEngine;
using UnityEngine.InputSystem;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine.InputSystem.Controls;
using System.Collections.Concurrent;
using AOT;
using UnityEngine.Scripting;
using UnityEngine.PlayerLoop;
using UnityEngine.LowLevel;
using System;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.DualShock;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using HarmonyLib;
#endif


[assembly : AlwaysLinkAssembly]
namespace MoreStories.GyroTools
{
    public class MotionSensorUpdate { }
    public static class GyroOverride
    {

        #region gyro_reader_methods

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        const string imu_library = "imu_reader";
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        const string imu_library = "libimu_reader";
#endif

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ControllerSensorCallback(int controllerIndex, float x, float y, float z);

        [DllImport(imu_library, CallingConvention = CallingConvention.Cdecl)]
        private static extern void register_gyro_callback(ControllerSensorCallback callback);
        
        [DllImport(imu_library, CallingConvention = CallingConvention.Cdecl)]
        private static extern void register_accel_callback(ControllerSensorCallback callback);

        [DllImport(imu_library, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool set_controller_imu_state(int controller_index, bool is_enabled);

        [DllImport(imu_library, CallingConvention = CallingConvention.Cdecl)]
        private static extern void start_sdl_loop();

        [DllImport(imu_library, CallingConvention = CallingConvention.Cdecl)]
        private static extern void stop_sdl_loop();

        #endregion

        #region internal_types

        struct MotionControls
        {
            Vector3Control[] imus;
            public Gamepad owner {get; private set;}

            public Vector3Control gyroscope     => this[ImuType.Gyroscope];
            public Vector3Control accelerometer => this[ImuType.Accelerometer];

            public Vector3Control this[ImuType type]
            {
                get         => imus[(int)type];
                private set => imus[(int)type] = value;
            } 

            public MotionControls(Gamepad owner, Vector3Control gyroscope, Vector3Control accelerometer)
            {
                this.owner = owner;
                imus = new Vector3Control[(int)ImuType.Count];

                this[ImuType.Gyroscope]     = gyroscope;
                this[ImuType.Accelerometer] = accelerometer;
            }

        }
        public struct ImuReading
        {
            public Vector3 value       {get; private set;}
            public int controllerIndex {get; private set;}
            
            public ImuReading(int controllerIndex, Vector3 value)
            {
                this.controllerIndex = controllerIndex;
                this.value           = value;
            }

            public ImuReading(int controllerIndex, float x, float y, float z)
            {
                this.controllerIndex = controllerIndex;
                value = new Vector3(x, y, z);
            }
        }

        [Serializable]
        public struct GyroControllerLayout
        {
            public string name;
            public string extend;
            public OverridenControl[] controls;
            //public
        }

        [Serializable]
        public struct OverridenControl
        {
            public string name;
            public string layout;
            public string format;
            public bool   synthetic;
            public int    offset;
            public string processors;
            public int bit;
        }

        public enum ImuType
        {
            Gyroscope,
            Accelerometer,
            Count = 2
        }
       
        #endregion

        #region layout_information

        public const string GamepadLayoutName  = "Gamepad";
        public const string DS4HIDLayoutName   = "DualShock4GamepadHID";
        public const string SwitchProLinuxName = "SwitchProControllerLinux";
        public const string IMUControlPath     = "IMU";
        public const string GyroControlPath    = IMUControlPath + "/gyro";
        public const string AccelControlPath   = IMUControlPath + "/accel";
        static GyroControllerLayout GamepadWithIMUOverride = new GyroControllerLayout
        {
            name = "GamepadWithIMU",
            extend = GamepadLayoutName,
            controls = new OverridenControl[]
            {
                new OverridenControl { name = IMUControlPath,   layout = IMUControlPath, synthetic = true, offset = 64 }, //Large offset so that it doesn't conflict with HID values
                new OverridenControl { name = AccelControlPath, layout = "Vector3",      synthetic = true, offset = 0 },
                new OverridenControl { name = GyroControlPath,  layout = "Vector3",      synthetic = true, offset = 12  }
            }
        };

        static GyroControllerLayout SwitchProCorrectedLayout = new GyroControllerLayout
        {
            name = SwitchProLinuxName,
            extend = GamepadLayoutName,
            controls = new OverridenControl[]
            {
                new OverridenControl { name = "buttonNorth", bit = (int)GamepadButton.West,  synthetic = true, layout = "Button"},
                new OverridenControl { name = "buttonWest",  bit = (int)GamepadButton.North, synthetic = true, layout = "Button"},
                new OverridenControl { name = "buttonSouth", bit = (int)GamepadButton.East,  synthetic = true, layout = "Button"},
                new OverridenControl { name = "buttonEast",  bit = (int)GamepadButton.South, synthetic = true, layout = "Button"},
            }
        };

        #endregion
        
        #region player_loop_imu_callback_insertion
        
        private static void AddingMotionSensorUpdateToPlayerLoop()
        {
            // Retrieve the default Player loop system. Get the current loop instead if the default was already modified previously.
            var defaultLoop = PlayerLoop.GetDefaultPlayerLoop();

            // Create a custom update system
            var myCustomUpdate = new PlayerLoopSystem
            {
                subSystemList = null,
                updateDelegate = FeedImuValues,
                type = typeof(MotionSensorUpdate)
            };
          
            // We want the IMU input buffer to be read as close to the time that the input system is updated
            // On windows using Early Update types breaks the input system so we use it on the latest update
            // On linux there are no issues with which updates to use, so we do it in Initialization before EarlyUpdate (Where the input is updated)
            // Differences should be negligeble anyways
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            var loopWithCustomUpdate = InsertSystemAfter<PostLateUpdate>(in defaultLoop, myCustomUpdate);
#else
            var loopWithCustomUpdate = InsertSystemAfter<Initialization>(in defaultLoop, myCustomUpdate);
#endif
            PlayerLoop.SetPlayerLoop(loopWithCustomUpdate);
        }

        private static PlayerLoopSystem InsertSystemAfter<T>(in PlayerLoopSystem loopSystem, PlayerLoopSystem newSystem) where T : struct
        {
            // Create a new root PlayerLoopSystem
            PlayerLoopSystem newPlayerLoop = new()
            {
                loopConditionFunction = loopSystem.loopConditionFunction,
                type = loopSystem.type,
                updateDelegate = loopSystem.updateDelegate,
                updateFunction = loopSystem.updateFunction
            };
            // Create a new list to populate with subsystems, including the custom system
            List<PlayerLoopSystem> newSubSystemList = new();

            //Iterate through the subsystems in the existing loop we passed in and add them to the new list
            if (loopSystem.subSystemList != null)
            {
                for (var i = 0; i < loopSystem.subSystemList.Length; i++)
                {
                    newSubSystemList.Add(loopSystem.subSystemList[i]);
                    // If the previously added subsystem is of the type to add after, add the custom system
                    if (loopSystem.subSystemList[i].type == typeof(T))
                    {
                        newSubSystemList.Add(newSystem);
                    }
                }
            }

            newPlayerLoop.subSystemList = newSubSystemList.ToArray();
            return newPlayerLoop;
        }
        
        #endregion

        static MotionControls[] motionControls;
        static ConcurrentQueue<ImuReading> gyroReadings  = new ConcurrentQueue<ImuReading>(), 
                                           accelReadings = new ConcurrentQueue<ImuReading>();
        
        static bool LoadImuReading(ImuType imuType, ref ImuReading imuReading) 
        => imuType switch
        {
            ImuType.Gyroscope     => gyroReadings.  TryDequeue(out imuReading),
            ImuType.Accelerometer => accelReadings. TryDequeue(out imuReading),
             _ => false
        };

        static void AddNewIMULayout()
        {
           
            InputSystem.RegisterLayout<IMUControl>(IMUControlPath);
            InputSystem.RegisterLayoutOverride(JsonUtility.ToJson(GamepadWithIMUOverride));

#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX

            InputSystem.RegisterLayout(JsonUtility.ToJson(SwitchProCorrectedLayout), SwitchProLinuxName, matches: new InputDeviceMatcher()
            .WithManufacturer("Nintendo Co., Ltd")
            .WithProduct("Nintendo Switch Pro Controller"));

#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

            var harmony = new Harmony("com.MoreStories.HIDPatches");    
            harmony.Patch(
            original: AccessTools.Method(typeof(DualShock4GamepadHID),
                "UnityEngine.InputSystem.LowLevel.IEventPreProcessor.PreProcessEvent"),
            prefix: new HarmonyMethod(typeof(SonyHIDPreProcessPatch), nameof(SonyHIDPreProcessPatch.Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(DualSenseGamepadHID),
                    "UnityEngine.InputSystem.LowLevel.IEventPreProcessor.PreProcessEvent"),
                prefix: new HarmonyMethod(typeof(SonyHIDPreProcessPatch), nameof(SonyHIDPreProcessPatch.Prefix))
            );

#endif
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void AddImuOverride() => AddNewIMULayout();
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ImplementIMU()
        {

#if UNITY_STANDALONE
            AddNewIMULayout();
#endif
            start_sdl_loop ();
            RefreshGamepadControls(null, InputDeviceChange.Added);
            InputSystem.onDeviceChange -= RefreshGamepadControls;
            InputSystem.onDeviceChange += RefreshGamepadControls;

            AddingMotionSensorUpdateToPlayerLoop();       

            register_gyro_callback  (ReadGyro);   
            register_accel_callback (ReadAccel);

            Application.quitting += OnQuit; 

        }

        static void FeedImuValues()
        {
            ImuReading imu = new ImuReading();
            DequeueImuValues(ImuType.Gyroscope,     ref imu);
            DequeueImuValues(ImuType.Accelerometer, ref imu);

        }

        static void DequeueImuValues(ImuType type, ref ImuReading imuReading)
        {  
            while(LoadImuReading(type, ref imuReading)) 
                InputSystem.QueueDeltaStateEvent(motionControls[imuReading.controllerIndex][type], imuReading.value);
        }

        static void OnQuit()
        {
            stop_sdl_loop();
            InputSystem.onDeviceChange -= RefreshGamepadControls;
        }
        
        /// According to the SDL wiki SDL uses a right hand coordinate system where Y is up
        /// Thus positive rotations are those seen from the positive side of an axis going counter clockwise
        /// 
        /// Unity uses a left hand coordinate system where Y is up
        /// Thus positive rotations are those seen from the positive side of an axis going clockwise
        /// 
        /// Thus we translate the values from SDL to be in line with the Unity standard
        [MonoPInvokeCallback (typeof(ControllerSensorCallback))]
        static void ReadGyro  (int controllerIndex, float x, float y, float z) => gyroReadings.  Enqueue(new ImuReading(controllerIndex, -x, -y, z));

        [MonoPInvokeCallback (typeof(ControllerSensorCallback))]
        static void ReadAccel (int controllerIndex, float x, float y, float z) => accelReadings. Enqueue(new ImuReading(controllerIndex,  x,  y, z));

        static void RefreshGamepadControls(InputDevice device, InputDeviceChange change)
        {
            if(change == InputDeviceChange.Added || change == InputDeviceChange.Disconnected)
            {
                var gamepads = Gamepad.all;
                motionControls = new MotionControls[gamepads.Count];

                for (int i = 0; i < gamepads.Count; i++)
                {
                    var gyro  = gamepads[i].TryGetChildControl<Vector3Control>(GyroControlPath);
                    var accel = gamepads[i].TryGetChildControl<Vector3Control>(AccelControlPath);

                    if (gyro == null || accel == null)
                    {
                        Debug.LogError("Motion sensor controls are missing from Input set, check if layout override is working properly");
                        return;
                    }
                    motionControls[i] = new MotionControls(gamepads[i], gyro, accel);
                }
            }
            
        }

    }
}


