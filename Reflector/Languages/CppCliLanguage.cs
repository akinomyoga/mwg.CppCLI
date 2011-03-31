using mwg.Reflector.CppCli;
using Reflector;
using Reflector.CodeModel;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Gen=System.Collections.Generic;

namespace Reflector.Languages {

	public partial class CppCliLanguage:ILanguage {
		public virtual ILanguageWriter GetWriter(IFormatter formatter,ILanguageWriterConfiguration configuration) {
			return new LanguageWriter(formatter,configuration);
		}

		public virtual string FileExtension{
			get{return "cpp";}
		}

		public virtual string Name{
			get{return "C++/CLI [茗]";}
		}

		public virtual bool Translate{get{return true;}}

		public partial class LanguageWriter:Helper,ILanguageWriter{
			private IFormatter formatter;
			private ILanguageWriterConfiguration configuration;
			internal Scope scope;

#if EXTRA_TEMP
			private int Block;
			private int NextBlock;
			private int SkipNullptrCount;
			internal bool[] EssentialTemporaries;
			internal string[] ExtraMappings;
			internal string[] ExtraTemporaries;
			private int[] TemporaryBlocks;
#endif
			internal TypeRef baseType;
			internal bool SkipWriteLine;
			internal bool SomeConstructor;
			internal bool SuppressOutput=false;

			public LanguageWriter(IFormatter formatter,ILanguageWriterConfiguration configuration) {
				this.formatter=formatter;
				this.configuration=configuration;
			}

			#region Primitive Write
			//===========================================================
			//		基本の書込
			//===========================================================

			internal void Write(string s) {
				if(!this.SuppressOutput) {
					this.formatter.Write(s);
				}
			}

			internal void WriteComment(string s) {
				if(!this.SuppressOutput) {
					this.formatter.WriteComment(s);
				}
			}

			internal void WriteDeclaration(string s) {
				if(!this.SuppressOutput) {
					this.formatter.WriteDeclaration(s);
				}
			}
			internal void WriteDeclaration(string s,object target) {
				if(!this.SuppressOutput) {
					this.formatter.WriteDeclaration(s,target);
				}
			}

			internal void WriteIndent() {
				if(!this.SuppressOutput) {
					this.formatter.WriteIndent();
				}
			}

			internal void WriteKeyword(string s) {
				if(!this.SuppressOutput) {
					this.formatter.WriteKeyword(s);
				}
			}

			internal void WriteLiteral(string s) {
				if(!this.SuppressOutput) {
					this.formatter.WriteLiteral(s);
				}
			}

			internal void WriteLine() {
				if(!this.SuppressOutput) {
					this.formatter.WriteLine();
				}
			}

			internal void WriteOutdent() {
				if(!this.SuppressOutput) {
					this.formatter.WriteOutdent();
				}
			}

			private void WriteProperty(string s,string t) {
				if(!this.SuppressOutput) {
					this.formatter.WriteProperty(s,t);
				}
			}

			internal void WriteReference(string s,string t,object o) {
				if(this.SuppressOutput) {
					return;
				}
#if FALSE
#warning WriteReference: 何故茲で内容を書き換える必要があるのか?
				string[] strArray2=new string[] { "System::Runtime::InteropServices::","System::" };
				for(int index=0;index<strArray2.Length;index++) {
					string str=strArray2[index];
					if(s.StartsWith(str)) {
						s=s.Remove(0,str.Length);
						break;
					}
				}
#endif
				this.formatter.WriteReference(s,t,o);
			}
			#endregion

			public void PushScope(){
				Scope.Push(ref this.scope);
			}
			public void PopScope(){
				Scope.Pop(ref this.scope);
			}

#if EXTRA_TEMP
			/// <summary>
			/// statementCollection の先頭の T var=nullptr; を調べます。
			/// </summary>
			/// <param name="iStatementCollection"></param>
			/// <returns></returns>
			private int GatherExtraTemporaries(IStatementCollection iStatementCollection) {
				if(this.ExtraTemporaries==null){
					Gen::List<string> list=new System.Collections.Generic.List<string>();

					foreach(IStatement state in iStatementCollection){
						// T var=nullptr;
						IExpressionStatement state_exp=state as IExpressionStatement;
						if(state_exp==null)break;

						IAssignExpression exp_assign=state_exp.Expression as IAssignExpression;
						if(exp_assign==null)break;

						IVariableDeclarationExpression exp_var=exp_assign.Target as IVariableDeclarationExpression;
						if(exp_var==null)break;

						ILiteralExpression exp_lit=exp_assign.Expression as ILiteralExpression;
						if(exp_lit==null||exp_lit.Value!=null)break;

						list.Add(exp_var.Variable.Name);
					}

					int count=list.Count;

					this.SkipNullptrCount=count;
					this.ExtraTemporaries=list.ToArray();
					this.ExtraMappings=new string[count];
					this.EssentialTemporaries=new bool[count];
					this.TemporaryBlocks=new int[count];
				}
				return this.SkipNullptrCount;
			}

			private string MapTemporaryName(string s) {
				if(this.ExtraTemporaries!=null) {
					for(int i=0;i<this.ExtraTemporaries.Length;i++) {
						if(!this.EssentialTemporaries[i]&&s==this.ExtraTemporaries[i]&&this.VerifyCorrectBlock(i)) {
							if(this.ExtraMappings[i]!=null) {
								return this.ExtraMappings[i];
							}
							this.EssentialTemporaries[i]=true;
							return "mapping error";
						}
					}
				}
				return s;
			}

			/// <summary>
			/// 初めに呼ばれたブロックを記録して、
			/// それ以外のブロックにも存在する事が分かった際には essential とマークして false。
			/// </summary>
			/// <param name="i"></param>
			/// <returns></returns>
			internal bool VerifyCorrectBlock(int i) {
				if(this.SuppressOutput){
					if(this.TemporaryBlocks[i]==0){
						this.TemporaryBlocks[i]=this.Block;
					}else if(this.TemporaryBlocks[i]!=this.Block){
						this.EssentialTemporaries[i]=true;
						return false;
					}
				}
				return true;
			}
#endif

