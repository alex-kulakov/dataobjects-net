// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Nikolaev
// Created:    2009.12.08

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Core.Helpers;
using Xtensive.Core.ObjectMapping.Model;
using Xtensive.Core.Resources;
using Xtensive.Core.Threading;

namespace Xtensive.Core.ObjectMapping
{
  /// <summary>
  /// Builder of mapping for <see cref="MapperBase{TComparisonResult}"/>.
  /// </summary>
  public sealed class MappingBuilder : IMappingBuilder
  {
    #region Nested classes

    private class MemberInfoCacheEntry
    {
      public readonly PropertyInfo Count;

      public readonly MethodInfo Add;


      // Constrcutors

      public MemberInfoCacheEntry(PropertyInfo count, MethodInfo add)
      {
        Count = count;
        Add = add;
      }
    }

    #endregion

    private static readonly ThreadSafeDictionary<Type, Pair<Type, MemberInfoCacheEntry>> collectionTypes =
      ThreadSafeDictionary<Type, Pair<Type, MemberInfoCacheEntry>>.Create(new object());
    private static readonly MemberInfoCacheEntry arrayCacheEntry =
      new MemberInfoCacheEntry(typeof (Array).GetProperty("Length"), null);
    public static readonly Func<object, SourcePropertyDescription, object>
      DefaultPrimitiveConverter = (source, sourceProperty) =>
        sourceProperty.SystemProperty.GetValue(source, null);
    private readonly Dictionary<PropertyInfo, PropertyInfo> propertyBindings =
      new Dictionary<PropertyInfo, PropertyInfo>();
    private readonly Dictionary<Type, TargetTypeDescription> enumTypes =
      new Dictionary<Type, TargetTypeDescription>();

    private readonly MappingDescription mappingDescription = new MappingDescription();

    /// <inheritdoc/>
    public IMappingBuilderAdapter<TSource, TTarget> MapType<TSource, TTarget, TKey>(
      Func<TSource, TKey> sourceKeyExtractor, Expression<Func<TTarget, TKey>> targetKeyExtractor)
    {
      return Register(sourceKeyExtractor, targetKeyExtractor, null);
    }

    /// <inheritdoc/>
    public IMappingBuilderAdapter<TSource, TTarget> MapType<TSource, TTarget, TKey>(
      Func<TSource, TKey> sourceKeyExtractor, Expression<Func<TTarget, TKey>> targetKeyExtractor,
      Func<TTarget, object[]> generatorArgumentsProvider)
    {
      ArgumentValidator.EnsureArgumentNotNull(generatorArgumentsProvider, "generatorArgumentsProvider");

      return Register(sourceKeyExtractor, targetKeyExtractor, generatorArgumentsProvider);
    }

    /// <inheritdoc/>
    public IMappingBuilderAdapter<TSource, TTarget> MapStructure<TSource, TTarget>()
      where TTarget : struct
    {
      mappingDescription.RegisterStructure(typeof (TSource), typeof (TTarget));
      return new MapperAdapter<TSource, TTarget>(this);
    }

    #region Private / internal methods

    internal void Register(Type source, Func<object, object> sourceKeyExtractor, Type target,
      Func<object, object> targetKeyExtractor, Func<object, object[]> instanceGenerator)
    {
      mappingDescription.Register(source, sourceKeyExtractor, target, targetKeyExtractor, instanceGenerator);
    }

    internal void RegisterProperty(PropertyInfo source, Func<object, object> converter, PropertyInfo target)
    {
      mappingDescription.RegisterProperty(source, converter, target);
      if (source != null)
        propertyBindings.Add(target, source);
      else
        TrackChanges(target, false);
    }

    internal void RegisterHeir(Type targetBase, Type source, Type target,
      Func<object, object[]> instanceGenerator)
    {
      mappingDescription.EnsureTargetTypeIsRegistered(targetBase);
      var targetBaseDescription = mappingDescription.TargetTypes[targetBase];
      if (!targetBaseDescription.SourceType.SystemType.IsAssignableFrom(source))
        throw new ArgumentException(String.Format(Strings.ExTypeXIsNotSubclassOfTypeY, source,
          targetBaseDescription.SourceType.SystemType));
      mappingDescription.Register(source, targetBaseDescription.SourceType.KeyExtractor, target,
        targetBaseDescription.KeyExtractor, instanceGenerator);
      var targetDescription = mappingDescription.TargetTypes[target];
    }

    internal void Ignore(PropertyInfo propertyInfo)
    {
      mappingDescription.Ignore(propertyInfo);
    }

    internal void TrackChanges(PropertyInfo propertyInfo, bool isEnabled)
    {
      mappingDescription.TrackChanges(propertyInfo, isEnabled);
    }

