using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Baracuda.Bedrock.Collections;
using Baracuda.Bedrock.Editor.Mediator;
using Baracuda.Bedrock.Editor.ScriptGeneration;
using Baracuda.Bedrock.Events;
using Baracuda.Bedrock.Generation;
using Baracuda.Bedrock.Locks;
using Baracuda.Bedrock.Pooling;
using Baracuda.Bedrock.Requests;
using Baracuda.Bedrock.Values;
using Baracuda.Utilities;
using Baracuda.Utilities.Pools;
using Baracuda.Utilities.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Baracuda.Bedrock.Editor.Generation
{
    public static class MediatorTypeGeneration
    {
        [UnityEditor.MenuItem("Tools/Mediator/Generate Types", priority = -1000)]
        public static async void GenerateMediatorClasses()
        {
            await GenerateMediatorClassesInternal();
        }

        private struct ProfilingResult
        {
            public Type[] Types { get; set; }
            public string FilePath { get; set; }
            public MediatorTypes MediatorTypes { get; set; }
            public string NameSpaceOverride { get; set; }
            public string Subfolder { get; set; }
        }

        private struct GenerationResult
        {
            public string FilePath { get; set; }
            public string FileContent { get; set; }
            public MediatorType MediatorType { get; set; }
        }

        private static async Task GenerateMediatorClassesInternal()
        {
            Debug.Log("Mediator", "Creating type profile");

            var suffixDictionary = MediatorEditorSettings.instance.MediatorTypeSuffix;
            var profiles = await Task.Run(() => GenerateMediatorClassProfileAsync(suffixDictionary));

            var reimportAssetPaths = new List<(string path, MediatorType type)>();
            var skippedAssetPaths = new List<(string path, MediatorType type)>();
            var operations = new List<Task>();
            foreach (var profile in profiles)
            {
                var filePath = profile.FilePath;
                var fileContent = profile.FileContent;
                var mediatorType = profile.MediatorType;
                var relativePath = AbsoluteToAssetPath(filePath);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(relativePath);
                if (asset != null && asset.text == fileContent)
                {
                    // Debug.Log("Mediator", $"Skipping script at: {relativePath}");
                    skippedAssetPaths.Add((relativePath, mediatorType));
                    continue;
                }

                reimportAssetPaths.Add((relativePath, mediatorType));
                var directoryPath = Path.GetDirectoryName(filePath)!;
                Directory.CreateDirectory(directoryPath);
                var writeOperation = File.WriteAllTextAsync(filePath, fileContent);
                operations.Add(writeOperation);
                Debug.Log("Mediator", $"Creating mediator script at: {relativePath}");
            }

            await Task.WhenAll(operations);

            UnityEditor.AssetDatabase.StartAssetEditing();
            foreach (var (path, type) in reimportAssetPaths)
            {
                UnityEditor.AssetDatabase.ImportAsset(path);
            }

            UnityEditor.AssetDatabase.StopAssetEditing();
            UnityEditor.AssetDatabase.Refresh();
            UnityEditor.AssetDatabase.StartAssetEditing();

            foreach (var (path, type) in reimportAssetPaths)
            {
                var settings = MediatorEditorSettings.instance;
                var assetIcon = settings.MediatorTypeIcons.TryGetValue(type, out var icon)
                    ? icon
                    : settings.FallbackIcon;

                SetIconForAsset(path, assetIcon);
            }

            foreach (var (path, type) in skippedAssetPaths)
            {
                var settings = MediatorEditorSettings.instance;
                var assetIcon = settings.MediatorTypeIcons.TryGetValue(type, out var icon)
                    ? icon
                    : settings.FallbackIcon;

                SetIconForAsset(path, assetIcon);
            }

            foreach (var (path, type) in reimportAssetPaths)
            {
                UnityEditor.AssetDatabase.ImportAsset(path);
            }
            foreach (var (path, type) in skippedAssetPaths)
            {
                UnityEditor.AssetDatabase.ImportAsset(path);
            }

            UnityEditor.AssetDatabase.StopAssetEditing();
        }

        private static void SetIconForAsset(string assetPath, Texture2D icon)
        {
            var metaFilePath = Path.Combine(Application.dataPath.RemoveFromEnd("Assets"), $"{assetPath}.meta");
            var iconPath = UnityEditor.AssetDatabase.GetAssetPath(icon);
            var iconGUID = UnityEditor.AssetDatabase.GUIDFromAssetPath(iconPath);
            var assetGUID = UnityEditor.AssetDatabase.GUIDFromAssetPath(assetPath);

            var attributes = File.GetAttributes(metaFilePath);

            var updatedAttributes = attributes & ~ FileAttributes.ReadOnly;
            File.SetAttributes(metaFilePath, updatedAttributes);

            var metaFileContent = "fileFormatVersion: 2\n" +
                                  $"guid: {assetGUID}\n" +
                                  "MonoImporter:\n" +
                                  "  externalObjects: {}\n" +
                                  "  serializedVersion: 2\n" +
                                  "  defaultReferences: []\n" +
                                  "  executionOrder: 0\n" +
                                  $"  icon: {{fileID: 2800000, guid: {iconGUID}, type: 3}}\n" +
                                  "  userData: \n" +
                                  "  assetBundleName: \n" +
                                  "  assetBundleVariant: ";

            File.WriteAllText(metaFilePath, metaFileContent);
            File.SetAttributes(metaFilePath, attributes);
        }

        private static Task<List<GenerationResult>> GenerateMediatorClassProfileAsync(
            IReadOnlyDictionary<MediatorType, string> suffixDictionary)
        {
            var assemblies = AssemblyProfiler.GetFilteredAssemblies();

            var profilingResults = new List<ProfilingResult>();

            foreach (var assembly in assemblies)
            {
                foreach (var attribute in assembly.GetCustomAttributes<GenerateMediatorForAttribute>())
                {
                    if (attribute.Types.Any(type => type.IsGenericTypeDefinition))
                    {
                        Debug.LogWarning("Mediator", "Cannot create mediator for generic type definition!");
                        continue;
                    }

                    var result = new ProfilingResult
                    {
                        Types = attribute.Types,
                        MediatorTypes = attribute.MediatorTypes,
                        NameSpaceOverride = attribute.NameSpace,
                        FilePath = attribute.Subfolder.IsNotNullOrWhitespace()
                            ? Path.Combine(
                                Path.GetDirectoryName(attribute.FilePath)!,
                                attribute.Subfolder,
                                Path.GetFileName(attribute.FilePath))
                            : attribute.FilePath
                    };

                    profilingResults.Add(result);
                }

                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type.IsGenericTypeDefinition)
                    {
                        continue;
                    }
                    foreach (var attribute in type.GetCustomAttributes<GenerateMediatorAttribute>())
                    {
                        var result = new ProfilingResult
                        {
                            Types = new[] { type },
                            MediatorTypes = attribute.MediatorTypes,
                            NameSpaceOverride = attribute.NameSpace,
                            Subfolder = attribute.Subfolder,
                            FilePath = attribute.Subfolder.IsNotNullOrWhitespace()
                                ? Path.Combine(
                                    Path.GetDirectoryName(attribute.FilePath)!,
                                    attribute.Subfolder,
                                    Path.GetFileName(attribute.FilePath))
                                : attribute.FilePath
                        };
                        profilingResults.Add(result);
                    }
                }
            }

            var results = new List<GenerationResult>();

            foreach (var profilingResult in profilingResults)
            {
                var mediatorFlags = profilingResult.MediatorTypes;
                var nameSpaceOverride = profilingResult.NameSpaceOverride;
                var types = profilingResult.Types;
                var length = profilingResult.Types.Length;
                var attributePath = profilingResult.FilePath;

                foreach (var flag in mediatorFlags.GetFlagsValueArray())
                {
                    var suffix = suffixDictionary.TryGetValue(FlagsToType(flag), out var value)
                        ? value
                        : MediatorEditorSettings.instance.FallbackSuffix;

                    switch (flag)
                    {
                        case MediatorTypes.None:
                            break;

                        case MediatorTypes.LockAsset when length == 1 && !types.First().IsStruct():
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(LockAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.LockAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.LockAsset when length > 1 && !types.First().IsStruct():
                        {
                            var mediator = CreateMediator(
                                types.Take(1).ToArray(),
                                typeof(LockAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.LockAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.ValueAssetConstant when length == 1:
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(ValueAssetSerialized<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.ValueAssetSerialized
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.ValueAssetRuntime when length == 1:
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(ValueAssetRuntime<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.ValueAssetRuntime
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.ValueAssetSave when length == 1:
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(SaveDataAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.ValueAssetSave
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.ValueAssetProperty when length == 1:
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(ValueAssetProperty<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.ValueAssetProperty
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.ValueAsset when length == 1:
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(ValueAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.ValueAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.ValueAsset when length > 1:
                        {
                            var mediator = CreateMediator(
                                types.Take(1).ToArray(),
                                typeof(ValueAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.ValueAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.EventAsset when length == 1:
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(EventAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.EventAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.EventAsset when length == 2:
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(EventAsset<,>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.EventAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.EventAsset when length == 3:
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(EventAsset<,,>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.EventAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.EventAsset when length == 4:
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(EventAsset<,,,>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.EventAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.ListAsset when length == 1:
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(ListAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.ListAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.ListAsset when length > 1:
                        {
                            var mediator = CreateMediator(
                                types.Take(1).ToArray(),
                                typeof(ListAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.ListAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.ArrayAsset when length == 1:
                        {
                            var mediator = CreateMediator(
                                types, typeof(ArrayAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.ArrayAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.ArrayAsset when length > 1:
                        {
                            var mediator = CreateMediator(
                                types.Take(1).ToArray(),
                                typeof(ArrayAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.ArrayAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.HashSetAsset when length == 1:
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(HashSetAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.HashSetAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.HashSetAsset when length > 1:
                        {
                            var mediator = CreateMediator(
                                types.Take(1).ToArray(),
                                typeof(HashSetAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.HashSetAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.SetAsset when length == 1:
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(SetAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.SetAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.SetAsset when length > 1:
                        {
                            var mediator = CreateMediator(
                                types.Take(1).ToArray(),
                                typeof(SetAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.SetAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.QueueAsset when length == 1:
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(QueueAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.QueueAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.QueueAsset when length > 1:
                        {
                            var mediator = CreateMediator(
                                types.Take(1).ToArray(),
                                typeof(QueueAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.QueueAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.StackAsset when length == 1:
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(StackAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.StackAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.StackAsset when length > 1:
                        {
                            var mediator = CreateMediator(
                                types.Take(1).ToArray(),
                                typeof(StackAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.StackAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.PoolAsset
                            when length == 1 && types.First().IsSubclassOrAssignable(typeof(Object)):
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(PoolAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.PoolAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.PoolAsset
                            when length > 1 && types.First().IsSubclassOrAssignable(typeof(Object)):
                        {
                            var mediator = CreateMediator(
                                types.Take(1).ToArray(),
                                typeof(PoolAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.PoolAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.RequestAsset when length == 1:
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(RequestAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.RequestAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.RequestAsset when length > 1:
                        {
                            var mediator = CreateMediator(
                                types.Take(1).ToArray(),
                                typeof(RequestAsset<>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.RequestAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.DictionaryAsset when length == 2:
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(DictionaryAsset<,>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.DictionaryAsset
                            };
                            results.Add(result);
                        }
                            break;

                        case MediatorTypes.MapAsset when length == 2:
                        {
                            var mediator = CreateMediator(
                                types,
                                typeof(MapAsset<,>),
                                suffix,
                                attributePath,
                                nameSpaceOverride);
                            var result = new GenerationResult
                            {
                                FilePath = mediator.filePath,
                                FileContent = mediator.script,
                                MediatorType = MediatorType.MapAsset
                            };
                            results.Add(result);
                        }
                            break;
                    }
                }
            }

            return Task.FromResult(results);

            (string filePath, string script) CreateMediator(Type[] types, Type mediatorType, string suffix,
                string attributePath, string nameSpaceOverride = null)
            {
                var script = CreateMediatorScript(types, mediatorType, suffix, nameSpaceOverride);
                var filePath = CreateMediatorFilePath(attributePath, types, suffix);
                return (filePath, script);
            }
        }

        private static string CreateMediatorScript(Type[] types, Type mediatorType, string suffix,
            string nameSpaceOverride = null)
        {
            var typeNameBuilder = StringBuilderPool.Get();
            foreach (var type in types)
            {
                typeNameBuilder.Append(SanitizedName(type));
            }
            typeNameBuilder.Append(suffix);

            var scriptBuilder = ScriptBuilder.CreateClass()
                .WithBaseClass(mediatorType.MakeGenericType(types))
                .WithName(StringBuilderPool.BuildAndRelease(typeNameBuilder))
                .WithNameSpace(nameSpaceOverride ?? types.First().Namespace)
                .WithAccessibility(types.All(type => type.IsPublic || type.IsNestedPublic)
                    ? AccessibilityModifiers.Public
                    : AccessibilityModifiers.Internal)
                .WithDependency(types);

            return scriptBuilder.Build();
        }

        private static string CreateMediatorFilePath(string attributePath, Type[] types, string suffix)
        {
            var typeNameBuilder = StringBuilderPool.Get();
            foreach (var type in types)
            {
                typeNameBuilder.Append(SanitizedName(type));
            }
            typeNameBuilder.Append(suffix);
            typeNameBuilder.Append(".g.cs");

            var directory = Path.GetDirectoryName(attributePath);
            var fileName = StringBuilderPool.BuildAndRelease(typeNameBuilder);
            return Path.Combine(directory!, fileName);
        }


        #region Helper

        private static string SanitizedName(Type type)
        {
            if (type.IsGenericType)
            {
                var builder = StringBuilderPool.Get();
                var argBuilder = StringBuilderPool.Get();

                var arguments = type.GetGenericArguments();

                foreach (var typeArg in arguments)
                {
                    var arg = SanitizedName(typeArg);

                    if (argBuilder.Length > 0)
                    {
                        argBuilder.AppendFormat(", {0}", arg);
                    }
                    else
                    {
                        argBuilder.Append(arg);
                    }
                }

                if (argBuilder.Length > 0)
                {
                    builder.AppendFormat("{0}{1}", type.Name!.Split('`')[0], argBuilder);
                }

                var retType = builder.ToString();

                StringBuilderPool.Release(builder);
                StringBuilderPool.Release(argBuilder);
                return retType.Replace('+', '.');
            }

            if (type == typeof(bool))
            {
                return "Bool";
            }
            if (type == typeof(byte))
            {
                return "Byte";
            }
            if (type == typeof(short))
            {
                return "Short";
            }
            if (type == typeof(int))
            {
                return "Int";
            }
            if (type == typeof(long))
            {
                return "Long";
            }
            if (type == typeof(float))
            {
                return "Float";
            }
            if (type == typeof(double))
            {
                return "Double";
            }
            if (type == typeof(string))
            {
                return "String";
            }

            var returnValue = type.Name!.Replace('+', '.');
            if (type.IsInterface)
            {
                return returnValue.Remove(0, 1);
            }
            return returnValue;
        }

        private static string AbsoluteToAssetPath(string path)
        {
            // Convert to an absolute path if it's a relative path
            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path));
            }

            // Ensure the path uses the correct slash type for Unity
            path = path.Replace("\\", "/");

            // Get the absolute paths to the Unity project's Assets and Packages folders
            var assetsAbsolutePath = Application.dataPath.Replace("\\", "/");
            var projectRootPath = Directory.GetParent(assetsAbsolutePath)!.FullName.Replace("\\", "/");
            var packagesAbsolutePath = Path.Combine(projectRootPath, "Packages").Replace("\\", "/");

            // Check if the absolute path is within the Unity Assets or Packages directory
            if (path.StartsWith(assetsAbsolutePath))
            {
                // Path is in Assets folder
                return "Assets" + path.Substring(assetsAbsolutePath.Length);
            }
            if (path.StartsWith(packagesAbsolutePath))
            {
                // Path is in Packages folder
                return path.Substring(projectRootPath.Length + 1);
            }
            UnityEngine.Debug.LogError(
                $"The provided path [{path}] is not within the Unity project's Assets or Packages directories.");
            return null;
        }

        private static MediatorType FlagsToType(MediatorTypes flags)
        {
            switch (flags)
            {
                case MediatorTypes.ValueAsset:
                    return MediatorType.ValueAsset;

                case MediatorTypes.EventAsset:
                    return MediatorType.EventAsset;

                case MediatorTypes.PoolAsset:
                    return MediatorType.PoolAsset;

                case MediatorTypes.RequestAsset:
                    return MediatorType.RequestAsset;

                case MediatorTypes.ListAsset:
                    return MediatorType.ListAsset;

                case MediatorTypes.ArrayAsset:
                    return MediatorType.ArrayAsset;

                case MediatorTypes.HashSetAsset:
                    return MediatorType.HashSetAsset;

                case MediatorTypes.SetAsset:
                    return MediatorType.SetAsset;

                case MediatorTypes.StackAsset:
                    return MediatorType.StackAsset;

                case MediatorTypes.QueueAsset:
                    return MediatorType.QueueAsset;

                case MediatorTypes.DictionaryAsset:
                    return MediatorType.DictionaryAsset;

                case MediatorTypes.MapAsset:
                    return MediatorType.MapAsset;

                case MediatorTypes.ValueAssetConstant:
                    return MediatorType.ValueAssetSerialized;

                case MediatorTypes.ValueAssetRuntime:
                    return MediatorType.ValueAssetRuntime;

                case MediatorTypes.ValueAssetSave:
                    return MediatorType.ValueAssetSave;

                case MediatorTypes.ValueAssetProperty:
                    return MediatorType.ValueAssetProperty;

                case MediatorTypes.LockAsset:
                    return MediatorType.LockAsset;

                default:
                    return MediatorType.None;
            }
        }

        #endregion
    }
}