			public static bool Type(IType type,string typeNamespace,string typeName){
				ITypeReference reference=type as ITypeReference;
				if(reference!=null) {
					return (reference.Namespace==typeNamespace)&&(reference.Name==typeName);
				}
				return false;
			}

			public virtual void WriteAssembly(IAssembly assembly) {
				this.Write("// Assembly");
				this.Write(" ");
				this.WriteDeclaration(assembly.Name);
				if(assembly.Version!=null) {
					this.Write(", ");
					this.Write("Version");
					this.Write(" ");
					this.Write(assembly.Version.ToString());
				}
				this.WriteLine();
				if(this.configuration["ShowCustomAttributes"]=="true"&&assembly.Attributes.Count!=0) {
					this.WriteLine();
					this.WriteCustomAttributeCollection(assembly,null);
				}
				this.WriteProperty("Location",assembly.Location);
				this.WriteProperty("Name",assembly.ToString());
				this.WriteProperty("Type",assembly.Type.ToString());
			}

			public virtual void WriteAssemblyReference(IAssemblyReference assemblyReference) {
				this.Write("// Assembly Reference");
				this.Write(" ");
				this.WriteDeclaration(assemblyReference.Name);
				this.WriteLine();
				this.WriteProperty("Version",assemblyReference.Version.ToString());
				this.WriteProperty("Name",assemblyReference.ToString());
			}

			private void WriteClassDeclaration(ITypeDeclaration typeDeclaration){
				bool isValueType=false;
				bool isInterface=false;
				if(base.IsValueType(typeDeclaration)){
					isValueType=true;
				}else if(typeDeclaration.Interface){
					isInterface=true;
				}

				bool isNativeCppClass=false;
				CustomAttributeProc attrProc=delegate(string attrName,ICustomAttribute attr) {
					if(attrName!="NativeCppClass"||!isValueType)
						return true;
					isNativeCppClass=true;
					return false;
				};

				if(typeDeclaration.Visibility==TypeVisibility.Private||typeDeclaration.Visibility==TypeVisibility.Public){
					this.__WriteCustomAttributeCollection(typeDeclaration,null,attrProc);
					if(!isNativeCppClass||typeDeclaration.Visibility==TypeVisibility.Public)
						this.__WriteTypeVisibilitySpecifier(typeDeclaration.Visibility);
				}else{
					this.__WriteTypeVisibilitySpecifier(typeDeclaration.Visibility);
					this.__WriteCustomAttributeCollection(typeDeclaration,null,attrProc);
				}

				this.WriteGenericParameters(typeDeclaration.GenericArguments);
				if(isValueType){
					this.WriteKeyword(isNativeCppClass?"class":"value class");
				}else if(isInterface){
					this.WriteKeyword("interface class");
				}else{
					this.WriteKeyword("ref class");
				}

				this.Write(" ");
				this.WriteDeclaration(NameMangling.UnDecorateName(typeDeclaration.Name));
				if(typeDeclaration.Abstract&&!isInterface) {
					this.Write(" ");
					this.WriteKeyword("abstract");
				}
				if(typeDeclaration.Sealed&&!isValueType) {
					this.Write(" ");
					this.WriteKeyword("sealed");
				}
				string s=" : ";
				if(((typeDeclaration.BaseType!=null)&&!base.IsObject(typeDeclaration.BaseType))&&!base.IsValueType(typeDeclaration.BaseType)) {
					this.Write(s);
					this.WriteKeyword("public");
					this.Write(" ");
					new TypeRef(typeDeclaration.BaseType).WriteNameWithRef(this);
					//this.__WriteTypeReference(typeDeclaration.BaseType);
					s=", ";
				}
				foreach(ITypeReference reference in typeDeclaration.Interfaces) {
					this.Write(s);
					this.WriteKeyword("public");
					this.Write(" ");
					//this.__WriteTypeReference(reference);
					new TypeRef(reference).WriteName(this);
					s=", ";
				}
				//this.WriteLine();
				//this.Write("{");
				if(this.configuration["ShowTypeDeclarationBody"]=="true") {
					this.Write(" {");
					this.WriteLine();
					this.WriteIndent();

					// 現在のアクセス修飾子を未指定状態に
					this.__WriteVisibilitySpecifier_Clear();

					ICollection fields=GetFields(typeDeclaration,this.configuration.Visibility);
					if(fields.Count>0) {
						this.__WriteLabelComment("Fields");
						foreach(IFieldDeclaration declaration3 in fields) {
							this.WriteFieldDeclaration(declaration3);
							this.WriteLine();
						}
					}
					ICollection properties=GetProperties(typeDeclaration,this.configuration.Visibility);
					if(properties.Count>0) {
						this.__WriteLabelComment("Properties");
						foreach(IPropertyDeclaration declaration2 in properties) {
							this.WritePropertyDeclaration(declaration2);
							this.WriteLine();
						}
					}
					ICollection events=GetEvents(typeDeclaration,this.configuration.Visibility);
					if(events.Count>0) {
						this.__WriteLabelComment("Events");
						foreach(IEventDeclaration declaration in events) {
							this.WriteEventDeclaration(declaration);
							this.WriteLine();
						}
					}
					ICollection methods=GetMethods(typeDeclaration,this.configuration.Visibility);
					if(methods.Count>0) {
						this.__WriteLabelComment("Methods");
						foreach(IMethodDeclaration declaration4 in methods) {
							this.WriteMethodDeclaration(declaration4);
							this.WriteLine();
						}
					}
					ICollection nestedTypes=GetNestedTypes(typeDeclaration,this.configuration.Visibility);
					if(nestedTypes.Count>0) {
						this.__WriteLabelComment("Nested types");
						foreach(ITypeDeclaration declaration5 in nestedTypes) {
							this.WriteTypeDeclaration(declaration5);
							this.WriteLine();
						}
					}

					// 現在のアクセス修飾子を未指定状態に
					this.__WriteVisibilitySpecifier_Clear();

					this.WriteOutdent();
					this.Write("}");
				}
				this.Write(";");
				this.WriteLine();
			}

