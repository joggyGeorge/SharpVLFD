﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace VLFD
{
    /// <summary>
    /// Utility methods for detecting platform and architecture.
    /// </summary>
    internal static class PlatformApis
    {
        const string UnityEngineApplicationClassName = "UnityEngine.Application, UnityEngine";
        const string XamarinAndroidObjectClassName = "Java.Lang.Object, Mono.Android";
        const string XamarinIOSObjectClassName = "Foundation.NSObject, Xamarin.iOS";

        static readonly bool isLinux;
        static readonly bool isMacOSX;
        static readonly bool isWindows;
        static readonly bool isMono;
        static readonly bool isNetCore;
        static readonly bool isArm64;
        static readonly bool isArm;
        static readonly bool isUnity;
        static readonly bool isUnityIOS;
        static readonly bool isXamarin;
        static readonly bool isXamarinIOS;
        static readonly bool isXamarinAndroid;

        static PlatformApis()
        {
#if NETSTANDARD2_0
            isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            isMacOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            isNetCore = RuntimeInformation.FrameworkDescription.StartsWith(".NET Core");
            isArm64 = RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
            isArm = RuntimeInformation.ProcessArchitecture == Architecture.Arm;
#else
            var platform = Environment.OSVersion.Platform;

            // PlatformID.MacOSX is never returned, commonly used trick is to identify Mac is by using uname.
            isMacOSX = (platform == PlatformID.Unix && GetUname() == "Darwin");
            isLinux = (platform == PlatformID.Unix && !isMacOSX);
            isWindows = (platform == PlatformID.Win32NT || platform == PlatformID.Win32S || platform == PlatformID.Win32Windows);
            isNetCore = false;
#endif
            isMono = Type.GetType("Mono.Runtime") != null;

            // Unity
            var unityApplicationClass = Type.GetType(UnityEngineApplicationClassName);
            if (unityApplicationClass != null)
            {
                isUnity = true;
                // Consult value of Application.platform via reflection
                // https://docs.unity3d.com/ScriptReference/Application-platform.html
                var platformProperty = unityApplicationClass.GetTypeInfo().GetProperty("platform");
                var unityRuntimePlatform = platformProperty?.GetValue(null)?.ToString();
                isUnityIOS = (unityRuntimePlatform == "IPhonePlayer");
            }
            else
            {
                isUnity = false;
                isUnityIOS = false;
            }

            // Xamarin
            isXamarinIOS = Type.GetType(XamarinIOSObjectClassName) != null;
            isXamarinAndroid = Type.GetType(XamarinAndroidObjectClassName) != null;
            isXamarin = isXamarinIOS || isXamarinAndroid;
        }

        public static bool IsLinux
        {
            get { return isLinux; }
        }

        public static bool IsMacOSX
        {
            get { return isMacOSX; }
        }

        public static bool IsWindows
        {
            get { return isWindows; }
        }

        public static bool IsMono
        {
            get { return isMono; }
        }

        /// <summary>
        /// true if running on Unity platform.
        /// </summary>
        public static bool IsUnity
        {
            get { return isUnity; }
        }

        /// <summary>
        /// true if running on Unity iOS, false otherwise.
        /// </summary>
        public static bool IsUnityIOS
        {
            get { return isUnityIOS; }
        }

        /// <summary>
        /// true if running on a Xamarin platform (either Xamarin.Android or Xamarin.iOS),
        /// false otherwise.
        /// </summary>
        public static bool IsXamarin
        {
            get { return isXamarin; }
        }

        /// <summary>
        /// true if running on Xamarin.iOS, false otherwise.
        /// </summary>
        public static bool IsXamarinIOS
        {
            get { return isXamarinIOS; }
        }

        /// <summary>
        /// true if running on Xamarin.Android, false otherwise.
        /// </summary>
        public static bool IsXamarinAndroid
        {
            get { return isXamarinAndroid; }
        }

        /// <summary>
        /// true if running on .NET Core (CoreCLR), false otherwise.
        /// </summary>
        public static bool IsNetCore
        {
            get { return isNetCore; }
        }

        public static bool IsArm
        {
            get { return isArm; }
        }

        public static bool IsArm64
        {
            get { return isArm64; }
        }

        public static bool Is64Bit
        {
            get { return IntPtr.Size == 8; }
        }

        /// <summary>
        /// Returns <c>UnityEngine.Application.platform</c> as a string.
        /// See https://docs.unity3d.com/ScriptReference/Application-platform.html for possible values.
        /// Value is obtained via reflection to avoid compile-time dependency on Unity.
        /// This method should only be called if <c>IsUnity</c> is <c>true</c>.
        /// </summary>
        public static string GetUnityRuntimePlatform()
        {
            //GrpcPreconditions.CheckState(IsUnity, "Not running on Unity.");
#if NETSTANDARD2_0
            return Type.GetType(UnityEngineApplicationClassName).GetTypeInfo().GetProperty("platform").GetValue(null).ToString();
#else
            return Type.GetType(UnityEngineApplicationClassName).GetProperty("platform").GetValue(null).ToString();
#endif
        }

        [DllImport("libc")]
        static extern int uname(IntPtr buf);

        static string GetUname()
        {
            var buffer = Marshal.AllocHGlobal(8192);
            try
            {
                if (uname(buffer) == 0)
                {
                    return Marshal.PtrToStringAnsi(buffer);
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }
    }
}
