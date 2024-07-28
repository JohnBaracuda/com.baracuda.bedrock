using System;
using Baracuda.Bedrock.Odin;
using Baracuda.Serialization;
using Baracuda.Utilities.Events;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Localization;

namespace Baracuda.Bedrock.Statistics
{
    public abstract class StatAsset : ScriptableObject
    {
        #region Fields

        [Foldout("Settings")]
        [Title("Localization")]
        [SerializeField] private LocalizedString displayName;
        [SerializeField] private LocalizedString description;

        [Title("Persistent Data")]
        [Tooltip("Determines the level on which the stat is saved. Profile specific or shared.")]
        [LabelText("Save To")]
        [SerializeField] private StorageLevel stage = StorageLevel.Profile;
        [Tooltip("When enabled, the objects inspector is repainted when the stat is updated.")]
        [SerializeField] private bool repaint;

        #endregion


        #region Public

        [PublicAPI]
        public string Name => displayName.IsEmpty ? name : displayName.GetLocalizedString();

        [PublicAPI]
        public string Description => description.IsEmpty ? "Missing Description" : description.GetLocalizedString();

        [PublicAPI]
        public abstract string ValueString { get; }

        [PublicAPI]
        public abstract Modification Type();

        [PublicAPI]
        public static Broadcast<StatAsset> Updated { get; } = new();

        #endregion


        #region Protected

        protected bool Repaint => repaint;

        protected ISaveProfile Profile =>
            stage switch
            {
                StorageLevel.Profile => FileSystem.Profile,
                StorageLevel.SharedProfile => FileSystem.SharedProfile,
                var _ => throw new ArgumentOutOfRangeException()
            };

        #endregion
    }
}