using System.Collections;
using System.Reflection;

namespace SwiftMapper
{
    /// <summary>
    /// Fast, lightweight object-to-object mapper.
    /// </summary>
    /// <remarks>
    /// Maps properties by name. Handles:
    /// <list type="bullet">
    /// <item><description>Value types and reference types when assignable</description></item>
    /// <item><description>Nested object mapping (by mapping matching property names)</description></item>
    /// <item><description>List&lt;T&gt; mapping by mapping each item</description></item>
    /// <item><description>Enum conversion to/from underlying integral or string names</description></item>
    /// </list>
    /// Read-only destination properties are skipped. Null sources are not allowed.
    /// </remarks>
    public class Mapper
    {
        /// <summary>
        /// Maps a <typeparamref name="TSource"/> instance to a new <typeparamref name="TDestination"/>.
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TDestination">Destination type</typeparam>
        /// <param name="source">Source instance (must not be null)</param>
        /// <param name="subObjectValues">Optional explicit values for destination sub-properties that are not present on source</param>
        /// <returns>A new <typeparamref name="TDestination"/> instance with mapped values</returns>
        public static TDestination Map<TSource, TDestination>(TSource source, params (string SubPropertyName, object PropertyValue)[] subObjectValues)
            where TSource : class
            where TDestination : class, new()
        {
            return Map<TSource, TDestination>(source, Enumerable.Empty<Type>(), subObjectValues);
        }

        /// <summary>
        /// Maps a <typeparamref name="TSource"/> instance to a new <typeparamref name="TDestination"/>.
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TDestination">Destination type</typeparam>
        /// <param name="source">Source instance (must not be null)</param>
        /// <param name="subProperties">Reserved for future configuration</param>
        /// <param name="subObjectValues">Optional explicit values for destination sub-properties that are not present on source</param>
        /// <returns>A new <typeparamref name="TDestination"/> instance with mapped values</returns>
        public static TDestination Map<TSource, TDestination>(TSource source, IEnumerable<Type> subProperties, params (string SubPropertyName, object PropertyValue)[] subObjectValues)
            where TSource : class
            where TDestination : class, new()
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            TDestination destination = new TDestination();

            return MapProperties(source, destination, subProperties, subObjectValues);
        }

