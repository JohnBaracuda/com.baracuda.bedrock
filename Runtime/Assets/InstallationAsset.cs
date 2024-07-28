using UnityEngine;

namespace Baracuda.Bedrock.Assets
{
    public abstract class InstallationAsset : ScriptableObject
    {
        public abstract void InstallDomainServices();

        public abstract void InstallRuntimeServices();
    }
}