using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AutoConverter.BusinessObjects.Core.BusinessEntities.V2
{
    #region Imports

    #endregion

    /// <summary>
    /// Non-public types and members here.
    /// </summary>
    partial class EntityMappingDefinition<TSourceEntity, TTargetEntity>
    {
        private readonly HashSet<IFieldMappingDefinition> _fieldMappings = new HashSet<IFieldMappingDefinition>();
        private static EntityMappingPerformer _entityMappingPerformer;

        // we understand we have a different lock object per generic type
        private static readonly object LockObject = new object();

        /// <summary>
        /// Called when chaining a mapping call.
        /// </summary>
        private void Replace(IFieldMappingDefinition original, IFieldMappingDefinition updated)
        {
            _fieldMappings.Remove(original);
            _fieldMappings.Add(updated);
        }

        #region Internal Interfaces

        /// <summary>
        /// This is the part of SourceFieldDefinition and TransformedSourceFieldDefinition that EntityMappingDefinition can see.
        /// </summary>
        private interface IFieldMappingDefinition
        {
            IFieldMappingPerformer Compile(string tagValue);
        }

        interface IFinalFieldMapping<out TTargetField>
        {
            void SetupStringifyFunc(Func<TTargetField, string> stringify);
        }

        /// <summary>
        /// What SourceFieldDefinition and TransformedSourceFieldDefinition turn themselves into.
        /// </summary>
        interface IFieldMappingPerformer
        {
            void Apply(TSourceEntity source, TTargetEntity target);
            void Apply(TTargetEntity fromEntity, TTargetEntity toEntity);
            KeyValuePair<string, string> Stringify(TSourceEntity source, TTargetEntity target);
        }

        #endregion

        #region Entity Mapping Performer

        /// <summary>
        /// Performs a mapping from {TSourceEntity} to {TTargetEntity}.
        /// This is publicly exposed, through the IEntityConverter interface.
        /// </summary>
        class EntityMappingPerformer
        {
            readonly List<IFieldMappingPerformer> fieldMappers;

            public HashSet<IFieldMappingDefinition> FieldMappings { get; private set; }

            public EntityMappingPerformer(HashSet<IFieldMappingDefinition> fieldMappings, string tagVlaue)
            {
                fieldMappers = fieldMappings.Select(z => z.Compile(tagVlaue)).ToList();
                FieldMappings = fieldMappings;
            }

            public TTargetEntity Convert(TSourceEntity sourceEntity)
            {
                var targetEntity = new TTargetEntity();

                foreach (var fieldMapper in fieldMappers)
                {
                    fieldMapper.Apply(sourceEntity, targetEntity);
                }

                return targetEntity;
            }

            public void Copy(TTargetEntity fromEntity, TTargetEntity toEntity)
            {
                foreach (var fieldMapper in fieldMappers)
                {
                    fieldMapper.Apply(fromEntity, toEntity);
                }
            }

            public Dictionary<string, string> Stringify(TSourceEntity sourceEntity)
            {
                var targetEntity = new TTargetEntity();
                var result = new Dictionary<string, string>();
                foreach (var fieldMapper in fieldMappers)
                {
                    var pair = fieldMapper.Stringify(sourceEntity, targetEntity);
                    result.Add(pair.Key, pair.Value);
                }

                return result;
            }
        }

        #endregion

        #region SourceFieldDefinition

        /// <summary>
        /// This defines the source field on {TSourceEntity}.
        /// </summary>
        class SourceFieldDefinition<TSourceField> : IFieldMappingDefinition, ISourceFieldDefinition<TTargetEntity, TSourceField>,
            IFinalFieldDefinition<TSourceField>
        {
            readonly Func<TSourceEntity, TSourceField> sourceFieldGetter;
            readonly Action<IFieldMappingDefinition, IFieldMappingDefinition> replaceWithFinal;
            private CompleteFieldMapping<TSourceField> final;

            public SourceFieldDefinition(Expression<Func<TSourceEntity, TSourceField>> sourceField, Action<IFieldMappingDefinition, IFieldMappingDefinition> replaceWithFinal)
            {
                sourceFieldGetter = sourceField.Compile();
                this.replaceWithFinal = replaceWithFinal;
            }

            #region ISourceFieldDefinition Implementation

            /// <summary>
            /// Link to a target field. This completes the mapping.
            /// </summary>
            IFinalFieldDefinition<TSourceField>
                ITargetFieldDefinition<TTargetEntity, TSourceField>.To(Expression<Func<TTargetEntity, TSourceField>> targetField)
            {
                final = new CompleteFieldMapping<TSourceField>(sourceFieldGetter, targetField);
                replaceWithFinal(this, final);

                return this;
            }

            void IFinalFieldDefinition<TSourceField>.Stringify(Func<TSourceField, string> stringify)
            {
                if (final == null)
                {
                    var error = string.Format("Can't stringify a field where only From() has been defined. Chain a call to Then() or To() after stringifying this field.");
                    throw new Exception(error);
                }

                final.SetupStringifyFunc(stringify);
            }

            /// <summary>
            /// Updated to a source field plus a transformation.
            /// </summary>
            ITransformedSourceFieldDefinition<TTargetEntity, TTargetField>
                ISourceFieldDefinition<TTargetEntity, TSourceField>.Then<TTargetField>(Func<TSourceField, TTargetField> transformation)
            {
                var intermediate = new TransformedSourceFieldDefinition<TSourceField, TTargetField>(sourceFieldGetter, transformation, replaceWithFinal);
                replaceWithFinal(this, intermediate);

                return intermediate;
            }

            #endregion

            #region IFieldMappingDefinition Implementation

            public IFieldMappingPerformer Compile(string tagValue)
            {
                var error = string.Format("Can't compile a field where only From() has been defined. Chain a call to Then() or To() after defining this field.");
                throw new Exception(error);
            }

            #endregion
        }

        #endregion

        #region TransformedSourceFieldDefinition

        /// <summary>
        /// Maps a field of type {TSourceField} on the entity {TSourceEntity} to a field of type {TTargetField} on the entity {TTargetEntity}.
        /// </summary>
        class TransformedSourceFieldDefinition<TSourceField, TTargetField> :
            ITransformedSourceFieldDefinition<TTargetEntity, TTargetField>,
            IFieldMappingDefinition,
            IFinalFieldDefinition<TTargetField>
        {
            readonly Func<TSourceEntity, TTargetField> combinedGetter;
            readonly Action<IFieldMappingDefinition, IFieldMappingDefinition> replaceWithFinal;
            CompleteFieldMapping<TTargetField> final;

            public TransformedSourceFieldDefinition(Func<TSourceEntity, TSourceField> sourceFieldGetter, Func<TSourceField, TTargetField> transformation, Action<IFieldMappingDefinition, IFieldMappingDefinition> replaceWithFinal)
            {
                combinedGetter = z => transformation(sourceFieldGetter(z));
                this.replaceWithFinal = replaceWithFinal;
            }

            /// <summary>
            /// Implements ITransformedSourceFieldDefinition.
            /// </summary>
            IFinalFieldDefinition<TTargetField> ITargetFieldDefinition<TTargetEntity, TTargetField>.To(Expression<Func<TTargetEntity, TTargetField>> targetField)
            {
                final = new CompleteFieldMapping<TTargetField>(combinedGetter, targetField);
                replaceWithFinal(this, final);

                return this;
            }

            void IFinalFieldDefinition<TTargetField>.Stringify(Func<TTargetField, string> stringify)
            {
                if (final == null)
                {
                    var error = string.Format("Can't stringify a field where only From() has been defined. Chain a call to Then() or To() after stringifying this field.");
                    throw new Exception(error);
                }

                final.SetupStringifyFunc(stringify);
            }

            /// <summary>
            /// Implements IFieldMappingDefinition.
            /// </summary>
            IFieldMappingPerformer IFieldMappingDefinition.Compile(string tagValue)
            {
                var error = string.Format("Can't compile a field where only From() and Then() have been defined. Chain a call to To() after defining this field.");
                throw new Exception(error);
            }
        }

        #endregion

        /// <summary>
        /// A field which has been successfully mapped from {TSourceEntity} to {TTargetEntity}. 
        /// </summary>
        class CompleteFieldMapping<TTargetField> : IFieldMappingDefinition, IFinalFieldMapping<TTargetField>
        {
            readonly Func<TSourceEntity, TTargetField> sourceFieldGetter;
            readonly Expression<Func<TTargetEntity, TTargetField>> targetField;
            Func<TTargetField, string> stringify;

            public CompleteFieldMapping(Func<TSourceEntity, TTargetField> sourceFieldGetter, Expression<Func<TTargetEntity, TTargetField>> targetField)
            {
                this.sourceFieldGetter = sourceFieldGetter;
                this.targetField = targetField;

                // verify that [targetField] does not have a coercion
                var unary = targetField.Body as UnaryExpression;

                if ((unary != null) && (unary.NodeType == ExpressionType.Convert))
                {
                    throw new Exception("The target field doesn't completely match the type of the source field. This is usually caused by an implicit conversion, for example int to int?. Replace this with an explicit cast using .Then() after .From().");
                }
            }

            /// <summary>
            /// Implements IFieldMappingDefinition.
            /// </summary>
            public IFieldMappingPerformer Compile(string tagValue)
            {
                return new FieldMappingPerformer<TTargetField>(sourceFieldGetter, targetField, stringify, tagValue);
            }

            public void SetupStringifyFunc(Func<TTargetField, string> stringify)
            {
                this.stringify = stringify;
            }
        }

        /// <summary>
        /// The actual IFieldMappingPerformer implementation.
        /// </summary>
        class FieldMappingPerformer<TTargetField> : IFieldMappingPerformer
        {
            readonly Func<TSourceEntity, TTargetField> sourceFieldGetter;
            readonly Action<TTargetEntity, TTargetField> targetFieldSetter;
            readonly Expression<Func<TTargetEntity, TTargetField>> targetField;
            readonly Func<TTargetEntity, TTargetField> targetFieldFunc;
            readonly Func<TTargetField, string> stringify;
            private string tagVlaue;
            private readonly bool isValueTpe;

            public FieldMappingPerformer(Func<TSourceEntity, TTargetField> sourceFieldGetter,
                Expression<Func<TTargetEntity, TTargetField>> targetField,
                Func<TTargetField, string> stringify,
                string tagVlaue)
            {
                this.sourceFieldGetter = sourceFieldGetter;
                this.targetField = targetField;
                targetFieldFunc = targetField.Compile();
                targetFieldSetter = ExpressionHelper.CreateFieldSetter(targetField);
                isValueTpe = typeof(TTargetField).IsValueType;

                if (stringify == null)
                {
                    stringify = z =>
                        {
                            if (isValueTpe)
                            {
                                // value type
                                return z.ToString();
                            }

                            if (EqualityComparer<TTargetField>.Default.Equals(z, default(TTargetField)))
                            {
                                // reference type null check
                                return string.Empty;
                            }

                            return z.ToString();
                        };
                }

                this.stringify = stringify;
                this.tagVlaue = tagVlaue;
            }

            public void Apply(TSourceEntity source, TTargetEntity target)
            {
                var sourceValue = sourceFieldGetter(source);
                targetFieldSetter(target, sourceValue);
            }

            public void Apply(TTargetEntity fromEntity, TTargetEntity toEntity)
            {
                var sourceValue = targetFieldFunc(fromEntity);
                targetFieldSetter(toEntity, sourceValue);
            }

            public KeyValuePair<string, string> Stringify(TSourceEntity source, TTargetEntity target)
            {
                Apply(source, target);

                var targetFieldString = stringify(targetFieldFunc(target));
                var simplePropertyNameAggregator = new SimplePropertyNameAggregator();
                simplePropertyNameAggregator.Visit(targetField.Body, false);

                if (string.IsNullOrEmpty(tagVlaue))
                {
                    var error =
                        $"Can't stringify a field where tag value is not defined for source entity: {typeof(TSourceEntity).Name}, " +
                        $"target entity: {typeof(TTargetEntity).Name}";

                    throw new Exception(error);
                }

                var keyFormat = tagVlaue + ".{0}";
                var result = new KeyValuePair<string, string>(
                    string.Format(keyFormat,simplePropertyNameAggregator.PropertyMetaData.PropertyInfoList[0].Name),
                    targetFieldString);

                return result;
            }
        }
    }
}