			private void WriteDelegateDeclaration(ITypeDeclaration typeDeclaration) {
				if(typeDeclaration.Visibility==TypeVisibility.Private||typeDeclaration.Visibility==TypeVisibility.Public) {
					this.__WriteCustomAttributeCollection(typeDeclaration,null);
					this.__WriteTypeVisibilitySpecifier(typeDeclaration.Visibility);
				} else {
					this.__WriteTypeVisibilitySpecifier(typeDeclaration.Visibility);
					this.__WriteCustomAttributeCollection(typeDeclaration,null);
				}
				this.WriteGenericParameters(typeDeclaration.GenericArguments);
				this.WriteKeyword("delegate");
				this.Write(" ");
				IMethodDeclaration method=GetMethod(typeDeclaration,"Invoke");

				//this.WriteType(method.ReturnType.Type,new WriteTypeMiddleCallback(null.WriteDelegateDeclMiddle),new DelegateDeclMiddleInfo(typeDeclaration,method),null,false);
				this.WriteType(method.ReturnType.Type,delegate(LanguageWriter lwriter,object delegateDecl) {
					lwriter.WriteDelegateDeclMiddle(delegateDecl);
				},new DelegateDeclMiddleInfo(typeDeclaration,method),null,false);

				this.Write(";");
				this.WriteLine();
			}
			private void WriteDelegateDeclMiddle(object delegateDecl) {
				DelegateDeclMiddleInfo info=delegateDecl as DelegateDeclMiddleInfo;
				this.WriteDeclaration(info.delegateDecl.Name);
				this.WriteMethodParameterCollection(info.invokeDecl.Parameters);
			}

			private void WriteEnumDeclaration(ITypeDeclaration typeDeclaration){
				bool isNativeCppClass=false;
				CustomAttributeProc attrProc=delegate(string attrName,ICustomAttribute attr){
					if(attrName!="NativeCppClass")
						return true;
					isNativeCppClass=true;
					return false;
				};
				if(typeDeclaration.Visibility==TypeVisibility.Private||typeDeclaration.Visibility==TypeVisibility.Public){
					this.__WriteCustomAttributeCollection(typeDeclaration,null,attrProc);
					if(!isNativeCppClass||typeDeclaration.Visibility==TypeVisibility.Public)
						this.__WriteTypeVisibilitySpecifier(typeDeclaration.Visibility);
				}else{
					this.__WriteTypeVisibilitySpecifier(typeDeclaration.Visibility);
					this.__WriteCustomAttributeCollection(typeDeclaration,null,attrProc);
				}

				this.WriteKeyword(isNativeCppClass?"enum":"enum class");
				this.Write(" ");
				this.WriteDeclaration(typeDeclaration.Name);
				if(this.configuration["ShowTypeDeclarationBody"]=="true") {
					ArrayList list=new ArrayList();
					foreach(IFieldDeclaration declaration in GetFields(typeDeclaration,this.configuration.Visibility)) {
						if(declaration.SpecialName&&declaration.Name=="value__") {
							IType fieldType=declaration.FieldType;
							if(!Type(fieldType,"System","Int32")) {
								ITypeReference typeReference=fieldType as ITypeReference;
								this.Write(" : ");
								// enum の基本型
								//this.__WriteTypeReference(typeReference);
								new TypeRef(typeReference).WriteName(this);
							}
						} else {
							list.Add(declaration);
						}
					}
					this.WriteLine();
					this.Write("{");
					this.WriteLine();
					this.WriteIndent();
					if(this.configuration["SortAlphabetically"]=="true") {
						list.Sort();
					}
					this.WriteComment("// Enumerators");
					foreach(IFieldDeclaration declaration2 in list) {
						this.WriteLine();
						this.WriteDeclaration(declaration2.Name);
						this.Write(" = ");
						this.WriteExpression(declaration2.Initializer);
						this.Write(",");
					}
					this.WriteOutdent();
					this.WriteLine();
					this.Write("};");
				}else{
					this.Write(";");
				}
				this.WriteLine();
			}

			internal void WriteExpressionCollection(IExpressionCollection iExpressionCollection) {
				bool first=true;
				foreach(IExpression exp_arg in iExpressionCollection) {
					if(first)first=false; else this.Write(", ");
					ExpressionWriter.WriteExpression(this,exp_arg,false);
				}
			}

			public virtual void WriteFieldDeclaration(IFieldDeclaration fieldDeclaration) {
				this.__WriteVisibilitySpecifier(fieldDeclaration.Visibility);
				if(fieldDeclaration.Literal) {
					this.WriteKeyword("literal");
					this.Write(" ");
				} else if(fieldDeclaration.Static) {
					this.WriteKeyword("static");
					this.Write(" ");
				}
				if(fieldDeclaration.ReadOnly) {
					this.WriteKeyword("initonly");
					this.Write(" ");
				}

				//this.WriteType(fieldDeclaration.FieldType,new WriteTypeMiddleCallback(null.WriteName),fieldDeclaration.Name,null,false);
				this.WriteType(fieldDeclaration.FieldType,delegate(LanguageWriter _this,object middle){
					_this.WriteDeclaration((middle as string)??"不明な名前");
					//_this.WriteName(middle);
				},NameMangling.UnDecorateName(fieldDeclaration.Name),null,false);

				IExpression initializer=fieldDeclaration.Initializer;
				if(initializer!=null) {
					this.Write(" = ");
					this.WriteExpression(initializer);
				}
				this.Write(";");
			}

			#region Write Reference
			internal void WriteMemberReference(IMemberReference iMemberReference) {
				IFieldReference iFieldReference=iMemberReference as IFieldReference;
				if(iFieldReference!=null) {
					this.WriteFieldReference(iFieldReference);
					return;
				}

				IMethodReference iMethodReference=iMemberReference as IMethodReference;
				if(iMethodReference!=null) {
					this.WriteMethodReference(iMethodReference);
					return;
				}

				IPropertyReference iPropertyReference=iMemberReference as IPropertyReference;
				if(iPropertyReference!=null) {
					this.WritePropertyReference(iPropertyReference);
					return;
				}

				IEventReference iEventReference=iMemberReference as IEventReference;
				if(iEventReference!=null) {
					this.WriteEventReference(iEventReference);
					return;
				}

				try {
					this.Write(iMemberReference.ToString());
				} catch(Exception exception) {
					this.Write(exception.ToString());
				}
			}

