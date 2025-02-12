﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by xsd, Version=4.8.4084.0.
// 
namespace Nucleus.Abstractions.Models.Extensions
{
  using System.Xml.Serialization;


  /// <remarks/>
  [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.4084.0")]
  [System.SerializableAttribute()]
  [System.Diagnostics.DebuggerStepThroughAttribute()]
  [System.ComponentModel.DesignerCategoryAttribute("code")]
  [System.Xml.Serialization.XmlTypeAttribute(Namespace = "urn:nucleus/schemas/package/1.0")]
  [System.Xml.Serialization.XmlRootAttribute("package", Namespace = "urn:nucleus/schemas/package/1.0", IsNullable = false)]
  public partial class Package
  {

    /// <remarks/>
    public string name;

    /// <remarks/>
    public string version;

    /// <remarks/>
    public Publisher publisher;

    /// <remarks/>
    public string description;

    /// <remarks/>
    public Compatibility compatibility;

    /// <remarks/>
    [System.Xml.Serialization.XmlArrayItemAttribute("component", IsNullable = false)]
    public Component[] components;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string id;
  }

  /// <remarks/>
  [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.4084.0")]
  [System.SerializableAttribute()]
  [System.Diagnostics.DebuggerStepThroughAttribute()]
  [System.ComponentModel.DesignerCategoryAttribute("code")]
  [System.Xml.Serialization.XmlTypeAttribute(Namespace = "urn:nucleus/schemas/package/1.0")]
  public partial class Publisher
  {

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string name;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string url;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string email;
  }

  /// <remarks/>
  [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.4084.0")]
  [System.SerializableAttribute()]
  [System.Diagnostics.DebuggerStepThroughAttribute()]
  [System.ComponentModel.DesignerCategoryAttribute("code")]
  [System.Xml.Serialization.XmlTypeAttribute(Namespace = "urn:nucleus/schemas/package/1.0")]
  public partial class Cleanup
  {

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("containerDefinition", typeof(ContainerDefinition))]
    [System.Xml.Serialization.XmlElementAttribute("file", typeof(FileDefinition))]
    [System.Xml.Serialization.XmlElementAttribute("folder", typeof(FolderDefinition))]
    [System.Xml.Serialization.XmlElementAttribute("layoutDefinition", typeof(LayoutDefinition))]
    [System.Xml.Serialization.XmlElementAttribute("moduleDefinition", typeof(ModuleDefinition))]
    public object[] Items;
  }

  /// <remarks/>
  [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.4084.0")]
  [System.SerializableAttribute()]
  [System.Diagnostics.DebuggerStepThroughAttribute()]
  [System.ComponentModel.DesignerCategoryAttribute("code")]
  [System.Xml.Serialization.XmlTypeAttribute(Namespace = "urn:nucleus/schemas/package/1.0")]
  public partial class ContainerDefinition
  {

    /// <remarks/>
    public string friendlyName;

    /// <remarks/>
    public string relativePath;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string id;
  }

  /// <remarks/>
  [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.4084.0")]
  [System.SerializableAttribute()]
  [System.Diagnostics.DebuggerStepThroughAttribute()]
  [System.ComponentModel.DesignerCategoryAttribute("code")]
  [System.Xml.Serialization.XmlTypeAttribute(Namespace = "urn:nucleus/schemas/package/1.0")]
  public partial class FileDefinition
  {

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string name;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public bool uncompress;

    /// <remarks/>
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public bool uncompressSpecified;
  }

  /// <remarks/>
  [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.4084.0")]
  [System.SerializableAttribute()]
  [System.Diagnostics.DebuggerStepThroughAttribute()]
  [System.ComponentModel.DesignerCategoryAttribute("code")]
  [System.Xml.Serialization.XmlTypeAttribute(Namespace = "urn:nucleus/schemas/package/1.0")]
  public partial class FolderDefinition
  {

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("file", typeof(FileDefinition))]
    [System.Xml.Serialization.XmlElementAttribute("folder", typeof(FolderDefinition))]
    public object[] Items;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string name;
  }

  /// <remarks/>
  [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.4084.0")]
  [System.SerializableAttribute()]
  [System.Diagnostics.DebuggerStepThroughAttribute()]
  [System.ComponentModel.DesignerCategoryAttribute("code")]
  [System.Xml.Serialization.XmlTypeAttribute(Namespace = "urn:nucleus/schemas/package/1.0")]
  public partial class LayoutDefinition
  {

    /// <remarks/>
    public string friendlyName;

    /// <remarks/>
    public string relativePath;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string id;
  }

  /// <remarks/>
  [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.4084.0")]
  [System.SerializableAttribute()]
  [System.Diagnostics.DebuggerStepThroughAttribute()]
  [System.ComponentModel.DesignerCategoryAttribute("code")]
  [System.Xml.Serialization.XmlTypeAttribute(Namespace = "urn:nucleus/schemas/package/1.0")]
  public partial class ModuleDefinition
  {

    /// <remarks/>
    public string friendlyName;

    /// <remarks/>
    public string helpUrl;

    /// <remarks/>
    public string extension;

    /// <remarks/>
    public string viewController;

    /// <remarks/>
    public string settingsController;

    /// <remarks/>
    public string viewAction;

    /// <remarks/>
    public string editAction;

    /// <remarks/>
    public string categories;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string id;
  }

  /// <remarks/>
  [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.4084.0")]
  [System.SerializableAttribute()]
  [System.Diagnostics.DebuggerStepThroughAttribute()]
  [System.ComponentModel.DesignerCategoryAttribute("code")]
  [System.Xml.Serialization.XmlTypeAttribute(Namespace = "urn:nucleus/schemas/package/1.0")]
  public partial class ControlPanelExtensionDefinition
  {

    /// <remarks/>
    public string friendlyName;

    /// <remarks/>
    public string helpUrl;

    /// <remarks/>
    public string description;

    /// <remarks/>
    public string controllerName;

    /// <remarks/>
    public string extensionName;

    /// <remarks/>
    public ControlPanelExtensionScope scope;

    /// <remarks/>
    public string editAction;

    /// <remarks/>
    public string icon;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string id;
  }

  /// <remarks/>
  [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.4084.0")]
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(Namespace = "urn:nucleus/schemas/package/1.0")]
  public enum ControlPanelExtensionScope
  {

    /// <remarks/>
    Global,

    /// <remarks/>
    Site,
  }

  /// <remarks/>
  [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.4084.0")]
  [System.SerializableAttribute()]
  [System.Diagnostics.DebuggerStepThroughAttribute()]
  [System.ComponentModel.DesignerCategoryAttribute("code")]
  [System.Xml.Serialization.XmlTypeAttribute(Namespace = "urn:nucleus/schemas/package/1.0")]
  public partial class Component
  {

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("cleanup", typeof(Cleanup))]
    [System.Xml.Serialization.XmlElementAttribute("containerDefinition", typeof(ContainerDefinition))]
    [System.Xml.Serialization.XmlElementAttribute("controlPanelExtensionDefinition", typeof(ControlPanelExtensionDefinition))]
    [System.Xml.Serialization.XmlElementAttribute("file", typeof(FileDefinition))]
    [System.Xml.Serialization.XmlElementAttribute("folder", typeof(FolderDefinition))]
    [System.Xml.Serialization.XmlElementAttribute("layoutDefinition", typeof(LayoutDefinition))]
    [System.Xml.Serialization.XmlElementAttribute("moduleDefinition", typeof(ModuleDefinition))]
    public object[] Items;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string folderName;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public bool optional;
  }

  /// <remarks/>
  [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.4084.0")]
  [System.SerializableAttribute()]
  [System.Diagnostics.DebuggerStepThroughAttribute()]
  [System.ComponentModel.DesignerCategoryAttribute("code")]
  [System.Xml.Serialization.XmlTypeAttribute(Namespace = "urn:nucleus/schemas/package/1.0")]
  public partial class Compatibility
  {

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string minVersion;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string maxVersion;
  }
}
