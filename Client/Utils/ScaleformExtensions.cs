using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace scaleformeter.Client.Utils
{
    public class ScaleformWideScreen : INativeValue, IDisposable
    {
        public ScaleformWideScreen(string scaleformID)
        {
            _handle = API.RequestScaleformMovieInstance(scaleformID);
        }

        ~ScaleformWideScreen()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (IsLoaded)
            {
                API.SetScaleformMovieAsNoLongerNeeded(ref _handle);
            }

            GC.SuppressFinalize(this);
        }


        public int Handle
        {
            get { return _handle; }
        }

        private int _handle;

        public override ulong NativeValue
        {
            get
            {
                return (ulong)Handle;
            }
            set
            {
                _handle = unchecked((int)value);
            }
        }

        public bool IsValid
        {
            get
            {
                return Handle != 0;
            }
        }
        public bool IsLoaded
        {
            get
            {
                return API.HasScaleformMovieLoaded(Handle);
            }
        }

        public async Task<object> GetResult<T>(string function, params object[] arguments)
        {
            API.BeginScaleformMovieMethod(Handle, function);
            foreach (object argument in arguments)
            {
                if (argument is int argInt)
                {
                    API.PushScaleformMovieMethodParameterInt(argInt);
                }
                else if (argument is string || argument is char)
                {
                    API.PushScaleformMovieMethodParameterString(argument.ToString());
                }
                else if (argument is double || argument is float)
                {
                    API.PushScaleformMovieMethodParameterFloat((float)argument);
                }
                else if (argument is bool argBool)
                {
                    API.PushScaleformMovieMethodParameterBool(argBool);
                }
                else
                {
                    throw new ArgumentException(string.Format("Unknown argument type '{0}' passed to scaleform with handle {1}...", argument.GetType().Name, Handle), "arguments");
                }
            }
            var handle = API.EndScaleformMovieMethodReturn();
            while (!API.IsScaleformMovieMethodReturnValueReady(handle))
                await BaseScript.Delay(0);
            switch (typeof(T).Name)
            {
                case "Int32":
                    return API.GetScaleformMovieMethodReturnValueInt(handle);
                case "String":
                    return API.GetScaleformMovieMethodReturnValueString(handle);
                case "Boolean":
                    return API.GetScaleformMovieMethodReturnValueBool(handle);
                default:
                    throw new ArgumentException($"Unsupported return type '{typeof(T).Name}' passed to scaleform with handle {Handle}...");
            }
        }

        public void CallFunction(string function, params object[] arguments)
        {
            API.BeginScaleformMovieMethod(Handle, function);
            foreach (object argument in arguments)
            {
                if (argument is int argInt)
                {
                    API.PushScaleformMovieMethodParameterInt(argInt);
                }
                else if (argument is string || argument is char)
                {
                    API.PushScaleformMovieMethodParameterString(argument.ToString());
                }
                else if (argument is double || argument is float)
                {
                    API.PushScaleformMovieMethodParameterFloat((float)argument);
                }
                else if (argument is bool argBool)
                {
                    API.PushScaleformMovieMethodParameterBool(argBool);
                }
                else
                {
                    throw new ArgumentException(string.Format("Unknown argument type '{0}' passed to scaleform with handle {1}...", argument.GetType().Name, Handle), "arguments");
                }
            }
            API.EndScaleformMovieMethod();
        }

        public void Render2D()
        {
            API.DrawScaleformMovieFullscreen(Handle, 255, 255, 255, 255, 0);
        }

        public void Render2DScreenSpace(PointF location, PointF size)
        {
            float x = location.X / Screen.Width;
            float y = location.Y / Screen.Height;
            float width = size.X / Screen.Width;
            float height = size.Y / Screen.Height;

            API.DrawScaleformMovie(Handle, x + (width / 2.0f), y + (height / 2.0f), width, height, 255, 255, 255, 255, 0);
        }

        public void Render3D(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            // Was trying some mad stuff out to try solve the 3d jittering issue, but failed...
            var posXRaw = position.X.ToString("G");
            var posYRaw = position.Y.ToString("G");
            var posZRaw = position.Z.ToString("G");
            var rotXRaw = rotation.X.ToString("G");
            var rotYRaw = rotation.Y.ToString("G");
            var rotZRaw = rotation.Z.ToString("G");
            var scaleXRaw = scale.X.ToString("G");
            var scaleYRaw = scale.Y.ToString("G");
            var scaleZRaw = scale.Z.ToString("G");
            var posX = float.Parse(posXRaw);
            var posY = float.Parse(posYRaw);
            var posZ = float.Parse(posZRaw);
            var rotX = float.Parse(rotXRaw);
            var rotY = float.Parse(rotYRaw);
            var rotZ = float.Parse(rotZRaw);
            var scaleX = float.Parse(scaleXRaw);
            var scaleY = float.Parse(scaleYRaw);
            var scaleZ = float.Parse(scaleZRaw);
            API.DrawScaleformMovie_3dNonAdditive(Handle, posX, posY, posZ, rotX, rotY, rotZ, 2.000f, 2.000f, 1.000f, scaleX, scaleY, scaleZ, 2);
        }

        public void Render3DAdditive(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            API.DrawScaleformMovie_3d(Handle, position.X, position.Y, position.Z, rotation.X, rotation.Y, rotation.Z, 2.0f, 2.0f, 1.0f, scale.X, scale.Y, scale.Z, 2);
        }

        public void Render3DOnEntity(Entity entityHandle, int boneId, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            Function.Call((Hash)3478355934ul, Handle, entityHandle, 0, 0, 0, 0, 0, 0, 2.0f, 2.0f, 1.0f, scale.X, scale.Y, scale.Z, 2);
        }

        public void DrawScaleformMovie_3dSolidAttachedToEntity(int entity, Vector3 pos, Vector3 rot, Vector3 scale)
        {
            Function.Call((Hash)2445233739ul, Handle, entity, pos.X, pos.Y, pos.Z, rot.X, rot.Y, rot.Z, 2.0f, 2.0f, 1.0f, scale.X, scale.Y, scale.Z);
        }
    }
}