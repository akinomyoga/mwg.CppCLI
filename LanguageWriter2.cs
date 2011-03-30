using Reflector;
using LWriter_t=Reflector.Languages.CppCliLanguage.LanguageWriter;
using Reflector.CodeModel;
using System;
using System.Globalization;
using mwg.Reflector.CppCli;

namespace mwg{
	public static partial class LanguageWriterHelper{
		//===========================================================
		//		文字リテラル
		//===========================================================
		public static string GetCharacterLiteral(char c){
			switch(c){
				case '\'':return @"\'";
				case '\0':return @"\0";
				case '\n':return @"\n";
				case '\r':return @"\r";
				case '\t':return @"\t";
				case '\v':return @"\v";
				case '\f':return @"\f";
				case '\b':return @"\b";
				default:
					return c.ToString(CultureInfo.InvariantCulture);
			}
		}
		//===========================================================
		//		文字列リテラル
		//===========================================================
		public unsafe static string GetStringLiteral(string value){
			System.Text.StringBuilder build=new System.Text.StringBuilder();
			build.Append('"');
			for(int num2=0;num2<value.Length;num2++) {
				char ch=value[num2];
				switch(ch){
					case '"':
						build.Append(@"\""");
						break;
					case '\0':build.Append(@"\0");break;
					case '\r':build.Append(@"\r");break;
					case '\n':build.Append(@"\n");break;
					case '\t':build.Append(@"\t");break;
					case '\v':build.Append(@"\v");break;
					case '\f':build.Append(@"\f");break;
					case '\b':build.Append(@"\b");break;
					default:
						if(ch<='\x0080'||IsRepresentableInDefaultEncoding(ch)){
							build.Append(ch.ToString(CultureInfo.InvariantCulture));
						}else{
							build.Append(@"\u"+((uint)ch).ToString("X4"));
						}
						break;
				}
			}
			build.Append('"');
			return build.ToString();
		}
		private unsafe static bool IsRepresentableInDefaultEncoding(char ch){
			byte* buff=stackalloc byte[8];
			int len=System.Text.Encoding.Default.GetBytes(&ch,1,buff,8);
			char ch2;
			System.Text.Encoding.Default.GetChars(buff,len,&ch2,1);
			return ch==ch2;
		}
	}
}

namespace Reflector.Languages {
	using Reflector;
	using Reflector.CodeModel;
	using System;
	using System.Collections;
	using System.Globalization;
	using System.IO;
	using System.Runtime.CompilerServices;
	using System.Runtime.InteropServices;
	public partial class CppCliLanguage:ILanguage{
		public partial class LanguageWriter:Helper,ILanguageWriter{
			public void WriteStatement(IStatement state){
				StatementWriter.WriteStatement(this,state);
			}

			internal void __WriteLabel(string name){
				this.WriteOutdent();
				this.WriteDeclaration(name);
				this.Write(":");
				this.WriteIndent();
				this.WriteLine();
			}
			private void __WriteLabelComment(string name){
				this.WriteOutdent();
				this.WriteLine();
				this.WriteComment("//");
				this.WriteLine();
				this.WriteComment("// "+name);
				this.WriteLine();
				this.WriteComment("//");
				this.WriteIndent();
				this.WriteLine();
			}
			private void __WriteMethodBody(IBlockStatement body){ //,bool ignoreSkip
				if(body!=null){
					this.Write(" {");
					this.WriteLine();
					this.WriteIndent();

#if EXTRA_TEMP
					this.GatherExtraTemporaries(body.Statements);
					if(this.SkipNullptrCount!=0) {
						using(Save4ExtraPass pass=new Save4ExtraPass(this)){
							this.NextBlock=1;
							this.Block=this.NextBlock;
							this.NextBlock++;
							this.WriteMethodBody(body);
						}
					}else
#endif
					{
						this.WriteMethodBody(body);
					}

					this.WriteOutdent();
#if FUNC_TRY
					if(ignoreSkip)
#endif
						this.Write("}");
				}else{
					this.Write(";");
				}

#if EXTRA_TEMP
				this.ExtraTemporaries=null;
#endif
			}

