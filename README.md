# AutoMappingConverter
An automatic entity mapper and/or converter that supports mapping definition at each field level via Lambda expression

GitHub repository:
<https://github.com/williamjingjiefeng/AutoConverter>

It features:

•	If you define a mapping between two classes of CustomerResult and Customer as follows:
	
	var def = EntityMappingDefinition<CustomerResult, Customer>("Customer");
	
	with the mapping at the field level chained up together as follows:

	def.From(z => z.YearsWithUs).Then(GetLoyalty).To(z => z.Loyalty);

	Once you call a def.Convert() on one CustomerResult object, we will return you an auto mapped Customer object. Entry point is Program.cs

•	As you can see, all mappings are strongly typed with Lambda expression, elimination of \<object\> generic parameters has been endorsed.

•	Compact "fluent" mapping behaviour.

•	Restricted API: all the internals are private, only a small number of public methods are available. It means the API surface is simple 
	even though there is complexity underneath.

•	Maintain difficulty level of the code – there are still tons of generic type parameters happening.

•	You can use this library for copying as well if you make From type as To type

Use Cases:

•	Maintain and enforce application level consistent object mapping.

•	Emphasize the immutability of some mission critical application constructs for the performance gain

Implementation details are explained as follows:

1. 	ISourceFieldDefinition is of ITargetFieldDefinition as well because:

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
		
	As you can see, mapping needs to call To() method on the source field definition, note in this case, TSourceField will be the same as TTargetField. 
	This could be either the case that TSourceField and TTargetField are of same type, or after Then() method is called on ISourceFieldDefinition. 
	It is valid to have a common base interface to both ISourceFieldDefinition and ITransformedSourceFieldDefinition, which would contain their common .To() 
	method, but it should be separate from IFinalFieldDefinition. This would reduce the need to check and throw exceptions for invalid Stringify() combinations. 
	With this arrangement, some unexpected method chains are impossible, e.g. .From().To().To(), or .From().Stringify(). Ideally .To() would be available from 
	ISourceFieldDefinintion and ITransformedSourceFieldDefinition but not from IFinalFieldDefinition, and Stringify() should only be available from 
	IFinalFieldDefinition but not the other two. 
	
2.	When Then() method is called, we create a new TransformedSourceFieldDefinition object, and add it into the hashset of IFieldMappingDefinition. As you can 
	see, TransformedSourceFieldDefinition is defined as follows:

        class TransformedSourceFieldDefinition<TSourceField, TTargetField> :
            ITransformedSourceFieldDefinition<TTargetEntity, TTargetField>,
            IFieldMappingDefinition,
            IFinalFieldDefinition<TTargetField>
        {
            readonly Func<TSourceEntity, TTargetField> combinedGetter;
            readonly Action<IFieldMappingDefinition, IFieldMappingDefinition> replaceWithFinal;
            CompleteFieldMapping<TTargetField> final;

            public TransformedSourceFieldDefinition(Func<TSourceEntity, TSourceField> sourceFieldGetter, Func<TSourceField, TTargetField> transformation, 
				Action<IFieldMappingDefinition, IFieldMappingDefinition> replaceWithFinal)
            {
                combinedGetter = z => transformation(sourceFieldGetter(z));
                this.replaceWithFinal = replaceWithFinal;
            }
		}
		
	The beauty of this is we apply transformation to get a combined getter with the type Func<TSourceEntity, TTargetField> in the constructor, when To() 
	method is called on this TransformedSourceFieldDefinition:
	
		IFinalFieldDefinition<TTargetField> ITargetFieldDefinition<TTargetEntity, TTargetField>.To(Expression<Func<TTargetEntity, TTargetField>> targetField)
            {
                final = new CompleteFieldMapping<TTargetField>(combinedGetter, targetField);
                replaceWithFinal(this, final);

                return this;
            }
			
	We will obtain a CompleteFieldMapping in the same way as SourceFieldDefinition. The essence at here is sourceFieldGetter has to be 
	Func<TSourceEntity, TTargetField> in CompleteFieldMapping. The reason that TransformedSourceFieldDefinition has to implement To() is 
	becuase its inetrface ITransformedSourceFieldDefinition extends ITargetFieldDefinition, which has defined To() method. Please note:
	
	From() 	   ==> 	defined in EntityMappingDefinition<TSourceEntity, TTargetEntity> as ISourceFieldDefinition<TTargetEntity, TSourceField> 
					From<TSourceField>(Expression<Func<TSourceEntity, TSourceField>> sourceField)
	
	Then() 	   ==> 	defined in ISourceFieldDefinition<TTargetEntity, TSourceField> as ITransformedSourceFieldDefinition<TTargetEntity, TTargetField> 
					Then<TTargetField>(Func<TSourceField, TTargetField> transformation);
	
	To()   	   ==> 	defined in ITargetFieldDefinition<TTargetEntity, TTargetField> as IFinalFieldDefinition<TTargetField> 
					To(Expression<Func<TTargetEntity, TTargetField>> targetField);
	
	Stringify() ==>	defined in IFinalFieldDefinition<out TTargetField> as void Stringify(Func<TTargetField, string> final);
	
	Why we have to return ISourceFieldDefinition<TTargetEntity, TSourceField> from "From()" is because type parameter TTargetEntity is needed in To() method.
	
