using System.Collections.Generic;
using Baracuda.Utilities.Types;
using FMOD.Studio;
using FMODUnity;
using JetBrains.Annotations;
using UnityEngine;
using StopMode = FMOD.Studio.STOP_MODE;
using Attribute3D = FMOD.ATTRIBUTES_3D;

namespace Baracuda.Bedrock.Audio
{
    public class AudioAsset : ScriptableObject
    {
        #region Inspector

        [SerializeField] private EventReference eventReference;
        [SerializeField] private Optional<float> volume = new(1f, false);

        #endregion


        #region Fields & Properties

        private readonly Dictionary<string, PARAMETER_ID> _parameterIds = new();

        [PublicAPI]
        public EventReference EventReference => eventReference;

        #endregion


        #region Play

        public void Play()
        {
            RuntimeManager.PlayOneShot(eventReference);
        }

        public void Play(Transform target)
        {
            var eventInstance = RuntimeManager.CreateInstance(eventReference);
            eventInstance.set3DAttributes(target.To3DAttributes());
            if (volume.TryGetValue(out var value))
            {
                eventInstance.setVolume(value);
            }
            eventInstance.start();
            eventInstance.release();
        }

        public void Play(Vector3 position)
        {
            RuntimeManager.PlayOneShot(eventReference, position);
        }

        public void Play(Vector3 position, in FmodParameter parameter)
        {
            var instance = CreateInstance();
            instance.set3DAttributes(position.To3DAttributes());
            instance.start();
            instance.setParameterByID(parameter.Id, parameter.Value);
            instance.release();
        }

        public void Play(in FmodParameter parameter)
        {
            var instance = CreateInstance();
            instance.start();
            instance.setParameterByID(parameter.Id, parameter.Value);
            instance.release();
        }

        #endregion


        #region Parameter

        public FmodParameter CreateParameter(string parameterName, float defaultValue = 1)
        {
            return new FmodParameter(GetParameterID(parameterName), defaultValue);
        }

        public PARAMETER_ID GetParameterID(string parameterName)
        {
            if (_parameterIds.TryGetValue(parameterName, out var id) is false)
            {
                var description = RuntimeManager.GetEventDescription(eventReference);
                description.getParameterDescriptionByName(parameterName, out var parameterDescription);
                id = parameterDescription.id;
                _parameterIds.Add(parameterName, id);
            }
            return id;
        }

        #endregion


        #region Instance

        public EventInstance CreateInstance()
        {
            return RuntimeManager.CreateInstance(eventReference);
        }

        public void StartInstance(out EventInstance audioInstance)
        {
            audioInstance = RuntimeManager.CreateInstance(eventReference);
            audioInstance.start();
            if (volume.Enabled)
            {
                audioInstance.setVolume(volume.Value);
            }
        }

        public void StartInstance(out EventInstance audioInstance, GameObject target)
        {
            audioInstance = RuntimeManager.CreateInstance(eventReference);
            audioInstance.start();
            RuntimeManager.AttachInstanceToGameObject(audioInstance, target.transform);
            if (volume.Enabled)
            {
                audioInstance.setVolume(volume.Value);
            }
        }

        public void StartInstance(out EventInstance audioInstance, Attribute3D attributes3D)
        {
            audioInstance = RuntimeManager.CreateInstance(eventReference);
            audioInstance.set3DAttributes(attributes3D);
            audioInstance.start();
            if (volume.Enabled)
            {
                audioInstance.setVolume(volume.Value);
            }
        }

        public void StopInstance(ref EventInstance instance, StopMode stopMode = StopMode.ALLOWFADEOUT)
        {
            instance.stop(stopMode);
            instance.release();
        }

        #endregion
    }
}