			internal void WriteEventReference(IEventReference evref) {
				TypeRef decltype=new TypeRef(evref.DeclaringType);
				TypeRef fldtype=new TypeRef(evref.EventType);

				//string fldmod="";
				//IEventDeclaration fldDecl=evref.Resolve();
				//if(fldDecl.Static) fldmod+="static ";
				//if(fldDecl.Literal) fldmod+="literal ";
				//if(fldDecl.ReadOnly) fldmod+="initonly ";

				this.WriteReference(
					evref.Name,
					string.Format("/* イベント */\r\n{0} {1}::{2};",fldtype.NameWithRef,decltype.FullName,evref.Name),
					evref
					);
			}

			internal void WriteParameterReference(IParameterReference paramref){
				this.WriteReference(
					paramref.Name,
					"/* パラメータ */\r\n"+new TypeRef(paramref.Resolve().ParameterType).NameWithRef+" "+paramref.Name+";",
					null
					);
			}

			internal void WriteFieldReference(IFieldReference iFieldReference) {
				TypeRef decltype=new TypeRef(iFieldReference.DeclaringType);
				TypeRef fldtype=new TypeRef(iFieldReference.FieldType);

				string fldmod="";
				IFieldDeclaration fldDecl=iFieldReference.Resolve();
				if(fldDecl.Static) fldmod+="static ";
				if(fldDecl.Literal) fldmod+="literal ";
				if(fldDecl.ReadOnly) fldmod+="initonly ";

				this.WriteReference(
					NameMangling.UnDecorateName(iFieldReference.Name),
					string.Format("/* フィールド */\r\n{3}{0} {1}::{2};",fldtype.NameWithRef,decltype.FullName,iFieldReference.Name,fldmod),
					iFieldReference
					);
			}

			internal void WritePropertyReference(IPropertyReference iPropertyReference) {
				TypeRef decltype=new TypeRef(iPropertyReference.DeclaringType);
				TypeRef proptype=new TypeRef(iPropertyReference.PropertyType);

				this.WriteReference(
					iPropertyReference.Name,
					string.Format("/* プロパティ */\r\nproperty {0} {1}::{2}{{}}",proptype.NameWithRef,decltype.FullName,iPropertyReference.Name),
					iPropertyReference
					);
			}

			internal void WriteVariableReference(IVariableReference var_ref){
				IVariableDeclaration vardecl=var_ref.Resolve();
				string name=this.scope[vardecl.Name].disp_name;
#if EXTRA_TEMP
				name=this.MapTemporaryName(name);
#endif
				string desc="/* ローカル変数 */\r\n"+new TypeRef(vardecl.VariableType).NameWithRef+" "+name+";";

				this.WriteReference(name,desc,null);
			}

			internal void WriteMethodReference(IMethodReference meth_ref) {
				TypeRef decltype=new TypeRef(meth_ref.DeclaringType);
				TypeRef rettype=new TypeRef(meth_ref.ReturnType.Type);
				string name=meth_ref.Name;
				
				// 説明の生成
				this.WriteReference(
					NameMangling.UnDecorateName(name),
					string.Format("/* メソッド */\r\n{0} {1}::{2}{4}({3});",
						rettype.NameWithRef,
						decltype.FullName,
						name,
						GetDesc(meth_ref.Parameters),
						GetDescOfGenericArguments(meth_ref.GenericArguments)
					),
					meth_ref
				);

				this.WriteGenericArguments(meth_ref.GenericArguments);
			}
			#endregion

			public static string GetDescOfGenericArguments(ITypeCollection g_args) {
				if(g_args==null||g_args.Count==0) return "";
				return "<"+GetDesc(g_args)+">";
			}
			public static string GetDesc(IParameterDeclarationCollection parameters){
				System.Text.StringBuilder desc=new System.Text.StringBuilder();
				bool first=true;
				foreach(IParameterDeclaration p in parameters){
					if(first)first=false;else desc.Append(", ");
					desc.Append(new TypeRef(p.ParameterType).FullNameWithRef);
				}
				return desc.ToString();
			}
			public static string GetDesc(ITypeCollection types) {
				System.Text.StringBuilder desc=new System.Text.StringBuilder();
				bool first=true;
				foreach(IType t in types){
					if(first)first=false;else desc.Append(", ");
					desc.Append(new TypeRef(t).NameWithRef);
				}
				return desc.ToString();
			}
			private void WriteGenericArguments(ITypeCollection c){
				if(c!=null&&c.Count!=0){
					this.Write("<");
					this.WriteTypeCollection(c);
					this.Write(">");
				}
			}

			private void WriteGenericParameters(ITypeCollection iTypeCollection) {
				if(iTypeCollection!=null&&iTypeCollection.Count!=0){
					this.WriteKeyword("generic");
					this.Write(" <");
					bool first=true;
					foreach(IType type in iTypeCollection) {
						if(first)first=false; else this.Write(", ");

						this.WriteKeyword("typename");
						this.Write(" ");
						this.WriteDeclaration(type.ToString());
					}
					this.Write(">");
					this.WriteLine();
				}
			}

			private void WriteGenericParameterConstraintCollection(ITypeCollection parameters) {
				if(parameters.Count>0) {
					for(int i=0;i<parameters.Count;i++) {
						IGenericParameter parameter=parameters[i] as IGenericParameter;
						if((parameter!=null)&&(parameter.Constraints.Count>0)) {
							bool flag=true;
							if(parameter.Constraints.Count==1) {
								ITypeReference reference=parameter.Constraints[0] as ITypeReference;
								if(reference!=null) {
									flag=!base.IsObject(reference);
								}
							}
							if(flag) {
								this.Write(" ");
								this.WriteKeyword("where");
								this.Write(" ");
								this.Write(parameter.Name);
								this.Write(":");
								this.Write(" ");
								string s="";
								for(int j=0;j<parameter.Constraints.Count;j++) {
									if(!(parameter.Constraints[j] is IDefaultConstructorConstraint)) {
										this.Write(s);
										this.WriteType(parameter.Constraints[j],null,null,null,false);
										s=", ";
									}
								}
							}
						}
						if(parameter.Attributes.Count>0) {
							string str="";
							for(int k=0;k<parameter.Attributes.Count;k++) {
								ICustomAttribute attribute=parameter.Attributes[k];
								ITypeReference declaringType=attribute.Constructor.DeclaringType as ITypeReference;
								ITypeReference reference2=attribute.Constructor.DeclaringType as ITypeReference;
								if(Type(attribute.Constructor.DeclaringType,"System->Runtime->CompilerServices","NewConstraintAttribute")) {
									this.Write(str);
									this.WriteKeyword("gcnew");
									this.Write("()");
									str=", ";
								}
							}
						}
					}
				}
			}

