using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;

namespace scaleformeter.Server
{
    public class Main : BaseScript
    {
        #region Fields

        public static Main Instance;
        public PlayerList Clients;
        public ExportDictionary ExportList;
        public readonly string ResourceName = API.GetCurrentResourceName();
        public bool DebugMode;
        private readonly Dictionary<Player, List<Entity>> _playerProps = [];

        #endregion

        #region Constructor

        public Main()
        {
            Instance = this;
            Clients = Players;
            ExportList = Exports;
            string debugMode = API.GetResourceMetadata(API.GetCurrentResourceName(), "scaleformeter_debug_mode", 0);
            DebugMode = debugMode == "yes" || debugMode == "true" || int.TryParse(debugMode, out int num) && num > 0;

            // Store the prop in the dictionary
            AddEventHandler("scaleformeter:createProp", new Action<Player, int, NetworkCallbackDelegate>(async ([FromSource] source, netId, cb) =>
            {
                try
                {
                    var currTime = API.GetGameTimer();
                    while (Entity.FromNetworkId(netId) == null && API.GetGameTimer() - currTime < 7000)
                    {
                        "Waiting for the prop to not be null...".Log();
                        await Delay(0);
                    }

                    if (Entity.FromNetworkId(netId) == null)
                    {
                        "Object was null".Error();
                        await cb(false);
                        return;
                    }

                    // Transform to prop from network id
                    Prop obj = Entity.FromNetworkId(netId) as Prop;

                    currTime = API.GetGameTimer();
                    while (!API.DoesEntityExist(obj.Handle) && API.GetGameTimer() - currTime < 7000)
                    {
                        "Waiting for the prop to exist...".Log();
                        await Delay(0);
                    }

                    if (!API.DoesEntityExist(obj.Handle))
                    {
                        "Prop didn't exist!".Error();
                        await cb(false);
                        return;
                    }

                    if (!_playerProps.ContainsKey(source))
                        _playerProps.Add(source, [obj]);
                    else
                        _playerProps[source].Add(obj);

                    $"Prop was created: {obj.Handle}:{Players[API.NetworkGetEntityOwner(obj.Handle)].Name}".Log();
                    await cb(true);
                }
                catch (Exception e)
                {
                    e.ToString().Error();
                    await cb(false);
                }
            }));

            // Delete all props spawned by the player
            AddEventHandler("scaleformeter:deleteProps", new Action<Player>(async ([FromSource] source) =>
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
            }));

            // Keeping the dictionary clean
            AddEventHandler("playerDropped", new Action<Player>(([FromSource] source) =>
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
            }));
        }

        #endregion

        #region Tools

        #region Add event handler statically

        public void AddEventHandler(string eventName, Delegate @delegate) => EventHandlers.Add(eventName, @delegate);

        #endregion

        #endregion
    }
}