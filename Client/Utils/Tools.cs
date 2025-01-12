using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Threading.Tasks;

namespace scaleformeter.Client
{
    internal class Tools
    {
        #region Fields

        public const float DegToRad = (float)Math.PI / 180.0f;

        #endregion

        #region Format timer

        public static string FormatTimer(int start, int curr)
        {
            int newTime;

            if (curr == 0) newTime = start;
            else newTime = curr - start;

            var ms = Math.Floor((double)newTime % 1000);
            var seconds = Math.Floor((double)newTime / 1000);
            var minutes = Math.Floor(seconds / 60); seconds = Math.Floor(seconds % 60);

            return string.Format("{0:0}:{1:00}:{2:000}", minutes, seconds, ms);
        }

        #endregion

        #region GetUserInput

        /// <summary>
        /// Get a user input text string.
        /// </summary>
        /// <returns></returns>
        public static async Task<string> GetUserInput() => await GetUserInput(null, null, 30);

        /// <summary>
        /// Get a user input text string.
        /// </summary>
        /// <param name="maxInputLength"></param>
        /// <returns></returns>
        public static async Task<string> GetUserInput(int maxInputLength) => await GetUserInput(null, null, maxInputLength);

        /// <summary>
        /// Get a user input text string.
        /// </summary>
        /// <param name="windowTitle"></param>
        /// <returns></returns>
        public static async Task<string> GetUserInput(string windowTitle) => await GetUserInput(windowTitle, null, 30);

        /// <summary>
        /// Get a user input text string.
        /// </summary>
        /// <param name="windowTitle"></param>
        /// <param name="maxInputLength"></param>
        /// <returns></returns>
        public static async Task<string> GetUserInput(string windowTitle, int maxInputLength) => await GetUserInput(windowTitle, null, maxInputLength);

        /// <summary>
        /// Get a user input text string.
        /// </summary>
        /// <param name="windowTitle"></param>
        /// <param name="defaultText"></param>
        /// <returns></returns>
        public static async Task<string> GetUserInput(string windowTitle, string defaultText) => await GetUserInput(windowTitle, defaultText, 30);

        /// <summary>
        /// Get a user input text string.
        /// </summary>
        /// <param name="windowTitle"></param>
        /// <param name="defaultText"></param>
        /// <param name="maxInputLength"></param>
        /// <returns></returns>
        public static async Task<string> GetUserInput(string windowTitle, string defaultText, int maxInputLength)
        {
            // Create the window title string.
            var spacer = "\t";
            API.AddTextEntry($"{API.GetCurrentResourceName().ToUpper()}_WINDOW_TITLE", $"{windowTitle ?? "Enter"}:{spacer}" /*+ "(MAX {maxInputLength} Characters)"*/);

            // Display the input box.
            API.DisplayOnscreenKeyboard(1, $"{API.GetCurrentResourceName().ToUpper()}_WINDOW_TITLE", "", defaultText ?? "", "", "", "", maxInputLength);
            await BaseScript.Delay(0);

            // Wait for a result.
            while (true)
            {
                int keyboardStatus = API.UpdateOnscreenKeyboard();
                DisableMovementControlsThisFrame(true, true);

                switch (keyboardStatus)
                {
                    case 3: // not displaying input field anymore somehow
                    case 2: // cancelled
                        return null;

                    case 1: // finished editing
                        return API.GetOnscreenKeyboardResult();

                    default:
                        await BaseScript.Delay(0);
                        break;
                }
            }
        }

        #endregion

        #region Disable Movement Controls

