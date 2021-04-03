using System;
using System.Linq;
using System.Linq.Expressions;

namespace AutoConverter.BusinessObjects.Core.BusinessEntities.V2
{
    #region Imports

    #endregion

    static class ExpressionHelper
    {
        public static Action<TEntity, TField> CreateFieldSetter<TEntity, TField>(Expression<Func<TEntity, TField>> field)
        {
            // only simple properties are supported
            if (!(field.Body is MemberExpression))
            {
                throw new Exception(
                    $"Only simple properties are supported; the body of '{field}' is not a MemberExpression.");
            }

            var simplePropertyNameAggregator = new SimplePropertyNameAggregator();
            simplePropertyNameAggregator.Visit(field.Body, false);

            // basically we are construction an action "(entity, value) => entity.SomeProperty = value"
            var entityParam = Expression.Parameter(typeof(TEntity), "entity");
            var inputParam = Expression.Parameter(typeof(TField), "value");

            var memberExpression = simplePropertyNameAggregator.PropertyMetaData.PropertyInfoList
                .Select(z => z.Name).Reverse().Aggregate<string, Expression>(entityParam, Expression.PropertyOrField);

            var binaryExpression = Expression.Assign(memberExpression, inputParam);

            var lambda = Expression.Lambda<Action<TEntity, TField>>(binaryExpression, entityParam, inputParam);
            var setter = lambda.Compile();

            return setter;
        }
    }
}