3.	SourceFieldDefinition vs TransformedSourceFieldDefinition

	final = new CompleteFieldMapping<TSourceField>(sourceFieldGetter, targetField);
	final = new CompleteFieldMapping<TTargetField>(combinedGetter, targetField);
	
	    class CompleteFieldMapping<TTargetField> : IFieldMappingDefinition, IFinalFieldMapping<TTargetField>
        {
            readonly Func<TSourceEntity, TTargetField> sourceFieldGetter;
            readonly Expression<Func<TTargetEntity, TTargetField>> targetField;
            Func<TTargetField, string> stringify;

            public CompleteFieldMapping(Func<TSourceEntity, TTargetField> sourceFieldGetter, Expression<Func<TTargetEntity, TTargetField>> targetField)
            {
			}
		}
		
	You can see:
		for SourceFieldDefinition: 				TSourceField is the same as TTargetField, sourceFieldGetter is of type Func<TSourceEntity, TSourceField>,
												targetField is of Expression<Func<TTargetEntity, TSourceField>>
		for TransformedSourceFieldDefinition: 	TTargetField is the same as TTargetField, combinedGetter is of type Func<TSourceEntity, TTargetField>, 
												targetField is of Expression<Func<TTargetEntity, TTargetField>>

4. 	When Convert() is called on EntityMappingDefinition, we will do the following two things:

	4.1	Firstly we call Compile() to create an EntityMappingPerformer class object, which will host all field mappings, and create a list of 
		field mapping performers for each field mapping in the hash set via compiling. Inside FieldMappingPerformer, the most important thing is creating 
		targetFieldSetter.
	4.2	Then we will call Convert() on the newly created EntityMappingPerformer object
	
5.	What this targetFieldSetter(target, sourceValue) in the method Apply(TSourceEntity source, TTargetEntity target) does is assigning source field's value 
	to target entity's corresponding field:

	    public static Action<TEntity, TField> CreateFieldSetter<TEntity, TField>(Expression<Func<TEntity, TField>> field)
        {
            // only simple properties are supported
            var memberExpr = field.Body as MemberExpression;
            if (memberExpr == null)
            {
                throw new Exception(string.Format("Only simple properties are supported; the body of '{0}' is not a MemberExpression.", field));
            }

            var simplePropertyNameAggregator = new SimplePropertyNameAggregator();
            simplePropertyNameAggregator.Visit(field.Body, false);

            // basically we are constructing an action "(entity, value) => entity.SomeProperty = value"
            var entityParam = Expression.Parameter(typeof(TEntity), "entity");
            var inputParam = Expression.Parameter(typeof(TField), "value");

			// this will make field.body of z => z.Preference.Hobby to member expression of {($entity.Preference).Hobby}, note PropertyInfoList[0] 
			// is Hobby property info and PropertyInfoList[1] is Preference property info, which is in the reverse order. Note we are supporting multiple 
			// layers of member expressions 
            var memberExpression = simplePropertyNameAggregator.PropertyMetaData.PropertyInfoList
                .Select(z => z.Name).Reverse().Aggregate<string, Expression>(entityParam, Expression.PropertyOrField);

            var binaryExpression = Expression.Assign(memberExpression, inputParam);

            var lambda = Expression.Lambda<Action<TEntity, TField>>(binaryExpression, entityParam, inputParam);
            var setter = lambda.Compile();

            return setter;
        }
		
	input field is of type Expression<Func<TTargetEntity, TTargetField>>. When the field body is as follows:
	
	field.Body
		{z.Preference.Hobby}
			CanReduce: false
			DebugView: "($z.Preference).Hobby"
			Expression: {z.Preference}
			Member: {System.String Hobby}
			NodeType: MemberAccess
			Type: {Name = "String" FullName = "System.String"}
			
	simplePropertyNameAggregator.PropertyMetaData.PropertyInfoList.Select(z => z.Name).Reverse().Aggregate<string, Expression>
	(entityParam, Expression.PropertyOrField) will return member expression as follows:
	
	memberExpression
		{entity.Preference.Hobby}
			CanReduce: false
			DebugView: "($entity.Preference).Hobby"
			Expression: {entity.Preference}
			Member: {System.String Hobby}
			NodeType: MemberAccess
			Type: {Name = "String" FullName = "System.String"}
			
	public static TAccumulate Aggregate<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func);
	public static MemberExpression PropertyOrField(Expression expression, string propertyOrFieldName);
	  
6.	Now we are hosting this sourceFieldGetter and targetFieldSetter in FieldMappingPerformer, when Convert() is called on EntityMappingPerformer, each 
	FieldMappingPerformer is looped and Apply() method is called, during which we do the following things to complete the conversion:

		var sourceValue = sourceFieldGetter(source);
		targetFieldSetter(target, sourceValue);
		
7.	Stringify() is supported in IFinalFieldDefinition, which will be passed to FieldMappingPerformer from EntityMappingPerformer during the course of compiling 
	CompleteFieldMapping as follows:

	7.1 EntityMappingDefinition --> Convert()
	7.2	Inside Convert(), EntityMappingDefinition --> Compile() --> Generate EntityMappingPerformer class with fieldMappings
	7.3	EntityMappingPerformer --> generate a list of FieldMappingPerformer
	7.4	EntityMappingPerformer --> Convert()
	7.5 Loop through its FieldMappingPerformers and perform Apply() on each of them
	
	Note we can't compile a field when only From() and Then() have been defined. We have to chain a call to To() after defining this field. Only 
	SourceFieldDefinition and TransformedSourceFieldDefinition have implemented IFinalFieldDefinition, which will call final.SetupStringifyFunc(stringify). 
	Final is of type CompleteFieldMapping.
	