        /// <summary>
        /// Disables all movement and camera related controls this frame.
        /// </summary>
        /// <param name="disableMovement"></param>
        /// <param name="disableCameraMovement"></param>
        public static void DisableMovementControlsThisFrame(bool disableMovement, bool disableCameraMovement)
        {
            if (disableMovement)
            {
                Game.DisableControlThisFrame(0, Control.MoveDown);
                Game.DisableControlThisFrame(0, Control.MoveDownOnly);
                Game.DisableControlThisFrame(0, Control.MoveLeft);
                Game.DisableControlThisFrame(0, Control.MoveLeftOnly);
                Game.DisableControlThisFrame(0, Control.MoveLeftRight);
                Game.DisableControlThisFrame(0, Control.MoveRight);
                Game.DisableControlThisFrame(0, Control.MoveRightOnly);
                Game.DisableControlThisFrame(0, Control.MoveUp);
                Game.DisableControlThisFrame(0, Control.MoveUpDown);
                Game.DisableControlThisFrame(0, Control.MoveUpOnly);
                Game.DisableControlThisFrame(0, Control.VehicleFlyMouseControlOverride);
                Game.DisableControlThisFrame(0, Control.VehicleMouseControlOverride);
                Game.DisableControlThisFrame(0, Control.VehicleMoveDown);
                Game.DisableControlThisFrame(0, Control.VehicleMoveDownOnly);
                Game.DisableControlThisFrame(0, Control.VehicleMoveLeft);
                Game.DisableControlThisFrame(0, Control.VehicleMoveLeftRight);
                Game.DisableControlThisFrame(0, Control.VehicleMoveRight);
                Game.DisableControlThisFrame(0, Control.VehicleMoveRightOnly);
                Game.DisableControlThisFrame(0, Control.VehicleMoveUp);
                Game.DisableControlThisFrame(0, Control.VehicleMoveUpDown);
                Game.DisableControlThisFrame(0, Control.VehicleSubMouseControlOverride);
                Game.DisableControlThisFrame(0, Control.Duck);
                Game.DisableControlThisFrame(0, Control.SelectWeapon);
            }
            if (disableCameraMovement)
            {
                Game.DisableControlThisFrame(0, Control.LookBehind);
                Game.DisableControlThisFrame(0, Control.LookDown);
                Game.DisableControlThisFrame(0, Control.LookDownOnly);
                Game.DisableControlThisFrame(0, Control.LookLeft);
                Game.DisableControlThisFrame(0, Control.LookLeftOnly);
                Game.DisableControlThisFrame(0, Control.LookLeftRight);
                Game.DisableControlThisFrame(0, Control.LookRight);
                Game.DisableControlThisFrame(0, Control.LookRightOnly);
                Game.DisableControlThisFrame(0, Control.LookUp);
                Game.DisableControlThisFrame(0, Control.LookUpDown);
                Game.DisableControlThisFrame(0, Control.LookUpOnly);
                Game.DisableControlThisFrame(0, Control.ScaledLookDownOnly);
                Game.DisableControlThisFrame(0, Control.ScaledLookLeftOnly);
                Game.DisableControlThisFrame(0, Control.ScaledLookLeftRight);
                Game.DisableControlThisFrame(0, Control.ScaledLookUpDown);
                Game.DisableControlThisFrame(0, Control.ScaledLookUpOnly);
                Game.DisableControlThisFrame(0, Control.VehicleDriveLook);
                Game.DisableControlThisFrame(0, Control.VehicleDriveLook2);
                Game.DisableControlThisFrame(0, Control.VehicleLookBehind);
                Game.DisableControlThisFrame(0, Control.VehicleLookLeft);
                Game.DisableControlThisFrame(0, Control.VehicleLookRight);
                Game.DisableControlThisFrame(0, Control.NextCamera);
                Game.DisableControlThisFrame(0, Control.VehicleFlyAttackCamera);
                Game.DisableControlThisFrame(0, Control.VehicleCinCam);
            }
        }

        #endregion

        #region Draw model dimensions math

        /// <summary>
        /// Draws the bounding box for the entity with the provided rgba color.
        /// </summary>
        /// <param name="ent"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        public static void DrawEntityBoundingBox(Entity ent, int r, int g, int b, int a)
        {
            var box = GetEntityBoundingBox(ent.Handle);
            DrawBoundingBox(box, r, g, b, a);
        }

