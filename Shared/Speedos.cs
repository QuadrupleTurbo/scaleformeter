using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using System.Collections;

#if CLIENT

using CitizenFX.Core.UI;
using scaleformeter.Client.Utils;

#endif

#if SERVER

using scaleformeter.Server;

#endif

namespace scaleformeter.Client
{
    public class Speedos
    {
        #region Fields

#if CLIENT

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
        /// The render target handle.
        /// </summary>
        private int _rtHandle;

        /// <summary>
        /// The current vehicle.
        /// </summary>
        private Vehicle _vehicle;

        /// <summary>
        /// The current vehicle name.
        /// </summary>
        private string _currentVehicleName;

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
        private readonly string _rtName = "clubhouse_plan_01a";

        /// <summary>
        /// The object name.
        /// </summary>
        private readonly string _objName = "bkr_prop_rt_clubhouse_plan_01a";

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
        private Dictionary<string, SpeedoConf> _speedoConfigs = [];

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

        // Task completion for the create prop
        private TaskCompletionSource<bool> _createPropTcs = new();

        // Task completion for the correct scaleform name
        private TaskCompletionSource<string> _correctSfTcs = new();

        // Is the vehicle type driftable
        private bool _isVehicleTypeDriftable;

        // All the dashboard light constants
        private const int DASH_LEFT_INDICATOR = 1;
        private const int DASH_RIGHT_INDICATOR = 2;
        private const int DASH_HANDBRAKE = 4;
        private const int DASH_ENGINE = 8;
        private const int DASH_GAS = 32;
        private const int DASH_OIL = 64;
        private const int DASH_HEADLIGHTS = 128;
        private const int DASH_HIGHBEAM = 256;
        private const int DASH_BATTERY = 512;

#endif

#if SERVER

        private readonly Dictionary<string, SpeedoConf> _speedoConfs = [];
        private readonly Dictionary<Player, List<Entity>> _playerProps = [];

#endif

        #endregion

        #region Constructor

#if CLIENT

        public Speedos()
        {
            // Add event handlers
            Main.Instance.AddEventHandler("swfLiveEditor:scaleformUpdated", new Action<string>(ScaleformUpdated));
            Main.Instance.AddEventHandler("onResourceStop", new Action<string>(OnResourceStop));
            Main.Instance.AddEventHandler("scaleformeter:requestConfigs:Client", new Action<string>(RequestConfigs));
            Main.Instance.AddEventHandler("scaleformeter:createProp:Client", new Action<bool>(CreateProp));
            Main.Instance.AddEventHandler("swfLiveEditor:getCorrectScaleform", new Action<string>(GetCorrectScaleform));

            // Add exports
            Main.Instance.ExportList.Add("IsVisible", IsVisibleExport);
            Main.Instance.ExportList.Add("SetDisplay", SetDisplayExport);
            Main.Instance.ExportList.Add("Prev", PrevExport);
            Main.Instance.ExportList.Add("Next", NextExport);
            Main.Instance.ExportList.Add("SetCurrentSpeedo", SetCurrentSpeedoExport);
            Main.Instance.ExportList.Add("UseMph", UseMphExport);
            Main.Instance.ExportList.Add("Use3d", Use3dExport);
            Main.Instance.ExportList.Add("GetAllSpeedoIds", GetAllSpeedoIdsExport);
            Main.Instance.ExportList.Add("GetAllSpeedoNames", GetAllSpeedoNamesExport);

            // Initialize scaleform
            ScaleformInit();

            "Attepmted to initialize the scaleform".Log();
        }

#endif

#if SERVER

        public Speedos()
        {
            // Add event handlers
            Main.Instance.AddEventHandler("scaleformeter:requestConfigs:Server", new Action<Player>(RequestConfigs));
            Main.Instance.AddEventHandler("scaleformeter:createProp:Server", new Action<Player, int>(CreateProp));
            Main.Instance.AddEventHandler("scaleformeter:deleteProps", new Action<Player>(DeleteProps));
            Main.Instance.AddEventHandler("playerDropped", new Action<Player>(PlayerDropped));

            // Load configs once
            LoadConfigs();
        }

#endif

        #endregion

        #region Events

#if CLIENT

        #region Scaleform updated

        private void ScaleformUpdated(string name = null) => ScaleformInit(name);

        #endregion

        #region On resource stop

        private async void OnResourceStop(string resourceName)
        {
            if (Main.Instance.ResourceName != resourceName) return;
            _obj?.Delete();
        }

