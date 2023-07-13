using Microsoft.Extensions.Caching.Memory;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;

namespace Entities
{
    public class FormTool
    {
        private MemoryCache _cache;

        public FormTool(MyMemoryCache memoryCache)
        {
            _cache = memoryCache.Cache;
        }

        public Type GetFormCfg(string formName, DbDataReader result, bool hasIdentity = true, bool hasChildren = false) =>
            this.GetFormCfg (formName, result, new List<string>(), hasIdentity, hasChildren);  
        
        public Type GetFormCfg(string formName, DbDataReader result, List<string> fields, bool hasIdentity = true, bool hasChildren = false)
        {
            Type generatedType = null;
            //if (formName == "" || !_cache.TryGetValue("FormCfg_" + formName, out generatedType))
            if ("" == "")
            {
                var assemblyName = new AssemblyName();
                assemblyName.Name = "tmpAssembly";
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                var module = assemblyBuilder.DefineDynamicModule("tmpModule");
                var typeBuilder = module.DefineType("TypeFormCfg_" + formName, TypeAttributes.Public | TypeAttributes.Class);
                string identityName = "";
                IReadOnlyCollection<DbColumn> cols = result.GetColumnSchema();
                foreach (DbColumn col in cols.Where(c => fields.Count == 0 || fields.Contains(c.ColumnName.ToLower())))
                {
                    var tFieldType = GetNullableType(col.DataType);
                    var fieldBuilder = typeBuilder.DefineField("_" + col.ColumnName, tFieldType, FieldAttributes.Private);
                    var propertyBuilder = typeBuilder.DefineProperty(col.ColumnName, PropertyAttributes.None, tFieldType, new Type[] { tFieldType });
                    var GetSetAttr = MethodAttributes.Public | MethodAttributes.HideBySig;
                    if ((bool)col.IsAutoIncrement) identityName = col.ColumnName;
                    var currGetPropMethodBuilder = typeBuilder.DefineMethod("get_value", GetSetAttr, tFieldType, Type.EmptyTypes);
                    var currGetIL = currGetPropMethodBuilder.GetILGenerator();
                    currGetIL.Emit(OpCodes.Ldarg_0);
                    currGetIL.Emit(OpCodes.Ldfld, fieldBuilder);
                    currGetIL.Emit(OpCodes.Ret);
                    var currSetPropMethodBuilder = typeBuilder.DefineMethod("set_value", GetSetAttr, null, new Type[] { tFieldType });
                    var currSetIL = currSetPropMethodBuilder.GetILGenerator();
                    currSetIL.Emit(OpCodes.Ldarg_0);
                    currSetIL.Emit(OpCodes.Ldarg_1);
                    currSetIL.Emit(OpCodes.Stfld, fieldBuilder);
                    currSetIL.Emit(OpCodes.Ret);
                    propertyBuilder.SetGetMethod(currGetPropMethodBuilder);
                    propertyBuilder.SetSetMethod(currSetPropMethodBuilder);
                }
                //DynamicMethod idn = new DynamicMethod("identityName", typeof(string), null, module);
                //ILGenerator ilg = idn.GetILGenerator();

                if (hasIdentity)
                {
                    var tFieldType2 = typeof(string);
                    var fieldBuilder2 = typeBuilder.DefineField("_identityName", tFieldType2, FieldAttributes.Private);
                    fieldBuilder2.SetConstant(identityName);
                    var GetSetAttr2 = MethodAttributes.Public | MethodAttributes.HideBySig;
                    var currGetPropMethodBuilder2 = typeBuilder.DefineMethod("identityName", GetSetAttr2, typeof(string), Type.EmptyTypes);
                    var currGetIL2 = currGetPropMethodBuilder2.GetILGenerator();
                    currGetIL2.Emit(OpCodes.Ldarg_0);
                    currGetIL2.Emit(OpCodes.Ldfld, fieldBuilder2);
                    currGetIL2.Emit(OpCodes.Ret);
                    var currSetPropMethodBuilder2 = typeBuilder.DefineMethod("set_identityName", GetSetAttr2, null, new Type[] { typeof(string) });
                    var currSetIL2 = currSetPropMethodBuilder2.GetILGenerator();
                    currSetIL2.Emit(OpCodes.Ldarg_0);
                    currSetIL2.Emit(OpCodes.Ldarg_1);
                    currSetIL2.Emit(OpCodes.Stfld, fieldBuilder2);
                    currSetIL2.Emit(OpCodes.Ret);
                }

                if (hasChildren)
                {
                    List<object> children = new List<object>();
                    var tFieldType3 = GetNullableType(children.GetType());
                    var fieldBuilder3 = typeBuilder.DefineField("_children", tFieldType3, FieldAttributes.Private);
                    var propertyBuilder = typeBuilder.DefineProperty("__children", PropertyAttributes.None, tFieldType3, new Type[] { tFieldType3 });
                    var GetSetAttr3 = MethodAttributes.Public | MethodAttributes.HideBySig;
                    var currGetPropMethodBuilder3 = typeBuilder.DefineMethod("get_value", GetSetAttr3, tFieldType3, Type.EmptyTypes);
                    var currGetIL3 = currGetPropMethodBuilder3.GetILGenerator();
                    currGetIL3.Emit(OpCodes.Ldarg_0);
                    currGetIL3.Emit(OpCodes.Ldfld, fieldBuilder3);
                    currGetIL3.Emit(OpCodes.Ret);
                    var currSetPropMethodBuilder3 = typeBuilder.DefineMethod("set_value", GetSetAttr3, null, new Type[] { tFieldType3 });
                    var currSetIL3 = currSetPropMethodBuilder3.GetILGenerator();
                    currSetIL3.Emit(OpCodes.Ldarg_0);
                    currSetIL3.Emit(OpCodes.Ldarg_1);
                    currSetIL3.Emit(OpCodes.Stfld, fieldBuilder3);
                    currSetIL3.Emit(OpCodes.Ret);
                    propertyBuilder.SetGetMethod(currGetPropMethodBuilder3);
                    propertyBuilder.SetSetMethod(currSetPropMethodBuilder3);
                }

                generatedType = typeBuilder.CreateType();
                //if(formName != "") _cache.Set("FormCfg_" + formName, generatedType);
            }

            return generatedType;
        }