			//===========================================================
			//		宣言
			//===========================================================
			public virtual void WritePropertyDeclaration(IPropertyDeclaration propDecl) {
				// アクセス修飾子情報
				IMethodDeclaration getter;
				IMethodDeclaration setter;
				MethodVisibility vis=__GetPropertyVisibility(propDecl,out getter,out setter);
				this.__WriteVisibilitySpecifier(vis);

#warning generic property
				// 修飾子
				if(getter!=null&&getter.Static||setter!=null&&setter.Static){
					this.WriteKeyword("static");
					this.Write(" ");
				}
				this.WriteKeyword("property");
				this.Write(" ");
				this.WriteType(propDecl.PropertyType,null,null,null,false);
				this.Write(" ");
				this.WriteDeclaration(propDecl.Name);
				IParameterDeclarationCollection parameters=propDecl.Parameters;
				if(parameters!=null){
					this.WritePropertyIndices(parameters);
				}

				//
				// 内容の書込
				//
				//---------------------------------------------------
				// inline == true → 宣言だけを書く場合
				// inline == false → アクセサのロジックも書く場合
				bool inline=!(getter!=null&&getter.Body!=null||setter!=null&&setter.Body!=null);
				this.Write("{");
				if(inline){
					this.Write(" ");
				}else{
					this.WriteIndent();
					this.WriteLine();
				}
				//-----------------------------------------
				this.PushScope();
				//-----------------------------------------
				if(getter!=null){
					if(getter.Visibility!=vis)
						this.__WriteVisibilitySpecifier(getter.Visibility,inline);
					this.__WritePropertyAccessorSignature("get",getter,inline);
					this.__WriteMethodBody(getter.Body as IBlockStatement); //,true
					if(inline){
						this.Write(" ");
					}else{
						this.WriteLine();
					}
				}
				//-----------------------------------------
				this.PopScope();
				this.PushScope();
				//-----------------------------------------
				if(setter!=null){
					if(getter!=null&&setter.Visibility!=getter.Visibility)
						this.__WriteVisibilitySpecifier(setter.Visibility,inline);
					this.__WritePropertyAccessorSignature("set",setter,inline);
					this.__WriteMethodBody(setter.Body as IBlockStatement); //,true
					if(inline){
						this.Write(" ");
					}else{
						this.WriteLine();
					}
				}
				//-----------------------------------------
				this.PopScope();
				//-----------------------------------------
				if(!inline){
					this.WriteOutdent();
				}
				this.Write("}");
			}
			private void __WritePropertyAccessorSignature(string name,IMethodDeclaration decl,bool inline){
				if(this.configuration["ShowCustomAttributes"]=="true"&&decl.Attributes.Count!=0) {
					this.WriteCustomAttributeCollection(decl,null);
					if(!inline)this.WriteLine();
				}

				if(/*!declaration.Interface&&*/decl.Virtual){
					this.WriteKeyword("virtual");
					this.Write(" ");
				}

				// 本体
				this.WriteType(decl.ReturnType.Type,null,null,null,false);
				this.Write(" ");
				this.WriteKeyword(name);
				this.WriteMethodParameterCollection(decl.Parameters);

				// 修飾子
				if(!decl.NewSlot&&decl.Final) {
					this.Write(" ");
					this.WriteKeyword("sealed");
				}
				if(decl.Virtual) {
					if(decl.Abstract) {
						this.Write(" ");
						this.WriteKeyword("abstract");
					}
					if(!decl.NewSlot) {
						this.Write(" ");
						this.WriteKeyword("override");
					}
				}
			}
			
			public virtual void WriteEventDeclaration(IEventDeclaration eventDecl) {
#warning イベント: アクセサによって virtual 系修飾子が異なる場合には未対応
				IMethodDeclaration add_decl=eventDecl.AddMethod.Resolve();
				this.__WriteVisibilitySpecifier(add_decl.Visibility);

				if(add_decl.Static){
					this.WriteKeyword("static");
					this.Write(" ");
				}

				if(add_decl.Abstract){
					this.WriteKeyword("abstract");
					this.Write(" ");
				}else if(add_decl.Overrides.Count!=0){
					this.WriteKeyword("override");
					this.Write(" ");
				}else if(add_decl.Final){
					this.WriteKeyword("sealed");
					this.Write(" ");
				}else if(add_decl.Virtual){
					this.WriteKeyword("virtual");
					this.Write(" ");
				}

				this.WriteKeyword("event");
				this.Write(" ");
				this.WriteType(eventDecl.EventType,null,null,null,false);
				this.Write(" ");
				this.WriteDeclaration(eventDecl.Name);
				this.Write(";");
			}

