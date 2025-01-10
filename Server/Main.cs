using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using scaleformeter.Client;

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

        #endregion

        #region Constructor

        public Main()
        {
            Instance = this;
            Clients = Players;
            ExportList = Exports;
            string debugMode = API.GetResourceMetadata(API.GetCurrentResourceName(), "scaleformeter_debug_mode", 0);
            DebugMode = debugMode == "yes" || debugMode == "true" || int.TryParse(debugMode, out int num) && num > 0;

            // Load the speedos
            new Speedos();
        }

        #endregion

        #region Tools

        #region Add event handler statically

        public void AddEventHandler(string eventName, Delegate @delegate) => EventHandlers.Add(eventName, @delegate);

        #endregion

        #endregion
    }
}