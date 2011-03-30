
using Reflector.CodeModel;
using Gen=System.Collections.Generic;

namespace mwg.Reflector.CppCli{
	public class MethodSignature{
		private IMethodDeclaration meth_decl;
		private string name;
		public string Name{
			get{return name;}
		}

		private MethodType m_type;
		internal MethodType Type{
			get{return this.m_type;}
		}

		#region MethodType & SpecialNames
		internal enum MethodType{
			Ordinary,
			ImplicitCast,
			ExplicitCast,
			Constructor,
			StaticConstructor,
			SpecialName,
			Operator,
		}
		private static Gen::Dictionary<string,string> operators
			=new Gen::Dictionary<string,string>();
		static MethodSignature(){
			operators["op_AddressOf"]="&";
			operators["op_OnesComplement"]="~";
			operators["op_PointerDereference"]="*";
			operators["op_UnaryNegation"]="-";
			operators["op_UnaryPlus"]="+";
			operators["op_Increment"]="++";
			operators["op_Decrement"]="--";

			operators["op_Addition"]="+";
			operators["op_Subtraction"]="-";
			operators["op_Multiply"]="*";
			operators["op_Division"]="/";
			operators["op_Modulus"]="%";

			operators["op_BitwiseAnd"]="&";
			operators["op_BitwiseOr"]="|";
			operators["op_ExclusiveOr"]="^";
			operators["op_LeftShift"]="<<";
			operators["op_RightShift"]=">>";

			operators["op_Equality"]="==";
			operators["op_Inequality"]="!=";
			operators["op_GreaterThanOrEqual"]=">=";
			operators["op_LessThanOrEqual"]="<=";
			operators["op_GreaterThan"]=">";
			operators["op_LessThan"]="<";

			operators["op_Negation"]="!";

			operators["op_LogicalAnd"]="&&";
			operators["op_LogicalOr"]="||";
			operators["op_LogicalNot"]="!";
//			operators["op_True"]=" true";
//			operators["op_False"]=" false";

			operators["op_MemberSelection"]="->";
			operators["op_PointerToMemberSelection"]="->*";
//			operators["op_Implicit"]="implicit ";
//			operators["op_Explicit"]="explicit ";

			// ’Ç‰Á
			operators["op_Comma"]=",";
			operators["op_FunctionCall"]="()";
			operators["op_Subscript"]="[]";
			operators["op_Assign"]="=";

			operators["op_AdditionAssignment"]="+=";
			operators["op_SubtractionAssignment"]="-=";
			operators["op_MultiplyAssignment"]="*=";
			operators["op_DivisionAssignment"]="/=";
			operators["op_ModulusAssignment"]="%=";

			operators["op_BitwiseAndAssignment"]="&=";
			operators["op_BitwiseOrAssignment"]="|=";
			operators["op_ExclusiveOrAssignment"]="^=";
			operators["op_LeftShiftAssignment"]="<<=";
			operators["op_RightShiftAssignment"]=">>=";
		}
		#endregion

		private ITypeDeclaration type_decl;
		public ITypeDeclaration DeclaringType{
			get{return type_decl;}
		}

		public MethodSignature(IMethodDeclaration methDecl){
			this.meth_decl=methDecl;
			this.name=meth_decl.Name;

			if(meth_decl.SpecialName){
				this.m_type=MethodType.SpecialName;
				switch(this.name){
					case "op_Implicit": this.m_type=MethodType.ImplicitCast;break;
					case "op_Explicit":	this.m_type=MethodType.ExplicitCast;break;
					case ".cctor":		this.m_type=MethodType.Constructor;break;
					case ".ctor":		this.m_type=MethodType.StaticConstructor;break;
					default:
						string op;
						if(operators.TryGetValue(name,out op)){
							this.name=op;
							this.m_type=MethodType.Operator;
						}
						break;
				}
			}else{
				this.m_type=MethodType.Ordinary;
			}

			type_decl=((ITypeReference)methDecl.DeclaringType).Resolve();
		}

		public bool IsGlobal{
			get{return type_decl.Name=="<Module>";}
		}
		public bool IsStatic{
			get{return !IsGlobal&&meth_decl.Static;}
		}
		public bool IsVirtual{
			get{return !type_decl.Interface&&meth_decl.Virtual;}
		}
		public bool IsSealed{
			get{return !meth_decl.NewSlot&&meth_decl.Final;}
		}
		public bool IsAbstract{
			get{return IsVirtual&&meth_decl.Abstract;}
		}
		public bool IsOverride{
			get{return IsVirtual&&!meth_decl.NewSlot;}
		}
	}
}