			//===========================================================
			//		Expression 分岐
			//===========================================================
			public virtual void WriteExpression(IExpression expression){
				if(expression==null)return;

				mwg.Reflector.CppCli.ExpressionWriter.WriteExpression(this,expression,false);

#if FALSE
#pragma warning disable 612

				IMemberInitializerExpression expression3=expression as IMemberInitializerExpression;
				if(expression3!=null) {
					this.WriteMemberInitializerExpression(expression3);
					return;
				}

				IAddressOutExpression expression27=expression as IAddressOutExpression;
				if(expression27!=null) {
					this.WriteAddressOutExpression(expression27);
					return;
				}

				IAddressReferenceExpression expression26=expression as IAddressReferenceExpression;
				if(expression26!=null) {
					this.WriteAddressReferenceExpression(expression26);
					return;
				}

				IDelegateCreateExpression iDelegateCreateExpression=expression as IDelegateCreateExpression;
				if(iDelegateCreateExpression!=null) {
					this.WriteDelegateCreateExpression(iDelegateCreateExpression);
					return;
				}

				IMethodInvokeExpression iMethodInvokeExpression=expression as IMethodInvokeExpression;
				if(iMethodInvokeExpression!=null) {
					this.WriteMethodInvokeExpression(iMethodInvokeExpression);
					return;
				}

				IVariableDeclarationExpression expression15=expression as IVariableDeclarationExpression;
				if(expression15!=null) {
					this.WriteVariableDeclaration(expression15.Variable);
					return;
				}

				ITypeOfExpression iTypeOfExpression=expression as ITypeOfExpression;
				if(iTypeOfExpression!=null) {
					this.WriteTypeOfExpression(iTypeOfExpression);
					return;
				}

				ISnippetExpression iSnippetExpression=expression as ISnippetExpression;
				if(iSnippetExpression!=null) {
					this.WriteSnippetExpression(iSnippetExpression);
					return;
				}

				IUnaryExpression iUnaryExpression=expression as IUnaryExpression;
				if(iUnaryExpression!=null) {
					this.WriteUnaryExpression(iUnaryExpression);
					return;
				}

				IObjectCreateExpression iObjectCreateExpression=expression as IObjectCreateExpression;
				if(iObjectCreateExpression!=null) {
					this.WriteObjectCreateExpression(iObjectCreateExpression);
					return;
				}

				IVariableReferenceExpression iVariableReferenceExpression=expression as IVariableReferenceExpression;
				if(iVariableReferenceExpression!=null) {
					this.WriteVariableReferenceExpression(iVariableReferenceExpression);
					return;
				}

				IThisReferenceExpression expression12=expression as IThisReferenceExpression;
				if(expression12!=null) {
					this.WriteThisReferenceExpression(expression12);
					return;
				}

				ITryCastExpression iTryCastExpression=expression as ITryCastExpression;
				if(iTryCastExpression!=null) {
					this.WriteTryCastExpression(iTryCastExpression);
					return;
				}

				IConditionExpression expression9=expression as IConditionExpression;
				if(expression9!=null) {
					this.WriteConditionExpression(expression9);
					return;
				}

				IFieldReferenceExpression iFieldReferenceExpression=expression as IFieldReferenceExpression;
				if(iFieldReferenceExpression!=null) {
					this.WriteFieldReferenceExpression(iFieldReferenceExpression);
					return;
				}

				IPropertyIndexerExpression iPropertyIndexerExpression=expression as IPropertyIndexerExpression;
				if(iPropertyIndexerExpression!=null) {
					this.WritePropertyIndexerExpression(iPropertyIndexerExpression);
					return;
				}

				ITypeReferenceExpression iTypeReferenceExpression=expression as ITypeReferenceExpression;
				if(iTypeReferenceExpression!=null) {
					this.WriteTypeReferenceExpression(iTypeReferenceExpression);
					return;
				}

				IMethodReferenceExpression iMethodReferenceExpression=expression as IMethodReferenceExpression;
				if(iMethodReferenceExpression!=null) {
					this.WriteMethodReferenceExpression(iMethodReferenceExpression);
					return;
				}

				IPropertyReferenceExpression iPropertyReferenceExpression=expression as IPropertyReferenceExpression;
				if(iPropertyReferenceExpression!=null) {
					this.WritePropertyReferenceExpression(iPropertyReferenceExpression);
					return;
				}

				ICastExpression expression5=expression as ICastExpression;
				if(expression5!=null) {
					this.WriteCastExpression(expression5);
					return;
				}

				ICanCastExpression iCanCastExpression=expression as ICanCastExpression;
				if(iCanCastExpression!=null) {
					this.WriteCanCastExpression(iCanCastExpression);
					return;
				}

				ICastExpression iCastExpression=expression as ICastExpression;
				if(iCastExpression!=null) {
					this.WriteCastExpression(iCastExpression);
					return;
				}

				ILiteralExpression literalExpression=expression as ILiteralExpression;
				if(literalExpression!=null) {
					this.WriteLiteralExpression(literalExpression);
					return;
				}

				IBinaryExpression iBinaryExpression=expression as IBinaryExpression;
				if(iBinaryExpression!=null) {
					mwg.Reflector.CppCli.ExpressionWriter.WriteExpression(this,expression,true);
					//this.WriteBinaryExpression(iBinaryExpression);
					return;
				}

				IArrayIndexerExpression expression30=expression as IArrayIndexerExpression;
				if(expression30!=null) {
					this.WriteArrayIndexerExpression(expression30);
					return;
				}

				IAddressDereferenceExpression expression29=expression as IAddressDereferenceExpression;
				if(expression29!=null) {
					this.WriteAddressDereferenceExpression(expression29);
					return;
				}

				IAddressOfExpression expression28=expression as IAddressOfExpression;
				if(expression28!=null) {
					this.WriteAddressOfExpression(expression28);
					return;
				}

				IArgumentListExpression expression25=expression as IArgumentListExpression;
				if(expression25!=null) {
					this.WriteArgumentListExpression(expression25);
					return;
				}

				IBaseReferenceExpression iBaseReferenceExpression=expression as IBaseReferenceExpression;
				if(iBaseReferenceExpression!=null) {
					this.WriteBaseReferenceExpression(iBaseReferenceExpression);
					return;
				}

				IArgumentReferenceExpression expression13=expression as IArgumentReferenceExpression;
				if(expression13!=null) {
					this.WriteArgumentReferenceExpression(expression13);
					return;
				}

				IArrayCreateExpression expression10=expression as IArrayCreateExpression;
				if(expression10!=null) {
					this.WriteArrayCreateExpression(expression10);
					return;
				}

				IAssignExpression iAssignExpression=expression as IAssignExpression;
				if(iAssignExpression!=null) {
					this.WriteAssignExpression(iAssignExpression);
					return;
				}

				IBlockExpression expression2=expression as IBlockExpression;
				if(expression2!=null) {
					this.WriteBlockExpression(expression2);
					return;
				}
#pragma warning restore 612

				this.Write(expression.ToString());
#endif
			}
			//===========================================================
			//		属性リスト
			//===========================================================
			/// <summary>
			/// カスタム属性リスト (改行付き) を出力します。
			/// </summary>
			/// <param name="typeDeclaration"></param>
			/// <param name="type"></param>
			private void __WriteCustomAttributeCollection(ITypeDeclaration typeDeclaration,IType type){
				if(this.configuration["ShowCustomAttributes"]!="true"||typeDeclaration==null||typeDeclaration.Attributes.Count==0) {
					return;
				}

				AttributeCollection attr_c=new AttributeCollection(this,typeDeclaration,type);
				attr_c.Write();
				this.WriteLine();
			}
			/// <summary>
			/// カスタム属性リスト (改行付き) を出力します。
			/// </summary>
			/// <param name="typeDeclaration"></param>
			/// <param name="type"></param>
			/// <param name="attrProc">特定の属性に対する処理を指定します。</param>
			private void __WriteCustomAttributeCollection(ITypeDeclaration typeDeclaration,IType type,CustomAttributeProc attrProc){
				if(this.configuration["ShowCustomAttributes"]!="true"||typeDeclaration==null||typeDeclaration.Attributes.Count==0){
					return;
				}
				AttributeCollection attr_c=new AttributeCollection(this,typeDeclaration,type);
				attr_c.Write(attrProc);
				this.WriteLine();
			}
			/// <summary>
			/// カスタム属性リストを出力します。
			/// </summary>
			/// <param name="provider"></param>
			/// <param name="type"></param>
			private void WriteCustomAttributeCollection(ICustomAttributeProvider provider,IType type) {
				if(this.configuration["ShowCustomAttributes"]!="true"||provider==null||provider.Attributes.Count==0){
					return;
				}

				AttributeCollection attr_c=new AttributeCollection(this,provider,type);
				attr_c.Write();
				return;
			}
			//===========================================================
			//		アクセス修飾子
			//===========================================================
			private enum AccessSpecifier{
				NoSpecified,

