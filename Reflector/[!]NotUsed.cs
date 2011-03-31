using Reflector;
using LWriter_t=Reflector.Languages.CppCliLanguage.LanguageWriter;
using LanguageWriter=Reflector.Languages.CppCliLanguage.LanguageWriter;
using Reflector.CodeModel;
using System;
using System.Globalization;

namespace mwg{
	public static partial class LanguageWriterHelper{
		[System.Obsolete]
		public static int lock_statement_index=0;
		[System.Obsolete]
		public const int XT_BINARY_BASE=10;

		[System.Obsolete]
		public enum ExpressionType {
			Unknown				=0,
			biAdd					=XT_BINARY_BASE+BinaryOperator.Add,
			biSubtract				=XT_BINARY_BASE+BinaryOperator.Subtract,
			biMultiply				=XT_BINARY_BASE+BinaryOperator.Multiply,
			biDivide				=XT_BINARY_BASE+BinaryOperator.Divide,
			biModulus				=XT_BINARY_BASE+BinaryOperator.Modulus,
			biShiftLeft				=XT_BINARY_BASE+BinaryOperator.ShiftLeft,
			biShiftRight			=XT_BINARY_BASE+BinaryOperator.ShiftRight,
			biIdentityEquality		=XT_BINARY_BASE+BinaryOperator.IdentityEquality,
			biIdentityInequality	=XT_BINARY_BASE+BinaryOperator.IdentityInequality,
			biValueEquality			=XT_BINARY_BASE+BinaryOperator.ValueEquality,
			biValueInequality		=XT_BINARY_BASE+BinaryOperator.ValueInequality,
			biBitwiseOr				=XT_BINARY_BASE+BinaryOperator.BitwiseOr,
			biBitwiseAnd			=XT_BINARY_BASE+BinaryOperator.BitwiseAnd,
			biBitwiseExclusiveOr	=XT_BINARY_BASE+BinaryOperator.BitwiseExclusiveOr,
			biBooleanOr				=XT_BINARY_BASE+BinaryOperator.BooleanOr,
			biBooleanAnd			=XT_BINARY_BASE+BinaryOperator.BooleanAnd,
			biLessThan				=XT_BINARY_BASE+BinaryOperator.LessThan,
			biLessThanOrEqual		=XT_BINARY_BASE+BinaryOperator.LessThanOrEqual,
			biGreaterThan			=XT_BINARY_BASE+BinaryOperator.GreaterThan,
			biGreaterThanOrEqual	=XT_BINARY_BASE+BinaryOperator.GreaterThanOrEqual,
		}
	
		[System.Obsolete]
		public static ExpressionType GetExpressionType(IExpression expression){
			IBinaryExpression binaryExpression=expression as IBinaryExpression;
			if(binaryExpression!=null){
				return (ExpressionType)(int)binaryExpression.Operator;
			}
			return ExpressionType.Unknown;
		}
		//===========================================================
		//		型の名前
		//===========================================================
		[System.Obsolete]
		public static string GetFullNameOfType(IType iType) {
			IOptionalModifier modifier=iType as IOptionalModifier;
			if(modifier!=null) {
				string nameFromType=GetFullNameOfType(modifier.ElementType);
				return (nameFromType+(" "+modifier.Modifier.ToString()));
			}
			ITypeReference typeReference=iType as ITypeReference;
			if(typeReference!=null) {
				return GetFullNameOfType(typeReference);
			}
			return iType.ToString();
		}
		[System.Obsolete]
		private static string GetFullNameOfType(ITypeReference typeRef) {
			string name;
			if(typeRef.Namespace==""){
				IType t=typeRef.Owner as IType;
				if(t!=null){
					name=GetFullNameOfType(t)+"::"+typeRef.Name;
				}else{
					name=typeRef.Name;
				}
			}else{
				name=typeRef.Namespace+"::"+typeRef.Name;
			}

			ITypeCollection genericArguments=typeRef.GenericArguments;
			if(genericArguments.Count>0) {
				string str2="";
				name=name+"<";

				foreach(IType type in genericArguments){
					name=name+str2;
					str2=",";
					name=name+GetFullNameOfType(type);
				}
				name=name+">";
			}

			return name.Replace(".","::").Replace("+","::");
		}
		[System.Obsolete]
		public static string GetNameOfType(IType iType) {
			IOptionalModifier modifier=iType as IOptionalModifier;
			if(modifier!=null) {
				string nameFromType=GetNameOfType(modifier.ElementType);
				return (nameFromType+(" "+modifier.Modifier.ToString()));
			}
			ITypeReference typeReference=iType as ITypeReference;
			if(typeReference!=null) {
				return GetNameOfType(typeReference);
			}
			return iType.ToString();
		}
		[System.Obsolete]
		private static string GetNameOfType(ITypeReference typeRef) {
			string name=typeRef.Name;
			if((typeRef.Namespace=="System")&&LWriter_t.specialTypeNames.Contains(name)) {
				name=(string)LWriter_t.specialTypeNames[name];
			}

			ITypeCollection genericArguments=typeRef.GenericArguments;
			if(genericArguments.Count>0) {
				string str2="";
				name=name+"<";

				foreach(IType type in genericArguments){
					name=name+str2;
					str2=",";
					name=name+GetNameOfType(type);
				}
				name=name+">";
			}

			return name.Replace(".","::").Replace("+","::");
		}
		//===========================================================
		//		属性の書き出し
		//===========================================================
		[System.Obsolete]
		private void WriteCustomAttribute(ICustomAttribute attr) {
			IMethodReference meth_ref=attr.Constructor;
			TypeRef decltype=new TypeRef(meth_ref.DeclaringType);
			string name=decltype.Name;

			// 参照付きで名前を指定
			this.WriteReference(
				name.EndsWith("Attribute")?name.Substring(0,name.Length-9):name,
				string.Format("/* 属性 コンストラクタ */\r\n{0}::{1}({2});",
					decltype.FullName,
					decltype.Name,
					GetDesc(meth_ref.Parameters)
				),
				meth_ref
			);

			if(attr.Arguments.Count!=0){
				this.Write("(");
				this.WriteExpressionCollection(attr.Arguments);
				this.Write(")");
			}
		}