        #endregion

        #region Request configs

        private void RequestConfigs(string json)
        {
            try
            {
                _speedoConfigs = Json.Parse<Dictionary<string, SpeedoConf>>(json);
                if (_speedoConfigs.Count == 0)
                    return;
                "Speedo configs have been received and loaded".Log();
            }
            catch (Exception e)
            {
                $"Error parsing speedo configs: {e.Message}".Error();
            }
        }

        #endregion

        #region Create prop

        private void CreateProp(bool success) => _createPropTcs.SetResult(success);

        #endregion

        #region Get correct scaleform

        private void GetCorrectScaleform(string gfx)
        {
            // Prevents other scaleforms interfering since the event is global
            if (!string.IsNullOrEmpty(gfx) && !gfx.StartsWith("scaleformeter")) return;

            // Set the result of the TaskCompletionSource
            _correctSfTcs.SetResult(gfx);
        }

        #endregion

#endif

#if SERVER

        #region Request configs

        private void RequestConfigs([FromSource] Player source)
        {
            "Requested configs from the client".Log();
            BaseScript.TriggerClientEvent(source, "scaleformeter:requestConfigs:Client", Json.Stringify(_speedoConfs));
        }

        #endregion

        #region Create prop

        private async void CreateProp([FromSource] Player source, int netId)
        {
            try
            {
                var currTime = API.GetGameTimer();
                while (Entity.FromNetworkId(netId) == null && API.GetGameTimer() - currTime < 7000)
                {
                    "Waiting for the prop to not be null...".Log();
                    await BaseScript.Delay(0);
                }

                if (Entity.FromNetworkId(netId) == null)
                {
                    "Object was null".Error();
                    source.TriggerEvent("scaleformeter:createProp:Client", false);
                    return;
                }

                // Transform to prop from network id
                Prop obj = Entity.FromNetworkId(netId) as Prop;

                currTime = API.GetGameTimer();
                while (!API.DoesEntityExist(obj.Handle) && API.GetGameTimer() - currTime < 7000)
                {
                    "Waiting for the prop to exist...".Log();
                    await BaseScript.Delay(0);
                }

                if (!API.DoesEntityExist(obj.Handle))
                {
                    "Prop didn't exist!".Error();
                    source.TriggerEvent("scaleformeter:createProp:Client", false);
                    return;
                }

                if (!_playerProps.ContainsKey(source))
                    _playerProps.Add(source, [obj]);
                else
                    _playerProps[source].Add(obj);

                $"Prop was created: {obj.Handle}:{Main.Instance.Clients[API.NetworkGetEntityOwner(obj.Handle)].Name}".Log();

                source.TriggerEvent("scaleformeter:createProp:Client", true);
            }
            catch (Exception e)
            {
                e.ToString().Error();
                source.TriggerEvent("scaleformeter:createProp:Client", false);
            }
        }

        #endregion

        #region Delete props

        private async void DeleteProps([FromSource] Player source)
        {
            try
            {
                if (_playerProps.ContainsKey(source))
                {
                    var newList = new List<Entity>(_playerProps[source]);
                    foreach (var prop in newList)
                    {
                        $"Prop {prop.Handle} was deleted from {source.Name}".Log();
                        API.DeleteEntity(prop.Handle);
                        _playerProps[source].Remove(prop);
                    }
                }
            }
            catch (Exception e)
            {
                e.ToString().Error();
            }
        }

        #endregion

        #region Player dropped

        private async void PlayerDropped([FromSource] Player source)
        {
            $"Player dropped {source.Name}".Log();
            if (_playerProps.ContainsKey(source))
            {
                var newList = new List<Entity>(_playerProps[source]);
                foreach (var prop in newList)
                {
                    $"Prop {prop.Handle} was deleted from {source.Name}".Log();
                    API.DeleteEntity(prop.Handle);
                }
                _playerProps.Remove(source);
            }
        }

        #endregion

#endif

        #endregion

        #region Exports

#if CLIENT

        #region Is visible

        public bool IsVisibleExport()
        {
            return _isDisplaying;
        }

        #endregion

        #region Set display

        public void SetDisplayExport(bool state)
        {
            DisplaySpeedo(state);
        }

        #endregion

        #region Prev

        public void PrevExport()
        {
            // Don't do anything if the scaleform isn't ready
            if (!CanInteractWithScaleform(true))
                return;

            _scaleform.CallFunction("SWITCH_SPEEDO_PREV", _display3D);
            GetCurrentSpeedo(true);
        }