				PrivateScope,
				Private,
				PrivateProtected,
				Internal,
				Protected,
				InternalProtected,
				Public,
			}
			private AccessSpecifier prevAccessSpec=AccessSpecifier.NoSpecified;
			private void __WriteVisibilitySpecifier_Clear(){
				this.prevAccessSpec=AccessSpecifier.NoSpecified;
			}

			private void __WriteVisibilitySpecifier(AccessSpecifier accessSpec,bool inline){
				if(accessSpec==prevAccessSpec)return;
				prevAccessSpec=accessSpec;

				if(!inline){
					this.WriteOutdent();
				}

				switch(accessSpec) {
					case AccessSpecifier.PrivateScope:
						this.Write("?PrivateScope? ");
						this.WriteComment("/* って何? */");
						break;

					case AccessSpecifier.Private:
						this.WriteKeyword("private");
						break;

					case AccessSpecifier.PrivateProtected:
						this.WriteKeyword("private protected");
						break;

					case AccessSpecifier.Internal:
						this.WriteKeyword("internal");
						break;

					case AccessSpecifier.Protected:
						this.WriteKeyword("protected");
						break;

					case AccessSpecifier.InternalProtected:
						this.WriteKeyword("protected internal");
						break;

					case AccessSpecifier.Public:
						this.WriteKeyword("public");
						break;
				}
				this.Write(":");

				if(!inline){
					this.WriteIndent();
					this.WriteLine();
				}else{
					this.Write(" ");
				}
			}