        /// <summary>
        /// [ <c>public static Type GetNullableType(Type TypeToConvert)</c> ]
        /// <para></para>
        /// Convert any Type to its Nullable&lt;T&gt; form, if possible
        /// </summary>
        /// <param name="TypeToConvert">The Type to convert</param>
        /// <returns>
        /// The Nullable&lt;T&gt; converted from the original type, the original type if it was already nullable, or null 
        /// if either <paramref name="TypeToConvert"/> could not be converted or if it was null.
        /// </returns>
        /// <remarks>
        /// To qualify to be converted to a nullable form, <paramref name="TypeToConvert"/> must contain a non-nullable value 
        /// type other than System.Void.  Otherwise, this method will return a null.
        /// </remarks>
        private static Type GetNullableType(Type TypeToConvert)
        {
            // Abort if no type supplied
            if (TypeToConvert == null)
                return null;

            // If the given type is already nullable, just return it
            if (IsTypeNullable(TypeToConvert))
                return TypeToConvert;

            // If the type is a ValueType and is not System.Void, convert it to a Nullable<Type>
            if (TypeToConvert.IsValueType && TypeToConvert != typeof(void))
                return typeof(Nullable<>).MakeGenericType(TypeToConvert);

            // Done - no conversion
            return null;
        }

        /// <summary>
        /// [ <c>public static bool IsTypeNullable(Type TypeToTest)</c> ]
        /// <para></para>
        /// Reports whether a given Type is nullable (Nullable&lt; Type &gt;)
        /// </summary>
        /// <param name="TypeToTest">The Type to test</param>
        /// <returns>
        /// true = The given Type is a Nullable&lt; Type &gt;; false = The type is not nullable, or <paramref name="TypeToTest"/> 
        /// is null.
        /// </returns>
        /// <remarks>
        /// This method tests <paramref name="TypeToTest"/> and reports whether it is nullable (i.e. whether it is either a 
        /// reference type or a form of the generic Nullable&lt; T &gt; type).
        /// </remarks>
        /// <seealso cref="GetNullableType"/>
        private static bool IsTypeNullable(Type TypeToTest)
        {
            // Abort if no type supplied
            if (TypeToTest == null)
                return false;

            // If this is not a value type, it is a reference type, so it is automatically nullable
            //  (NOTE: All forms of Nullable<T> are value types)
            if (!TypeToTest.IsValueType)
                return true;

            // Report whether an underlying Type exists (if it does, TypeToTest is a nullable Type)
            return Nullable.GetUnderlyingType(TypeToTest) != null;
        }

        //public Type GetFormCfg2(string formName, DbDataReader result)
        //{
        //    Type generatedType = null;
        //    if (formName == "" || !_cache.TryGetValue("FormCfg_" + formName, out generatedType))
        //    {
        //        var list = new Dictionary<string, string>();
        //        IReadOnlyCollection<DbColumn> cols = result.GetColumnSchema();
        //        int numCols = cols.Count;
        //        int colInd = 0;
        //        DynamicProperty[] props = new DynamicProperty[numCols];
        //        foreach (DbColumn col in cols)
        //        {
        //            list.Add(col.ColumnName, col.DataType.AssemblyQualifiedName.ToString());
        //            props[colInd] = new DynamicProperty(col.ColumnName, col.DataType);
        //            colInd++;
        //        }
        //        //IEnumerable<DynamicProperty> props = list.Select(property => new DynamicProperty(property.Key, Type.GetType(property.Value))).ToList();
        //        //generatedType = DynamicExpression.CreateClass(props);
        //    }
        //    return generatedType;
        //}
    }
}