			internal void WriteAsLiteral(object value){
				if(value is bool){
					this.WriteLiteral((bool)value?"true":"false");
					return;
				}
				if(value is char){
					this.Write("'");
					this.WriteLiteral(mwg.LanguageWriterHelper.GetCharacterLiteral((char)value));
					this.Write("'");
					return;
				}
				if(value is sbyte){
					sbyte i8=(sbyte)value;

					if(i8<0) {
						if(i8==sbyte.MinValue) {
							this.WriteLiteral("System::SByte::MinValue");
							return;
						}
						i8=(sbyte)(-i8);
						this.Write("-");
					}

					this.WriteLiteral(i8.ToString(CultureInfo.InvariantCulture));
					return;
				}
				if(value is byte) {
					this.WriteLiteral(((byte)value).ToString(CultureInfo.InvariantCulture));
					return;
				}
				if(value is short) {
					short i16=(short)value;

					if(i16<0) {
						if(i16==short.MinValue) {
							this.WriteLiteral("System::Int16::MinValue");
							return;
						}
						i16=(short)(-i16);
						this.Write("-");
					}

					this.WriteLiteral(i16.ToString(CultureInfo.InvariantCulture));
					return;
				}
				if(value is ushort) {
					this.WriteLiteral(((ushort)value).ToString(CultureInfo.InvariantCulture));
					return;
				}
				if(value is int) {
					int i32=(int)value;

					if(i32<0) {
						if(i32==int.MinValue) {
							this.WriteLiteral("System::Int32::MinValue");
							return;
						}
						i32=-i32;
						this.Write("-");
					}

					this.WriteLiteral(i32.ToString(CultureInfo.InvariantCulture));
					return;
				}
				if(value is uint) {
					this.WriteLiteral(((uint)value).ToString(CultureInfo.InvariantCulture)+"u");
					return;
				}
				if(value is long) {
					long i64=(long)value;

					if(i64<0) {
						if(i64==long.MinValue) {
							this.WriteLiteral("System::Int64::MinValue");
							return;
						}
						i64=-i64;
						this.Write("-");
					}

					this.WriteLiteral(i64.ToString(CultureInfo.InvariantCulture)+"i64"); // LL
					return;
				}
				if(value is ulong) {
					this.WriteLiteral(((ulong)value).ToString(CultureInfo.InvariantCulture)+"ui64"); // uLL
					return;
				}
				if(value is float) {
					this.WriteLiteral(((float)value).ToString(CultureInfo.InvariantCulture)+"f");
					return;
				}
				if(value is double) {
					this.WriteLiteral(((double)value).ToString(CultureInfo.InvariantCulture));
					return;
				}
				if(value is decimal) {
					this.WriteLiteral(((decimal)value).ToString(CultureInfo.InvariantCulture));
					return;
				}
				if(value==null) {
					this.WriteLiteral("nullptr");
					return;
				}

				// 文字列の時
				string str3=value as string;
				if(str3!=null) {
					this.WriteLiteral(mwg.LanguageWriterHelper.GetStringLiteral(str3));
					return;
				}

				byte[] buffer=value as byte[];
				if(buffer!=null) {
					string s="{ ";
					for(int i=0;i<buffer.Length;i++) {
						this.Write(s);
						this.WriteLiteral("0x"+buffer[i].ToString("X2"));
						s=",";
					}
					this.Write(" }");
					return;
				}

				this.Write(string.Format("/* 謎のリテラルです! {0} */",value));
				return;
			}

			private void WriteMethodBody(IBlockStatement statement) {
				if(this.configuration["ShowMethodDeclarationBody"]=="true"&&statement!=null){
					StatementWriter.WriteStatement(this,statement);
				}
			}

