using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Baracuda.Bedrock.Cursor;
using Baracuda.Bedrock.Locks;
using Baracuda.Bedrock.Odin;
using Baracuda.Bedrock.Services;
using Baracuda.UI;
using Baracuda.Utilities.Collections;
using Baracuda.Utilities.Events;
using Baracuda.Utilities.Types;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Baracuda.Bedrock.Input
{
    /// <summary>
    ///     Input Manager handles input state (controller or desktop) and input map states.
    /// </summary>
    public partial class InputManager
    {
        #region Fields

        [SerializeField] [Required] private PlayerInput playerInput;
        [SerializeField] [Required] private InputActionAsset inputActionAsset;
        [SerializeField] [Required] private InputActionReference navigateInputAction;
        [SerializeField] [Required] private InputActionReference escapeInputAction;
        [SerializeField] [Required] private InputActionReference[] mouseInputActions;

        [Header("Schemes")]
        [SerializeField] private string[] controllerSchemes;

        private readonly Lock _escapeInputBlocker = new();
        private readonly StackList<Func<EscapeUsage>> _escapeConsumerStack = new();
        private readonly List<Action> _escapeListener = new();

        [ReadonlyInspector]
        private List<object> EscapeConsumer => _escapeConsumerStack.Select(item => item.Target).ToList();

        private readonly Broadcast _onBecameControllerScheme = new();
        private readonly Broadcast _onBecameDesktopScheme = new();
        private readonly Broadcast _onNavigationInputReceived = new();
        private readonly Broadcast _onMouseInputReceived = new();

        [ReadonlyInspector]
        private readonly Dictionary<InputActionMapReference, HashSet<object>> _inputActionMapProvider = new();

        [ReadonlyInspector]
        private readonly Dictionary<InputActionMapReference, HashSet<object>> _inputActionMapBlocker = new();

        private readonly HashSet<InputActionMap> _mapsToEnable = new();
        private readonly HashSet<InputActionMap> _mapsToDisable = new();

        private const string NavigationEventCursorBlocker = nameof(NavigationEventCursorBlocker);
        private const string GamepadSchemeCursorBlocker = nameof(GamepadSchemeCursorBlocker);

        #endregion


        #region Input Map Provider

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BlockActionMapInternal(InputActionMapReference actionMapReference, object provider)
        {
            if (!_inputActionMapBlocker.TryGetValue(actionMapReference, out var hashSet))
            {
                hashSet = new HashSet<object>();
                _inputActionMapBlocker.Add(actionMapReference, hashSet);
            }

            hashSet.Add(provider);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UnblockActionMapInternal(InputActionMapReference actionMapReference, object provider)
        {
            if (!_inputActionMapBlocker.TryGetValue(actionMapReference, out var hashSet))
            {
                hashSet = new HashSet<object>();
                _inputActionMapBlocker.Add(actionMapReference, hashSet);
            }

            hashSet.Remove(provider);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProvideActionMapInternal(InputActionMapReference actionMapReference, object provider)
        {
            if (!_inputActionMapProvider.TryGetValue(actionMapReference, out var hashSet))
            {
                hashSet = new HashSet<object>();
                _inputActionMapProvider.Add(actionMapReference, hashSet);
            }

            hashSet.Add(provider);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WithdrawActionMapInternal(InputActionMapReference actionMapReference, object provider)
        {
            if (!_inputActionMapProvider.TryGetValue(actionMapReference, out var hashSet))
            {
                hashSet = new HashSet<object>();
                _inputActionMapProvider.Add(actionMapReference, hashSet);
            }

            hashSet.Remove(provider);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateInputActionMaps()
        {
            inputActionAsset.Enable();

            _mapsToEnable.Clear();
            _mapsToDisable.Clear();

            foreach (var (inputActionMapName, actionMapProviderSet) in _inputActionMapProvider.Reverse())
            {
                var actionMap = inputActionAsset.FindActionMap(inputActionMapName, true);
                var shouldMapBeEnabled = actionMapProviderSet.Any();

                if (shouldMapBeEnabled)
                {
                    _mapsToEnable.Add(actionMap);
                }
                else
                {
                    _mapsToDisable.Add(actionMap);
                }
            }

            foreach (var (inputActionMapName, actionMapBlockerSet) in _inputActionMapBlocker.Reverse())
            {
                var actionMap = inputActionAsset.FindActionMap(inputActionMapName, true);
                var shouldMapBeDisabled = actionMapBlockerSet.Any();

                if (shouldMapBeDisabled)
                {
                    _mapsToDisable.Add(actionMap);
                }
                else
                {
                    _mapsToEnable.Remove(actionMap);
                }
            }

            foreach (var inputActionMap in _mapsToEnable)
            {
                inputActionMap.Enable();
            }

            foreach (var inputActionMap in _mapsToDisable)
            {
                inputActionMap.Disable();
            }

            _mapsToEnable.Clear();
            _mapsToDisable.Clear();
        }

        #endregion


        #region EscapeUsage Listener

        public void AddEscapeConsumer(Func<EscapeUsage> listener)
        {
            _escapeConsumerStack.PushUnique(listener);
        }

        public void RemoveEscapeConsumer(Func<EscapeUsage> listener)
        {
            _escapeConsumerStack.Remove(listener);
        }

        public void AddDiscreteEscapeListener(Action listener)
        {
            _escapeListener.Add(listener);
        }

        public void RemoveDiscreteEscapeListener(Action listener)
        {
            _escapeListener.Remove(listener);
        }

        public void BlockEscapeInput(object blocker)
        {
            _escapeInputBlocker.Add(blocker);
        }

        public async void UnlockEscapeInput(object blocker)
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            _escapeInputBlocker.Remove(blocker);
        }

        private void OnEscapePressed(InputAction.CallbackContext context)
        {
            if (_escapeInputBlocker.HasAny())
            {
                return;
            }

            foreach (var action in _escapeListener)
            {
                action();
            }

            foreach (var consumer in _escapeConsumerStack.Reverse())
            {
                if (consumer() is EscapeUsage.ConsumedEscape)
                {
                    break;
                }
            }
        }

        #endregion


        #region Setup & Shutdown

        private void Awake()
        {
            playerInput.onControlsChanged += OnControlsChanged;
            navigateInputAction.action.performed += OnNavigationInput;
            inputActionAsset.Enable();
            foreach (var inputActionMap in inputActionAsset.actionMaps)
            {
                inputActionMap.Disable();
                _inputActionMapProvider.Add(new InputActionMapReference(inputActionMap), new HashSet<object>());
            }
            foreach (var inputActionReference in mouseInputActions)
            {
                inputActionReference.action.performed += OnMouseInput;
            }
            escapeInputAction.action.performed += OnEscapePressed;
        }

        private void Start()
        {
            playerInput.uiInputModule = ServiceLocator.Get<InputSystemUIInputModule>();
        }

        private void OnDestroy()
        {
            playerInput.onControlsChanged -= OnControlsChanged;
            navigateInputAction.action.performed -= OnNavigationInput;
            foreach (var inputActionReference in mouseInputActions)
            {
                inputActionReference.action.performed -= OnMouseInput;
            }
            escapeInputAction.action.performed -= OnEscapePressed;
            IsGamepadScheme = false;
            _onBecameControllerScheme.Clear();
            _onBecameDesktopScheme.Clear();
            _onNavigationInputReceived.Clear();
            _onMouseInputReceived.Clear();
        }

        private void LateUpdate()
        {
            UpdateInputActionMaps();
        }

        #endregion


        #region Callbacks

        private void OnControlsChanged(PlayerInput input)
        {
            var wasControllerScheme = IsGamepadScheme;
            IsGamepadScheme = controllerSchemes.Contains(input.currentControlScheme);

            if (wasControllerScheme == IsGamepadScheme)
            {
                return;
            }

            var cursorManager = ServiceLocator.Get<CursorManager>();
            if (IsGamepadScheme)
            {
                _onBecameControllerScheme.Raise();
                cursorManager.BlockCursorVisibility(GamepadSchemeCursorBlocker);
            }
            else
            {
                _onBecameDesktopScheme.Raise();
                cursorManager.UnblockCursorVisibility(GamepadSchemeCursorBlocker);
            }
        }

        private void OnNavigationInput(InputAction.CallbackContext context)
        {
            InteractionMode = InteractionMode.NavigationInput;
            if (EnableNavigationEvents)
            {
                _onNavigationInputReceived.Raise();
            }

            var cursorManager = ServiceLocator.Get<CursorManager>();

            if (IsDesktopScheme)
            {
                cursorManager.BlockCursorVisibility(NavigationEventCursorBlocker);
            }
            else
            {
                cursorManager.UnblockCursorVisibility(NavigationEventCursorBlocker);
            }
        }

        private void OnMouseInput(InputAction.CallbackContext context)
        {
            InteractionMode = InteractionMode.Mouse;
            if (EnableNavigationEvents)
            {
                _onMouseInputReceived.Raise();
            }

            var cursorManager = ServiceLocator.Get<CursorManager>();
            if (IsDesktopScheme)
            {
                cursorManager.UnblockCursorVisibility(NavigationEventCursorBlocker);
            }
        }

        #endregion
    }
}