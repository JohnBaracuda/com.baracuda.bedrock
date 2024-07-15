using System;
using System.Runtime.CompilerServices;
using Baracuda.Utilities.Events;

namespace Baracuda.Bedrock.Events
{
    public static class EventExtensions
    {
        #region Null Check Event Inject

        /// <summary>
        ///     Inject the event if it is not null.
        /// </summary>
        /// <returns>true if the event was not null</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryRaise(this EventAsset eventAsset)
        {
            if (eventAsset == null)
            {
                return false;
            }

            eventAsset.Raise();
            return true;
        }

        /// <summary>
        ///     Inject the event if it is not null.
        /// </summary>
        /// <returns>true if the event was not null</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryRaise<T>(this EventAsset<T> eventAsset, T value)
        {
            if (eventAsset == null)
            {
                return false;
            }

            eventAsset.Raise(value);
            return true;
        }

        /// <summary>
        ///     Inject the event if it is not null.
        /// </summary>
        /// <returns>true if the event was not null</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryRaise<T1, T2>(this EventAsset<T1, T2> eventAsset, T1 value1, T2 value2)
        {
            if (eventAsset == null)
            {
                return false;
            }

            eventAsset.Raise(value1, value2);
            return true;
        }

        /// <summary>
        ///     Inject the event if it is not null.
        /// </summary>
        /// <returns>true if the event was not null</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryRaise<T1, T2, T3>(this EventAsset<T1, T2, T3> eventAsset, T1 value1, T2 value2, T3 value3)
        {
            if (eventAsset == null)
            {
                return false;
            }

            eventAsset.Raise(value1, value2, value3);
            return true;
        }

        /// <summary>
        ///     Inject the event if it is not null.
        /// </summary>
        /// <returns>true if the event was not null</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryRaise<T1, T2, T3, T4>(this EventAsset<T1, T2, T3, T4> eventAsset, T1 value1, T2 value2,
            T3 value3, T4 value4)
        {
            if (eventAsset == null)
            {
                return false;
            }

            eventAsset.Raise(value1, value2, value3, value4);
            return true;
        }

        #endregion


        #region Lambda Subscriptions

        /// <summary>
        ///     Subscribe an anonymous lambda to an event.
        ///     Returns a handle that can be used to remove the lambda from the event.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LambdaHandle AddLambda(this Broadcast receiver, Action lambda)
        {
            receiver.Add(lambda);
            return new LambdaHandle(() => { receiver.Remove(lambda); });
        }

        /// <summary>
        ///     Subscribe an anonymous lambda to an event.
        ///     Returns a handle that can be used to remove the lambda from the event.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LambdaHandle AddLambda<T>(this Broadcast<T> receiver, Action<T> lambda)
        {
            receiver.Add(lambda);
            return new LambdaHandle(() => { receiver.Remove(lambda); });
        }

        /// <summary>
        ///     Subscribe an anonymous lambda to an event.
        ///     Returns a handle that can be used to remove the lambda from the event.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LambdaHandle AddLambda<T1, T2>(this Broadcast<T1, T2> receiver, Action<T1, T2> lambda)
        {
            receiver.Add(lambda);
            return new LambdaHandle(() => { receiver.Remove(lambda); });
        }

        /// <summary>
        ///     Subscribe an anonymous lambda to an event.
        ///     Returns a handle that can be used to remove the lambda from the event.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LambdaHandle AddLambda<T1, T2, T3>(this Broadcast<T1, T2, T3> receiver, Action<T1, T2, T3> lambda)
        {
            receiver.Add(lambda);
            return new LambdaHandle(() => { receiver.Remove(lambda); });
        }

        /// <summary>
        ///     Subscribe an anonymous lambda to an event.
        ///     Returns a handle that can be used to remove the lambda from the event.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LambdaHandle AddLambda<T1, T2, T3, T4>(this Broadcast<T1, T2, T3, T4> receiver,
            Action<T1, T2, T3, T4> lambda)
        {
            receiver.Add(lambda);
            return new LambdaHandle(() => { receiver.Remove(lambda); });
        }

        #endregion
    }
}