			public virtual void WriteMethodDeclaration(IMethodDeclaration meth_decl) {
				this.__WriteVisibilitySpecifier(meth_decl.Visibility);

				this.PushScope();
				//-----------------------------------------
#if FUNC_TRY
				this.SkipTryCount=0;
#endif
//				this.refOnStack=new Hashtable();
				this.SomeConstructor=false;

#if FALSE
				ITypeDeclaration declaration=(meth_decl.DeclaringType as ITypeReference).Resolve();
				MethodNameExt ext=new MethodNameExt(meth_decl.Name,meth_decl.Overrides,declaration.Name);
				this.baseType=new TypeRef(declaration.BaseType);

				bool isGlobal=declaration.Name=="<Module>";

				if(!isGlobal&&!declaration.Interface&&(!meth_decl.SpecialName||meth_decl.Name!=".cctor")) {
					this.__WriteVisibilitySpecifier(meth_decl.Visibility);
				}
				this.WriteGenericArguments(declaration.GenericArguments);
				this.WriteGenericArguments(meth_decl.GenericArguments);

				if(this.configuration["ShowCustomAttributes"]=="true"&&meth_decl.Attributes.Count!=0) {
					this.WriteCustomAttributeCollection(meth_decl,null);
					this.WriteLine();
				}

				if(!isGlobal&&meth_decl.Static){
					this.WriteKeyword("static");
					this.Write(" ");
				}
				if(!declaration.Interface&&meth_decl.Virtual) {
					this.WriteKeyword("virtual");
					this.Write(" ");
				}

				if(ext.Constructor||ext.StaticConstructor) {
					this.SomeConstructor=true;
					this.WriteMethodDeclMiddle(meth_decl);
				}else if(ext.ImplicitOrExplicit){
					if(ext.Explicit){
						this.WriteKeyword("explicit");
						this.Write(" ");
					}
					this.WriteKeyword("operator");
					this.Write(" ");
					this.WriteType(meth_decl.ReturnType.Type,null,null,null,false);
					this.Write("(");
					this.Write(")");
				}else{
					//this.WriteType(methodDeclaration.ReturnType.Type,new WriteTypeMiddleCallback(null.WriteMethodDeclMiddle),methodDeclaration,methodDeclaration.ReturnType,false);
					//this.WriteType(meth_decl.ReturnType.Type,delegate(LanguageWriter _this,object methodDecl) {
					//	_this.WriteMethodDeclMiddle(methodDecl);
					//},meth_decl,meth_decl.ReturnType,false);
					if(this.configuration["ShowCustomAttributes"]=="true"){
						this.WriteCustomAttributeCollection(meth_decl.ReturnType,meth_decl.ReturnType.Type);
					}
					new TypeRef(meth_decl.ReturnType.Type).WriteNameWithRef(this);
					this.Write(" ");
					this.WriteMethodDeclMiddle(meth_decl);
				}

				if(!meth_decl.NewSlot&&meth_decl.Final) {
					this.Write(" ");
					this.WriteKeyword("sealed");
				}
				if(meth_decl.Virtual) {
					if(meth_decl.Abstract) {
						this.Write(" ");
						this.WriteKeyword("abstract");
					}
					if(!meth_decl.NewSlot) {
						this.Write(" ");
						this.WriteKeyword("override");
					}
				}

#else
				MethodSignature m_sig=new MethodSignature(meth_decl);
				this.baseType=new TypeRef(m_sig.DeclaringType.BaseType);
				if(!m_sig.IsGlobal&&m_sig.DeclaringType.Interface&&m_sig.Type!=MethodSignature.MethodType.StaticConstructor){
					this.__WriteVisibilitySpecifier(meth_decl.Visibility);
				}
				this.WriteGenericParameters(m_sig.DeclaringType.GenericArguments);
				this.WriteGenericParameters(meth_decl.GenericArguments);
				if(this.configuration["ShowCustomAttributes"]=="true"&&meth_decl.Attributes.Count!=0) {
					this.WriteCustomAttributeCollection(meth_decl,null);
					this.WriteLine();
				}
				if(m_sig.IsStatic){
					this.WriteKeyword("static");
					this.Write(" ");
				}
				if(m_sig.IsVirtual){
					this.WriteKeyword("virtual");
					this.Write(" ");
				}

				switch(m_sig.Type){
					case MethodSignature.MethodType.StaticConstructor:
					case MethodSignature.MethodType.Constructor:
						this.SomeConstructor=true;
						this.WriteDeclaration(m_sig.DeclaringType.Name);
						this.WriteMethodParameterCollection(meth_decl.Parameters);
						break;
					case MethodSignature.MethodType.ExplicitCast:
						this.WriteKeyword("explicit");
						this.Write(" ");
						goto l_implicit;
					case MethodSignature.MethodType.ImplicitCast:
					l_implicit:
						this.WriteKeyword("operator");
						this.Write(" ");
						this.WriteType(meth_decl.ReturnType.Type,null,null,null,false);
						this.WriteMethodParameterCollection(meth_decl.Parameters);
						//this.Write("(");
						//this.Write(")");
						break;
					case MethodSignature.MethodType.Operator:
					default:
						// 戻り値
						if(this.configuration["ShowCustomAttributes"]=="true"){
							this.WriteCustomAttributeCollection(meth_decl.ReturnType,meth_decl.ReturnType.Type);
						}
						new TypeRef(meth_decl.ReturnType.Type).WriteNameWithRef(this);
						this.Write(" ");

						// 関数名
						if(m_sig.Type==MethodSignature.MethodType.Operator){
							this.WriteKeyword("operator");
						}
						this.WriteDeclaration(NameMangling.UnDecorateName(m_sig.Name));
						
						// 引数リスト
						this.WriteMethodParameterCollection(meth_decl.Parameters);
						break;
				}

				if(m_sig.IsSealed){
					this.Write(" ");
					this.WriteKeyword("sealed");
				}
				if(m_sig.IsAbstract){
					this.Write(" ");
					this.WriteKeyword("abstract");
				}
				if(m_sig.IsOverride){
					this.Write(" ");
					this.WriteKeyword("override");
				}
#endif

				ICollection overrides=meth_decl.Overrides;
				if(overrides.Count>0) {
					string s=" = ";
					foreach(IMethodReference reference in overrides) {
						this.Write(s);
						this.WriteReference(reference.Name,"",reference);
						s=", ";
					}
				}

#if FUNC_TRY
				bool ignoreSkip=true;
#endif

				this.WriteGenericParameterConstraintCollection(meth_decl.GenericArguments);

				try{
					IConstructorDeclaration ctor_decl=meth_decl as IConstructorDeclaration;
					if(ctor_decl!=null) { // ext.Constructor
						WriteMethodDecl_ctor_body(ctor_decl);
					}else{
						this.__WriteMethodBody(meth_decl.Body as IBlockStatement); //,ignoreSkip
					}
				}catch(System.Exception e){
					this.Write("\n**** Error ****\n"+e.ToString());
				}
				//-----------------------------------------
				this.PopScope();
			}
			private void WriteMethodDecl_ctor_body(IConstructorDeclaration ctor_decl){
				bool first=true;

				IMethodInvokeExpression initializer=ctor_decl.Initializer;
				if(initializer!=null){
					IMethodReferenceExpression method=initializer.Method as IMethodReferenceExpression;
					if(method!=null&&initializer.Arguments.Count!=0) {
						first=false;
						this.Write(" : ");
						this.WriteExpression(method.Target);
						this.Write("(");
						this.WriteExpressionCollection(initializer.Arguments);
						this.Write(")");
					}
				}

				IStatement body=ctor_decl.Body as IStatement;
				if(body!=null&&this.configuration["ShowMethodDeclarationBody"]=="true"){

//					this.WriteMethodBody(body);
					bool deleg=true;	// delegate constructor が続いている事を示す
					foreach(IStatement state in StatementAnalyze.ModifyStatements(body)){
						if(deleg){
							IExpressionStatement state_exp=state as IExpressionStatement;
							if(state_exp==null)goto fail;
							IAssignExpression exp_assign=state_exp.Expression as IAssignExpression;
							if(exp_assign==null)goto fail;

							IFieldReferenceExpression exp_fld=exp_assign.Target as IFieldReferenceExpression;
							if(exp_fld==null)goto fail;
							IThisReferenceExpression exp_this=exp_fld.Target as IThisReferenceExpression;
							if(exp_this==null)goto fail;
							
							if(first){
								first=false;
								this.Write(" : ");
							}else{
								this.Write(", ");
							}

							this.WriteFieldReference(exp_fld.Field);
							ExpressionWriter.WriteExpression(this,exp_assign.Expression,true);
							continue;
						fail:
							this.Write(" {");
							this.WriteLine();
							this.WriteIndent();
							deleg=false;
						}

						StatementWriter.WriteStatement(this,state);
					}

					if(deleg){
						// 未だ本体を書き始めて居ない時
						this.Write(" {}");
					}else{
						// 既に本体を書き込んでいる場合
						this.WriteOutdent();
						this.Write("}");
					}
				}else{
					this.Write(";");
				}
				
			}

