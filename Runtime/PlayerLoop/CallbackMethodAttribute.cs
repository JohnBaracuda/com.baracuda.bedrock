using System;
using JetBrains.Annotations;

namespace Baracuda.Bedrock.PlayerLoop
{
    /// <summary>
    ///     Mark a method in a registered scriptable object or behaviour that is then called during a specific callback.
    /// </summary>
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    public class CallbackMethodAttribute : Attribute
    {
        public Segment Segment { get; }
        public string Custom { get; }

        /// <summary>
        ///     Mark a method in a registered scriptable object or behaviour that is then called during the <see cref="Segment" />
        /// </summary>
        /// <param name="segment">The <see cref="Segment" /> during which the method is called.</param>
        public CallbackMethodAttribute(Segment segment)
        {
            Custom = null;
            Segment = segment;
        }

        /// <summary>
        ///     Mark a method in a registered scriptable object or behaviour that is then called during the passed custom callback.
        /// </summary>
        /// <param name="callback">The callback during which the method is called.</param>
        public CallbackMethodAttribute(string callback)
        {
            Custom = callback;
            Segment = Segment.Custom;
        }
    }

    /// <summary>
    ///     Method is called when the games subsystems are initialized.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CallbackOnInitializationAttribute : CallbackMethodAttribute
    {
        /// <summary>
        ///     Method is called when the games subsystems are initialized.
        /// </summary>
        public CallbackOnInitializationAttribute() : base(Segment.InitializationCompleted)
        {
        }
    }

    /// <summary>
    ///     Method is called when the application is shutdown.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CallbackOnApplicationQuitAttribute : CallbackMethodAttribute
    {
        /// <summary>
        ///     Method is called when the application is shutdown.
        /// </summary>
        public CallbackOnApplicationQuitAttribute() : base(Segment.ApplicationQuit)
        {
        }
    }

    /// <summary>
    ///     Method is called when entering edit mode (editor only)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CallbackOnEnterEditModeAttribute : CallbackMethodAttribute
    {
        /// <summary>
        ///     Method is called when entering edit mode (editor only)
        /// </summary>
        public CallbackOnEnterEditModeAttribute() : base(Segment.EnteredEditMode)
        {
        }
    }

    /// <summary>
    ///     Method is called when exiting edit mode (editor only)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CallbackOnExitEditModeAttribute : CallbackMethodAttribute
    {
        /// <summary>
        ///     Method is called when exiting edit mode (editor only)
        /// </summary>
        public CallbackOnExitEditModeAttribute() : base(Segment.ExitingEditMode)
        {
        }
    }

    /// <summary>
    ///     Method is called when entering play mode (editor only)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CallbackOnEnterPlayModeAttribute : CallbackMethodAttribute
    {
        /// <summary>
        ///     Method is called when entering play mode (editor only)
        /// </summary>
        public CallbackOnEnterPlayModeAttribute() : base(Segment.EnteredPlayMode)
        {
        }
    }

    /// <summary>
    ///     Method is called when exiting play mode (editor only)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CallbackOnExitPlayModeAttribute : CallbackMethodAttribute
    {
        /// <summary>
        ///     Method is called when exiting play mode (editor only)
        /// </summary>
        public CallbackOnExitPlayModeAttribute() : base(Segment.ExitingPlayMode)
        {
        }
    }

    /// <summary>
    ///     Method is called after a build has completed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CallbackOnBuildPostprocessorAttribute : CallbackMethodAttribute
    {
        /// <summary>
        ///     Method is called after a build has completed.
        /// </summary>
        public CallbackOnBuildPostprocessorAttribute() : base(Segment.BuildPostprocessor)
        {
        }
    }
}