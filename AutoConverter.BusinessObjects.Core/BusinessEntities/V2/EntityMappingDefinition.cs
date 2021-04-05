using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AutoConverter.BusinessObjects.Core.BusinessEntities.V2
{
    #region Imports

    #endregion

    /// <summary>
    /// Describes a mapping from {TSourceEntity} to {TTargetEntity}.
    /// </summary>
    public partial class EntityMappingDefinition<TSourceEntity, TTargetEntity> : IEntityConverter<TSourceEntity, TTargetEntity>
        where TTargetEntity : new()
    {
        private readonly Func<HashSet<IFieldMappingDefinition>, bool>
            _shouldGenerateEntityMappingPerformer = ShouldGenerateEntityMappingPerformer;

        private readonly string _tagValue;

        public EntityMappingDefinition()
        {
            
        }

        public EntityMappingDefinition(string tagValue)
        {
            this._tagValue = tagValue;
        }

        /// <summary>
        /// Begin a new field mapping by indicating the source field.
        /// You must call .To() or .Then() on the result of this method.
        /// </summary>
        public ISourceFieldDefinition<TTargetEntity, TSourceField> From<TSourceField>(Expression<Func<TSourceEntity, TSourceField>> sourceField)
        {
            var fieldMapping = new SourceFieldDefinition<TSourceField>(sourceField, Replace);
            _fieldMappings.Add(fieldMapping);

            return fieldMapping;
        }

        public TTargetEntity Convert(TSourceEntity sourceEntity)
        {
            _entityMappingPerformer = Compile();

            return _entityMappingPerformer.Convert(sourceEntity);
        }

        public Dictionary<string, string> Stringify(TSourceEntity sourceEntity)
        {
            _entityMappingPerformer = Compile();

            return _entityMappingPerformer.Stringify(sourceEntity);
        }

        private EntityMappingPerformer Compile()
        {
            if (_shouldGenerateEntityMappingPerformer(_fieldMappings))
            {
                lock (LockObject)
                {
                    if (_shouldGenerateEntityMappingPerformer(_fieldMappings))
                    {
                        _entityMappingPerformer = new EntityMappingPerformer(_fieldMappings, _tagValue);
                    }
                }
            }

            return _entityMappingPerformer;
        }

        private static bool ShouldGenerateEntityMappingPerformer(HashSet<IFieldMappingDefinition> fieldMappingDefinitions)
        {
            return _entityMappingPerformer == null
                || _entityMappingPerformer.FieldMappings != fieldMappingDefinitions;
        }
    }

    /// <summary>
    /// An object which converts entities from {TSourceEntity} to {TTargetEntity}. You build one of these using EntityMappingDefinition.
    /// </summary>
    public interface IEntityConverter<in TSourceEntity, out TTargetEntity>
    {
        TTargetEntity Convert(TSourceEntity sourceEntity);
        Dictionary<string, string> Stringify(TSourceEntity sourceEntity);
    }

    /// <summary>
    /// Publicly visible version of SourceFieldDefinition.
    /// </summary>
    public interface ISourceFieldDefinition<TTargetEntity, TSourceField>
        : ITargetFieldDefinition<TTargetEntity, TSourceField>
    {
        ITransformedSourceFieldDefinition<TTargetEntity, TTargetField> Then<TTargetField>(Func<TSourceField, TTargetField> transformation);
    }

    /// <summary>
    /// Publicly visible version of TransformedSourceFieldDefinition.
    /// </summary>
    public interface ITransformedSourceFieldDefinition<TTargetEntity, TTargetField>
        : ITargetFieldDefinition<TTargetEntity, TTargetField>
    {
    }

    public interface ITargetFieldDefinition<TTargetEntity, TTargetField>
    {
        /// <summary>
        /// Map to the target field. Note: implicit conversions in [targetField] to {TTargetField} are not allowed and will throw errors at runtime. You should add explicit casts if need be.
        /// </summary>
        IFinalFieldDefinition<TTargetField> To(Expression<Func<TTargetEntity, TTargetField>> targetField);
    }

    public interface IFinalFieldDefinition<out TTargetField>
    {
        void Stringify(Func<TTargetField, string> final);
    }
}