    internal MappingDescription Complete()
    {
      mappingDescription.EnsureNotLocked();
      mappingDescription.RegisterDefaultPrimitiveTypes();
      BindHierarchies();
      BuildTargetDescriptions();
      mappingDescription.RegisterEnumTypes(enumTypes);
      mappingDescription.Lock(true);
      var modelValidator = new ModelValidator();
      modelValidator.Validate(mappingDescription);
      return mappingDescription;
    }

    private IMappingBuilderAdapter<TSource, TTarget> Register<TSource, TTarget, TKey>(
      Func<TSource, TKey> sourceKeyExtractor, Expression<Func<TTarget, TKey>> targetKeyExtractor,
      Func<TTarget, object[]> generatorArgumentsProvider)
    {
      ArgumentValidator.EnsureArgumentNotNull(sourceKeyExtractor, "sourceKeyExtractor");
      ArgumentValidator.EnsureArgumentNotNull(targetKeyExtractor, "targetKeyExtractor");
      var compiledTargetKeyExtractor = targetKeyExtractor.Compile();
      PropertyInfo targetProperty;
      var isPropertyExtracted = MappingHelper.TryExtractProperty(targetKeyExtractor, "targetKeyExtractor",
        out targetProperty);
      var adaptedArgumentsProvider = generatorArgumentsProvider!=null
        ? (Func<object, object[]>) (target => generatorArgumentsProvider.Invoke((TTarget) target))
        : null;
      var adapteSourceKeyExtractor = MappingHelper.AdaptDelegate(sourceKeyExtractor);
      Register(typeof (TSource), adapteSourceKeyExtractor, typeof (TTarget),
        MappingHelper.AdaptDelegate(compiledTargetKeyExtractor), adaptedArgumentsProvider);
      if (isPropertyExtracted)
        RegisterProperty(null, adapteSourceKeyExtractor, targetProperty);
      return new MapperAdapter<TSource, TTarget>(this);
    }

    private void BindHierarchies()
    {
      var roots = GetHierarchies();
      BindHierarchy(roots);
    }

    private List<TargetTypeDescription> GetHierarchies()
    {
      var result = new List<TargetTypeDescription>();
      foreach (var type in mappingDescription.TargetTypes.Values) {
        var ancestor = type.SystemType.BaseType;
        while (true) {
          if (ancestor==null || ancestor==typeof (object)) {
            result.Add(type);
            break;
          }
          TargetTypeDescription ancestorDescription;
          if (mappingDescription.TargetTypes.TryGetValue(ancestor, out ancestorDescription)) {
            ancestorDescription.AddDirectDescendant(type);
            break;
          }
          ancestor = ancestor.BaseType;
        }
      }
      return result;
    }

    private void BindHierarchy(IEnumerable<TargetTypeDescription> ancestors)
    {
      foreach (var ancestor in ancestors) {
        var directDescendants = ancestor.DirectDescendants;
        if (directDescendants == null)
          continue;
        foreach (var descendant in directDescendants)
          InheritProperties(ancestor, descendant);
        BindHierarchy(directDescendants);
      }
    }

    private void InheritProperties(TargetTypeDescription ancestor, TargetTypeDescription descendant)
    {
      foreach (var property in ancestor.Properties.Values.Cast<TargetPropertyDescription>()) {
        var descendantSystemProperty = descendant.SystemType.GetProperty(property.SystemProperty.Name);
        if (descendant.Properties.ContainsKey(descendantSystemProperty))
          continue;
        if (property.Converter!=null) {
          PropertyInfo sourceSystemProperty = null;
          if (property.SourceProperty!=null)
            sourceSystemProperty = descendant.SourceType.SystemType
              .GetProperty(property.SourceProperty.SystemProperty.Name);
          mappingDescription.RegisterInherited(sourceSystemProperty, property.Converter,
            descendantSystemProperty);
        }
        if (property.IsIgnored)
          mappingDescription.Ignore(descendantSystemProperty);
        mappingDescription.TrackChanges(descendantSystemProperty, !property.IsChangeTrackingDisabled);
      }
    }

    private void BuildTargetDescriptions()
    {
      foreach (var targetType in mappingDescription.TargetTypes.Values) {
        if (targetType.ObjectKind==ObjectKind.Primitive)
          continue;
        ApplyCustomBindings(targetType);
        BuildTargetPropertyDescriptions(targetType);
      }
    }

    private void BuildTargetPropertyDescriptions(TargetTypeDescription targetType)
    {
      var properties = targetType.SystemType.GetProperties();
      for (var i = 0; i < properties.Length; i++) {
        var property = properties[i];
        PropertyDescription propertyDescription;
        if (targetType.Properties.TryGetValue(property, out propertyDescription))
          BindToSource(targetType, (TargetPropertyDescription) propertyDescription);
        else {
          var newProperty = new TargetPropertyDescription(property, targetType);
          BindToSource(targetType, newProperty);
          targetType.AddProperty(newProperty);
        }
      }
    }

