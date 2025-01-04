using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using Newtonsoft.Json;
using scaleformeter.Client.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace scaleformeter.Client
{
    public class Speedos
    {
        #region Fields

        /// <summary>
        /// To indicate if the scaleform is ready.
        /// </summary>
        private bool _scaleformIsReady;

        /// <summary>
        /// Scaleform handle.
        /// </summary>
        private ScaleformWideScreen _scaleform;

        /// <summary>
        /// Whether the speedo's displaying or not.
        /// </summary>
        private bool _isDisplaying;

        /// <summary>
        /// Whether to display the 3D scaleform or not.
        /// </summary>
        private bool _display3D = false;

        /// <summary>
        /// Use mph/kmh (mph by default).
        /// </summary>
        private bool _useMph = true;

        /// <summary>
        /// Testing.
        /// </summary>
        private float _currBoost = 0;

        /// <summary>
        /// The render target handle.
        /// </summary>
        private int _rtHandle;

        /// <summary>
        /// The current vehicle.
        /// </summary>
        private Vehicle _vehicle;

        /// <summary>
        /// To indicate if the box has been created.
        /// </summary>
        private bool _hasBoxBeenCreated;

        /// <summary>
        /// The box object.
        /// </summary>
        private Prop _obj = null;

        /// <summary>
        /// The render target name.
        /// </summary>
        private string _rtName = "clubhouse_plan_01a";

        /// <summary>
        /// The object name.
        /// </summary>
        private string _objName = "bkr_prop_rt_clubhouse_plan_01a";

        /// <summary>
        /// The object opacity.
        /// </summary>
        private int _objOpacity = 255;

        /// <summary>
        /// The current speedometer configuration.
        /// </summary>
        private SpeedoConf _currentConf;

        /// <summary>
        /// The main configuration.
        /// </summary>
        private MainConf _mainConf = new();

        /// <summary>
        /// All the speedometer configurations.
        /// </summary>
        private readonly Dictionary<string, SpeedoConf> _speedoConfigs = [];

        /// <summary>
        /// The last resolution.
        /// </summary>
        private Size _lastResolution;

        /// <summary>
        /// To indicate if the speedo is scaled to the vehicle dimensions.
        /// </summary>
        private readonly bool _scale3DToModelDimensions = true;

        /// <summary>
        /// To indicate if the player is creating the box.
        /// </summary>
        private bool _creatingBox;

        /// <summary>
        /// To indicate if the player is deleting the box.
        /// </summary>
        private bool _isDeletingBox;

        #endregion

        #region Constructor

        public Speedos()
        {
            // Add event handlers
            Main.Instance.AddEventHandler("swfLiveEditor:scaleformUpdated", new Action<string>(ScaleformUpdated));
            Main.Instance.AddEventHandler("onResourceStop", new Action<string>(OnResourceStop));

            // Initialize scaleform
            ScaleformInit();

            // Register commands
            API.RegisterCommand("sfm", new Action<int, List<object>, string>(async (source, args, raw) =>
            {
                if (args.Count == 0)
                {
                    DisplaySpeedo();
                    return;
                }
                else if (args.Count == 1)
                {
                    switch (args[0])
                    {
                        case "prev":
                            _scaleform.CallFunction("SWITCH_SPEEDO_PREV", _display3D);
                            GetCurrentSpeedo(true);
                            break;
                        case "next":
                            _scaleform.CallFunction("SWITCH_SPEEDO_NEXT", _display3D);
                            GetCurrentSpeedo(true);
                            break;
                        case "unit":
                            _scaleform.CallFunction("SWITCH_SPEED_UNIT");
                            _useMph = !_useMph;
                            API.SetResourceKvp("scaleformeter:useMph", _useMph.ToString());
                            break;
                        case "dim":
                            _display3D = !_display3D;
                            _scaleform.CallFunction("SWITCH_SPEEDO_DIMENSION", _display3D);
                            if (_obj != null)
                                _obj.Opacity = !_display3D ? 0 : _objOpacity;
                            break;
                    }
                }
            }), false);
        }

        #endregion

        #region Events

        private void ScaleformUpdated(string name) => ScaleformInit(name);

        private async void OnResourceStop(string resourceName)
        {
            if (API.GetCurrentResourceName() != resourceName) return;
            _obj?.Delete();
        }

        #endregion

        #region Ticks

        #region Scaleform thread

        private async Task ScaleformThread()
        {
            // General checks
            if (!_scaleformIsReady || !_isDisplaying || API.IsPauseMenuActive() || !Screen.Fading.IsFadedIn || API.GetFollowVehicleCamViewMode() == 4 || _creatingBox || _isDeletingBox)
                return;

            // Check if the vehicle is appropriate
            if (_vehicle != null && _vehicle.Exists())
            {
                var model = _vehicle.Model;
                if (model.IsBicycle || model.IsBoat || model.IsHelicopter || model.IsPlane || model.IsTrain || model.IsCargobob)
                    return;
            }

            // Detect if the player's current vehicle has changed
            if (Game.PlayerPed.IsInVehicle())
            {
                var currentVehicle = Game.PlayerPed.CurrentVehicle;
                if (_vehicle != currentVehicle)
                {
                    if (_vehicle != null)
                    {
                        await DeleteBox();
                    }
                    _vehicle = currentVehicle;
                }
                if (!_vehicle.Exists())
                {
                    await DeleteBox();
                    _vehicle = currentVehicle;
                }
                if (_vehicle.GetPedOnSeat(VehicleSeat.Driver) != Game.PlayerPed)
                    return;

                if (_obj != null && !_obj.Exists())
                    await DeleteBox();

                var ignition = _vehicle.IsEngineRunning;
                var speed = _vehicle.Speed;
                var kmh = speed * 3.6f;
                var mph = speed * 2.23693629f;
                var gear = _vehicle.CurrentGear;
                var rpm = _vehicle.CurrentRPM;
                var accel = API.GetControlNormal(0, (int)Control.VehicleAccelerate);
                var brake = API.GetControlNormal(0, (int)Control.VehicleBrake);
                var handbrake = API.GetControlNormal(0, (int)Control.VehicleHandbrake);
                var abs = (API.GetVehicleWheelSpeed(_vehicle.Handle, 0) == 0.0) && (_vehicle.Speed > 0.0);
                var lights = _vehicle.AreLightsOn || _vehicle.AreHighBeamsOn;
                var classType = _vehicle.ClassType;
                var isDrifting = IsDrifting(_vehicle);

                _scaleform.CallFunction("SET_SPEEDO_INFO", ignition, kmh, mph, gear, rpm, accel, brake, handbrake, abs, lights, isDrifting, (int)classType);

                // The scaleform needs to adjust to the new resolution
                if (Screen.Resolution != _lastResolution)
                {
                    _lastResolution = Screen.Resolution;
                    ScaleformInit();
                }

                if (!_display3D)
                {
                    _scaleform.Render2D();
                }
                else
                {
                    if (!_hasBoxBeenCreated)
                    {
                        await CreateBox();
                        _hasBoxBeenCreated = true;
                    }
                    else
                    {
                        if (_obj == null)
                        {
                            await CreateBox();
                            _hasBoxBeenCreated = true;
                        }
                        else
                        {
                            API.SetTextRenderId(_rtHandle);
                            API.Set_2dLayer(4);
                            API.SetScaleformFitRendertarget(_scaleform.Handle, true);
                            API.SetScriptGfxDrawBehindPausemenu(true);
                            _scaleform.Render2D();
                            API.SetTextRenderId(API.GetDefaultScriptRendertargetRenderId());
                            API.SetScriptGfxDrawBehindPausemenu(false);
                            if (Main.Instance.DebugMode)
                            {
                                Tools.DrawEntityBoundingBox(_vehicle, 250, 150, 0, 100);
                                Vector3[] array = Tools.GetEntityBoundingBox(_vehicle.Handle);
                                for (int i = 0; i < array.Length; i++)
                                {
                                    Vector3 item = array[i];
                                    Tools.DrawText3D(item, i.ToString());
                                }
                            }
                        }
                    }
                }
            }

            await Task.FromResult(0);
        }

        #endregion

        #endregion

        #region Tools

        #region Scaleform init

        private async void ScaleformInit(string gfx = null)
        {
            if (!string.IsNullOrEmpty(gfx) && !gfx.StartsWith("scaleformeter"))
                return;

            // Load all the configs (only once)
            if (string.IsNullOrEmpty(gfx) && !_scaleformIsReady)
            {
                _mainConf = Json.Parse<MainConf>(API.LoadResourceFile(API.GetCurrentResourceName(), "configs/main.json"));
                var custom1 = Json.Parse<SpeedoConf>(API.LoadResourceFile(API.GetCurrentResourceName(), "configs/speedos/custom1.json"));
                var custom2 = Json.Parse<SpeedoConf>(API.LoadResourceFile(API.GetCurrentResourceName(), "configs/speedos/custom2.json"));
                if (_mainConf == null)
                {
                    "Main config has an error, please check the config syntax.".Error();
                    return;
                }
                else if (custom1 == null)
                {
                    "Speedometer Custom1 config has an error, please check the config syntax.".Error();
                    return;
                }
                else if (custom2 == null)
                {
                    "Speedometer Custom2 config has an error, please check the config syntax.".Error();
                    return;
                }
                _speedoConfigs.Add("custom1", custom1);
                _speedoConfigs.Add("custom2", custom2);
            }

            // Not ready yet
            _scaleformIsReady = false;

            // Dispose the current scaleform if it's loaded
            if (_scaleform != null && _scaleform.IsLoaded)
            {
                "Disposed current scaleform".Alert();
                _scaleform.Dispose();
            }

            // This is only for live editing from the scaleform editor (which is private)
            if (API.GetResourceState("swfLiveEditor") == "started")
            {
                // Create a TaskCompletionSource to await the event completion
                var result = new TaskCompletionSource<string>();

                // Get the correct scaleform from the server
                BaseScript.TriggerServerEvent("swfLiveEditor:getCorrectScaleform", "scaleformeter", new Action<string>(result.SetResult));

                // Wait until the event is completed
                gfx = await result.Task;
            }
            else
                gfx ??= "scaleformeter";

            // Request scaleform
            _scaleform = new ScaleformWideScreen(gfx);

            // Wait until scaleform is loaded
            while (!_scaleform.IsLoaded)
                await BaseScript.Delay(0);

            // Get whether the speed unit is mph or kmh
            var speedUnitKvp = API.GetResourceKvpString("scaleformeter:useMph");
            $"Speed unit KVP: {speedUnitKvp}".Log();
            _useMph = !string.IsNullOrEmpty(speedUnitKvp) ? bool.Parse(speedUnitKvp) : _useMph;

            // Send the configs to the scaleform
            foreach (var conf in _speedoConfigs)
            {
                string colour = $"{conf.Value.ThemeColour.R},{conf.Value.ThemeColour.G},{conf.Value.ThemeColour.B}";
                _scaleform.CallFunction("SET_SPEEDO_CONFIG", conf.Key, conf.Value.Opacity * 100, colour, conf.Value.PosOffset2D.X / 1000, conf.Value.PosOffset2D.Y / 1000, conf.Value.PosOffset2D.Scale, conf.Value.PosOffset3D.Scale, _useMph);
            }

            // Set the display state depending on the saved KVP state
            var displayStateKvp = API.GetResourceKvpString("scaleformeter:displayState");
            $"Display state KVP: {displayStateKvp}".Log();
            if (!string.IsNullOrEmpty(displayStateKvp))
                DisplaySpeedo(bool.Parse(displayStateKvp));

            // Set the current speedo
            SetCurrentSpeedo();

            // Store the current resolution
            _lastResolution = Screen.Resolution;

            // Attach the ticks
            Main.Instance.AttachTick(ScaleformThread);

            // Register the key mapping
            API.RegisterKeyMapping("sfm", "Scaleformeter", "keyboard", _mainConf.DefaultDisplayKey);

            // Scaleform is ready
            _scaleformIsReady = true;

            "Scaleform is ready".Log();
        }

        #endregion

        #region Display speedo

        /// <summary>
        /// To open/close the menu
        /// </summary>
        private async void DisplaySpeedo(bool? overrite = null)
        {
            _isDisplaying = overrite != null ? (bool)overrite : !_isDisplaying;

            // Toggle the display state
            _scaleform.CallFunction("DISPLAY", _isDisplaying);

            // Save display state to kvp
            API.SetResourceKvp("scaleformeter:displayState", _isDisplaying.ToString());
        }

        #endregion

        #region Get display state

        private async Task<bool> GetDisplayState()
        {
            return (bool)await _scaleform.GetResult<bool>("GET_DISPLAY_STATE");
        }

        #endregion

        #region Get current speedo

        private async void GetCurrentSpeedo(bool special = false)
        {
            string id = (string)await _scaleform.GetResult<string>("GET_CURRENT_SPEEDO");
            if (_speedoConfigs.ContainsKey(id))
            {
                _currentConf = _speedoConfigs[id];
                API.SetResourceKvp("scaleformeter:lastSpeedo", id);
            }
            if (special)
                UpdateBoxParams();
        }

        #endregion

        #region Set current speedo

        private void SetCurrentSpeedo()
        {
            // Get the current speedo from the kvp
            string currentSpeedo = API.GetResourceKvpString("scaleformeter:lastSpeedo");

            // If the kvp doesn't exist, set the first speedo as default
            if (string.IsNullOrEmpty(currentSpeedo))
            {
                var defaultSpeedo = _speedoConfigs.First();
                _currentConf = defaultSpeedo.Value;
                API.SetResourceKvp("scaleformeter:lastSpeedo", defaultSpeedo.Key);
                _scaleform.CallFunction("SET_CURRENT_SPEEDO_BY_ID", defaultSpeedo.Key, _display3D);
                return;
            }

            // If for some reason the speedo is found in the kvp, but doesn't exist in the configs, set the first speedo as default
            if (!_speedoConfigs.ContainsKey(currentSpeedo))
            {
                var defaultSpeedo = _speedoConfigs.First();
                _currentConf = defaultSpeedo.Value;
                API.SetResourceKvp("scaleformeter:lastSpeedo", defaultSpeedo.Key);
                _scaleform.CallFunction("SET_CURRENT_SPEEDO_BY_ID", defaultSpeedo.Key, _display3D);
                return;
            }

            // Should be safe to set the speedo from the kvp now
            _currentConf = _speedoConfigs[currentSpeedo];
            API.SetResourceKvp("scaleformeter:lastSpeedo", currentSpeedo);
            _scaleform.CallFunction("SET_CURRENT_SPEEDO_BY_ID", currentSpeedo, _display3D);
        }

        private bool IsDrifting(Vehicle vehicle)
        {
            if (vehicle.Model.IsBike || vehicle.Model.IsBicycle || vehicle.Model.IsBoat || vehicle.Model.IsHelicopter || vehicle.Model.IsPlane || vehicle.Model.IsTrain || vehicle.Model.IsCargobob)
                return false;

            float speed = API.GetEntitySpeed(vehicle.Handle);
            Vector3 relativeVector = API.GetEntitySpeedVector(vehicle.Handle, true);
            double angle = Math.Acos(relativeVector.Y / speed) * 180f / Math.PI;

            if (double.IsNaN(angle))
            {
                angle = 0;
            }

            return speed * 3.6f > 15 && vehicle.CurrentGear != 0 && angle > 15;
        }

        private async Task CreateBox()
        {
            _creatingBox = true;
            var model = Game.GenerateHashASCII(_objName);
            if (_obj == null)
            {
                API.RequestModel(model);
                while (!API.HasModelLoaded(model))
                    await BaseScript.Delay(0);

                if (!API.HasModelLoaded(model))
                {
                    "Failed to load the prop model!".Error();
                    return;
                }

                // Create the prop
                _obj = await World.CreateProp(_objName, _vehicle.Position, _vehicle.Rotation, false, false);

                // Wait for the id to exist in the network
                var currTime = Game.GameTime;
                while (!_obj.Exists() && Game.GameTime - currTime < 7000)
                {
                    await BaseScript.Delay(100);
                }

                if (!_obj.Exists())
                {
                    "Prop doesn't exist!".Error();
                    return;
                }

                // Create a TaskCompletionSource to await the event completion
                var tcs = new TaskCompletionSource<bool>();

                BaseScript.TriggerServerEvent("scaleformeter:createProp", _obj.NetworkId, new Action<bool>(tcs.SetResult));

                // Wait until the event is completed
                var success = await tcs.Task;

                if (!success)
                {
                    "Failed to initialise the prop on the server!".Error();
                    return;
                }

                _rtHandle = CreateNamedRenderTarget(_rtName, model);
                API.SetModelAsNoLongerNeeded(model);
                API.SetEntityCollision(_obj.Handle, false, false);
            }

            // Update the box parameters
            UpdateBoxParams();

            _creatingBox = false;
        }

        #endregion

        #region Update box params

        private void UpdateBoxParams()
        {
            // Check if the object exists
            if (_obj == null || !_obj.Exists())
                return;

            // Settings
            _obj.Opacity = _objOpacity;

            // Get vehicle dimensions    
            Vector3 vehicleDimensions = _vehicle.Model.GetDimensions();

            // Adjust position based on vehicle dimensions if needed
            Vector3 adjustedPos = new(
                !_vehicle.Model.IsBike ? _scale3DToModelDimensions ? _currentConf.PosOffset3D.X * vehicleDimensions.X : _currentConf.PosOffset3D.X : 1.3f,
                !_vehicle.Model.IsBike ? _scale3DToModelDimensions ? _currentConf.PosOffset3D.Y * vehicleDimensions.Y : _currentConf.PosOffset3D.Y : -1,
                !_vehicle.Model.IsBike ? _scale3DToModelDimensions ? _currentConf.PosOffset3D.Z * vehicleDimensions.Z : _currentConf.PosOffset3D.Z : 0.5f
            );

            // Attach the object to the vehicle
            EntityBone chassis = _vehicle.Bones["chassis"];
            _obj.AttachTo(chassis, adjustedPos, new(0, 0, _currentConf.PosOffset3D.Rot));
        }

        #endregion

        #region Delete box

        private async Task DeleteBox()
        {
            _isDeletingBox = true;
            "Deleting box...".Warning();
            if (_obj != null)
            {
                //API.ReleaseNamedRendertarget(_rtName);
                // So since the handle exists, but the object doesn't, let's try to delete it on the server
                BaseScript.TriggerServerEvent("scaleformeter:deleteProps");
                await BaseScript.Delay(1000);
                _obj = null;
                "Box deleted".Log();
                _hasBoxBeenCreated = false;
            }
            _isDeletingBox = false;
        }

        #endregion

        #region Create named render target

        private int CreateNamedRenderTarget(string name, uint model)
        {
            int handle = 0;
            if (!API.IsNamedRendertargetRegistered(name))
                API.RegisterNamedRendertarget(name, false);
            if (!API.IsNamedRendertargetLinked(model))
                API.LinkNamedRendertarget(model);
            if (API.IsNamedRendertargetRegistered(name))
                handle = API.GetNamedRendertargetRenderId(name);
            return handle;
        }

        #endregion

        #region Get turbo boost

        public float GetTurboBoost(Vehicle vehicle)
        {
            var turboData = new Turbo()
            {
                maxBoost = 15f,
                maxVacuum = 7.25f,
                vacuumRate = 0.91f,
                rpmSpoolStart = 0.3f,
                rpmSpoolEnd = 0.85f,
                boostRate = 0.899f
            };

            float currentBoost;
            float newBoost;
            float maxBoost = turboData.maxBoost / 14.5038f;
            float maxVacuum = (turboData.maxVacuum / 14.5038f) * -1;

            if (!API.IsToggleModOn(vehicle.Handle, 18) || !API.GetIsVehicleEngineRunning(vehicle.Handle))
            {
                currentBoost = _currBoost;
                newBoost = Tools.Lerp(currentBoost, 0.0f, 1.0f - (float)Math.Pow(1.0f - turboData.vacuumRate, 0.0166667f));
                return newBoost;
            }

            currentBoost = _currBoost;
            currentBoost = Tools.Clamp(currentBoost, maxVacuum, maxBoost);

            float rpm = vehicle.CurrentRPM;

            float boostClosed = Tools.Map(rpm, 0.2f, 1.0f, 0.0f, maxVacuum);
            boostClosed = Tools.Clamp(boostClosed, maxVacuum, 0.0f);

            float boostWOT = Tools.Map(rpm, turboData.rpmSpoolStart, turboData.rpmSpoolEnd, 0.0f, maxBoost);
            boostWOT = Tools.Clamp(boostWOT, 0.0f, maxBoost);

            float now = Tools.Map(API.GetVehicleThrottleOffset(vehicle.Handle), 0.0f, 1.0f, boostClosed, boostWOT);

            float lerpRate;
            if (now > currentBoost)
                lerpRate = turboData.boostRate;
            else
                lerpRate = turboData.vacuumRate;

            newBoost = Tools.Lerp(currentBoost, now, 1.0f - (float)Math.Pow(1.0f - lerpRate, 0.0166667f));
            float limBoost = maxBoost;

            newBoost = Tools.Clamp(newBoost, maxVacuum, maxBoost);


            return newBoost;
        }

        public struct Turbo
        {
            public float maxBoost;
            public float minBoost;
            public float maxVacuum;
            public float vacuumRate;
            public float rpmSpoolStart;
            public float rpmSpoolEnd;
            public float boostRate;
        };

        #endregion

        #region Wipe KVPs

        /// <summary>
        /// Mostly for debug purposes.
        /// </summary>
        private void WipeKvps()
        {
            API.DeleteResourceKvp("scaleformeter:displayState");
            API.DeleteResourceKvp("scaleformeter:lastSpeedo");
            API.DeleteResourceKvp("scaleformeter:useMph");
        }

        #endregion

        #endregion

        #region Classes

        public class MainConf
        {
            [JsonProperty("defaultDisplayKey")]
            public string DefaultDisplayKey { get; set; }
        }

        public class SpeedoConf
        {
            [JsonProperty("opacity")]
            public float Opacity { get; set; }

            [JsonProperty("themeColour")]
            public Rgb ThemeColour { get; set; }

            [JsonProperty("2dPosOffset")]
            public PosOffset2D PosOffset2D { get; set; }

            [JsonProperty("3dPosOffset")]
            public PosOffset3D PosOffset3D { get; set; }
        }


        public class Rgb
        {
            [JsonProperty("r")]
            public int R { get; set; }

            [JsonProperty("g")]
            public int G { get; set; }

            [JsonProperty("b")]
            public int B { get; set; }
        }


        public class PosOffset2D
        {
            [JsonProperty("x")]
            public float X { get; set; }

            [JsonProperty("y")]
            public float Y { get; set; }

            [JsonProperty("scale")]
            public float Scale { get; set; }
        }

        public class PosOffset3D
        {
            [JsonProperty("x")]
            public float X { get; set; }

            [JsonProperty("y")]
            public float Y { get; set; }

            [JsonProperty("z")]
            public float Z { get; set; }

            [JsonProperty("rot")]
            public float Rot { get; set; }

            [JsonProperty("scale")]
            public float Scale { get; set; }
        }

        #endregion
    }
}