			internal void WriteMethodParameterCollection(IParameterDeclarationCollection parameters) {
				this.Write("(");
				bool first=true;
				foreach(IParameterDeclaration declaration in parameters) {
					if(first)first=false;else this.Write(", ");
					new TypeRef(declaration.ParameterType).WriteNameWithRef(this);
					if(declaration.Name!=""){
						this.Write(" ");
						this.Write(declaration.Name);
					}
					//this.Write(s);
					//this.WriteType(declaration.ParameterType,new WriteTypeMiddleCallback(null.WriteName),declaration.Name,declaration,false);
					//this.WriteType(declaration.ParameterType,delegate(LanguageWriter _this,object middle) {
					//	_this.WriteName(middle);
					//},declaration.Name,declaration,false);
					//s=", ";
				}
				this.Write(")");
			}

			public virtual void WriteModule(IModule module) {
				this.Write("// Module");
				this.Write(" ");
				this.WriteDeclaration(module.Name);
				this.WriteLine();
				if((this.configuration["ShowCustomAttributes"]=="true")&&(module.Attributes.Count!=0)) {
					this.WriteLine();
					this.WriteLine();
				}
				this.WriteProperty("Version",module.Version.ToString());
				this.WriteProperty("Location",module.Location);
				string path=Environment.ExpandEnvironmentVariables(module.Location);
				if(File.Exists(path)) {
					//					FileStream stream;
					//					FileStream /*modopt(IsConst)*/ stream2=new FileStream(path,FileMode.Open,FileAccess.Read);
					FileStream stream=new FileStream(path,FileMode.Open,FileAccess.Read);
					try {
						//						stream=stream2;
						this.WriteProperty("Size",stream.Length+" Bytes");
					} catch {
						stream.Dispose();
					}
					stream.Dispose();
				}
			}

			public virtual void WriteModuleReference(IModuleReference moduleReference) {
				this.Write("// Module Reference");
				this.Write(" ");
				this.WriteDeclaration(moduleReference.Name);
				this.WriteLine();
			}

			private void WriteName(object name) {
				try {
					if(name!=null) {
						this.Write(name as string);
					}
				} catch(Exception) {
					this.Write("exception deanwi");
				}
			}

			public virtual void WriteNamespace(INamespace namespaceDeclaration) {
				if(!(this.configuration["ShowNamespaceBody"]=="true")) {
					this.WriteKeyword("namespace");
					this.Write(" ");
					string s=namespaceDeclaration.Name.Replace(".","::");
					this.WriteDeclaration(s);
				}else{
					string[] ns_dirs=namespaceDeclaration.Name.Split('.');
					if(ns_dirs.Length>0)foreach(string str2 in ns_dirs) {
						this.WriteKeyword("namespace");
						this.Write(" ");
						this.WriteDeclaration(str2);
						this.Write(" { ");
						this.WriteLine();
					}
					this.WriteIndent();

					Gen::List<ITypeDeclaration> list=new System.Collections.Generic.List<ITypeDeclaration>();
					foreach(ITypeDeclaration declaration in namespaceDeclaration.Types) {
						if(IsVisible(declaration,this.configuration.Visibility)) {
							list.Add(declaration);
						}
					}

					if(this.configuration["SortAlphabetically"]=="true")list.Sort();

					for(int i=0;i<list.Count;i++){
						if(i!=0)this.WriteLine();
						this.WriteTypeDeclaration(list[i]);
					}

					this.WriteOutdent();
					if(ns_dirs.Length>0)for(int j=ns_dirs.Length;j>0;j--) {
						this.Write("}");
						this.WriteLine();
					}
				}
			}

			private void WritePropertyIndices(IParameterDeclarationCollection parameters) {
				if(parameters.Count!=0) {
					this.Write("[");
					bool first=true;
					foreach(IParameterDeclaration declaration in parameters) {
						if(first) first=false; else this.Write(", ");
						this.WriteType(declaration.ParameterType,null,null,null,false);
					}
					this.Write("]");
				}
			}

			public virtual unsafe void WriteResource(IResource resource) {
				this.Write("// ");
				ResourceVisibility visibility=resource.Visibility;
				if(visibility==ResourceVisibility.Public){
					this.WriteKeyword("public");
				}else if(visibility==ResourceVisibility.Private) {
					this.WriteKeyword("private");
				}
				this.Write(" ");
				this.WriteKeyword("resource");
				this.Write(" ");
				this.WriteDeclaration(resource.Name);
				this.WriteLine();
				IEmbeddedResource resource3=resource as IEmbeddedResource;
				if(resource3!=null) {
					this.WriteProperty("種類","埋め込みリソース");
					this.WriteProperty("Size",resource3.Value.Length.ToString()+" bytes");
				}
				IFileResource resource2=resource as IFileResource;
				if(resource2!=null) {
					this.WriteProperty("種類","ファイルリソース");
					this.WriteProperty("Location",resource2.Location);
				}
			}