			private void __WriteVisibilitySpecifier(MethodVisibility visibility){
				switch(visibility){
					case MethodVisibility.PrivateScope:
						this.__WriteVisibilitySpecifier(AccessSpecifier.PrivateScope,false);
						break;
					case MethodVisibility.Private:
						this.__WriteVisibilitySpecifier(AccessSpecifier.Private,false);
						break;
					case MethodVisibility.FamilyAndAssembly:
						this.__WriteVisibilitySpecifier(AccessSpecifier.PrivateProtected,false);
						break;
					case MethodVisibility.Assembly:
						this.__WriteVisibilitySpecifier(AccessSpecifier.Internal,false);
						break;
					case MethodVisibility.Family:
						this.__WriteVisibilitySpecifier(AccessSpecifier.Protected,false);
						break;
					case MethodVisibility.FamilyOrAssembly:
						this.__WriteVisibilitySpecifier(AccessSpecifier.InternalProtected,false);
						break;
					case MethodVisibility.Public:
						this.__WriteVisibilitySpecifier(AccessSpecifier.Public,false);
						break;
				}
			}

			private void __WriteVisibilitySpecifier(MethodVisibility visibility,bool inline){
				switch(visibility){
					case MethodVisibility.PrivateScope:
						this.__WriteVisibilitySpecifier(AccessSpecifier.PrivateScope,inline);
						break;
					case MethodVisibility.Private:
						this.__WriteVisibilitySpecifier(AccessSpecifier.Private,inline);
						break;
					case MethodVisibility.FamilyAndAssembly:
						this.__WriteVisibilitySpecifier(AccessSpecifier.PrivateProtected,inline);
						break;
					case MethodVisibility.Assembly:
						this.__WriteVisibilitySpecifier(AccessSpecifier.Internal,inline);
						break;
					case MethodVisibility.Family:
						this.__WriteVisibilitySpecifier(AccessSpecifier.Protected,inline);
						break;
					case MethodVisibility.FamilyOrAssembly:
						this.__WriteVisibilitySpecifier(AccessSpecifier.InternalProtected,inline);
						break;
					case MethodVisibility.Public:
						this.__WriteVisibilitySpecifier(AccessSpecifier.Public,inline);
						break;
				}
			}