        /// <summary>
        /// Gets the bounding box of the entity model in world coordinates, used by <see cref="DrawEntityBoundingBox(Entity, int, int, int, int)"/>.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        internal static Vector3[] GetEntityBoundingBox(int entity)
        {
            Vector3 min = Vector3.Zero;
            Vector3 max = Vector3.Zero;

            API.GetModelDimensions((uint)API.GetEntityModel(entity), ref min, ref max);
            //const float pad = 0f;
            const float pad = 0.001f;
            var retval = new Vector3[8]
            {
                // Bottom
                API.GetOffsetFromEntityInWorldCoords(entity, min.X - pad, min.Y - pad, min.Z - pad),
                API.GetOffsetFromEntityInWorldCoords(entity, max.X + pad, min.Y - pad, min.Z - pad),
                API.GetOffsetFromEntityInWorldCoords(entity, max.X + pad, max.Y + pad, min.Z - pad),
                API.GetOffsetFromEntityInWorldCoords(entity, min.X - pad, max.Y + pad, min.Z - pad),

                // Top
                API.GetOffsetFromEntityInWorldCoords(entity, min.X - pad, min.Y - pad, max.Z + pad),
                API.GetOffsetFromEntityInWorldCoords(entity, max.X + pad, min.Y - pad, max.Z + pad),
                API.GetOffsetFromEntityInWorldCoords(entity, max.X + pad, max.Y + pad, max.Z + pad),
                API.GetOffsetFromEntityInWorldCoords(entity, min.X - pad, max.Y + pad, max.Z + pad)
            };
            return retval;
        }

        /// <summary>
        /// Draws the edge poly faces and the edge lines for the specific box coordinates using the specified rgba color.
        /// </summary>
        /// <param name="box"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        private static void DrawBoundingBox(Vector3[] box, int r, int g, int b, int a)
        {
            var polyMatrix = GetBoundingBoxPolyMatrix(box);
            var edgeMatrix = GetBoundingBoxEdgeMatrix(box);

            DrawPolyMatrix(polyMatrix, r, g, b, a);
            DrawEdgeMatrix(edgeMatrix, 255, 255, 255, a);
        }

        /// <summary>
        /// Gets the coordinates for all poly box faces.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        private static Vector3[][] GetBoundingBoxPolyMatrix(Vector3[] box)
        {
            return
            [
                [box[2], box[1], box[0]],
                [box[3], box[2], box[0]],

                [box[4], box[5], box[6]],
                [box[4], box[6], box[7]],

                [box[2], box[3], box[6]],
                [box[7], box[6], box[3]],

                [box[0], box[1], box[4]],
                [box[5], box[4], box[1]],

                [box[1], box[2], box[5]],
                [box[2], box[6], box[5]],

                [box[4], box[7], box[3]],
                [box[4], box[3], box[0]]
            ];
        }

        /// <summary>
        /// Gets the coordinates for all edge coordinates.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        public static Vector3[][] GetBoundingBoxEdgeMatrix(Vector3[] box)
        {
            return
            [
                [box[0], box[1]],
                [box[1], box[2]],
                [box[2], box[3]],
                [box[3], box[0]],

                [box[4], box[5]],
                [box[5], box[6]],
                [box[6], box[7]],
                [box[7], box[4]],

                [box[0], box[4]],
                [box[1], box[5]],
                [box[2], box[6]],
                [box[3], box[7]]
            ];
        }

        /// <summary>
        /// Draws the poly matrix faces.
        /// </summary>
        /// <param name="polyCollection"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        private static void DrawPolyMatrix(Vector3[][] polyCollection, int r, int g, int b, int a)
        {
            foreach (var poly in polyCollection)
            {
                float x1 = poly[0].X;
                float y1 = poly[0].Y;
                float z1 = poly[0].Z;

                float x2 = poly[1].X;
                float y2 = poly[1].Y;
                float z2 = poly[1].Z;

                float x3 = poly[2].X;
                float y3 = poly[2].Y;
                float z3 = poly[2].Z;
                API.DrawPoly(x1, y1, z1, x2, y2, z2, x3, y3, z3, r, g, b, a);
            }
        }

