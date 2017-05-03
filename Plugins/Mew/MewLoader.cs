﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;
using ff14bot.AClasses;
using ff14bot.Helpers;
using ICSharpCode.SharpZipLib.Zip;
using ProtoBuf;

namespace MewLoader
{
    public class PluginLoader : BotPlugin
    {
        // Change these settings to reflect your project!
        private const int ProjectId = 28;

        private const string ProjectName = "Mew";
        private const string ProjectMainType = "Mew.Mew";
        private const string ProjectAssemblyName = "Mew.dll";
        private static readonly Color logColor = Colors.DeepPink;
        public override string Description => "Gathering Manager UI";
        public override string Author => "Entrax";
        public override string ButtonText => "Settings";
        public override Version Version => new Version(0, 2, 2);
        public override bool WantButton => true;

        #region Meta Data
        private static readonly object locker = new object();
        private static readonly string projectAssembly = Path.Combine(Environment.CurrentDirectory, $@"Plugins\{ProjectName}\{ProjectAssemblyName}");
        private static readonly string mahappsAssembly = Path.Combine(Environment.CurrentDirectory, $@"Plugins\{ProjectName}\MahApps.Metro.dll");
        private static readonly string siuneAssembly = Path.Combine(Environment.CurrentDirectory, $@"Plugins\{ProjectName}\Siune.Client.dll");
        private static readonly string versionPath = Path.Combine(Environment.CurrentDirectory, $@"Plugins\{ProjectName}\version.txt");
        private static readonly string baseDir = Path.Combine(Environment.CurrentDirectory, $@"Plugins\{ProjectName}");
        private static readonly string projectTypeFolder = Path.Combine(Environment.CurrentDirectory, @"Plugins");
        private static Action onInitialize, onShutdown, onEnabled, onDisabled, onButtonPress, onPulse;
        private static bool updated;
        #endregion

        #region Constructor

        public PluginLoader()
        {
            lock (locker)
            {
                if (updated) { return; }
                updated = true;
            }
            Task.Factory.StartNew(Update);
        }
        #endregion

        #region Overrides
        public override string Name => ProjectName;
        public override void OnInitialize() => onInitialize?.Invoke();
        public override void OnShutdown() => onShutdown?.Invoke();
        public override void OnEnabled() => onEnabled?.Invoke();
        public override void OnDisabled() => onDisabled?.Invoke();
        public override void OnButtonPress() => onButtonPress?.Invoke();
        public override void OnPulse() => onPulse?.Invoke();
        #endregion

        #region Injection Methods
        private static void Load()
        {
            RedirectAssembly();

            var assembly = LoadAssembly(projectAssembly);
            if (assembly == null) { return; }

            Type baseType;
            try { baseType = assembly.GetType(ProjectMainType); }
            catch (Exception e)
            {
                Log(e.ToString());
                return;
            }

            object Plugin;
            try { Plugin = Activator.CreateInstance(baseType); }
            catch (Exception e)
            {
                Log(e.ToString());
                return;
            }


            if (Plugin == null)
            {
                Log("[Error] Could not load main type.");
                return;
            }

			var type = Plugin.GetType();
			onInitialize = (Action)type.GetProperty("OnInitializeAction").GetValue(Plugin);
			onShutdown = (Action)type.GetProperty("OnShutdownAction").GetValue(Plugin);
			onEnabled = (Action)type.GetProperty("OnEnabledAction").GetValue(Plugin);
			onDisabled = (Action)type.GetProperty("OnDisabledAction").GetValue(Plugin);
			onButtonPress = (Action)type.GetProperty("OnButtonPressAction").GetValue(Plugin);
			onPulse = (Action)type.GetProperty("OnPulseAction").GetValue(Plugin);

            Log($"{ProjectName} loaded.");
        }

        private static Assembly LoadAssembly(string path)
        {
            if (!File.Exists(path)) { return null; }

            Assembly assembly = null;
            try { assembly = Assembly.LoadFrom(path); }
            catch (Exception e) { Logging.WriteException(e); }

            return assembly;
        }
        #endregion