			public virtual void WriteType(IType type,WriteTypeMiddleCallback callback,object middle,ICustomAttributeProvider __unnamed003,[MarshalAs(UnmanagedType.U1)] bool fRefOnStack) {
				if(this.configuration["ShowCustomAttributes"]=="true"&&__unnamed003!=null) {
					this.WriteCustomAttributeCollection(__unnamed003,type);
				}

				TypeRef type2=new TypeRef(type);
				if(fRefOnStack)
					type2.WriteName(this);
				else
					type2.WriteNameWithRef(this);
#if FALSE
				ITypeReference typeReference=type as ITypeReference;
				if(typeReference!=null) {
					this.__WriteTypeReference(typeReference);
					if(!base.IsValueType(typeReference)&&!fRefOnStack) {
						this.Write("^");
					}
					goto phase2;
				}

				IArrayType type2=type as IArrayType;
				if(type2!=null) {
					this.WriteKeyword("array");
					this.Write("<");
					this.WriteType(type2.ElementType,null,null,null,false);
					if(type2.Dimensions.Count>1) {
						this.Write(", "+type2.Dimensions.Count.ToString());
					}
					this.Write(">^");
					goto phase2;
				}

				IPointerType type4=type as IPointerType;
				if(type4!=null) {
					this.WriteType(type4.ElementType,null,null,null,false);
#warning const * はないのか?
					this.Write("*");
					goto phase2;
				}

				IReferenceType type3=type as IReferenceType;
				if(type3!=null) {
					this.WriteType(type3.ElementType,null,null,null,false);
					this.Write("%");
					goto phase2;
				}

				IOptionalModifier modifier2=type as IOptionalModifier;
				if(modifier2!=null) {
					this.WriteModifiedType(modifier2.Modifier,modifier2.ElementType,false,fRefOnStack);
					goto phase2;
				}

				IRequiredModifier modifier=type as IRequiredModifier;
				if(modifier!=null) {
					this.WriteModifiedType(modifier.Modifier,modifier.ElementType,true,fRefOnStack);
					goto phase2;
				}

				if(type is IFunctionPointer) {
					IFunctionPointer fptr=(IFunctionPointer)type;
#warning 関数ポインタのシグニチャ

					this.Write("funcptr");
					goto phase2;
				}

				IGenericParameter parameter=type as IGenericParameter;
				if(parameter!=null) {
					this.Write(parameter.Name);
					goto phase2;
				}

				IGenericArgument argument=type as IGenericArgument;
				if(argument!=null) {
					this.WriteType(argument.Resolve(),null,null,null,false);
					goto phase2;
				}

				if(type is IValueTypeConstraint) {
					this.WriteKeyword("value class");
				} else if(!(type is IDefaultConstructorConstraint)) {
					this.Write("WHAT TYPE IS IT?");
				}
			phase2:
#endif
				if(callback!=null) {
					this.Write(" ");
					callback(this,middle);
				}
			}

			private void WriteTypeCollection(ITypeCollection iTypeCollection) {
				bool first=true;
				foreach(IType type in iTypeCollection) {
					if(first) first=false; else this.Write(", ");
					this.WriteType(type,null,null,null,false);
				}
			}

			public virtual void WriteTypeDeclaration(ITypeDeclaration typeDeclaration) {
				if(base.IsDelegate(typeDeclaration)) {
					this.WriteDelegateDeclaration(typeDeclaration);
				} else if(base.IsEnumeration(typeDeclaration)) {
					this.WriteEnumDeclaration(typeDeclaration);
				} else {
					this.WriteClassDeclaration(typeDeclaration);
				}
			}

			private class DelegateDeclMiddleInfo {
				private ITypeDeclaration backing_store_delegateDecl;
				private IMethodDeclaration backing_store_invokeDecl;

				internal DelegateDeclMiddleInfo(ITypeDeclaration delegateDecl_,IMethodDeclaration invokeDecl_) {
					this.delegateDecl=delegateDecl_;
					this.invokeDecl=invokeDecl_;
				}

				internal ITypeDeclaration delegateDecl {
					get {
						return this.backing_store_delegateDecl;
					}
					set {
						this.backing_store_delegateDecl=value;
					}
				}

				internal IMethodDeclaration invokeDecl {
					get {
						return this.backing_store_invokeDecl;
					}
					set {
						this.backing_store_invokeDecl=value;
					}
				}
			}

#if EXTRA_TEMP
			public class NewBlock:IDisposable {
				public int original_block;
				public CppCliLanguage.LanguageWriter m_r;

				public NewBlock(CppCliLanguage.LanguageWriter r) {
					this.m_r=r;
					this.original_block=this.m_r.Block;
					this.m_r.Block=this.m_r.NextBlock++;
				}

				private void cppdtor_NewBlock() {
					this.m_r.Block=this.original_block;
				}

				public void Dispose() {
					this.Dispose(true);
					GC.SuppressFinalize(this);
				}

				protected virtual void Dispose([MarshalAs(UnmanagedType.U1)] bool flag1) {
					if(flag1) {
						this.cppdtor_NewBlock();
					} else {
						//base.Finalize();
					}
				}
			}

			public class Save4ExtraPass:IDisposable {
				public CppCliLanguage.LanguageWriter m_r;
				public int SkipTryCount;

				public Save4ExtraPass(CppCliLanguage.LanguageWriter r) {
					this.m_r=r;
					this.m_r.SuppressOutput=true;
					this.SkipTryCount=this.m_r.SkipTryCount;
				}

				private void cppdtor_Save4ExtraPass() {
					this.m_r.SuppressOutput=false;
					this.m_r.SkipTryCount=this.SkipTryCount;
					this.m_r.ExtraMappings=new string[this.m_r.SkipNullptrCount];
				}

				public void Dispose() {
					this.Dispose(true);
					GC.SuppressFinalize(this);
				}

				protected virtual void Dispose([MarshalAs(UnmanagedType.U1)] bool flag1) {
					if(flag1) {
						this.cppdtor_Save4ExtraPass();
					} else {
						//base.Finalize();
					}
				}
			}
#endif

			public delegate void WriteTypeMiddleCallback(CppCliLanguage.LanguageWriter languageWriter,object middle);
		}
	}
}