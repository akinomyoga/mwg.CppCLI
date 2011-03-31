namespace Reflector {
	using Reflector.CodeModel;
	using System;
	using System.Collections;
	using System.Globalization;
	using System.IO;

	public class Helper {
		private static int ii=0;

		public static IMethodDeclaration GetAddMethod(IEventReference value) {
			IEventDeclaration declaration2=value.Resolve();
			if(declaration2.AddMethod!=null) {
				return declaration2.AddMethod.Resolve();
			}
			return null;
		}

		public static IAssemblyReference GetAssemblyReference(IType value) {
			ITypeReference reference=value as ITypeReference;
			if(reference!=null) {
				ITypeReference owner=reference.Owner as ITypeReference;
				if(owner!=null) {
					return GetAssemblyReference(owner);
				}
				IModuleReference reference4=reference.Owner as IModuleReference;
				if(reference4!=null) {
					return reference4.Resolve().Assembly;
				}
				IAssemblyReference reference3=reference.Owner as IAssemblyReference;
				if(reference3!=null) {
					return reference3;
				}
			}
			throw new NotSupportedException();
		}

		public static ICollection GetEvents(ITypeDeclaration value,IVisibilityConfiguration visibility) {
			ArrayList list=new ArrayList(0);
			IEventDeclarationCollection events=value.Events;
			if(events.Count>0) {
				foreach(IEventDeclaration declaration in events) {
					if((visibility==null)||IsVisible(declaration,visibility)) {
						list.Add(declaration);
					}
				}
				list.Sort();
			}
			return list;
		}

		public static ICollection GetFields(ITypeDeclaration value,IVisibilityConfiguration visibility) {
			ArrayList list=new ArrayList(0);
			IFieldDeclarationCollection fields=value.Fields;
			if(fields.Count>0) {
				foreach(IFieldDeclaration declaration in fields) {
					if((visibility==null)||IsVisible(declaration,visibility)) {
						list.Add(declaration);
					}
				}
				list.Sort();
			}
			return list;
		}

		public static IMethodDeclaration GetGetMethod(IPropertyReference value) {
			IPropertyDeclaration declaration2=value.Resolve();
			if(declaration2.GetMethod!=null) {
				return declaration2.GetMethod.Resolve();
			}
			return null;
		}

		public void GetHelp(IBlockExpression iBlockExpression) {
			ii++;
		}

		private static ICollection GetInterfaces(ITypeDeclaration value) {
			ArrayList list=new ArrayList(0);
			list.AddRange(value.Interfaces);
			if(value.BaseType!=null) {
				ITypeDeclaration declaration2=value.BaseType.Resolve();
				foreach(ITypeReference reference in declaration2.Interfaces) {
					if(list.Contains(reference)) {
						list.Remove(reference);
					}
				}
			}
			foreach(ITypeReference reference in value.Interfaces) {
				ITypeDeclaration declaration=reference.Resolve();
				foreach(ITypeReference reference2 in declaration.Interfaces) {
					if(list.Contains(reference2)) {
						list.Remove(reference2);
					}
				}
			}
			ITypeReference[] array=new ITypeReference[list.Count];
			list.CopyTo(array,0);
			return array;
		}

		public static ICollection GetInterfaces(ITypeDeclaration value,IVisibilityConfiguration visibility) {
			ArrayList list=new ArrayList(0);
			foreach(ITypeReference reference in GetInterfaces(value)) {
				if(IsVisible(reference,visibility)) {
					list.Add(reference);
				}
			}
			list.Sort();
			return list;
		}

		public static IMethodDeclaration GetInvokeMethod(IEventReference value) {
			IEventDeclaration declaration2=value.Resolve();
			if(declaration2.InvokeMethod!=null) {
				return declaration2.InvokeMethod.Resolve();
			}
			return null;
		}

		public static IMethodDeclaration GetMethod(ITypeDeclaration value,string methodName) {
			IMethodDeclarationCollection methods=value.Methods;
			for(int i=0;i<methods.Count;i++) {
				if(methodName==methods[i].Name) {
					return methods[i];
				}
			}
			return null;
		}

		public static ICollection GetMethods(ITypeDeclaration value,IVisibilityConfiguration visibility) {
			ArrayList list=new ArrayList(0);
			IMethodDeclarationCollection methods=value.Methods;
			if(methods.Count>0) {
				foreach(IMethodDeclaration declaration3 in methods) {
					if((visibility==null)||IsVisible(declaration3,visibility)) {
						list.Add(declaration3);
					}
				}
				foreach(IPropertyDeclaration declaration2 in value.Properties) {
					if(declaration2.SetMethod!=null) {
						list.Remove(declaration2.SetMethod.Resolve());
					}
					if(declaration2.GetMethod!=null) {
						list.Remove(declaration2.GetMethod.Resolve());
					}
				}
				foreach(IEventDeclaration declaration in value.Events) {
					if(declaration.AddMethod!=null) {
						list.Remove(declaration.AddMethod.Resolve());
					}
					if(declaration.RemoveMethod!=null) {
						list.Remove(declaration.RemoveMethod.Resolve());
					}
					if(declaration.InvokeMethod!=null) {
						list.Remove(declaration.InvokeMethod.Resolve());
					}
				}
				list.Sort();
			}
			return list;
		}

		public string GetName(IEventReference value) {
			return value.Name;
		}

		public string GetName(IFieldReference value) {
			IType fieldType=value.FieldType;
			IType declaringType=value.DeclaringType;
			if(fieldType.Equals(declaringType)) {
				ITypeReference reference=fieldType as ITypeReference;
				if((reference!=null)&&this.IsEnumeration(reference)) {
					return value.Name;
				}
			}
			return (value.Name+" : "+value.FieldType.ToString());
		}

		public string GetName(IMethodReference value) {
			ITypeCollection genericArguments=value.GenericArguments;
			if(genericArguments.Count>0) {
				using(StringWriter writer=new StringWriter(CultureInfo.InvariantCulture)) {
					for(int i=0;i<genericArguments.Count;i++) {
						if(i!=0) {
							writer.Write(", ");
						}
						IType type=genericArguments[i];
						if(type!=null) {
							writer.Write(type.ToString());
						} else {
							writer.Write("???");
						}
					}
					return (value.Name+"<"+writer.ToString()+">");
				}
			}
			return value.Name;
		}

		public string GetName(IPropertyReference value) {
			IParameterDeclarationCollection parameters=value.Parameters;
			if(parameters.Count>0) {
				using(StringWriter writer=new StringWriter(CultureInfo.InvariantCulture)) {
					for(int i=0;i<parameters.Count;i++) {
						if(i!=0) {
							writer.Write(", ");
						}
						writer.Write(parameters[i].ParameterType.ToString());
					}
					return (value.Name+"["+writer.ToString()+"] : "+value.PropertyType.ToString());
				}
			}
			return (value.Name+" : "+value.PropertyType.ToString());
		}

		public string GetName(ITypeReference value) {
			if(value==null) {
				throw new NotSupportedException();
			}
			ITypeCollection genericArguments=value.GenericArguments;
			if(genericArguments.Count>0) {
				using(StringWriter writer=new StringWriter(CultureInfo.InvariantCulture)) {
					for(int i=0;i<genericArguments.Count;i++) {
						if(i!=0) {
							writer.Write(",");
						}
						IType iType=genericArguments[i];
						if(iType!=null) {
							this.TypeWriter(writer,iType);
						}
					}
					return (value.Name+"<"+writer.ToString()+">");
				}
			}
			return value.Name;
		}

		public string GetNameWithDeclaringType(IEventReference value) {
			return (this.GetNameWithResolutionScope(value.DeclaringType as ITypeReference)+"."+this.GetName(value));
		}

		public string GetNameWithDeclaringType(IFieldReference value) {
			return (this.GetNameWithResolutionScope(value.DeclaringType as ITypeReference)+"."+this.GetName(value));
		}

		public string GetNameWithDeclaringType(IMethodReference value) {
			ITypeReference declaringType=value.DeclaringType as ITypeReference;
			if(declaringType!=null) {
				return (this.GetNameWithResolutionScope(declaringType)+"."+this.GetNameWithParameterList(value));
			}
			IArrayType type=value.DeclaringType as IArrayType;
			if(type==null) {
				throw new NotSupportedException();
			}
			return (type.ToString()+"."+this.GetNameWithParameterList(value));
		}

		public string GetNameWithDeclaringType(IPropertyReference value) {
			return (this.GetNameWithResolutionScope(value.DeclaringType as ITypeReference)+"."+this.GetName(value));
		}

		public string GetNameWithParameterList(IMethodReference value) {
			using(StringWriter writer=new StringWriter(CultureInfo.InvariantCulture)) {
				writer.Write(this.GetName(value));
				writer.Write("(");
				IParameterDeclarationCollection parameters=value.Parameters;
				for(int i=0;i<parameters.Count;i++) {
					if(i!=0) {
						writer.Write(", ");
					}
					writer.Write(parameters[i].ParameterType.ToString());
				}
				if(value.CallingConvention==MethodCallingConvention.VariableArguments) {
					if(value.Parameters.Count>0) {
						writer.Write(", ");
					}
					writer.Write("...");
				}
				writer.Write(")");
				if((value.Name!=".ctor")&&(value.Name!=".cctor")) {
					writer.Write(" : ");
					writer.Write(value.ReturnType.Type.ToString());
				}
				return writer.ToString();
			}
		}

		public string GetNameWithResolutionScope(ITypeReference value) {
			if(value==null) {
				throw new NotSupportedException();
			}
			ITypeReference owner=value.Owner as ITypeReference;
			if(owner!=null) {
				return (this.GetNameWithResolutionScope(owner)+"+"+this.GetName(value));
			}
			string str2=value.Namespace;
			if(str2.Length==0) {
				return this.GetName(value);
			}
			return (str2+"."+this.GetName(value));
		}

		public static ICollection GetNestedTypes(ITypeDeclaration value,IVisibilityConfiguration visibility) {
			ArrayList list=new ArrayList(0);
			ITypeDeclarationCollection nestedTypes=value.NestedTypes;
			if(nestedTypes.Count>0) {
				foreach(ITypeDeclaration declaration in nestedTypes) {
					if(IsVisible(declaration,visibility)) {
						list.Add(declaration);
					}
				}
				list.Sort();
			}
			return list;
		}

		public static ICollection GetProperties(ITypeDeclaration value,IVisibilityConfiguration visibility) {
			ArrayList list=new ArrayList(0);
			IPropertyDeclarationCollection properties=value.Properties;
			if(properties.Count>0) {
				foreach(IPropertyDeclaration declaration in properties) {
					if((visibility==null)||IsVisible(declaration,visibility)) {
						list.Add(declaration);
					}
				}
				list.Sort();
			}
			return list;
		}

		public static IMethodDeclaration GetRemoveMethod(IEventReference value) {
			IEventDeclaration declaration2=value.Resolve();
			if(declaration2.RemoveMethod!=null) {
				return declaration2.RemoveMethod.Resolve();
			}
			return null;
		}

		public string GetResolutionScope(ITypeReference value) {
			IModule owner=value.Owner as IModule;
			if(owner!=null) {
				return value.Namespace;
			}
			ITypeDeclaration declaration=value.Owner as ITypeDeclaration;
			if(declaration==null) {
				throw new NotSupportedException();
			}
			return (this.GetResolutionScope(declaration)+"+"+this.GetName(declaration));
		}

		public static IMethodDeclaration GetSetMethod(IPropertyReference value) {
			IPropertyDeclaration declaration2=value.Resolve();
			if(declaration2.SetMethod!=null) {
				return declaration2.SetMethod.Resolve();
			}
			return null;
		}

		public static MethodVisibility GetVisibility(IEventReference value) {
			IMethodDeclaration addMethod=GetAddMethod(value);
			IMethodDeclaration removeMethod=GetRemoveMethod(value);
			IMethodDeclaration invokeMethod=GetInvokeMethod(value);
			if(((addMethod!=null)&&(removeMethod!=null))&&(invokeMethod!=null)) {
				if((addMethod.Visibility==removeMethod.Visibility)&&(addMethod.Visibility==invokeMethod.Visibility)) {
					return addMethod.Visibility;
				}
			} else if((addMethod!=null)&&(removeMethod!=null)) {
				if(addMethod.Visibility==removeMethod.Visibility) {
					return addMethod.Visibility;
				}
			} else if((addMethod!=null)&&(invokeMethod!=null)) {
				if(addMethod.Visibility==invokeMethod.Visibility) {
					return addMethod.Visibility;
				}
			} else if((removeMethod!=null)&&(invokeMethod!=null)) {
				if(removeMethod.Visibility==invokeMethod.Visibility) {
					return removeMethod.Visibility;
				}
			} else {
				if(addMethod!=null) {
					return addMethod.Visibility;
				}
				if(removeMethod!=null) {
					return removeMethod.Visibility;
				}
				if(invokeMethod!=null) {
					return invokeMethod.Visibility;
				}
			}
			return MethodVisibility.Public;
		}

		public static MethodVisibility GetVisibility(IPropertyReference value) {
			IMethodDeclaration getMethod=GetGetMethod(value);
			IMethodDeclaration setMethod=GetSetMethod(value);
			MethodVisibility @public=MethodVisibility.Public;
			if((setMethod!=null)&&(getMethod!=null)) {
				if(getMethod.Visibility==setMethod.Visibility) {
					@public=getMethod.Visibility;
				}
				return @public;
			}
			if(setMethod!=null) {
				return setMethod.Visibility;
			}
			if(getMethod!=null) {
				@public=getMethod.Visibility;
			}
			return @public;
		}

		public bool IsDelegate(ITypeReference value) {
			if(value!=null) {
				if((value.Name=="MulticastDelegate")&&(value.Namespace=="System")) {
					return false;
				}
				ITypeDeclaration declaration=value.Resolve();
				if(declaration==null) {
					return false;
				}
				ITypeReference baseType=declaration.BaseType;
				return ((((baseType!=null)&&(baseType.Namespace=="System"))&&((baseType.Name=="MulticastDelegate")||(baseType.Name=="Delegate")))&&(baseType.Namespace=="System"));
			}
			return false;
		}

		public bool IsEnumeration(ITypeReference value) {
			if(value!=null) {
				ITypeDeclaration declaration=value.Resolve();
				if(declaration==null) {
					return false;
				}
				ITypeReference baseType=declaration.BaseType;
				return (((baseType!=null)&&(baseType.Name=="Enum"))&&(baseType.Namespace=="System"));
			}
			return false;
		}

		public bool IsObject(ITypeReference value) {
			return true;
		}

		public static bool IsStatic(IEventReference value) {
			bool flag=false;
			if(GetAddMethod(value)!=null) {
				flag|=GetAddMethod(value).Static;
			}
			if(GetRemoveMethod(value)!=null) {
				flag|=GetRemoveMethod(value).Static;
			}
			if(GetInvokeMethod(value)!=null) {
				flag|=GetInvokeMethod(value).Static;
			}
			return flag;
		}

		public static bool IsStatic(IPropertyReference value) {
			IMethodDeclaration setMethod=GetSetMethod(value);
			IMethodDeclaration getMethod=GetGetMethod(value);
			bool flag=false;
			flag|=(setMethod!=null)&&setMethod.Static;
			return (flag|((getMethod!=null)&&getMethod.Static));
		}

		public bool IsValueType(ITypeReference value) {
			if(value!=null) {
				ITypeDeclaration declaration=value.Resolve();
				if(declaration==null) {
					return false;
				}
				ITypeReference baseType=declaration.BaseType;
				return (((baseType!=null)&&((baseType.Name=="ValueType")||(baseType.Name=="Enum")))&&(baseType.Namespace=="System"));
			}
			return false;
		}

		public static bool IsVisible(IEventReference value,IVisibilityConfiguration visibility) {
			if(IsVisible(value.DeclaringType,visibility)) {
				switch(GetVisibility(value)) {
					case MethodVisibility.PrivateScope:
					case MethodVisibility.Private:
						return visibility.Private;

					case MethodVisibility.FamilyAndAssembly:
						return visibility.FamilyAndAssembly;

					case MethodVisibility.Assembly:
						return visibility.Assembly;

					case MethodVisibility.Family:
						return visibility.Family;

					case MethodVisibility.FamilyOrAssembly:
						return visibility.FamilyOrAssembly;

					case MethodVisibility.Public:
						return visibility.Public;
				}
				throw new NotSupportedException();
			}
			return false;
		}

		public static bool IsVisible(IFieldReference value,IVisibilityConfiguration visibility) {
			if(IsVisible(value.DeclaringType,visibility)) {
				IFieldDeclaration declaration=value.Resolve();
				if(declaration==null) {
					return true;
				}
				switch(declaration.Visibility) {
					case FieldVisibility.PrivateScope:
						return visibility.Private;

					case FieldVisibility.Private:
						return visibility.Private;

					case FieldVisibility.FamilyAndAssembly:
						return visibility.FamilyAndAssembly;

					case FieldVisibility.Assembly:
						return visibility.Assembly;

					case FieldVisibility.Family:
						return visibility.Family;

					case FieldVisibility.FamilyOrAssembly:
						return visibility.FamilyOrAssembly;

					case FieldVisibility.Public:
						return visibility.Public;
				}
				throw new NotSupportedException();
			}
			return false;
		}

		public static bool IsVisible(IMethodReference value,IVisibilityConfiguration visibility) {
			if(IsVisible(value.DeclaringType,visibility)) {
				switch(value.Resolve().Visibility) {
					case MethodVisibility.PrivateScope:
					case MethodVisibility.Private:
						return visibility.Private;

					case MethodVisibility.FamilyAndAssembly:
						return visibility.FamilyAndAssembly;

					case MethodVisibility.Assembly:
						return visibility.Assembly;

					case MethodVisibility.Family:
						return visibility.Family;

					case MethodVisibility.FamilyOrAssembly:
						return visibility.FamilyOrAssembly;

					case MethodVisibility.Public:
						return visibility.Public;
				}
				throw new NotSupportedException();
			}
			return false;
		}

		public static bool IsVisible(IPropertyReference value,IVisibilityConfiguration visibility) {
			if(IsVisible(value.DeclaringType,visibility)) {
				switch(GetVisibility(value)) {
					case MethodVisibility.PrivateScope:
					case MethodVisibility.Private:
						return visibility.Private;

					case MethodVisibility.FamilyAndAssembly:
						return visibility.FamilyAndAssembly;

					case MethodVisibility.Assembly:
						return visibility.Assembly;

					case MethodVisibility.Family:
						return visibility.Family;

					case MethodVisibility.FamilyOrAssembly:
						return visibility.FamilyOrAssembly;

					case MethodVisibility.Public:
						return visibility.Public;
				}
				throw new NotSupportedException();
			}
			return false;
		}

		public static bool IsVisible(IType value,IVisibilityConfiguration visibility) {
			ITypeReference reference=value as ITypeReference;
			if(reference==null) {
				throw new NotSupportedException();
			}
			ITypeReference owner=reference.Owner as ITypeReference;
			if((owner!=null)&&!IsVisible(owner,visibility)) {
				return false;
			}
			ITypeDeclaration declaration=reference.Resolve();
			if(declaration==null) {
				return true;
			}
			switch(declaration.Visibility) {
				case TypeVisibility.Private:
				case TypeVisibility.NestedPrivate:
					return visibility.Private;

				case TypeVisibility.Public:
				case TypeVisibility.NestedPublic:
					return visibility.Public;

				case TypeVisibility.NestedFamily:
					return visibility.Family;

				case TypeVisibility.NestedAssembly:
					return visibility.Assembly;

				case TypeVisibility.NestedFamilyAndAssembly:
					return visibility.FamilyAndAssembly;

				case TypeVisibility.NestedFamilyOrAssembly:
					return visibility.FamilyOrAssembly;
			}
			throw new NotImplementedException();
		}

		public virtual void TypeWriter(StringWriter writer,IType iType) {
			writer.Write(iType.ToString());
		}
	}
}

