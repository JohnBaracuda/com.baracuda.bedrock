using System;
using Baracuda.UI;
using Baracuda.Utilities.Types;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Baracuda.Bedrock.Input
{
    /// <summary>
    ///     Input Manager handles input state (controller or desktop) and input map states.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public partial class InputManager : MonoBehaviour
    {
        #region Properties

        public bool IsGamepadScheme { get; private set; }
        public bool IsDesktopScheme => !IsGamepadScheme;
        public InteractionMode InteractionMode { get; private set; }
        public bool EnableNavigationEvents { get; set; } = true;
        public PlayerInput PlayerInput => playerInput;

        #endregion


        #region Events

        public event Action BecameControllerScheme
        {
            add => _onBecameControllerScheme.Add(value);
            remove => _onBecameControllerScheme.Remove(value);
        }

        public event Action BecameDesktopScheme
        {
            add => _onBecameDesktopScheme.Add(value);
            remove => _onBecameDesktopScheme.Remove(value);
        }

        public event Action NavigationInputReceived
        {
            add => _onNavigationInputReceived.Add(value);
            remove => _onNavigationInputReceived.Remove(value);
        }

        public event Action MouseInputReceived
        {
            add => _onMouseInputReceived.Add(value);
            remove => _onMouseInputReceived.Remove(value);
        }

        #endregion


        #region Methods: Action Maps

        public void BlockActionMap(InputActionMapReference actionMap, object source)
        {
            BlockActionMapInternal(actionMap, source);
        }

        public void UnblockActionMap(InputActionMapReference actionMap, object source)
        {
            UnblockActionMapInternal(actionMap, source);
        }

        public void AddActionMapSource(InputActionMapReference actionMapReference, object source)
        {
            ProvideActionMapInternal(actionMapReference, source);
        }

        public void RemoveActionMapSource(InputActionMapReference actionMapReference, object source)
        {
            WithdrawActionMapInternal(actionMapReference, source);
        }

        #endregion
    }
}