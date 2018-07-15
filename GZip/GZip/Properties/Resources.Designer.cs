﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GZip.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("GZip.Properties.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to Please enter arguments up to the following pattern: [compress/decompress] [Source file] [Destination file].
        /// </summary>
        internal static string ErrorArgsIsEmpty {
            get {
                return ResourceManager.GetString("ErrorArgsIsEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The first argument shall be: [compress] or [decompress]..
        /// </summary>
        internal static string ErrorFirstArg {
            get {
                return ResourceManager.GetString("ErrorFirstArg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No source file was found..
        /// </summary>
        internal static string ErrorInputFileNotFound {
            get {
                return ResourceManager.GetString("ErrorInputFileNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Source and destination files shall be different..
        /// </summary>
        internal static string ErrorInputOutputFIlesSellBeDifferent {
            get {
                return ResourceManager.GetString("ErrorInputOutputFIlesSellBeDifferent", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Memory not enough.
        /// </summary>
        internal static string ErrorMemoryNotEnough {
            get {
                return ResourceManager.GetString("ErrorMemoryNotEnough", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Destination file already exists. Please indicate the different file name..
        /// </summary>
        internal static string ErrorOutputFileExist {
            get {
                return ResourceManager.GetString("ErrorOutputFileExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No destination file name was specified..
        /// </summary>
        internal static string ErrorOutputFileNotspecified {
            get {
                return ResourceManager.GetString("ErrorOutputFileNotspecified", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No source file name was specified..
        /// </summary>
        internal static string ErrorSecondArg {
            get {
                return ResourceManager.GetString("ErrorSecondArg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Input file is empty.
        /// </summary>
        internal static string InputFileIsEmpty {
            get {
                return ResourceManager.GetString("InputFileIsEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Opertion is cancelling.
        /// </summary>
        internal static string OperationCancelling {
            get {
                return ResourceManager.GetString("OperationCancelling", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Operation is not started.
        /// </summary>
        internal static string OperationIsNotStarted {
            get {
                return ResourceManager.GetString("OperationIsNotStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to File processing was started.
        /// </summary>
        internal static string OperationWasStarted {
            get {
                return ResourceManager.GetString("OperationWasStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Operation was stopped.
        /// </summary>
        internal static string OperationWasStopped {
            get {
                return ResourceManager.GetString("OperationWasStopped", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Validation passed..
        /// </summary>
        internal static string ParameterInstalled {
            get {
                return ResourceManager.GetString("ParameterInstalled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The type of operation is not set.
        /// </summary>
        internal static string UnknownOperation {
            get {
                return ResourceManager.GetString("UnknownOperation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Work is done.
        /// </summary>
        internal static string WorkIsDone {
            get {
                return ResourceManager.GetString("WorkIsDone", resourceCulture);
            }
        }
    }
}
