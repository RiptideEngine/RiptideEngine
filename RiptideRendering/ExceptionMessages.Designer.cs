﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace RiptideRendering {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class ExceptionMessages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal ExceptionMessages() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("RiptideRendering.ExceptionMessages", typeof(ExceptionMessages).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot create GpuResource with dimension of &apos;Unknown&apos;..
        /// </summary>
        internal static string CannotCreateUnknownDimensionResource {
            get {
                return ResourceManager.GetString("CannotCreateUnknownDimensionResource", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot create GpuResource with Depth or Array Size of 0..
        /// </summary>
        internal static string CannotCreateZeroDepthOrArrayResource {
            get {
                return ResourceManager.GetString("CannotCreateZeroDepthOrArrayResource", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot create GpuResource with Width of 0 byte..
        /// </summary>
        internal static string CannotCreateZeroWidthResource {
            get {
                return ResourceManager.GetString("CannotCreateZeroWidthResource", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot create rendering command because the command list has been closed..
        /// </summary>
        internal static string CommandListClosed {
            get {
                return ResourceManager.GetString("CommandListClosed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot set resource because pipeline state is unbounded or isn&apos;t a {0} pipeline state..
        /// </summary>
        internal static string InvalidPipelineStateBounded {
            get {
                return ResourceManager.GetString("InvalidPipelineStateBounded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provided {0} object is not a valid {1} object..
        /// </summary>
        internal static string InvalidPlatformObjectArgument {
            get {
                return ResourceManager.GetString("InvalidPlatformObjectArgument", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot create GpuResource with an undefined dimension..
        /// </summary>
        internal static string UndefinedDimensionResource {
            get {
                return ResourceManager.GetString("UndefinedDimensionResource", resourceCulture);
            }
        }
    }
}