        /// <summary>
        /// Draws the edge lines for the model dimensions.
        /// </summary>
        /// <param name="linesCollection"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        private static void DrawEdgeMatrix(Vector3[][] linesCollection, int r, int g, int b, int a)
        {
            foreach (var line in linesCollection)
            {
                float x1 = line[0].X;
                float y1 = line[0].Y;
                float z1 = line[0].Z;

                float x2 = line[1].X;
                float y2 = line[1].Y;
                float z2 = line[1].Z;

                API.DrawLine(x1, y1, z1, x2, y2, z2, r, g, b, a);
            }
        }

        #endregion

        #region Draw 3d text

        public static void DrawText3D(Vector3 pos, string text)
        {
            float _x = 0;
            float _y = 0;
            bool onScreen = API.World3dToScreen2d(pos.X, pos.Y, pos.Z, ref _x, ref _y);
            Vector3 camCoords = API.GetGameplayCamCoords();
            float dist = Vector3.Distance(camCoords, pos);

            float scale = (1 / dist) * 2;
            float fov = (1 / API.GetGameplayCamFov()) * 100;
            scale = scale * fov;

            if (onScreen)
            {
                API.SetTextScale(0.5f * scale, 0.5f * scale);
                API.SetTextFont(8);
                API.SetTextProportional(true);
                API.SetTextColour(255, 255, 255, 255);
                API.SetTextDropshadow(0, 0, 0, 0, 255);
                API.SetTextEdge(2, 0, 0, 0, 150);
                API.SetTextDropShadow();
                API.SetTextOutline();
                API.SetTextEntry("STRING");
                API.SetTextCentre(true);
                API.AddTextComponentString(text);
                API.DrawText(_x, _y);
            }
        }

        #endregion

        #region To title case

        public static string ToTitleCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }
            return string.Join(" ", words);
        }

        #endregion

        #region Math utils

        public static int Map(int value, int fromLow, int fromHigh, int toLow, int toHigh)
        {
            return (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
        }

        public static float Map(float x, float in_min, float in_max, float out_min, float out_max, bool clamp = false)
        {
            float r = (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
            if (clamp) r = Clamp(r, out_min, out_max);
            return r;
        }

        public static float Clamp(float val, float min, float max)
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        /// <summary>
        /// Lerps two float values by a step
        /// </summary>
        /// <returns>lerped float value in between two supplied</returns>
        public static float Lerp(float current, float target, float by)
        {
            return current * (1 - by) + target * by;
        }

        /// <summary>
        /// Calculates angle between two vectors
        /// </summary>
        /// <returns>Angle between vectors in degrees</returns>
        public static float AngleBetween(Vector3 a, Vector3 b)
        {
            double sinA = a.X * b.Y - b.X * a.Y;
            double cosA = a.X * b.X + a.Y * b.Y;
            return (float)Math.Atan2(sinA, cosA) / DegToRad;
        }

        public static Vector3 RotateRadians(Vector3 v, float degree)
        {
            float ca = float.Parse(Math.Cos(degree).ToString());
            float sa = float.Parse(Math.Sin(degree).ToString());
            return new Vector3(ca * v.X - sa * v.Y, sa * v.X + ca * v.Y, v.Z);
        }

        public static Vector3 RotateAroundAxis(Vector3 v, Vector3 axis, float angle)
        {
            return Vector3.TransformCoordinate(v, Matrix.RotationAxis(Vector3.Normalize(axis), angle));
        }

        public static float Fmod(float a, float b)
        {
            return a - b * float.Parse(Math.Floor(a / b).ToString());
        }

        public static Vector3 QuaternionToEuler(Quaternion q)
        {
            double r11 = -2 * (q.X * q.Y - q.W * q.Z);
            double r12 = q.W * q.W - q.X * q.X + q.Y * q.Y - q.Z * q.Z;
            double r21 = 2 * (q.Y * q.Z + q.W * q.X);
            double r31 = -2 * (q.X * q.Z - q.W * q.Y);
            double r32 = q.W * q.W - q.X * q.X - q.Y * q.Y + q.Z * q.Z;

            float ax = (float)Math.Asin(r21);
            float ay = (float)Math.Atan2(r31, r32);
            float az = (float)Math.Atan2(r11, r12);

            return new Vector3(ax / DegToRad, ay / DegToRad, az / DegToRad);
        }

        #endregion
    }
}
