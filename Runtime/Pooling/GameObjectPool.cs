﻿using UnityEngine;

namespace Baracuda.Utility.Pooling
{
    public class GameObjectPool : PoolAsset<GameObject>
    {
        protected override void OnGetElementFromPool(GameObject element)
        {
            element.SetActive(true);
        }

        protected override void OnReleaseElementToPool(GameObject element)
        {
            element.SetActive(false);
        }
    }
}