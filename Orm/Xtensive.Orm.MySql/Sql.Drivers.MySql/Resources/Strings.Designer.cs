﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.19408
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Xtensive.Sql.Drivers.MySql.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Xtensive.Sql.Drivers.MySql.Resources.Strings", typeof(Strings).Assembly);
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
        ///   Looks up a localized string similar to Date type {0} is not supported..
        /// </summary>
        internal static string ExCannotSupportEnum {
            get {
                return ResourceManager.GetString("ExCannotSupportEnum", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MySQL supports CURSORS only on stores procedures and functions..
        /// </summary>
        internal static string ExCursorsOnlyForProcsAndFuncs {
            get {
                return ResourceManager.GetString("ExCursorsOnlyForProcsAndFuncs", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MySQL does not support sequences..
        /// </summary>
        internal static string ExDoesNotSupportSequences {
            get {
                return ResourceManager.GetString("ExDoesNotSupportSequences", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid Boolean String.
        /// </summary>
        internal static string ExInvalidBooleanStringX {
            get {
                return ResourceManager.GetString("ExInvalidBooleanStringX", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MySQL version below 5.0 is not supported..
        /// </summary>
        internal static string ExMySqlBelow50IsNotSupported {
            get {
                return ResourceManager.GetString("ExMySqlBelow50IsNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MySQL Username is required..
        /// </summary>
        internal static string ExUserNameRequired {
            get {
                return ResourceManager.GetString("ExUserNameRequired", resourceCulture);
            }
        }
    }
}