		[System.Obsolete]
		private string GetCustomAttributeName(ICustomAttribute iCustomAttribute) {
			//ITypeReference declaringType=iCustomAttribute.Constructor.DeclaringType as ITypeReference;
			//string nameFromTypeReference=this.GetNameFromTypeReference(declaringType);
			string nameFromTypeReference=new TypeRef(iCustomAttribute.Constructor.DeclaringType).Name;
			if(nameFromTypeReference.EndsWith("Attribute")) {
				nameFromTypeReference=nameFromTypeReference.Substring(0,nameFromTypeReference.Length-9);
			}
			return nameFromTypeReference;
		}
		
//			internal delegate bool CustomAttributeProc(ICustomAttribute attr);
//			private void WriteCustomAttributeCollection(ICustomAttributeProvider provider,IType type,CustomAttributeProc proc){
//			}
		private void WriteCustomAttributeCollection(ICustomAttributeProvider provider,IType type) {
			if(this.configuration["ShowCustomAttributes"]!="true"||provider==null||provider.Attributes.Count==0) {
				return;
			}

			Gen::List<ICustomAttribute> attrs=new System.Collections.Generic.List<ICustomAttribute>();
			bool containsParams=false;
			foreach(ICustomAttribute attr in provider.Attributes){
				if(attr==null)continue;
				switch(GetCustomAttributeName(attr)){
					case "ParamArray":
					case "System::ParamArray":
						containsParams=true;
						continue;
				
					case "MarshalAs":
					case "System::Runtime::InteropServices::MarshalAs":
						IExpressionCollection arguments=attr.Arguments;
						if(arguments==null)break;
						IFieldReferenceExpression exp_fld=arguments[0] as IFieldReferenceExpression;
						if(exp_fld==null)break;
						ITypeReferenceExpression target=exp_fld.Target as ITypeReferenceExpression;
						if(target==null||target.Type.Name!="UnmanagedType")break;
						IFieldReference field=exp_fld.Field;
						if(field.Name=="U1"){
							if(Type(type,"System","Boolean")) {
								continue;
							}
						}else if(field.Name=="U2"&&Type(type,"System","Char")){
							continue;
						}
						break;
				}

				attrs.Add(attr);
			}
#if FALSE
			ArrayList list=new ArrayList();
			list.AddRange(provider.Attributes);
			for(int i=list.Count-1;i>=0;i--) {
				ICustomAttribute iCustomAttribute=list[i] as ICustomAttribute;
				if(iCustomAttribute!=null) {
					switch(this.GetCustomAttributeName(iCustomAttribute)) {
						case "ParamArray":
						case "System::ParamArray":
							containsParams=true;
							list.RemoveAt(i);
							break;

						case "MarshalAs":
						case "System::Runtime::InteropServices::MarshalAs":
							IExpressionCollection arguments=iCustomAttribute.Arguments;
							if(arguments==null) break;
							IFieldReferenceExpression expression=arguments[0] as IFieldReferenceExpression;
							if(expression==null) break;
							ITypeReferenceExpression target=expression.Target as ITypeReferenceExpression;
							if(target==null||target.Type.Name!="UnmanagedType") break;
							IFieldReference field=expression.Field;
							if(field.Name=="U1") {
								if(this.Type(type,"System","Boolean")) {
									list.RemoveAt(i);
								}
							}else if(field.Name=="U2"&&this.Type(type,"System","Char")){
								list.RemoveAt(i);
							}
							break;
					}
				}
			}
#endif

			if(containsParams)this.Write("... ");
			
			bool isAsmOrModule=false;
			string s=null;
			if(provider is IAssembly){
				s="assembly";
				isAsmOrModule=true;
			}else if(provider is IModule){
				s="module";
				isAsmOrModule=true;
			}else if(provider is IMethodReturnType){
				s="returnvalue";
			}

			if(isAsmOrModule){
				foreach(ICustomAttribute attr in attrs) {
					this.Write("[");
					this.WriteKeyword(s);
					this.Write(": ");
					this.WriteCustomAttribute(attr);
					this.Write("]");
					this.WriteLine();
				}
			}else if(!this.SkipWriteLine){
				// 名前空間順に並び替え
				attrs.Sort(delegate(ICustomAttribute l,ICustomAttribute r){
					string l_name=((ITypeReference)l.Constructor.DeclaringType).Namespace;
					string r_name=((ITypeReference)r.Constructor.DeclaringType).Namespace;
					return l_name.CompareTo(r_name);
				});


				bool new_ns=true;
				string prev_ns=null;
				foreach(ICustomAttribute attr in attrs){
					// 名前空間の変わり目
					string ns=((ITypeReference)attr.Constructor.DeclaringType).Namespace;
					if(prev_ns!=ns){
						prev_ns=ns;
						if(!new_ns){
							this.Write("]");
							this.WriteLine();
							new_ns=true;
						}
					}

					// 新しい名前空間
					if(new_ns){
						this.Write("[");
						if(s!=null){
							this.WriteKeyword(s);
							this.Write(": ");
						}
						new_ns=false;
					}else{
						this.Write(", ");
					}

					this.WriteCustomAttribute(attr);
				}
				this.Write("]");
			}else{
				this.Write("[");
				if(s!=null){
					this.WriteKeyword(s);
					this.Write(": ");
				}

				bool first=true;
				foreach(ICustomAttribute attr in attrs){
					if(first)first=false;else this.Write(", ");
					this.WriteCustomAttribute(attr);
				}
				this.Write("]");
			}
		}
	}
}

namespace mwg.Reflector{
	public partial class StatementWriter{
		[System.Obsolete]
		private static void WriteUsing(LanguageWriter w,IUsingStatement state){
			//IVariableDeclaration var_decl=state.Variable as IVariableDeclaration;
			//if(var_decl==null)
			//	throw new System.Exception("using の形式が予想と異なります。予想: IVariableDeclaraion 実際: "+state.Variable.GetType().GetInterfaces()[1].ToString());

			IAssignExpression assig=state.Expression as IAssignExpression;
			if(assig==null)
				throw new InterfaceNotImplementedException("Unexpected using-statement expression interface",typeof(IAssignExpression),state.Expression);

			// 変数の宣言の場合
			do{
				IVariableDeclarationExpression var_decl_x=assig.Target as IVariableDeclarationExpression;
				if(var_decl_x==null)continue;

				IVariableDeclaration var_decl=var_decl_x.Variable as IVariableDeclaration;
				if(var_decl==null)continue;

				WriteLocalRefVariable(
					w,
					new LocalRefVariableStatement(var_decl,assig.Expression,state.Body)
				);
				return;
			}while(false);

			throw new InterfaceNotImplementedException("×実装中×",typeof(IVariableDeclarationExpression),assig.Target);
		}
	}
}