        #endregion

        #region Next

        public void NextExport()
        {
            // Don't do anything if the scaleform isn't ready
            if (!CanInteractWithScaleform(true))
                return;

            _scaleform.CallFunction("SWITCH_SPEEDO_NEXT", _display3D);
            GetCurrentSpeedo(true);
        }

        #endregion

        #region Set current speedo

        public void SetCurrentSpeedoExport(string name)
        {
            // Don't do anything if the scaleform isn't ready
            if (!CanInteractWithScaleform(true))
                return;

            SetCurrentSpeedo(name);
        }

        #endregion

        #region Unit

        public void UseMphExport(bool state)
        {
            // Don't do anything if the scaleform isn't ready
            if (!CanInteractWithScaleform(true))
                return;

            _useMph = state;
            _scaleform.CallFunction("SWITCH_SPEED_UNIT", _useMph);
            API.SetResourceKvp("scaleformeter:useMph", _useMph.ToString());
        }

        #endregion

        #region Use 3D

        public void Use3dExport(bool state)
        {
            // Don't do anything if the scaleform isn't ready
            if (!CanInteractWithScaleform(true))
                return;

            _display3D = state;
            _scaleform.CallFunction("SWITCH_SPEEDO_DIMENSION", _display3D);
            if (_obj != null)
                _obj.Opacity = !_display3D ? 0 : (int)(_currentConf.Opacity * 255);
        }

        #endregion

        #region Get all speedo ids

        public string[] GetAllSpeedoIdsExport()
        {
            return [.. _speedoConfigs.Keys];
        }

        #endregion

        #region Get all speedo names

        public string[] GetAllSpeedoNamesExport()
        {
            return [.. _speedoConfigs.Values.Select(x => x.Name)];
        }

        #endregion

#endif

        #endregion

        #region Ticks

#if CLIENT

        #region Scaleform thread

        private async Task ScaleformThread()
        {
            if (!CanInteractWithScaleform(false))
                return;

            var currentVehicle = Game.PlayerPed.CurrentVehicle;
            if (_vehicle != currentVehicle)
            {
                if (_vehicle != null)
                    await DeleteBox();
                _vehicle = currentVehicle;
                _currentVehicleName = Tools.ToTitleCase(Game.GetGXTEntry(_vehicle.DisplayName));
                
                // Cache expensive model checks
                var model = _vehicle.Model;
                _isVehicleTypeDriftable = !(model.IsBike || model.IsBicycle || model.IsBoat || 
                                             model.IsHelicopter || model.IsPlane || model.IsTrain || 
                                             model.IsCargobob);
            }
            
            if (!_vehicle.Exists())
            {
                await DeleteBox();
                _vehicle = currentVehicle;
                _currentVehicleName = Tools.ToTitleCase(Game.GetGXTEntry(_vehicle.DisplayName));
            }
            
            if (_vehicle.GetPedOnSeat(VehicleSeat.Driver) != Game.PlayerPed)
                return;

            if (_obj != null && !_obj.Exists())
                await DeleteBox();

            // Cache vehicle handle and model to avoid repeated property access
            int vehicleHandle = _vehicle.Handle;
            
            // All the vehicle data - use cached handle
            var ignition = _vehicle.IsEngineRunning;
            var speed = API.GetEntitySpeed(vehicleHandle);
            var kmh = speed * 3.6f;
            var mph = speed * 2.23693629f;
            var gear = _vehicle.CurrentGear;
            var rpm = _vehicle.CurrentRPM;
            var accel = API.GetControlNormal(0, 71); // Use constant instead of enum cast
            var brake = API.GetControlNormal(0, 72);
            var dashLights = API.GetVehicleDashboardLights();

            var isLeftIndicatorOn = (dashLights & DASH_LEFT_INDICATOR) != 0;
            var isRightIndicatorOn = (dashLights & DASH_RIGHT_INDICATOR) != 0;
            var isHandbrakeLightOn = (dashLights & DASH_HANDBRAKE) != 0;
            var isEngineLightOn = (dashLights & DASH_ENGINE) != 0;
            var isAbsLightOn = GetAbsState(vehicleHandle, speed);
            var isGasLightOn = (dashLights & DASH_GAS) != 0;
            var isOilLightOn = (dashLights & DASH_OIL) != 0;
            var isHeadLightsOn = (dashLights & DASH_HEADLIGHTS) != 0;
            var isHighBeamLightsOn = (dashLights & DASH_HIGHBEAM) != 0;
            var isBatteryLightOn = (dashLights & DASH_BATTERY) != 0;
            var isDrifting = IsDrifting(vehicleHandle, speed); // Pass values to avoid re-calculating
            var classType = (int)_vehicle.ClassType;

            _scaleform.CallFunction
            (
                "SET_SPEEDO_INFO",
                ignition,
                kmh,
                mph,
                gear,
                rpm,
                accel,
                brake,
                isLeftIndicatorOn,
                isRightIndicatorOn,
                isHandbrakeLightOn,
                isEngineLightOn,
                isAbsLightOn,
                isGasLightOn,
                isOilLightOn,
                isHeadLightsOn,
                isHighBeamLightsOn,
                isBatteryLightOn,
                isDrifting,
                classType,
                _currentVehicleName,
                _currentConf.Shake
            );

            // Resolution check - only get resolution once
            var currentResolution = Screen.Resolution;
            if (currentResolution != _lastResolution)
            {
                _lastResolution = currentResolution;
                ScaleformInit();
            }

            if (!_display3D)
            {
                _scaleform.Render2D();
                return;
            }

            if (!_hasBoxBeenCreated || _obj == null)
            {
                await CreateBox();
                _hasBoxBeenCreated = true;
                return;
            }

            // 3D rendering
            API.SetTextRenderId(_rtHandle);
            API.Set_2dLayer(4);
            API.SetScaleformFitRendertarget(_scaleform.Handle, true);
            API.SetScriptGfxDrawBehindPausemenu(true);
            _scaleform.Render2D();
            API.SetTextRenderId(API.GetDefaultScriptRendertargetRenderId());
            API.SetScriptGfxDrawBehindPausemenu(false);
            
            // Debug rendering - already behind debug flag, good!
            if (Main.Instance.DebugMode)
            {
                Tools.DrawEntityBoundingBox(_vehicle, 250, 150, 0, 100);
                Vector3[] array = Tools.GetEntityBoundingBox(vehicleHandle);
                for (int i = 0; i < array.Length; i++)
                {
                    Tools.DrawText3D(array[i], i.ToString());
                }
            }
        }