        /// <summary>
        /// Internal property mapping routine.
        /// </summary>
        private static TDestination MapProperties<TSource, TDestination>(TSource source, TDestination destination, IEnumerable<Type> subProperties, params (string SubPropertyName, object PropertyValue)[] subObjectValues)
            where TSource : class
            where TDestination : class, new()
        {
            var sourceProperties = typeof(TSource).GetProperties();
            var destinationProperties = typeof(TDestination).GetProperties();

            foreach (var destinationProperty in destinationProperties)
            {
                if (!destinationProperty.CanWrite)
                    continue;

                var sourceProperty = sourceProperties.FirstOrDefault(p => p.Name == destinationProperty.Name);

                if (sourceProperty != null)
                {
                    var sourceValue = sourceProperty.GetValue(source);
                    var destinationType = Nullable.GetUnderlyingType(destinationProperty.PropertyType) ?? destinationProperty.PropertyType;

                    // Check if either source or destination property type is an enum
                    bool sourceIsEnum = sourceProperty.PropertyType.IsEnum;
                    bool destinationIsEnum = destinationType.IsEnum;

                    if (sourceValue != null && (sourceIsEnum || destinationIsEnum))
                    {
                        if (sourceIsEnum && !destinationIsEnum)
                        {
                            var enumIntegerValue = Convert.ChangeType(sourceValue, Enum.GetUnderlyingType(sourceProperty.PropertyType));
                            destinationProperty.SetValue(destination, enumIntegerValue);
                        }
                        else if (!sourceIsEnum && destinationIsEnum)
                        {
                            if (Enum.TryParse(destinationType, sourceValue.ToString(), out object? parsedEnumValue))
                            {
                                destinationProperty.SetValue(destination, parsedEnumValue);
                            }
                        }
                    }
                    else
                    {
                        // If null, just set null
                        if (sourceValue == null)
                        {
                            destinationProperty.SetValue(destination, null);
                        }
                        // If types are assignable, set directly
                        else if (destinationProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                        {
                            destinationProperty.SetValue(destination, sourceValue);
                        }
                        // If destination expects a List<T> and source is enumerable, map the list
                        else if (destinationProperty.PropertyType.IsGenericType && destinationProperty.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            Type subPropertyType = destinationProperty.PropertyType.GetGenericArguments()[0];
                            if (sourceValue is IEnumerable enumerableValue)
                            {
                                var listMethodDefinition = typeof(Mapper)
                                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                                    .FirstOrDefault(m => m.Name == "MapSubPropertyList" && m.IsGenericMethodDefinition);

                                if (listMethodDefinition != null)
                                {
                                    MethodInfo mapSubPropertyMethod = listMethodDefinition.MakeGenericMethod(subPropertyType);
                                    var subObjectList = mapSubPropertyMethod.Invoke(null, new object[] { enumerableValue.Cast<object>(), subObjectValues });
                                    destinationProperty.SetValue(destination, subObjectList);
                                }
                                else
                                {
                                    var listInstance = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(subPropertyType))!;
                                    foreach (var item in enumerableValue)
                                    {
                                        if (item == null) continue;
                                        var mapped = MapSubProperty(subPropertyType, item);
                                        listInstance.Add(mapped);
                                    }
                                    destinationProperty.SetValue(destination, listInstance);
                                }
                            }
                        }
                        // Otherwise, attempt to map nested object
                        else
                        {
                            var subObject = MapSubProperty(destinationProperty.PropertyType, sourceValue);
                            destinationProperty.SetValue(destination, subObject);
                        }
                    }
                }
                else
                {
                    // If the property is a sub-object, map it using the Map method
                    var subPropertyValue = subObjectValues.FirstOrDefault(x => x.SubPropertyName == destinationProperty.Name);
                    if (subPropertyValue.PropertyValue != null)
                    {
                        if (destinationProperty.PropertyType.IsGenericType && destinationProperty.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            Type subPropertyType = destinationProperty.PropertyType.GetGenericArguments()[0];
                            if (subPropertyValue.PropertyValue is IEnumerable enumerableValue)
                            {
                                var listMethodDefinition = typeof(Mapper)
                                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                                    .FirstOrDefault(m => m.Name == "MapSubPropertyList" && m.IsGenericMethodDefinition);

                                if (listMethodDefinition != null)
                                {
                                    MethodInfo mapSubPropertyMethod = listMethodDefinition.MakeGenericMethod(subPropertyType);
                                    var subObjectList = mapSubPropertyMethod.Invoke(null, new object[] { enumerableValue.Cast<object>(), subObjectValues });
                                    destinationProperty.SetValue(destination, subObjectList);
                                }
                                else
                                {
                                    var listInstance = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(subPropertyType))!;
                                    foreach (var item in enumerableValue)
                                    {
                                        if (item == null) continue;
                                        var mapped = MapSubProperty(subPropertyType, item);
                                        listInstance.Add(mapped);
                                    }
                                    destinationProperty.SetValue(destination, listInstance);
                                }
                            }
                        }
                        else
                        {
                            Type subPropertyType = destinationProperty.PropertyType;
                            var subPropertyValueObject = subPropertyValue.PropertyValue;
                            var subObject = MapSubProperty(subPropertyType, subPropertyValueObject);
                            destinationProperty.SetValue(destination, subObject);
                        }
                    }
                }
            }
            return destination;
        }

        private static object MapSubProperty(Type subPropertyType, object propertyValue)
        {
            if (propertyValue == null)
                return null!;

            Type destinationType = Nullable.GetUnderlyingType(subPropertyType) ?? subPropertyType;
            var methodDefinition = typeof(Mapper)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "ConvertToSubProperty" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);

            if (methodDefinition == null)
            {
                // Fallback: attempt manual shallow mapping by name
                if (destinationType.IsInstanceOfType(propertyValue))
                    return propertyValue;

                if (destinationType == typeof(string) || destinationType.IsValueType)
                    return null!;

                var destinationInstance = Activator.CreateInstance(destinationType);
                if (destinationInstance == null)
                    return null!;

                var sourceProperties = propertyValue.GetType().GetProperties();
                var destProperties = destinationType.GetProperties();

                foreach (var destProp in destProperties)
                {
                    if (!destProp.CanWrite) continue;
                    var srcProp = sourceProperties.FirstOrDefault(p => p.Name == destProp.Name);
                    if (srcProp == null) continue;

                    var srcVal = srcProp.GetValue(propertyValue);
                    if (srcVal == null)
                    {
                        destProp.SetValue(destinationInstance, null);
                        continue;
                    }

                    var destPropType = Nullable.GetUnderlyingType(destProp.PropertyType) ?? destProp.PropertyType;
                    bool srcIsEnum = srcProp.PropertyType.IsEnum;
                    bool destIsEnum = destPropType.IsEnum;

                    if (srcIsEnum && !destIsEnum)
                    {
                        var enumIntegerValue = Convert.ChangeType(srcVal, Enum.GetUnderlyingType(srcProp.PropertyType));
                        destProp.SetValue(destinationInstance, enumIntegerValue);
                        continue;
                    }

                    if (!srcIsEnum && destIsEnum)
                    {
                        if (Enum.TryParse(destPropType, srcVal.ToString(), out object? parsedEnumValue))
                        {
                            destProp.SetValue(destinationInstance, parsedEnumValue);
                        }
                        continue;
                    }

                    if (destProp.PropertyType.IsAssignableFrom(srcProp.PropertyType))
                    {
                        destProp.SetValue(destinationInstance, srcVal);
                        continue;
                    }

                    if (destProp.PropertyType.IsGenericType && destProp.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        Type itemType = destProp.PropertyType.GetGenericArguments()[0];
                        if (srcVal is IEnumerable enumerableValue)
                        {
                            var listInstance = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType))!;
                            foreach (var item in enumerableValue)
                            {
                                if (item == null) continue;
                                var mappedItem = MapSubProperty(itemType, item);
                                listInstance.Add(mappedItem);
                            }
                            destProp.SetValue(destinationInstance, listInstance);
                        }
                        continue;
                    }

                    // Nested object mapping
                    var nested = MapSubProperty(destProp.PropertyType, srcVal);
                    destProp.SetValue(destinationInstance, nested);
                }

                return destinationInstance;
            }

            try
            {
                var mapMethod = methodDefinition.MakeGenericMethod(destinationType);
                return mapMethod.Invoke(null, new object[] { propertyValue }) ?? null!;
            }
            catch
            {
                // Fallback: attempt manual shallow mapping by name
                if (destinationType.IsInstanceOfType(propertyValue))
                    return propertyValue;

                if (destinationType == typeof(string) || destinationType.IsValueType)
                    return null!;

                var destinationInstance = Activator.CreateInstance(destinationType);
                if (destinationInstance == null)
                    return null!;

                var sourceProperties = propertyValue.GetType().GetProperties();
                var destProperties = destinationType.GetProperties();

                foreach (var destProp in destProperties)
                {
                    if (!destProp.CanWrite) continue;
                    var srcProp = sourceProperties.FirstOrDefault(p => p.Name == destProp.Name);
                    if (srcProp == null) continue;

                    var srcVal = srcProp.GetValue(propertyValue);
                    if (srcVal == null)
                    {
                        destProp.SetValue(destinationInstance, null);
                        continue;
                    }

                    var destPropType = Nullable.GetUnderlyingType(destProp.PropertyType) ?? destProp.PropertyType;
                    bool srcIsEnum = srcProp.PropertyType.IsEnum;
                    bool destIsEnum = destPropType.IsEnum;

                    if (srcIsEnum && !destIsEnum)
                    {
                        var enumIntegerValue = Convert.ChangeType(srcVal, Enum.GetUnderlyingType(srcProp.PropertyType));
                        destProp.SetValue(destinationInstance, enumIntegerValue);
                        continue;
                    }

                    if (!srcIsEnum && destIsEnum)
                    {
                        if (Enum.TryParse(destPropType, srcVal.ToString(), out object? parsedEnumValue))
                        {
                            destProp.SetValue(destinationInstance, parsedEnumValue);
                        }
                        continue;
                    }

                    if (destProp.PropertyType.IsAssignableFrom(srcProp.PropertyType))
                    {
                        destProp.SetValue(destinationInstance, srcVal);
                        continue;
                    }

                    if (destProp.PropertyType.IsGenericType && destProp.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        Type itemType = destProp.PropertyType.GetGenericArguments()[0];
                        if (srcVal is IEnumerable enumerableValue)
                        {
                            var listInstance = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType))!;
                            foreach (var item in enumerableValue)
                            {
                                if (item == null) continue;
                                var mappedItem = MapSubProperty(itemType, item);
                                listInstance.Add(mappedItem);
                            }
                            destProp.SetValue(destinationInstance, listInstance);
                        }
                        continue;
                    }

                    // Nested object mapping
                    var nested = MapSubProperty(destProp.PropertyType, srcVal);
                    destProp.SetValue(destinationInstance, nested);
                }

                return destinationInstance;
            }

        }
    }
}