namespace mwg.Reflector.CppCli{
	public struct TypeRef{
		[System.Obsolete]
		private static string Modify(TypeRef modifier,string elemType,bool required) {
			if(modifier.IsType("System.Runtime.CompilerServices","Volatile")){
				return "volatile "+elemType;
			}else if(modifier.IsType("System.Runtime.CompilerServices","IsConst")){
				return "const "+elemType; // RefOnStack の場合には const は要らない
			}else if(modifier.IsType("System.Runtime.CompilerServices","IsLong")) {
				switch(elemType){
					//case "System::UInt32":
					case "unsigned int":
					case "UInt32":
						return "unsigned long";
					//case "System::Int32":
					case "int":
					case "Int32":
						return "long";
					//case "System::Double":
					case "double":
					case "Double":
						return "long double";
				}
			}else if(modifier.IsType("System.Runtime.CompilerServices","IsExplicitlyDereferenced")){
				if(elemType.EndsWith("%")){
					return "pinptr<"+elemType.Substring(0,elemType.Length-1)+">";
				}
			}

			return elemType+" mod"+(required?"req":"opt")+"("+modifier.Name+")";
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
		public partial class LanguageWriter:Helper,ILanguageWriter {

			[System.Obsolete]
			private void WriteMethodDeclMiddle(object methodDecl){
				IMethodDeclaration declaration=methodDecl as IMethodDeclaration;
				ITypeDeclaration declaration2=(declaration.DeclaringType as ITypeReference).Resolve();
				MethodNameExt ext=new MethodNameExt(declaration.Name,declaration.Overrides,declaration2.Name);
				this.WriteDeclaration(ext.Name);
				this.WriteMethodParameterCollection(declaration.Parameters);
			}

			[System.Obsolete]
			private bool WriteMethodDecl_ctor_deleg(IConstructorDeclaration ctor_decl){
				IMethodInvokeExpression initializer=ctor_decl.Initializer;
				if(initializer!=null){
					IMethodReferenceExpression method=initializer.Method as IMethodReferenceExpression;
					if(method!=null&&initializer.Arguments.Count!=0) {
						this.Write(" : ");
						this.WriteExpression(method.Target);
						this.Write("(");
						this.WriteExpressionCollection(initializer.Arguments);
						this.Write(")");
					}
					return true;
				}

				IBlockStatement block_body=ctor_decl.Body as IBlockStatement;
				if(block_body==null)return true;

#if EXTRA_TEMP
				IStatementCollection statements=block_body.Statements;
				this.GatherExtraTemporaries(statements);
				if(this.SkipNullptrCount>=statements.Count)return true;

				ITryCatchFinallyStatement state_tcf=statements[this.SkipNullptrCount] as ITryCatchFinallyStatement;
				if(state_tcf==null||state_tcf.Try==null)return true;
#else
				IStatementCollection statements=block_body.Statements;
				ITryCatchFinallyStatement state_tcf=statements[0] as ITryCatchFinallyStatement;
#endif

				foreach(IStatement state in state_tcf.Try.Statements) {
					IExpressionStatement state_exp=state as IExpressionStatement;
					if(state_exp==null)break;

					bool skip=false;
					switch(ExpressionWriter.GetExpressionType(state_exp.Expression)){
						case ExpressionWriter.ExpressionType.Assign:
							skip=((IAssignExpression)state_exp.Expression).Target is IFieldReferenceExpression;
							break;
						case ExpressionWriter.ExpressionType.VariableDeclaration:
							skip=true;
							break;
						case ExpressionWriter.ExpressionType.MethodInvoke:
							IMethodReferenceExpression meth_ref
								=((IMethodInvokeExpression)state_exp.Expression).Method
								as IMethodReferenceExpression;
							skip=meth_ref.Target is IBaseReferenceExpression;
							break;
						default:
							skip=false;
							break;
					}

					if(!skip)break;
					this.SkipTryCount++;
				}

				if(this.SkipTryCount==0)return true;

				this.WriteLine();
				this.WriteIndent();
				this.Write("try : ");

				bool first=true;
				for(int i=0;i<this.SkipTryCount;i++) {
					IExpressionStatement state_exp=state_tcf.Try.Statements[i] as IExpressionStatement;
					if(state_exp==null)break;

					switch(ExpressionWriter.GetExpressionType(state_exp.Expression)){
						case ExpressionWriter.ExpressionType.Assign:
							IAssignExpression exp_assign=(IAssignExpression)state_exp.Expression;
							IFieldReferenceExpression exp_fld=exp_assign.Target as IFieldReferenceExpression;
							if(first)first=false;else this.Write(", ");
							this.Write(exp_fld.Field.Name);
							this.Write("(");
							this.WriteExpression(exp_assign.Expression);
							this.Write(")");
							break;
						case ExpressionWriter.ExpressionType.VariableDeclaration:
							break;
						case ExpressionWriter.ExpressionType.MethodInvoke:
							if(first)first=false;else this.Write(", ");
							this.baseType.WriteName(this);
							this.Write("(");
							this.WriteExpressionCollection(((IMethodInvokeExpression)state_exp.Expression).Arguments);
							this.Write(")");
							break;
						default:
							throw new System.Exception("!! コンストラクタ デリゲーション !!");
					}
				}

				return false;
			}

			[System.Obsolete]
			internal void NotAReference(string Name) {
				this.refOnStack[Name]=Name;
			}

			[System.Obsolete]
			public Hashtable refOnStack;

			[System.Obsolete]
			internal int SkipTryCount;

			[System.Obsolete]
			internal void WriteVariableDeclaration(IVariableDeclaration iVariableDeclaration) {
				new TypeRef(iVariableDeclaration.VariableType).WriteNameWithRef(this);
				this.Write(" ");
				this.WriteDeclaration((iVariableDeclaration.Name as string)??"<不明な識別子>");
			}

			[System.Obsolete]
			private string GetVariableName(IExpression iExpression) {
				IVariableDeclarationExpression expression2=iExpression as IVariableDeclarationExpression;
				if(expression2!=null) {
					return expression2.Variable.Name;
				}
				IVariableReferenceExpression expression=iExpression as IVariableReferenceExpression;
				if(expression!=null) {
					return expression.Variable.Resolve().Name;
				}
				return null;
			}

			/// <summary>
			/// statementCollection の先頭に連なる変数宣言を集める
			/// 1. 一回目の loop で数を数える
			/// 2. 必要な大きさのコンテナを用意
			/// 2. 二回目の loop で格納
			/// </summary>
			/// <param name="iStatementCollection"></param>
			/// <returns></returns>
			[System.Obsolete]
			private int orig_GatherExtraTemporaries(IStatementCollection iStatementCollection) {
				if(this.ExtraTemporaries!=null) {
					goto Label_00CF;
				}
				int num2=0;
			Label_0013:
				if(num2>=2) {
					goto Label_00CF;
				}
				int index=0;
			Label_0022:
				if(index<iStatementCollection.Count) {
					IStatement statement=iStatementCollection[index];
					IAssignExpression expression=statement as IAssignExpression;
					if(expression!=null) {
						ILiteralExpression expression3=expression.Expression as ILiteralExpression;
						if((expression3!=null)&&(expression3.Value==null)) {
							IVariableDeclarationExpression target=expression.Target as IVariableDeclarationExpression;
							if(target!=null) {
								if(num2==1) {
									this.ExtraTemporaries[index]=target.Variable.Name;
								}
								index++;
								goto Label_0022;
							}
						}
					}
				}
				if(num2==0) {
					this.SkipNullptrCount=index;
					this.ExtraTemporaries=new string[index];
					this.ExtraMappings=new string[this.SkipNullptrCount];
					this.EssentialTemporaries=new bool[this.SkipNullptrCount];
					this.TemporaryBlocks=new int[this.SkipNullptrCount];
				}
				num2++;
				goto Label_0013;
			Label_00CF:
				return this.SkipNullptrCount;
			}

			#region WriteStatement 系統

			[System.Obsolete]
			[return: MarshalAs(UnmanagedType.U1)]
			internal bool BlankStatement(IStatement iStatement) {
				if(iStatement==null) {
					return true;
				}
				IBlockStatement statement=iStatement as IBlockStatement;
				if(statement!=null) {
					IStatementCollection statements=statement.Statements;
					if(statements!=null) {
						return (statements.Count==0);
					}
				}
				return false;
			}

			[System.Obsolete]
			private void WriteAttachEventStatement(IAttachEventStatement iAttachEventStatement) {
				this.WriteEventReferenceExpression(iAttachEventStatement.Event);
				this.Write(" += ");
				this.WriteExpression(iAttachEventStatement.Listener);
				this.Write(";");
				this.WriteLine();
			}

			[System.Obsolete]
			private void WriteBlockStatement(IBlockStatement blockStatement) {
				if(blockStatement.Statements.Count>0) {
					this.WriteStatementCollection(blockStatement.Statements,0);
				}
			}

			[System.Obsolete]
			private void WriteBreakStatement(IBreakStatement iBreakStatement) {
				this.WriteKeyword("break");
				this.Write(";");
				this.WriteLine();
			}

			[System.Obsolete]
			private void WriteConditionStatement(IConditionStatement iConditionStatement) {
				using(NewBlock block3=new NewBlock(this)) {
					this.WriteKeyword("if");
					this.Write(" ");
					this.Write("(");
					this.WriteExpression(iConditionStatement.Condition);
					this.Write(") ");

					using(NewBlock block2=new NewBlock(this)) {
						this.__WriteExtendedStatement(iConditionStatement.Then);
						if(!this.BlankStatement(iConditionStatement.Else)) {
							using(NewBlock block=new NewBlock(this)) {
								this.Write(" ");
								this.WriteKeyword("else");
								this.Write(" ");
								this.__WriteExtendedStatement(iConditionStatement.Else);
							}
						}
						this.WriteLine();
					}
				}
			}

			[System.Obsolete]
			private void WriteContinueStatement(IContinueStatement iContinueStatement) {
				this.WriteKeyword("continue");
				this.Write(";");
				this.WriteLine();
			}

			[System.Obsolete]
			private void WriteDoStatement(IDoStatement iDoStatement) {
				this.WriteKeyword("do");
				//				this.WriteLine();
				this.Write(" {");
				this.WriteLine();
				this.WriteIndent();
				if(iDoStatement.Body!=null) {
					this.WriteStatement(iDoStatement.Body);
				}
				this.WriteOutdent();
				this.Write("} ");
				//				this.WriteLine();
				this.WriteKeyword("while");
				this.Write("(");
				if(iDoStatement.Condition!=null) {
					this.SkipWriteLine=true;
					this.WriteExpression(iDoStatement.Condition);
					this.SkipWriteLine=false;
				}
				this.Write(");");
				this.WriteLine();
			}

			[System.Obsolete]
			private void WriteEventReferenceExpression(IEventReferenceExpression iEventReferenceExpression) {
				if(iEventReferenceExpression.Target!=null) {
					ITypeReferenceExpression target=iEventReferenceExpression.Target as ITypeReferenceExpression;
					if(target!=null) {
						new TypeRef(target.Type).WriteName(this);
						//this.WriteTypeReferenceExpression(target);
						this.Write("::");
					} else {
						this.WriteExpression(iEventReferenceExpression.Target);
						IVariableReferenceExpression expression=iEventReferenceExpression.Target as IVariableReferenceExpression;
						if(expression!=null) {
							IVariableReference variable=expression.Variable;
							if(variable!=null) {
								IVariableDeclaration declaration=variable.Resolve();
								if(declaration!=null) {
									ITypeReference variableType=declaration.VariableType as ITypeReference;
									if(base.IsValueType(variableType)) {
										this.Write(".");
									} else {
										this.Write("->");
									}
								}
							}
						} else {
							this.Write("->");
						}
					}
				}
			}

			[System.Obsolete]
			private void WriteExpressionStatement(IExpressionStatement statement1) {
				this.WriteExpression(statement1.Expression);
				if(!this.SkipWriteLine) {
					this.Write(";");
					this.WriteLine();
				}
			}

			[System.Obsolete]
			private void WriteExtendedStatement(IStatement statement) {
				this.Write("{");
				this.WriteLine();
				this.WriteIndent();
				this.WriteStatement(statement);
				this.WriteOutdent();
				this.Write("}");
				this.WriteLine();
			}

			[System.Obsolete]
			private void WriteForEachStatement(IForEachStatement iForEachStatement) {
				this.WriteKeyword("for each");
				this.Write(" ");
				this.Write("(");
				this.WriteVariableDeclaration(iForEachStatement.Variable);
				this.Write(" ");
				this.WriteKeyword("in");
				this.Write(" ");
				this.SkipWriteLine=true;
				this.WriteExpression(iForEachStatement.Expression);
				this.SkipWriteLine=false;
				//this.Write(")");
				//this.WriteLine();
				//this.Write("{");
				this.Write(") {");
				this.WriteLine();
				this.WriteIndent();
				if(iForEachStatement.Body!=null) {
					this.WriteBlockStatement(iForEachStatement.Body);
				}
				this.WriteOutdent();
				this.Write("}");
				this.WriteLine();
			}

			[System.Obsolete]
			private void WriteForStatement(IForStatement iForStatement) {
				this.WriteKeyword("for");
				this.Write(" ");
				this.Write("(");
				if(iForStatement.Initializer!=null) {
					this.SkipWriteLine=true;
					this.WriteStatement(iForStatement.Initializer);
					this.SkipWriteLine=false;
					this.Write(" ");
				}
				this.Write("; ");
				if(iForStatement.Condition!=null) {
					this.SkipWriteLine=true;
					this.WriteExpression(iForStatement.Condition);
					this.SkipWriteLine=false;
				}
				this.Write("; ");
				if(iForStatement.Increment!=null) {
					this.SkipWriteLine=true;
					this.WriteStatement(iForStatement.Increment);
					this.SkipWriteLine=false;
				}
				//				this.Write(")");
				//				this.WriteLine();
				//				this.Write("{");
				this.Write(") {");
				this.WriteLine();
				this.WriteIndent();
				if(iForStatement.Body!=null) {
					this.WriteStatement(iForStatement.Body);
				}
				this.WriteOutdent();
				this.Write("}");
				this.WriteLine();
			}

			[System.Obsolete]
			private void WriteGotoStatement(IGotoStatement iGotoStatement) {
				this.WriteKeyword("goto");
				this.Write(" ");
				this.WriteDeclaration(iGotoStatement.Name);
				this.Write(";");
				this.WriteLine();
			}

			[System.Obsolete]
			private void WriteLabeledStatement(ILabeledStatement iLabeledStatement) {
				this.__WriteLabel(iLabeledStatement.Name);
				if(iLabeledStatement.Statement!=null) {
					this.WriteStatement(iLabeledStatement.Statement);
				}
			}

			[System.Obsolete]
			private void WriteLockStatement(ILockStatement statement) {
				this.Write("{");
				this.WriteIndent();
				this.WriteLine();

				// msclr::lock を初期化
				this.WriteReference("msclr","",null);
				this.Write("::");
				this.WriteReference("lock","#include <msclr/lock.h> で使用して下さい",null);
				this.Write(" ");
				this.WriteDeclaration("lock_statement_"+mwg.LanguageWriterHelper.lock_statement_index++.ToString("X4"));
				this.Write("(");
				this.WriteExpression(statement.Expression);
				this.Write(");");
				this.WriteLine();

				// 中身を書込
				if(statement.Body!=null) {
					this.WriteBlockStatement(statement.Body);
				}

				this.WriteOutdent();
				this.Write("}");
				this.WriteLine();
			}

			[System.Obsolete]
			private void WriteMethodReturnStatement(IMethodReturnStatement iMethodReturnStatement) {
				this.WriteKeyword("return");
				if(iMethodReturnStatement.Expression!=null) {
					this.Write(" ");
					this.WriteExpression(iMethodReturnStatement.Expression);
				}
				this.Write(";");
				this.WriteLine();
			}

			[System.Obsolete]
			private void WriteRemoveEventStatement(IRemoveEventStatement iRemoveEventStatement) {
				this.WriteEventReferenceExpression(iRemoveEventStatement.Event);
				this.Write(" -= ");
				this.WriteExpression(iRemoveEventStatement.Listener);
				this.Write(";");
				this.WriteLine();
			}

			[System.Obsolete]
			public virtual void WriteStatement(IStatement statement) {
				try {
					ILockStatement statement20=statement as ILockStatement;
					if(statement20!=null) {
						this.WriteLockStatement(statement20);
						return;
					}
					IBreakStatement iBreakStatement=statement as IBreakStatement;
					if(iBreakStatement!=null) {
						this.WriteBreakStatement(iBreakStatement);
						return;
					}
					IDoStatement iDoStatement=statement as IDoStatement;
					if(iDoStatement!=null) {
						this.WriteDoStatement(iDoStatement);
						return;
					}
					IGotoStatement iGotoStatement=statement as IGotoStatement;
					if(iGotoStatement!=null) {
						this.WriteGotoStatement(iGotoStatement);
						return;
					}
					ILabeledStatement iLabeledStatement=statement as ILabeledStatement;
					if(iLabeledStatement!=null) {
						this.WriteLabeledStatement(iLabeledStatement);
						return;
					}
					IMethodReturnStatement iMethodReturnStatement=statement as IMethodReturnStatement;
					if(iMethodReturnStatement!=null) {
						this.WriteMethodReturnStatement(iMethodReturnStatement);
						return;
					}
					IForStatement iForStatement=statement as IForStatement;
					if(iForStatement!=null) {
						this.WriteForStatement(iForStatement);
						return;
					}
					IWhileStatement iWhileStatement=statement as IWhileStatement;
					if(iWhileStatement!=null) {
						this.WriteWhileStatement(iWhileStatement);
						return;
					}
					IConditionStatement iConditionStatement=statement as IConditionStatement;
					if(iConditionStatement!=null) {
						this.WriteConditionStatement(iConditionStatement);
						return;
					}
					IBlockStatement blockStatement=statement as IBlockStatement;
					if(blockStatement!=null) {
						this.WriteBlockStatement(blockStatement);
						return;
					}
					IForEachStatement iForEachStatement=statement as IForEachStatement;
					if(iForEachStatement!=null) {
						this.WriteForEachStatement(iForEachStatement);
						return;
					}
					IThrowExceptionStatement iThrowExceptionStatement=statement as IThrowExceptionStatement;
					if(iThrowExceptionStatement!=null) {
						this.WriteThrowExceptionStatement(iThrowExceptionStatement);
						return;
					}

					ITryCatchFinallyStatement statement8=statement as ITryCatchFinallyStatement;
					if(statement8!=null) {
						this.WriteTryCatchFinallyStatement(statement8);
						return;
					}

					IExpressionStatement statement7=statement as IExpressionStatement;
					if(statement7!=null) {
						this.WriteExpressionStatement(statement7);
						return;
					}

					IAttachEventStatement iAttachEventStatement=statement as IAttachEventStatement;
					if(iAttachEventStatement!=null) {
						this.WriteAttachEventStatement(iAttachEventStatement);
						return;
					}

					IRemoveEventStatement iRemoveEventStatement=statement as IRemoveEventStatement;
					if(iRemoveEventStatement!=null) {
						this.WriteRemoveEventStatement(iRemoveEventStatement);
						return;
					}

					ISwitchStatement iSwitchStatement=statement as ISwitchStatement;
					if(iSwitchStatement!=null) {
						this.WriteSwitchStatement(iSwitchStatement);
						return;
					}

					IContinueStatement iContinueStatement=statement as IContinueStatement;
					if(iContinueStatement!=null) {
						this.WriteContinueStatement(iContinueStatement);
						return;
					}

					IUsingStatement iUsingStatement=statement as IUsingStatement;
					if(iUsingStatement!=null) {
						this.WriteUsingStatement(iUsingStatement);
						return;
					}

					IMemoryCopyStatement iMemoryCopyStatement=statement as IMemoryCopyStatement;
					if(iMemoryCopyStatement!=null) {
						this.__WriteMemoryCopyStatement(iMemoryCopyStatement);
						return;
					}

					try {
						this.WriteComment("/* 未対応 "+statement.ToString()+" */");
						this.WriteLine();
					} catch(Exception exception) {
						this.Write(exception.ToString());
						this.Write(";");
					}
				} finally {
					this.SkipWriteLine=false;
				}
			}

			[System.Obsolete]
			internal void WriteStatementCollection(IStatementCollection statementCollection,int First) {
				foreach(IStatement current in StatementAnalyze.ModifyStatements(statementCollection)){
#if FALSE
					if(current is LabelStatement){
						this.__WriteLabel(((LabelStatement)current).label_name);
					}else if(current is DeleteStatement){
						this.WriteKeyword("delete");
						this.Write(" ");
						this.WriteExpression(((DeleteStatement)current).deleteTarget);
						this.Write(";");
						this.WriteLine();
					}else if(current is LocalRefVariableStatement){
						LocalRefVariableStatement state_lrv=(LocalRefVariableStatement)current;

						this.Write("{");
						this.WriteLine();
						this.WriteIndent();
						if(!this.SuppressOutput) {
							this.NotAReference(state_lrv.var_name);
						}
						
						// 変数の初期化
						IOptionalModifier modopt=state_lrv.var_type as IOptionalModifier;
						if(modopt.Modifier.Namespace=="System.Runtime.CompilerServices"&&modopt.Modifier.Name=="IsConst"){
							state_lrv.var_type=modopt.ElementType;
						}
						new TypeRef(state_lrv.var_type).WriteName(this);

						// 変数名
						this.Write(" ");
						this.Write(state_lrv.var_name);

						// 初期化子
						IObjectCreateExpression exp_create=state_lrv.exp as IObjectCreateExpression;
						if(exp_create!=null){
							if(exp_create.Arguments.Count==0){
								this.WriteComment(" /* 既定のコンストラクタ */");
							}else{
								bool first=true;
								this.Write("(");
								foreach(IExpression exp_arg in exp_create.Arguments){
									if(first)first=false;else this.Write(", ");
									this.WriteExpression(exp_arg);
								}
								this.Write(")");
							}
						}else{
							this.WriteExpression(state_lrv.exp);
						}
						this.Write(";");

						// 後に続く内容
						this.WriteLine();
						this.WriteBlockStatement(state_lrv.block);
						this.WriteOutdent();
						this.Write("}");
						this.WriteLine();
						if(state_lrv.labelname!=null){
							this.__WriteLabel(state_lrv.labelname);
							this.Write(";");
							this.WriteLine();
						}
					}else if(current is DefaultConstructionStatement){
						DefaultConstructionStatement state_dc=(DefaultConstructionStatement)current;
						this.WriteType(state_dc.var_type,null,null,null,false);
						this.Write(" ");
						this.WriteDeclaration(state_dc.var_name);
						this.Write("; ");
						this.WriteComment("// 既定のコンストラクタ");
						this.WriteLine();
					}else{
						this.WriteStatement(current);
					}
#endif
					this.WriteStatement(current);
				}
			}

			[System.Obsolete]
			private void WriteSwitchCaseCondition(IExpression iConditionCase) {
				IBinaryExpression expression=iConditionCase as IBinaryExpression;
				if(expression!=null) {
					if(expression.Operator==BinaryOperator.BooleanOr) {
						this.WriteSwitchCaseCondition(expression.Left);
						this.WriteSwitchCaseCondition(expression.Right);
					}
				} else {
					this.WriteKeyword("case");
					this.Write(" ");
					this.WriteExpression(iConditionCase);
					this.Write(":");
					this.WriteLine();
				}
			}

			[System.Obsolete]
			private void WriteSwitchStatement(ISwitchStatement iSwitchStatement) {
				this.WriteKeyword("switch");
				this.Write(" (");
				this.WriteExpression(iSwitchStatement.Expression);
				//this.Write(")");
				//this.WriteLine();
				//this.Write("{");
				this.Write(") {");
				this.WriteLine();
				this.WriteIndent();
				foreach(ISwitchCase case3 in iSwitchStatement.Cases) {
					IConditionCase @case=case3 as IConditionCase;
					if(@case!=null) {
						this.WriteSwitchCaseCondition(@case.Condition);
						//this.Write("{");
						//this.WriteLine();
						this.WriteIndent();
						if(@case.Body!=null) {
							this.WriteStatement(@case.Body);
						}
						this.WriteOutdent();
						//this.Write("}");
						//this.WriteLine();
					}
					IDefaultCase case2=case3 as IDefaultCase;
					if(case2!=null) {
						this.WriteKeyword("default");
						this.Write(":");
						this.WriteLine();
						//this.Write("{");
						//this.WriteLine();
						this.WriteIndent();
						if(case2.Body!=null) {
							this.WriteStatement(case2.Body);
						}
						this.WriteOutdent();
						//this.Write("}");
						//this.WriteLine();
					}
				}
				this.WriteOutdent();
				this.Write("}");
				this.WriteLine();
			}

			[System.Obsolete]
			private void WriteThrowExceptionStatement(IThrowExceptionStatement iThrowExceptionStatement) {
				this.WriteKeyword("throw");
				if(iThrowExceptionStatement.Expression!=null) {
					this.Write(" ");
					this.WriteExpression(iThrowExceptionStatement.Expression);
				}
				this.Write(";");
				this.WriteLine();
			}

			[System.Obsolete]
			private void WriteTryCatchFinallyStatement(ITryCatchFinallyStatement statement) {
				int skipTryCount=this.SkipTryCount;
				this.SkipTryCount=0;
#if FALSE
				if(statement.Try!=null) {
					foreach(ICatchClause current in statement.CatchClauses) {
						if(current.Body.Statements.Count!=0)goto write_try;
					}

					if(statement.Finally.Statements.Count!=0)goto write_try;
					if(statement.Fault.Statements.Count!=0)goto write_try;

					// 中身が何もない場合
					this.WriteBlockStatement(statement.Try);
					if(skipTryCount!=0) {
						this.WriteOutdent();
						this.Write("}");
						this.WriteLine();
					}
					return;
				}
			write_try:
#endif

				if(skipTryCount==0) {
					this.WriteKeyword("try");
					//this.WriteLine();
					//this.Write("{");
					this.Write(" {");
					this.WriteLine();
					this.WriteIndent();
				}
				//
				//	try の中身
				//
				if(statement.Try!=null) {
					IStatementCollection statementCollection=statement.Try.Statements;
					if(statementCollection.Count>0) {
						for(int num2=0;num2<skipTryCount;num2++) {
							IStatement statement2=statementCollection[num2];
							IAssignExpression expression3=statement2 as IAssignExpression;
							if(expression3!=null) {
								if(expression3.Target is IFieldReferenceExpression) {
									continue;
								}
							} else {
								IExpressionStatement statement4=statement2 as IExpressionStatement;
								if(statement4!=null) {
									IMethodInvokeExpression expression2=statement4.Expression as IMethodInvokeExpression;
									if(expression2!=null) {
										IMethodReferenceExpression method=expression2.Method as IMethodReferenceExpression;
										if((method!=null)&&(method.Target is IBaseReferenceExpression)) {
											continue;
										}
									}
								}
							}

							this.WriteStatement(statement2);
						}
						this.WriteStatementCollection(statementCollection,skipTryCount);
					}
				}
				this.WriteOutdent();
				this.Write("}");
				//				this.WriteLine();

				//
				// catch 節
				//
				foreach(ICatchClause clause in statement.CatchClauses) {
					this.Write(" ");
					this.WriteKeyword("catch");
					ITypeReference variableType=(ITypeReference)clause.Variable.VariableType;
					bool flag3=clause.Variable.ToString().Length==0;
					bool flag2=base.IsObject(variableType);

					bool catchAll=false;
					IStatementCollection clause_body=clause.Body==null?null:clause.Body.Statements;

					// 特別な場合 catch(...)
					TypeRef type=new TypeRef(clause.Variable.VariableType);
					if(type.IsType("System","Object")&&clause.Condition!=null){
						ISnippetExpression iSnippent=clause.Condition as ISnippetExpression;
						if(iSnippent!=null&&iSnippent.Value=="?"&&StatementAnalyze.IsCatchAllClause(ref clause_body)){
							catchAll=true;
						}
					}

					if(catchAll){
						this.Write(" (...)");
					}else{
						if(!flag3||!flag2){
							this.Write(" (");
							type.WriteNameWithRef(this);
							this.Write(" ");
							this.WriteDeclaration(clause.Variable.Name);
							this.Write(")");
						}

						if(clause.Condition!=null) {
							this.Write(" ");
							this.WriteKeyword("when");
							this.Write(" ");
							this.Write("(");
							this.WriteExpression(clause.Condition);
							this.Write(")");
						}
					}

					this.Write(" {");
					this.WriteLine();
					this.WriteIndent();

					if(clause_body!=null) {
						for(int num=0;num<clause_body.Count;num++){
							IStatement statement3=clause_body[num];
							if(!this.SomeConstructor||num+1<clause_body.Count||!(statement3 is IThrowExceptionStatement)) {
								this.WriteStatement(statement3);
							}
						}
					}

					this.WriteOutdent();
					this.Write("}");
				}

				//
				// fault 節
				//
				if((statement.Fault!=null)&&(statement.Fault.Statements.Count>0)) {
					this.Write(" ");
					this.WriteKeyword("fault");
					//					this.WriteLine();
					//					this.Write("{");
					this.Write(" {");
					this.WriteLine();
					this.WriteIndent();
					if(statement.Fault!=null) {
						this.WriteStatement(statement.Fault);
					}
					this.WriteOutdent();
					this.Write("}");
					//					this.WriteLine();
				}

				//
				// finally 節
				//
				if((statement.Finally!=null)&&(statement.Finally.Statements.Count>0)) {
					this.Write(" ");
					this.WriteKeyword("finally");
					//this.WriteLine();
					//this.Write("{");
					this.Write(" {");
					this.WriteLine();
					this.WriteIndent();
					if(statement.Finally!=null) {
						this.WriteStatement(statement.Finally);
					}
					this.WriteOutdent();
					this.Write("}");
					//					this.WriteLine();
				}

				this.WriteLine();
			}

			[System.Obsolete]
			private void WriteUsingStatement(IUsingStatement iUsingStatement) {
			}

			[System.Obsolete]
			private void WriteWhileStatement(IWhileStatement iWhileStatement) {
				this.WriteKeyword("while");
				this.Write(" ");
				this.Write("(");
				if(iWhileStatement.Condition!=null) {
					this.SkipWriteLine=true;
					this.WriteExpression(iWhileStatement.Condition);
					this.SkipWriteLine=false;
				}
				//this.Write(")");
				//this.WriteLine();
				//this.Write("{");
				this.Write(") {");
				this.WriteLine();
				this.WriteIndent();
				if(iWhileStatement.Body!=null) {
					this.WriteStatement(iWhileStatement.Body);
				}
				this.WriteOutdent();
				this.Write("}");
				this.WriteLine();
			}

			[System.Obsolete]
			private void __WriteMemoryCopyStatement(IMemoryCopyStatement memcpyStatement){
				this.Write("::");
				this.WriteReference("memcpy","Crt 関数 #include <memory.h>",null);
				this.Write("(");
				this.WriteExpression(memcpyStatement.Destination);
				this.Write(", ");
				this.WriteExpression(memcpyStatement.Source);
				this.Write(", ");
				this.WriteExpression(memcpyStatement.Length);
				this.Write(");");
				this.WriteLine();
			}

			[System.Obsolete]
			private void __WriteExtendedStatement(IStatement statement) {
				this.Write("{");
				this.WriteLine();
				this.WriteIndent();
				this.WriteStatement(statement);
				this.WriteOutdent();
				this.Write("}");
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="var_deleted">削除される予定の変数</param>
			/// <param name="next_state">
			/// 本当に削除命令なのかを確認する為に、次の文を指定。
			/// if(disposable!=nullptr)disposable->Dispose(); なら OK
			/// </param>
			/// <returns>delete だったら、それを書き込んで true 。そうでなかったら、何もせずに戻る。</returns>
			[System.Obsolete]
			private bool __WriteDeleteStatement(string disposable_name,IExpression var_deleted,IStatement next_state){
				//---------------------------------------------------
				//		if(disposable==nullptr)
				//---------------------------------------------------
				// if
				IConditionStatement cond=next_state as IConditionStatement;
				if(cond==null)return false;

				// ==
				IBinaryExpression exp_bin=cond.Condition as IBinaryExpression;
				if(exp_bin==null||exp_bin.Operator!=BinaryOperator.IdentityInequality)return false;

				// disposable
				IVariableReferenceExpression exp_var=exp_bin.Left as IVariableReferenceExpression;
				if(exp_var==null||exp_var.Variable.Resolve().Name!=disposable_name)return false;

				// nullptr
				ILiteralExpression exp_lit=exp_bin.Right as ILiteralExpression;
				if(exp_lit==null||exp_lit.Value!=null)return false;

				//---------------------------------------------------
				//		disposable->Dispose();
				//---------------------------------------------------
				// 単文
				IStatementCollection states_then=cond.Then.Statements;
				if(states_then==null||states_then.Count!=1)return false;

				IExpressionStatement state_exp=states_then[0] as IExpressionStatement;
				if(state_exp==null)return false;
				
				// **->**();
				IMethodInvokeExpression exp_inv=state_exp.Expression as IMethodInvokeExpression;
				if(exp_inv==null||exp_inv.Arguments.Count!=0)return false;

				// **->Dispose();
				IMethodReferenceExpression ref_dispose=exp_inv.Method as IMethodReferenceExpression;
				if(ref_dispose==null||ref_dispose.Method.Name!="Dispose")return false;

				// disposable->Dispose();
				exp_var=ref_dispose.Target as IVariableReferenceExpression;
				if(exp_var==null||exp_var.Variable.Resolve().Name!=disposable_name)return false;

				//---------------------------------------------------
				//		delete disposable
				//---------------------------------------------------
				this.WriteKeyword("delete");
				this.Write(" ");
				this.WriteExpression(var_deleted);
				this.Write(";");
				this.WriteLine();
				return true;
			}
			#endregion

			#region WriteExpression 系統

			[System.Obsolete]
			private void WriteAddressOutExpression(IAddressOutExpression expression) {
				this.WriteExpression(expression.Expression);
			}

			[System.Obsolete]
			private void WriteAddressReferenceExpression(IAddressReferenceExpression expression) {
				this.WriteExpression(expression.Expression);
			}

			[System.Obsolete]
			private void WriteDelegateCreateExpression(IDelegateCreateExpression iDelegateCreateExpression) {
				this.Write("(IDelegateCreateExpression NYI)");
			}

			[System.Obsolete]
			private void WriteAddressDereferenceExpression(IAddressDereferenceExpression expression) {
				this.Write("*(");
				this.WriteExpression(expression.Expression);
				this.Write(")");
			}

			[System.Obsolete]
			private void WriteAddressOfExpression(IAddressOfExpression expression) {
				this.Write("&");
				this.WriteExpression(expression.Expression);
			}

			[System.Obsolete]
			private void WriteArgumentListExpression(IArgumentListExpression expression) {
				this.WriteKeyword("__arglist");
			}

			[System.Obsolete]
			private void WriteArrayCreateExpression(IArrayCreateExpression expression1) {
				IBlockExpression initializer=expression1.Initializer;
				if(initializer!=null) {
					this.WriteBlockExpression(initializer);
				} else {
					this.WriteKeyword("gcnew");
					this.Write(" ");
					this.WriteKeyword("array");
					this.Write("<");
					this.WriteType(expression1.Type,null,null,null,false);
					if(expression1.Dimensions.Count>1) {
						this.Write(", "+expression1.Dimensions.Count.ToString());
					}
					this.Write(">");
					this.Write("(");
					this.WriteExpressionCollection(expression1.Dimensions);
					this.Write(")");
				}
			}

			[System.Obsolete]
			private void WriteArrayIndexerExpression(IArrayIndexerExpression expression) {
				this.WriteExpression(expression.Target);
				this.Write("[");
				bool first=true;
				foreach(IExpression expression2 in expression.Indices) {
					if(first) {
						first=false;
					} else {
						this.Write(",");
					}
					this.WriteExpression(expression2);
				}
				this.Write("]");
			}

			[System.Obsolete]
			private void WriteAssignExpression(IAssignExpression iAssignExpression) {
				IVariableDeclarationExpression target=iAssignExpression.Target as IVariableDeclarationExpression;
				IVariableReferenceExpression expression2=iAssignExpression.Target as IVariableReferenceExpression;
				IVariableReferenceExpression expression=iAssignExpression.Expression as IVariableReferenceExpression;
				if(this.ExtraTemporaries!=null) {
					for(int i=0;i<this.ExtraTemporaries.Length;i++) {
						if(!this.EssentialTemporaries[i]&&((target!=null)&&(target.Variable.Name==this.ExtraTemporaries[i]))) {
							return;
						}
					}
					if((expression2!=null)&&(expression!=null)) {
						int index=-1;
						int num2=-1;
						for(int j=0;j<this.ExtraTemporaries.Length;j++) {
							if(expression2.Variable.ToString()==this.ExtraTemporaries[j]) {
								index=j;
								this.VerifyCorrectBlock(j);
							}
							if(expression.Variable.ToString()==this.ExtraTemporaries[j]) {
								num2=j;
								this.VerifyCorrectBlock(j);
							}
						}
						if((index>=0)&&!this.EssentialTemporaries[index]) {
							string str;
							if((num2>=0)&&!this.EssentialTemporaries[num2]) {
								if(this.ExtraMappings[num2]==null) {
									this.EssentialTemporaries[num2]=true;
									str=null;
								} else {
									str=this.ExtraMappings[num2];
								}
							} else {
								str=expression.Variable.ToString();
							}
							if((str!=null)&&(expression2.Variable.ToString()[0]==str[0])) {
								this.ExtraMappings[index]=str;
								return;
							}
						}
					}
				}
				this.WriteExpression(iAssignExpression.Target);
				this.Write(" = ");
				this.WriteExpression(iAssignExpression.Expression);
			}

			[System.Obsolete]
			private void WriteBaseReferenceExpression(IBaseReferenceExpression iBaseReferenceExpression) {
				this.baseType.WriteName(this);
			}

			[System.Obsolete]
			private void WriteBinaryExpression(IBinaryExpression iBinaryExpression) {
				this.Write("(");
				this.WriteExpression(iBinaryExpression.Left);
				this.Write(" ");
				this.WriteBinaryOperator(iBinaryExpression.Operator);
				this.Write(" ");
				this.WriteExpression(iBinaryExpression.Right);
				this.Write(")");
			}

			[System.Obsolete]
			private void WriteBlockExpression(IBlockExpression expression1) {
				this.Write("{");
				this.WriteExpressionCollection(expression1.Expressions);
				this.Write("}");
			}

			[System.Obsolete]
			private void WriteCanCastExpression(ICanCastExpression iCanCastExpression) {
				this.Write("(");
				this.WriteKeyword("dynamic_cast");
				this.Write("<");
				this.WriteType(iCanCastExpression.TargetType,null,null,null,false);
				this.Write(">");
				this.Write("(");
				this.WriteExpression(iCanCastExpression.Expression);
				this.Write(")");
				this.Write(" != ");
				this.WriteLiteral("nullptr");
				this.Write(")");
			}

			[System.Obsolete]
			private void WriteCastExpression(ICastExpression iCastExpression) {
				this.Write("((");
				this.WriteType(iCastExpression.TargetType,null,null,null,false);
				this.Write(") ");
				this.WriteExpression(iCastExpression.Expression);
				this.Write(")");
			}

			[System.Obsolete]
			private void WriteConditionExpression(IConditionExpression expression1) {
				this.Write("(");
				this.WriteExpression(expression1.Condition);
				this.Write(" ? ");
				this.WriteExpression(expression1.Then);
				this.Write(" : ");
				this.WriteExpression(expression1.Else);
				this.Write(")");
			}

			[System.Obsolete]
			private void WriteUnaryExpression(IUnaryExpression iUnaryExpression) {
				switch(iUnaryExpression.Operator) {
					case UnaryOperator.Negate:
						this.Write("-");
						this.WriteExpression(iUnaryExpression.Expression);
						break;

					case UnaryOperator.BooleanNot:
						this.Write("!");
						this.WriteExpression(iUnaryExpression.Expression);
						break;

					case UnaryOperator.BitwiseNot:
						this.Write("~");
						this.WriteExpression(iUnaryExpression.Expression);
						break;

					case UnaryOperator.PreIncrement:
						this.Write("++");
						this.WriteExpression(iUnaryExpression.Expression);
						break;

					case UnaryOperator.PreDecrement:
						this.Write("--");
						this.WriteExpression(iUnaryExpression.Expression);
						break;

					case UnaryOperator.PostIncrement:
						this.WriteExpression(iUnaryExpression.Expression);
						this.Write("++");
						break;

					case UnaryOperator.PostDecrement:
						this.WriteExpression(iUnaryExpression.Expression);
						this.Write("--");
						break;

					default:
						throw new NotSupportedException(iUnaryExpression.Operator.ToString());
				}
			}

			[System.Obsolete]
			private void WriteArgumentReferenceExpression(IArgumentReferenceExpression expression1) {
				IParameterReference paramref=expression1.Parameter;
				this.WriteParameterReference(paramref);
			}

			[System.Obsolete]
			private void WriteVariableReferenceExpression(IVariableReferenceExpression iVariableReferenceExpression) {
				IVariableDeclaration vardecl=iVariableReferenceExpression.Variable.Resolve();
				string name=vardecl.Name;
				string name2=this.MapTemporaryName(name);
				string desc="/* ローカル変数 */\r\n"+new TypeRef(vardecl.VariableType).NameWithRef+" "+name2+";";
				//object o=iVariableReferenceExpression;
				//if(name2!=name)o=null;

				this.WriteReference(name2,desc,null);
			}

			[System.Obsolete]
			private void WriteFieldReferenceExpression(IFieldReferenceExpression iFieldReferenceExpression) {
				if(iFieldReferenceExpression.Target!=null) {
					ITypeReferenceExpression target=iFieldReferenceExpression.Target as ITypeReferenceExpression;
					if(target!=null) {
						this.WriteTypeReferenceExpression(target);
						this.Write("::");
					} else {
						this.WriteExpression(iFieldReferenceExpression.Target);
						IVariableReferenceExpression expression=iFieldReferenceExpression.Target as IVariableReferenceExpression;
						if(expression!=null) {
							IVariableReference variable=expression.Variable;
							if(variable!=null) {
								IVariableDeclaration declaration=variable.Resolve();
								if(declaration!=null) {
									ITypeReference variableType=declaration.VariableType as ITypeReference;
									if(base.IsValueType(variableType)) {
										this.Write(".");
									} else {
										this.Write("->");
									}
								}
							}
						} else {
							this.Write("->");
						}
					}
				}
				this.WriteFieldReference(iFieldReferenceExpression.Field);
			}

			[System.Obsolete]
			private void WriteLiteralExpression(ILiteralExpression literalExpression) {
				this.WriteAsLiteral(literalExpression.Value);
			}

			[System.Obsolete]
			private void WriteMemberInitializerExpression(IMemberInitializerExpression expression1) {
				this.WriteMemberReference(expression1.Member);
				this.Write(" = ");
				this.WriteExpression(expression1.Value);
			}

			[System.Obsolete]
			private void WriteMethodInvokeExpression(IMethodInvokeExpression iMethodInvokeExpression) {
				this.WriteExpression(iMethodInvokeExpression.Method);
				this.Write("(");
				this.WriteExpressionCollection(iMethodInvokeExpression.Arguments);
				this.Write(")");
			}

			[System.Obsolete]
			private void WriteMethodReferenceExpression(IMethodReferenceExpression iMethodReferenceExpression) {
				if(iMethodReferenceExpression.Target!=null) {
					ITypeReferenceExpression target=iMethodReferenceExpression.Target as ITypeReferenceExpression;
					if(target!=null) {
						this.WriteTypeReferenceExpression(target);
						this.Write("::");
					} else {
						this.WriteExpression(iMethodReferenceExpression.Target);
						IVariableReferenceExpression expression=iMethodReferenceExpression.Target as IVariableReferenceExpression;
						if(expression!=null) {
							IVariableReference variable=expression.Variable;
							if(variable!=null) {
								IVariableDeclaration declaration=variable.Resolve();
								if(declaration!=null) {
									if(base.IsValueType(declaration.VariableType as ITypeReference)||this.refOnStack.Contains(this.MapTemporaryName(variable.ToString()))) {
										this.Write(".");
									} else {
										this.Write("->");
									}
								}
							}
						} else {
							this.Write("->");
						}
					}
				}
				this.WriteMethodReference(iMethodReferenceExpression.Method);
			}

			[System.Obsolete]
			private void WriteObjectCreateExpression(IObjectCreateExpression iObjectCreateExpression) {
				if(iObjectCreateExpression.Constructor==null) {
					// 構造体のデフォルトコンストラクタ (StatementAnalyze でフィルタリングされている筈だけれども)
					this.WriteType(iObjectCreateExpression.Type,null,null,null,true);
					this.Write("(");
#warning 構造体デフォルトコンストラクタ 対応漏れがあるかもしれないので保留
					this.WriteComment("/* C++/CLI では本来は T value; 等と書く筈です。*/");
					this.Write(") ");
				} else {
					ITypeReference declaringType=iObjectCreateExpression.Constructor.DeclaringType as ITypeReference;
					if(declaringType!=null) {
						if(!base.IsValueType(declaringType)) {
							this.WriteKeyword("gcnew");
							this.Write(" ");
						}
						//this.__WriteTypeReference(declaringType);
						new TypeRef(declaringType).WriteName(this);
					}
					this.Write("(");
					this.WriteExpressionCollection(iObjectCreateExpression.Arguments);
					this.Write(") ");
				}
			}

			[System.Obsolete]
			private void WritePropertyReferenceExpression(IPropertyReferenceExpression iPropertyReferenceExpression) {
				if(iPropertyReferenceExpression.Target!=null) {
					ITypeReferenceExpression target=iPropertyReferenceExpression.Target as ITypeReferenceExpression;
					if(target!=null) {
						this.WriteTypeReferenceExpression(target);
						this.Write("::");
					} else {
						this.WriteExpression(iPropertyReferenceExpression.Target);
						IVariableReferenceExpression expression=iPropertyReferenceExpression.Target as IVariableReferenceExpression;
						if(expression!=null) {
							IVariableReference variable=expression.Variable;
							if(variable!=null) {
								IVariableDeclaration declaration=variable.Resolve();
								if(declaration!=null) {
									ITypeReference variableType=declaration.VariableType as ITypeReference;
									if(base.IsValueType(variableType)) {
										this.Write(".");
									} else {
										this.Write("->");
									}
								}
							}
						} else {
							this.Write("->");
						}
					}
				}
				this.WritePropertyReference(iPropertyReferenceExpression.Property);
			}

			[System.Obsolete]
			private void WritePropertyIndexerExpression(IPropertyIndexerExpression iPropertyIndexerExpression) {
				this.WritePropertyReferenceExpression(iPropertyIndexerExpression.Target);
				this.Write("[");
				this.WriteExpressionCollection(iPropertyIndexerExpression.Indices);
				this.Write("]");
			}

			[System.Obsolete]
			private void WriteSnippetExpression(ISnippetExpression iSnippent) {
				//this.Write("(ISnippetExpression NYI)");
				this.WriteAsLiteral(iSnippent.Value);
			}

			[System.Obsolete]
			private void WriteThisReferenceExpression(IThisReferenceExpression iThisExpression) {
				this.WriteKeyword("this");
			}

			[System.Obsolete]
			private void WriteTryCastExpression(ITryCastExpression iTryCastExpression) {
				this.WriteKeyword("dynamic_cast");
				this.Write("<");
				this.WriteType(iTryCastExpression.TargetType,null,null,null,false);
				this.Write(">");
				this.Write("(");
				this.WriteExpression(iTryCastExpression.Expression);
				this.Write(")");
			}

			[System.Obsolete]
			private void WriteTypeOfExpression(ITypeOfExpression iTypeOfExpression) {
				//this.WriteType(iTypeOfExpression.Type,null,null,null,false);
				new TypeRef(iTypeOfExpression.Type).WriteName(this);
				this.Write("::");
				this.WriteKeyword("typeid");
			}

			[System.Obsolete]
			private void WriteTypeReferenceExpression(ITypeReferenceExpression iTypeReferenceExpression) {
				if(iTypeReferenceExpression.Type.Name!="UnmanagedType"&&iTypeReferenceExpression.Type.Name!="<Module>") {
					//this.__WriteTypeReference(iTypeReferenceExpression.Type);
					new TypeRef(iTypeReferenceExpression.Type).WriteName(this);
				}
			}
			#endregion

			#region WriteType 系統
			[System.Obsolete]
			public static Hashtable specialTypeNames=new Hashtable();
			[System.Obsolete]
			static void InitializeSpecialTypeNames() {
				specialTypeNames["Void"]="void";
				specialTypeNames["Boolean"]="bool";
				specialTypeNames["Char"]="wchar_t";
				specialTypeNames["SByte"]="char";
				specialTypeNames["Byte"]="unsigned char";
				specialTypeNames["Int16"]="short";
				specialTypeNames["UInt16"]="unsigned short";
				specialTypeNames["Int32"]="int";
				specialTypeNames["UInt32"]="unsigned int";
				specialTypeNames["Int64"]="long long";
				specialTypeNames["UInt64"]="unsigned long long";
				specialTypeNames["Single"]="float";
				specialTypeNames["Double"]="double";
			}

			[System.Obsolete]
			private void __WriteTypeReference(string name,ITypeReference typeReference) {
				string t=(typeReference.Namespace+"."+typeReference.Name).Replace(".","::").Replace("+","::");
				this.WriteReference(name,t,typeReference);
			}
			[System.Obsolete]
			private void __WriteTypeReference(ITypeReference typeReference) {
				string name=typeReference.Name;
				if((typeReference.Namespace=="System")&&specialTypeNames.Contains(name)) {
					name=(string)specialTypeNames[name];
				}
				name=name.Replace(".","::").Replace("+","::");

				string t=(typeReference.Namespace+"."+typeReference.Name).Replace(".","::").Replace("+","::");
				this.WriteReference(name,t,typeReference);

				// Generic Arguments の書込
				ITypeCollection genericArguments=typeReference.GenericArguments;
				if(genericArguments.Count>0) {
					bool first=true;
					this.Write("<");
					foreach(IType type in genericArguments){
						if(first){
							first=false;
						}else{
							this.Write(",");
						}
						this.WriteType(type,null,null,null,false);
					}
					this.Write(">");
				}
			}

			[System.Obsolete]
			private void WriteModifiedType(ITypeReference modifier,IType type,[MarshalAs(UnmanagedType.U1)] bool required,[MarshalAs(UnmanagedType.U1)] bool fRefOnStack) {
				if(this.Type(modifier,"System.Runtime.CompilerServices","Volatile")) {
					this.WriteKeyword("volatile");
					this.Write(" ");
					this.WriteType(type,null,null,null,fRefOnStack);
					return;
				} else if(this.Type(modifier,"System.Runtime.CompilerServices","IsConst")) {
					if(!fRefOnStack) {
						this.WriteKeyword("const");
						this.Write(" ");
					}
					this.WriteType(type,null,null,null,fRefOnStack);
					return;
				} else if(this.Type(modifier,"System.Runtime.CompilerServices","IsLong")) {
					if(this.Type(type,"System","UInt32")) {
						this.__WriteTypeReference("unsigned long",(ITypeReference)type);
						//this.WriteKeyword("unsigned long");
						return;
					} else if(this.Type(type,"System","Int32")) {
						this.__WriteTypeReference("long",(ITypeReference)type);
						return;
					}
				} else if(this.Type(modifier,"System.Runtime.CompilerServices","IsExplicitlyDereferenced")) {
					IReferenceType ireftype=type as IReferenceType;
					if(ireftype!=null) {
						this.WriteKeyword("pin_ptr");
						this.Write("<");
						this.WriteType(ireftype.ElementType,null,null,null,false);
						this.Write(">");
						return;
					}
				}

				string str;
				this.WriteType(type,null,null,null,fRefOnStack);
				this.Write(" ");
				if(required) {
					str="modreq";
				} else {
					str="modopt";
				}
				this.WriteKeyword(str);
				this.Write("(");
				this.WriteType(modifier,null,null,null,false);
				this.Write(")");
			}

			[System.Obsolete]
			public virtual string GetNameFromType(IType iType) {
				IOptionalModifier modifier=iType as IOptionalModifier;
				if(modifier!=null) {
					string nameFromType=this.GetNameFromType(modifier.ElementType);
					//					if(0!=0) {
					//						if(nameFromType=="int") {
					//							nameFromType="long";
					//						}
					//						return nameFromType;
					//					}
					return (nameFromType+(" "+modifier.Modifier.ToString()));
				}
				ITypeReference typeReference=iType as ITypeReference;
				if(typeReference!=null) {
					return this.GetNameFromTypeReference(typeReference);
				}
				return iType.ToString();
			}

			[System.Obsolete]
			private string GetNameFromTypeReference(ITypeReference typeReference) {
				string name=typeReference.Name;
				if((typeReference.Namespace=="System")&&specialTypeNames.Contains(name)) {
					name=(string)specialTypeNames[name];
				}

				ITypeCollection genericArguments=typeReference.GenericArguments;
				if(genericArguments.Count>0) {
					string str2="";
					name=name+"<";
					foreach(IType type in genericArguments) {
						name=name+str2;
						str2=",";
						name=name+this.GetNameFromType(type);
					}
					name=name+">";
				}

				return name.Replace(".","::").Replace("+","::");
			}

			[System.Obsolete]
			public override void TypeWriter(StringWriter writer,IType iType) {
				writer.Write(this.GetNameFromType(iType));
			}

			[System.Obsolete]
			private void WriteTypeReference(ITypeReference typeReference) {
				string nameFromTypeReference=this.GetNameFromTypeReference(typeReference);
				string t=(typeReference.Namespace+"."+typeReference.Name).Replace(".","::").Replace("+","::");
				this.WriteReference(nameFromTypeReference,t,typeReference);
			}
			#endregion

			#region WriteVisibility 系統
			[System.Obsolete]
			private void WriteFieldVisibilitySpecifier(FieldVisibility visibility) {
				switch(visibility) {
					case FieldVisibility.PrivateScope:
						this.WriteKeyword("?PrivateScope?");
						break;

					case FieldVisibility.Private:
						this.WriteKeyword("private");
						break;

					case FieldVisibility.FamilyAndAssembly:
						this.WriteKeyword("private protected");
						break;

					case FieldVisibility.Assembly:
						this.WriteKeyword("internal");
						break;

					case FieldVisibility.Family:
						this.WriteKeyword("protected");
						break;

					case FieldVisibility.FamilyOrAssembly:
						this.WriteKeyword("public protected");
						break;

					case FieldVisibility.Public:
						this.WriteKeyword("public");
						break;
				}
				this.Write(":");
				this.WriteLine();
			}

			[System.Obsolete]
			private void WriteMethodVisibilitySpecifier(MethodVisibility visibility) {
				switch(visibility) {
					case MethodVisibility.PrivateScope:
						this.WriteKeyword("?PrivateScope?");
						break;

					case MethodVisibility.Private:
						this.WriteKeyword("private");
						break;

					case MethodVisibility.FamilyAndAssembly:
						this.WriteKeyword("private protected");
						break;

					case MethodVisibility.Assembly:
						this.WriteKeyword("internal");
						break;

					case MethodVisibility.Family:
						this.WriteKeyword("protected");
						break;

					case MethodVisibility.FamilyOrAssembly:
						this.WriteKeyword("public protected");
						break;

					case MethodVisibility.Public:
						this.WriteKeyword("public");
						break;
				}
				this.Write(":");
			}
			[System.Obsolete]
			private void WriteTypeVisibilitySpecifier(TypeVisibility visibility) {
				switch(visibility) {
					case TypeVisibility.Private:
						this.WriteKeyword("private");
						goto writespace;

					case TypeVisibility.Public:
						this.WriteKeyword("public");
						goto writespace;

					case TypeVisibility.NestedPublic:
						this.WriteKeyword("public");
						goto writecolon;

					case TypeVisibility.NestedPrivate:
						this.WriteKeyword("private");
						goto writecolon;

					case TypeVisibility.NestedFamily:
						this.WriteKeyword("protected");
						goto writecolon;

					case TypeVisibility.NestedAssembly:
						this.WriteKeyword("internal");
						goto writecolon;

					case TypeVisibility.NestedFamilyAndAssembly:
						this.WriteKeyword("protected private");
						this.WriteKeyword("");
						goto writecolon;

					case TypeVisibility.NestedFamilyOrAssembly:
						this.WriteKeyword("protected public");
						this.WriteKeyword("");
						goto writecolon;

					default:
						break;

					writecolon:
						this.Write(":");
						this.WriteLine();
						break;

					writespace:
						this.Write(" ");
						break;
				}
			}
			#endregion
		}
	}
}