        #endregion

#endif

        #endregion

        #region Tools

#if CLIENT

        #region Scaleform init

        private async void ScaleformInit(string gfx = null)
        {
            if (!string.IsNullOrEmpty(gfx) && !gfx.StartsWith("scaleformeter"))
            {
                "[Scaleformeter] Invalid scaleform name".Error();
                return;
            }

            // Declare the current time
            int currTime;

            // Load all the configs (only once)
            if (string.IsNullOrEmpty(gfx) && !_scaleformIsReady)
            {
                // Load the main config from the client
                _mainConf = Json.Parse<MainConf>(API.LoadResourceFile(API.GetCurrentResourceName(), "configs/main.json"));

                // We need the main config to be loaded
                if (_mainConf == null)
                {
                    "Main config has an error, please check the config syntax.".Error();
                    return;
                }

                "Triggering the event for the configs...".Log();

                // Request the configs from the server
                BaseScript.TriggerServerEvent("scaleformeter:requestConfigs:Server");

                currTime = Game.GameTime;
                while (_speedoConfigs.Count == 0 && Game.GameTime - currTime < 7000)
                    await BaseScript.Delay(0);

                "Configs have been received and loaded".Log();
            }

            // If the configs are empty, return
            if (_speedoConfigs.Count == 0)
            {
                "No speedo configs found!".Error();
                return;
            }

            // Not ready yet
            _scaleformIsReady = false;

            // Dispose the current scaleform if it's loaded
            if (_scaleform != null && _scaleform.IsLoaded)
            {
                "Disposed current scaleform".Alert();
                _scaleform.Dispose();
            }

#if DEBUG

            // This is only for live editing from the scaleform editor (which is private)
            if (API.GetResourceState("swfLiveEditor") == "started")
            {
                _correctSfTcs = new();

                // Get the correct scaleform from the server
                BaseScript.TriggerServerEvent("swfLiveEditor:getCorrectScaleform", "scaleformeter");

                // Wait until the event is completed
                gfx = await _correctSfTcs.Task;
            }
            else
                gfx ??= "scaleformeter";

#else

           // If the scaleform name is not provided, use the default one
            gfx ??= "scaleformeter";

#endif

            // Request scaleform
            _scaleform = new ScaleformWideScreen(gfx);

            // Wait until scaleform is loaded
            currTime = Game.GameTime;
            while (!_scaleform.IsLoaded && Game.GameTime - currTime < 7000)
                await BaseScript.Delay(0);

            // This shouldn't happen...
            if (!_scaleform.IsLoaded)
            {
                "[Scaleformeter] Failed to load the scaleform!".Error();
                return;
            }

            // Get whether the speed unit is mph or kmh
            var speedUnitKvp = API.GetResourceKvpString("scaleformeter:useMph");
            $"Speed unit use mph?: {speedUnitKvp}".Log();
            _useMph = !string.IsNullOrEmpty(speedUnitKvp) ? bool.Parse(speedUnitKvp) : _useMph;

            // Send the configs to the scaleform
            foreach (var conf in _speedoConfigs)
            {
                string colour = $"{conf.Value.ThemeColour.R},{conf.Value.ThemeColour.G},{conf.Value.ThemeColour.B}";
                _scaleform.CallFunction
                (
                    "SET_SPEEDO_CONFIG",
                    conf.Key,
                    conf.Value.Opacity * 100 /* Scaleforms are 0 - 100 alpha */,
                    colour,
                    conf.Value.PosOffset2D.X / 1000,
                    conf.Value.PosOffset2D.Y / 1000,
                    conf.Value.PosOffset2D.Scale,
                    conf.Value.PosOffset3D.Scale,
                    _useMph
                );
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

            // Register commands (only if specified in the main config)
            if (_mainConf.ExposeCommands)
            {
                API.RegisterCommand("sfm", new Action<int, List<object>, string>(async (source, args, raw) =>
                {
                    if (args.Count == 0)
                    {
                        DisplaySpeedo();
                        return;
                    }
                    else if (args.Count == 1)
                    {
                        // Don't do anything if the scaleform isn't ready
                        if (!CanInteractWithScaleform(true))
                            return;

                        switch (args[0].ToString().ToLower())
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
                                _useMph = !_useMph;
                                _scaleform.CallFunction("SWITCH_SPEED_UNIT", _useMph);
                                API.SetResourceKvp("scaleformeter:useMph", _useMph.ToString());
                                break;
                            case "dim":
                                _display3D = !_display3D;
                                _scaleform.CallFunction("SWITCH_SPEEDO_DIMENSION", _display3D);
                                if (_obj != null)
                                    _obj.Opacity = !_display3D ? 0 : (int)(_currentConf.Opacity * 255);
                                break;
                        }
                    }
                }), false);
            }

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

        private void SetCurrentSpeedo(string name = null)
        {
            // Get the current speedo from the kvp or the custom name if it's provided
            string currentSpeedo = !string.IsNullOrEmpty(name) ? name : API.GetResourceKvpString("scaleformeter:lastSpeedo");

            // If the kvp doesn't exist, set the first speedo as default, only if the custom name isn't provided
            if (string.IsNullOrEmpty(currentSpeedo) && name == null)
            {
                var defaultSpeedo = _speedoConfigs.First();
                _currentConf = defaultSpeedo.Value;
                API.SetResourceKvp("scaleformeter:lastSpeedo", defaultSpeedo.Key);
                _scaleform.CallFunction("SET_CURRENT_SPEEDO_BY_ID", defaultSpeedo.Key, _display3D);
                "No last speedo found, setting the first speedo as default".Log();
                return;
            }

            // If for some reason the speedo is found in the kvp, but doesn't exist in the configs, set the first speedo as default, only if the custom name isn't provided
            if (!_speedoConfigs.ContainsKey(currentSpeedo) && name == null)
            {
                var defaultSpeedo = _speedoConfigs.First();
                _currentConf = defaultSpeedo.Value;
                API.SetResourceKvp("scaleformeter:lastSpeedo", defaultSpeedo.Key);
                _scaleform.CallFunction("SET_CURRENT_SPEEDO_BY_ID", defaultSpeedo.Key, _display3D);
                "Last speedo is found, but doesn't exist anymore, setting the first speedo as default".Log();
                return;
            }

            // This check is for the custom speedo name
            if (!string.IsNullOrEmpty(name))
            {
                if (_speedoConfigs.ContainsKey(name))
                    currentSpeedo = name;
                else
                {
                    "This speedo doesn't exist, please check the name".Error();
                    return;
                }
            }

            // Should be safe to set the speedo
            _currentConf = _speedoConfigs[currentSpeedo];
            API.SetResourceKvp("scaleformeter:lastSpeedo", currentSpeedo);
            _scaleform.CallFunction("SET_CURRENT_SPEEDO_BY_ID", currentSpeedo, _display3D);
            "Last speedo found, setting it as default".Log();
        }

        #endregion

        #region Create box

        private async Task CreateBox()
        {
            _creatingBox = true;
            var model = Game.GenerateHashASCII(_objName);
            if (_obj == null)
            {
                API.RequestModel(model);
                var currTime = Game.GameTime;
                while (!API.HasModelLoaded(model) && Game.GameTime - currTime < 7000)
                    await BaseScript.Delay(0);

                if (!API.HasModelLoaded(model))
                {
                    "Failed to load the prop model!".Error();
                    return;
                }

                // Create the prop
                _obj = await World.CreateProp(_objName, _vehicle.Position, _vehicle.Rotation, false, false);

                // Wait for the id to exist in the network
                currTime = Game.GameTime;
                while (!_obj.Exists() && Game.GameTime - currTime < 7000)
                {
                    await BaseScript.Delay(100);
                }

                if (!_obj.Exists())
                {
                    "Prop doesn't exist!".Error();
                    return;
                }

                // Send the prop to the server to be registered
                BaseScript.TriggerServerEvent("scaleformeter:createProp:Server", _obj.NetworkId);

                // Wait until the event is completed
                var success = await _createPropTcs.Task;

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

        private void UpdateBoxParams()
        {
            // Check if the object exists
            if (_obj == null || !_obj.Exists())
            {
                "Updating box params, object doesn't exist".Log();
                return;
            }

            // Settings
            _obj.Opacity = (int)(_currentConf.Opacity * 255);

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

        #region Is drifting

        private bool IsDrifting(int vehicleHandle, float speed)
        {
            if (speed * 3.6f <= 15 || !_isVehicleTypeDriftable)
                return false;

            Vector3 relativeVector = API.GetEntitySpeedVector(vehicleHandle, true);
            double angle = Math.Acos(relativeVector.Y / speed) * 180.0 / Math.PI;

            return !double.IsNaN(angle) && _vehicle.CurrentGear != 0 && angle > 15;
        }

        #endregion

        #region Get abs state

        private bool GetAbsState(int vehicleHandle, float speed)
        {
            // Use cached driftable check (excludes bicycles, boats, etc.)
            if (!_isVehicleTypeDriftable)
                return false;
            
            return API.GetVehicleWheelSpeed(vehicleHandle, 0) == 0.0 && speed > 0.0;
        }

        #endregion

        #region Can intereact with scaleform

        public bool CanInteractWithScaleform(bool notify)
        {
            bool state = _scaleformIsReady && _isDisplaying && !API.IsPauseMenuActive() && Screen.Fading.IsFadedIn && API.GetFollowVehicleCamViewMode() != 4 && !_creatingBox && !_isDeletingBox && Game.PlayerPed.IsInVehicle() && !API.IsPlayerSwitchInProgress();
            if (!state && notify)
                "You can't interact with the scaleform right now".Error();
            return state;
        }

        #endregion

#endif

#if SERVER

        #region Load configs

        private void LoadConfigs()
        {
            // Get all the possible speedo configurations
            Directory.EnumerateFiles($"{API.GetResourcePath(Main.Instance.ResourceName)}/configs/speedos", "*.json").ToList().ForEach(file =>
            {
                try
                {
                    var speedo = Json.Parse<SpeedoConf>(API.LoadResourceFile(Main.Instance.ResourceName, $"configs/speedos/{Path.GetFileNameWithoutExtension(file)}.json"));
                    if (speedo != null)
                    {
                        $"{Path.GetFileNameWithoutExtension(file)}.json has been loaded!".Log();
                        if (speedo.Enabled)
                            _speedoConfs.Add(Path.GetFileNameWithoutExtension(file), speedo);
                    }
                    else
                        $"{Path.GetFileNameWithoutExtension(file)} has an error, please check the config syntax.".Error();
                }
                catch (Exception e)
                {
                    $"Error loading speedo {Path.GetFileNameWithoutExtension(file)}, here's the error: {e.Message}".Error();
                }
            });
        }

        #endregion

#endif

            #endregion

            #region Classes

        public class MainConf
        {
            [JsonProperty("defaultDisplayKey")]
            public string DefaultDisplayKey { get; set; }

            [JsonProperty("exposeCommands")]
            public bool ExposeCommands { get; set; }
        }

        public class SpeedoConf
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("enabled")]
            public bool Enabled { get; set; }

            [JsonProperty("shake")]
            public bool Shake { get; set; }

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