    private void BindToSource(TargetTypeDescription targetType,
      TargetPropertyDescription propertyDescription)
    {
      SetPropertyAttributes(propertyDescription);
      if (propertyDescription.Converter == null && !propertyDescription.IsIgnored) {
        var sourceProperty = targetType.SourceType.SystemType
          .GetProperty(propertyDescription.SystemProperty.Name);
        propertyDescription.SourceProperty = AddSourceDescription(targetType.SourceType, sourceProperty);
        if (propertyDescription.ValueType.ObjectKind==ObjectKind.Primitive)
          propertyDescription.Converter = DefaultPrimitiveConverter;
      }
    }

    private void ApplyCustomBindings(TargetTypeDescription targetType)
    {
      foreach (var pair in targetType.Properties) {
        PropertyInfo sourceProperty;
        if (propertyBindings.TryGetValue(pair.Value.SystemProperty, out sourceProperty)) {
          ((TargetPropertyDescription) pair.Value).SourceProperty =
            AddSourceDescription(targetType.SourceType, sourceProperty);
        }
      }
    }

    private SourcePropertyDescription AddSourceDescription(SourceTypeDescription sourceType,
      PropertyInfo property)
    {
      PropertyDescription result;
      if (sourceType.Properties.TryGetValue(property, out result))
        return (SourcePropertyDescription) result;
      var propertyDesc = new SourcePropertyDescription(property, sourceType);
      SetPropertyAttributes(propertyDesc);
      sourceType.AddProperty(propertyDesc);
      return propertyDesc;
    }

    private void SetPropertyAttributes(PropertyDescription property)
    {
      Pair<Type, MemberInfoCacheEntry>? cacheItem = null;
      Pair<Type, MemberInfoCacheEntry> foundItem;
      if (collectionTypes.TryGetValue(property.SystemProperty.PropertyType, out foundItem))
        cacheItem = foundItem;
      else if (property.SystemProperty.PropertyType.IsArray)
        cacheItem = GenerateArrayCacheItem(property.SystemProperty);
      else if (MappingHelper.IsCollectionCandidate(property.SystemProperty.PropertyType))
        cacheItem = GenerateCollectionCacheItem(property.SystemProperty);
      if (cacheItem!=null) {
        property.IsCollection = true;
        if (property is TargetPropertyDescription) {
          var targetProperty = ((TargetPropertyDescription) property);
          targetProperty.ValueType =
            mappingDescription.GetTargetTypeDescription(cacheItem.Value.First);
        }
        property.CountProperty = cacheItem.Value.Second.Count;
        property.AddMethod = cacheItem.Value.Second.Add;
      }
      else if (property is TargetPropertyDescription) {
        var propertyType = property.SystemProperty.PropertyType;
        var targetProperty = (TargetPropertyDescription) property;
        if (propertyType.IsEnum)
          targetProperty.ValueType = GetEnumDescription(propertyType);
        else
          targetProperty.ValueType = mappingDescription.GetTargetTypeDescription(propertyType);
      }
    }

    private TargetTypeDescription GetEnumDescription(Type propertyType)
    {
      TargetTypeDescription result;
      if (!enumTypes.TryGetValue(propertyType, out result)) {
        result = new TargetTypeDescription(propertyType);
        enumTypes.Add(propertyType, result);
      }
      return result;
    }

    private static Pair<Type, MemberInfoCacheEntry>? GenerateCollectionCacheItem(PropertyInfo systemProperty)
    {
      Pair<Type, MemberInfoCacheEntry>? result = null;
      Type interfaceType;
      Type itemType;
      if (MappingHelper.TryGetCollectionInterface(systemProperty.PropertyType, out interfaceType, out itemType))
        result = collectionTypes.GetValue(systemProperty.PropertyType, GenerateCollectionTypesCacheItem,
          interfaceType);
      return result;
    }

    private static Pair<Type, MemberInfoCacheEntry> GenerateArrayCacheItem(PropertyInfo systemProperty)
    {
      return collectionTypes.GetValue(systemProperty.PropertyType,
        (type, propertyCacheEntry) =>
          new Pair<Type, MemberInfoCacheEntry>(type.GetElementType(), propertyCacheEntry),
        arrayCacheEntry);
    }

    private static Pair<Type, MemberInfoCacheEntry> GenerateCollectionTypesCacheItem(Type type,
      Type interfaceType)
    {
      return new Pair<Type, MemberInfoCacheEntry>(interfaceType.GetGenericArguments()[0],
        new MemberInfoCacheEntry(interfaceType.GetProperty("Count"), interfaceType.GetMethod("Add")));
    }

    #endregion
  }
}