			private void __WriteVisibilitySpecifier(FieldVisibility visibility) {
				switch(visibility){
					case FieldVisibility.PrivateScope:
						this.__WriteVisibilitySpecifier(AccessSpecifier.PrivateScope,false);
						break;
					case FieldVisibility.Private:
						this.__WriteVisibilitySpecifier(AccessSpecifier.Private,false);
						break;
					case FieldVisibility.FamilyAndAssembly:
						this.__WriteVisibilitySpecifier(AccessSpecifier.PrivateProtected,false);
						break;
					case FieldVisibility.Assembly:
						this.__WriteVisibilitySpecifier(AccessSpecifier.Internal,false);
						break;
					case FieldVisibility.Family:
						this.__WriteVisibilitySpecifier(AccessSpecifier.Protected,false);
						break;
					case FieldVisibility.FamilyOrAssembly:
						this.__WriteVisibilitySpecifier(AccessSpecifier.InternalProtected,false);
						break;
					case FieldVisibility.Public:
						this.__WriteVisibilitySpecifier(AccessSpecifier.Public,false);
						break;
				}
			}

			private void __WriteTypeVisibilitySpecifier(TypeVisibility visibility) {
				switch(visibility) {
					case TypeVisibility.Private:
						this.WriteKeyword("private");
						goto writespace;

					case TypeVisibility.Public:
						this.WriteKeyword("public");
						goto writespace;
					writespace:
						this.Write(" ");
						break;

					case TypeVisibility.NestedPublic:
						this.__WriteVisibilitySpecifier(AccessSpecifier.Public,false);
						break;
					case TypeVisibility.NestedPrivate:
						this.__WriteVisibilitySpecifier(AccessSpecifier.Private,false);
						break;
					case TypeVisibility.NestedFamily:
						this.__WriteVisibilitySpecifier(AccessSpecifier.Protected,false);
						break;

					case TypeVisibility.NestedAssembly:
						this.__WriteVisibilitySpecifier(AccessSpecifier.Internal,false);
						break;

					case TypeVisibility.NestedFamilyAndAssembly:
						this.__WriteVisibilitySpecifier(AccessSpecifier.PrivateProtected,false);
						break;

					case TypeVisibility.NestedFamilyOrAssembly:
						this.__WriteVisibilitySpecifier(AccessSpecifier.InternalProtected,false);
						break;
				}
			}
			private static MethodVisibility __GetPropertyVisibility(IPropertyDeclaration propDecl,out IMethodDeclaration getter,out IMethodDeclaration setter) {
				IMethodReference getref=propDecl.GetMethod;
				IMethodReference setref=propDecl.SetMethod;
				if(getref==null){
					getter=null;
					if(setref==null){
						setter=null;
						return MethodVisibility.Private;
					}else{
						setter=setref.Resolve();
						return setter.Visibility;
					}
				}else{
					getter=getref.Resolve();
					if(setref==null){
						setter=null;
						return getter.Visibility;
					}else{
						setter=setref.Resolve();
						MethodVisibility max=(MethodVisibility)System.Math.Max((int)getter.Visibility,(int)setter.Visibility);
						MethodVisibility min=(MethodVisibility)System.Math.Min((int)getter.Visibility,(int)setter.Visibility);
						return min==MethodVisibility.Assembly&&max==MethodVisibility.Family?MethodVisibility.FamilyOrAssembly:max;
					}
				}
			}
		}
	}
}
