using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Threading.Tasks;

namespace scaleformeter.Client
{
    public class Main : BaseScript
    {
        #region Fields

        public static Main Instance;
        public ExportDictionary ExportList;
        public readonly string ResourceName = API.GetCurrentResourceName();
        public bool DebugMode;

        #endregion

        #region Constructor

        public Main()
        {
            Instance = this;
            ExportList = Exports;
            string debugMode = API.GetResourceMetadata(API.GetCurrentResourceName(), "scaleformeter_debug_mode", 0);
            DebugMode = debugMode == "yes" || debugMode == "true" || int.TryParse(debugMode, out int num) && num > 0;

            // Load the speedos
            new Speedos();

            "Client started".Log();
        }

        #endregion

        #region Tools

        #region Add event handler statically

        public void AddEventHandler(string eventName, Delegate @delegate) => EventHandlers.Add(eventName, @delegate);

        #endregion

        #region Attach tick statically

        public void AttachTick(Func<Task> task)
        {
            Tick += task;
            $"Attached tick: {task.Method.Name}".Log();
        }

        #endregion

        #region Detach tick statically

        public void DetachTick(Func<Task> task)
        {
            Tick -= task;
            $"Detached tick: {task.Method.Name}".Log();
        }

        #endregion

        #region Register key mapping

        public void RegisterKeyMapping(string command, string description, string defaultKey, Delegate @delegate)
        {
            API.RegisterKeyMapping(command, description, "keyboard", defaultKey);
            API.RegisterCommand(command, @delegate, false);
        }

        #endregion

        #endregion
    }
}