        #region Automatic Update Methods
        private void Update()
        {
            var stopwatch = Stopwatch.StartNew();
            var local = GetLocalVersion();
            var message = new VersionMessage { LocalVersion = local, ProductId = ProjectId };
            var responseMessage = GetLatestVersion(message).Result;
            var latest = responseMessage?.LatestVersion;

            if (local == latest || latest == null)
            {
                Load();
                return;
            }

            Log($"Updating to {latest}.");
            var bytes = responseMessage.Data;

            if (bytes == null || bytes.Length == 0)
            {
                Log("[Error] Bad product data returned.");
                return;
            }

            if (!Clean(baseDir))
            {
                Log("[Error] Could not clean directory for update.");
                return;
            }

            if (!Extract(bytes, projectTypeFolder))
            {
                Log("[Error] Could not extract new files.");
                return;
            }

            if (File.Exists(versionPath)) { File.Delete(versionPath); }
            try { File.WriteAllText(versionPath, latest); }
            catch (Exception e) { Log(e.ToString()); }

            stopwatch.Stop();
            Log($"Update complete in {stopwatch.ElapsedMilliseconds} ms.");
            Load();
        }

        private static string GetLocalVersion()
        {
            if (!File.Exists(versionPath)) { return null; }
            try
            {
                var version = File.ReadAllText(versionPath);
                return version;
            }
            catch { return null; }
        }

        private static bool Clean(string directory)
        {
            foreach (var file in new DirectoryInfo(directory).GetFiles())
            {
                try { file.Delete(); }
                catch { return false; }
            }

            foreach (var dir in new DirectoryInfo(directory).GetDirectories())
            {
                try { dir.Delete(true); }
                catch { return false; }
            }

            return true;
        }

        private static bool Extract(byte[] files, string directory)
        {
            using (var stream = new MemoryStream(files))
            {
                var zip = new FastZip();

                try { zip.ExtractZip(stream, directory, FastZip.Overwrite.Always, null, null, null, false, true); }
                catch (Exception e)
                {
                    Log(e.ToString());
                    return false;
                }
            }

            return true;
        }

        private static async Task<VersionMessage> GetLatestVersion(VersionMessage message)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-protobuf"));
                client.BaseAddress = new Uri("http://siune.net");

                var bytes = ToBytes(message);
                var content = new ByteArrayContent(bytes);

                HttpResponseMessage response;
                try { response = await client.PostAsync("/api/products/version", content); }
                catch (Exception e)
                {
                    Log(e.Message);
                    return null;
                }

                byte[] responseMessageBytes;
                try { responseMessageBytes = await response.Content.ReadAsByteArrayAsync(); }
                catch (Exception e)
                {
                    Log(e.Message);
                    return null;
                }

                var responseMessage = FromBytes<VersionMessage>(responseMessageBytes);
                return responseMessage;
            }
        }

        private static T FromBytes<T>(byte[] data)
        {
            var obj = default(T);
            using (var stream = new MemoryStream(data))
            {
                try { obj = Serializer.Deserialize<T>(stream); }
                catch (Exception e) { Console.WriteLine(e); }
            }

            return obj;
        }

        private static byte[] ToBytes<T>(T obj)
        {
            byte[] data;
            using (var stream = new MemoryStream())
            {
                try { Serializer.Serialize(stream, obj); }
                catch (Exception e) { Console.WriteLine(e); }

                data = stream.ToArray();
            }

            return data;
        }
        #endregion

        #region Helpers
        private static void Log(string message)
        {
            Logging.Write(logColor, $"[Auto-Updater][{ProjectName}] {message}");
        }

        public static void RedirectAssembly()
        {
            ResolveEventHandler handler = (sender, args) =>
            {
                var name = Assembly.GetEntryAssembly().GetName().Name;
                var requestedAssembly = new AssemblyName(args.Name);
                return requestedAssembly.Name != name ? null : Assembly.GetEntryAssembly();
            };

            AppDomain.CurrentDomain.AssemblyResolve += handler;

            ResolveEventHandler mahappsHandler = (sender, args) =>
            {
                var requestedAssembly = new AssemblyName(args.Name);
                return requestedAssembly.Name != "MahApps.Metro" ? null : Assembly.LoadFrom(mahappsAssembly);
            };

            AppDomain.CurrentDomain.AssemblyResolve += mahappsHandler;

            ResolveEventHandler siuneHandler = (sender, args) =>
            {
                var requestedAssembly = new AssemblyName(args.Name);
                return requestedAssembly.Name != "Siune.Client" ? null : Assembly.LoadFrom(siuneAssembly);
            };

            AppDomain.CurrentDomain.AssemblyResolve += siuneHandler;
        }

        [ProtoContract]
        public class VersionMessage
        {
            [ProtoMember(1)]
            public int ProductId { get; set; }

            [ProtoMember(2)]
            public string LocalVersion { get; set; }

            [ProtoMember(3)]
            public string LatestVersion { get; set; }

            [ProtoMember(4, IsPacked = true)]
            public byte[] Data { get; set; } = new byte[0];
        }
        #endregion
    }
}